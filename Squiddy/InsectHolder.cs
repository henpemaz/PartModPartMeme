using UnityEngine;

namespace Squiddy
{
	public class InsectHolder : PlayerCarryableItem, IPlayerEdible, IDrawable
	{
		private class AbstractInsectHolder : AbstractPhysicalObject
		{
			public AbstractInsectHolder(World world, AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : base(world, type, realizedObject, pos, ID) { }
			public override void IsEnteringDen(WorldCoordinate den)
			{
				base.IsEnteringDen(den);
				if (this.realizedObject is InsectHolder holder)
				{
					holder.insect.Destroy();
				}
			}
		}
		public InsectHolder(CosmeticInsect insect, Player p, Room room)
			: base(new AbstractInsectHolder(room.world, AbstractPhysicalObject.AbstractObjectType.AttachedBee, null, room.GetWorldCoordinate(insect.pos), room.game.GetNewID())
			{ destroyOnAbstraction = true })
		{
			this.insect = insect;
			this.p = p;
			this.bodyChunks = new BodyChunk[] { new BodyChunk(this, 0, insect.pos, 2f, 0.01f) };
			this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
		}

		public override string ToString()
		{
			return "InsectHolder of " + insect.type.ToString();
		}

		public override void Update(bool eu)
		{
			if (this.grabbedBy.Count == 0 && p.pickUpCandidate != this) Destroy();
			if (this.insect.slatedForDeletetion) Destroy();
			base.Update(eu);
			if (slatedForDeletetion) return;
			if (this.grabbedBy.Count != 0)
			{
				insect.pos = this.firstChunk.pos;
				insect.vel = 0.8f * insect.vel + this.firstChunk.vel;
			}
			else
			{
				this.firstChunk.pos = insect.pos;
				this.firstChunk.vel = insect.vel;
			}
		}

		int bites = 1;
		public readonly CosmeticInsect insect;
		private readonly Player p;

		public int BitesLeft => bites;

		public int FoodPoints => 0;

		public bool Edible => true;

		public bool AutomaticPickUp => false;

		public void BitByPlayer(Creature.Grasp grasp, bool eu)
		{
			this.bites--;
			this.room.PlaySound(SoundID.Slugcat_Bite_Fly, base.firstChunk.pos);
			this.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
			if (this.bites < 1)
			{
				(grasp.grabber as Player).ObjectEaten(this);
				grasp.Release();
				Destroy();
				insect.Destroy();
			}
		}

		public void ThrowByPlayer() { }

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			this.insect.InitiateSprites(sLeaser, rCam);
			foreach (var sprt in sLeaser.sprites)
			{
				sprt.color = base.blinkColor;
			}
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			this.insect.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			if (this.blink > 0 && UnityEngine.Random.value < 0.5f)
			{
				foreach (var sprt in sLeaser.sprites)
				{
					sprt.isVisible = true;
					sprt.color = base.blinkColor;
				}
			}
			else
			{
				foreach (var sprt in sLeaser.sprites)
				{
					sprt.isVisible = false;
				}
			}
			if (base.slatedForDeletetion || this.room != rCam.room)
			{
				sLeaser.CleanSpritesAndRemove();
			}
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) { }
	}
}