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
			foreach (PseudoWingcatGraphics.WingPart wingPart in this.wingParts)
			{
				wingPart.InitiateSprites(sLeaser, rCam);
			}
			base.InitiateSprites(sLeaser, rCam);
		}

		public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			foreach (PseudoWingcatGraphics.WingPart wingPart in this.wingParts)
			{
				wingPart.AddToContainer(sLeaser, rCam, newContatiner);
			}
			base.AddToContainer(sLeaser, rCam, newContatiner);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			foreach (SlugcatHand slugcatHand in this.hands)
			{
				slugcatHand.mode = Limb.Mode.Retracted;
				slugcatHand.retractCounter = 0;
			}
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			for (int j = 0; j < this.wingParts.Count; j++)
			{
				this.wingParts[j].DrawSprites(sLeaser, rCam, timeStacker, camPos, j);
			}
			for (int k = 0; k < 2; k++)
			{
				sLeaser.sprites[7 + k].isVisible = false;
			}
		}

		public int wingPartCount = 3;

		public List<PseudoWingcatGraphics.WingPart> wingParts;

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
			}

			public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				if (this.mesh != null)
				{
					this.mesh.RemoveFromContainer();
				}
				TriangleMesh.Triangle[] tris = new TriangleMesh.Triangle[]
				{
					new TriangleMesh.Triangle(0, 1, 2),
					new TriangleMesh.Triangle(1, 2, 3),
					new TriangleMesh.Triangle(2, 3, 4),
					new TriangleMesh.Triangle(3, 4, 5),
					new TriangleMesh.Triangle(4, 5, 6),
					new TriangleMesh.Triangle(5, 6, 7)
				};
				if (this.blankSprite != null)
				{
					this.blankSprite.RemoveFromContainer();
				}
				this.blankSprite = new FSprite("Futile_White", true);
				this.mesh = new TriangleMesh("Futile_White", tris, true, false);
			}

			public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
			{
				if (newContatiner == null)
				{
					newContatiner = rCam.ReturnFContainer("Midground");
				}
				this.mesh.RemoveFromContainer();
				newContatiner.AddChild(this.mesh);
				this.blankSprite.RemoveFromContainer();
				newContatiner.AddChild(this.blankSprite);
			}

			public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, int index)
			{
				float num = (float)((index % 2 == 0) ? 1 : -1);
				float t = (float)(index / 2) / (float)(this.owner.wingPartCount / 2);
				BodyChunk bodyChunk = this.owner.owner.bodyChunks[0];
				BodyChunk bodyChunk2 = this.owner.owner.bodyChunks[1];
				Vector2 b = Vector2.Lerp(Vector2.Lerp(bodyChunk.lastPos, bodyChunk.pos, timeStacker), Vector2.Lerp(bodyChunk2.lastPos, bodyChunk2.pos, timeStacker), 0.5f - Mathf.Lerp(0f, 0.3f, t));
				float num2 = Custom.VecToDeg(Vector2.Lerp(bodyChunk.lastPos, bodyChunk.pos, timeStacker) - b);
				num2 += (float)(index / 2) * 25f * num;
				Vector2 to = Vector2.Lerp(this.owner.head.lastPos, this.owner.head.pos, timeStacker) + Custom.DegToVec(num2 + 80f * num) * 10f;
				Vector2 to2 = Vector2.Lerp(this.owner.head.lastPos, this.owner.head.pos, timeStacker) + Custom.DegToVec(num2 + 80f * num) * (10f + this.length);
				this.targetClosePos = Vector2.Lerp(this.targetClosePos, to, Mathf.Lerp(0.7f, 0.4f, t));
				this.targetFarPos = Vector2.Lerp(this.targetFarPos, to2, Mathf.Lerp(0.7f, 0.4f, t));
				Vector2 a = Custom.PerpendicularVector(this.targetClosePos, this.targetFarPos);
				for (int i = 0; i < 3; i++)
				{
					this.mesh.MoveVertice(i * 2 + 1, Vector2.Lerp(this.targetClosePos, this.targetFarPos, ((float)i + 1f) / 4f) + a * this.width - camPos);
					this.mesh.MoveVertice(i * 2 + 2, Vector2.Lerp(this.targetClosePos, this.targetFarPos, ((float)i + 1f) / 4f) - a * this.width - camPos);
				}
				this.mesh.MoveVertice(0, this.targetClosePos - camPos);
				this.mesh.MoveVertice(7, this.targetFarPos - camPos);
			}

			public TriangleMesh mesh;

			private FSprite blankSprite;

			public PseudoWingcatGraphics owner;

			public float length;

			public float width;

			public Vector2 targetFarPos;

			public Vector2 targetClosePos;
		}
	}
}
