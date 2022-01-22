using System;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	public class PseudoWingcatGraphics : PlayerGraphics
	{
		public PseudoWingcatGraphics(PhysicalObject ow) : base(ow)
		{
			this.wingParts = new List<PseudoWingcatGraphics.WingPart>();
			for (int i = 0; i < this.wingPartCount; i++)
			{
				this.wingParts.Add(new PseudoWingcatGraphics.WingPart(this, i == 0));
				this.wingParts.Add(new PseudoWingcatGraphics.WingPart(this, i == 0));
			}
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			this.initLock = true;
			base.InitiateSprites(sLeaser, rCam);
			foreach (PseudoWingcatGraphics.WingPart wingPart in this.wingParts)
			{
				wingPart.InitiateSprites(sLeaser, rCam);
			}
			AddToContainerImpl(sLeaser, rCam, null);
			this.initLock = false;
		}

		public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			base.AddToContainer(sLeaser, rCam, newContatiner);
			if (!this.initLock) AddToContainerImpl(sLeaser, rCam, newContatiner);
		}

		public void AddToContainerImpl(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
			foreach (PseudoWingcatGraphics.WingPart wingPart in this.wingParts)
			{
				wingPart.AddToContainer(sLeaser, rCam, newContatiner);
			}
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			for (int j = 0; j < this.wingParts.Count; j++)
			{
				this.wingParts[j].DrawSprites(sLeaser, rCam, timeStacker, camPos, j);
			}
			for (int k = 0; k < 2; k++)
			{
				sLeaser.sprites[7 + k].isVisible = false;
			}
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos); // base last because its responsible for removing stuff
		}

        public override void Update()
        {
            base.Update();
			foreach (SlugcatHand slugcatHand in this.hands)
			{
				slugcatHand.mode = Limb.Mode.Retracted;
				slugcatHand.retractCounter = 0;
			}
			for (int j = 0; j < this.wingParts.Count; j++)
			{
				this.wingParts[j].Update(j);
			}
		}

        public override void Reset()
        {
            base.Reset();
			for (int j = 0; j < this.wingParts.Count; j++)
			{
				this.wingParts[j].Reset();
			}
		}

        public int wingPartCount = 3;

		public List<PseudoWingcatGraphics.WingPart> wingParts;
        private bool initLock;

        public class WingPart
		{
			public WingPart(PseudoWingcatGraphics owner, bool isLarge = false)
			{
				this.owner = owner;
				if (isLarge)
				{
					this.length = 25f;
					this.width = 3f;
					return;
				}
				this.length = 15f;
				this.width = 2f;

				Reset();
			}

			public void Update(int index)
			{
				float num = (float)((index % 2 == 0) ? 1 : -1);
				float t = (float)(index / 2) / (float)(this.owner.wingPartCount / 2);
				Vector2 b = Vector2.Lerp(this.owner.owner.bodyChunks[0].pos, this.owner.owner.bodyChunks[1].pos, 0.5f - Mathf.Lerp(0f, 0.3f, t));
				float num2 = Custom.VecToDeg(this.owner.owner.bodyChunks[0].pos - b);
				num2 += (float)(index / 2) * 25f * num;
				Vector2 to = this.owner.head.pos + Custom.DegToVec(num2 + 80f * num) * 10f;
				Vector2 to2 = this.owner.head.pos + Custom.DegToVec(num2 + 80f * num) * (10f + this.length);
				targetFarLastPos = targetFarPos;
				targetCloseLastPos = targetClosePos;
				targetClosePos = Vector2.Lerp(this.targetClosePos, to, Mathf.Lerp(0.7f, 0.4f, t));
				targetFarPos = Vector2.Lerp(this.targetFarPos, to2, Mathf.Lerp(0.7f, 0.4f, t));
			}
			public void Reset()
			{
				targetFarPos = this.owner.owner.bodyChunks[0].pos;
				targetClosePos = this.owner.owner.bodyChunks[0].pos;
				targetFarLastPos = targetFarPos;
				targetCloseLastPos = targetClosePos;
			}

			public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
				{
					new TriangleMesh.Triangle(0, 1, 2),
					new TriangleMesh.Triangle(1, 2, 3),
					new TriangleMesh.Triangle(2, 3, 4),
					new TriangleMesh.Triangle(3, 4, 5),
					new TriangleMesh.Triangle(4, 5, 6),
					new TriangleMesh.Triangle(5, 6, 7)
				};
				int l = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, l + 1);
				this.mesh = l;
				sLeaser.sprites[l] = new TriangleMesh("Futile_White", tris, true, false);
			}

			public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
			{
				if (newContatiner == null)
				{
					newContatiner = rCam.ReturnFContainer("Midground");
				}
				//this.mesh.RemoveFromContainer();
				newContatiner.AddChild(sLeaser.sprites[mesh]);
			}

			public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, int index)
			{
				Vector2 tcp = Vector2.Lerp(targetCloseLastPos, targetClosePos, timeStacker);
				Vector2 tfp = Vector2.Lerp(targetFarLastPos, targetFarPos, timeStacker);
				Vector2 a = Custom.PerpendicularVector(tcp, tfp);
				for (int i = 0; i < 3; i++)
				{
					(sLeaser.sprites[mesh] as TriangleMesh).MoveVertice(i * 2 + 1, Vector2.Lerp(tcp, tfp, ((float)i + 1f) / 4f) + a * this.width - camPos);
					(sLeaser.sprites[mesh] as TriangleMesh).MoveVertice(i * 2 + 2, Vector2.Lerp(tcp, tfp, ((float)i + 1f) / 4f) - a * this.width - camPos);
				}
				(sLeaser.sprites[mesh] as TriangleMesh).MoveVertice(0, tcp - camPos);
				(sLeaser.sprites[mesh] as TriangleMesh).MoveVertice(7, tfp - camPos);
			}


			public int mesh;

			public PseudoWingcatGraphics owner;

			public float length;

			public float width;
            public Vector2 targetFarPos;
			public Vector2 targetClosePos;
            private Vector2 targetFarLastPos;
            private Vector2 targetCloseLastPos;
        }
	}
}
