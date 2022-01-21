using System;
using RWCustom;
using UnityEngine;

namespace ZandrasCharacterPackPort
{

	public class KineticatHalo : UpdatableAndDeletable, IDrawable
	{
		public KineticatHalo(Player owner, int firstSprite)
		{
			this.owner = owner;
			this.pGraphics = (owner.graphicsModule as PlayerGraphics);
			this.firstSprite = firstSprite;
			this.totalSprites = 2;
			this.firstBitSprite = firstSprite + this.totalSprites;
			this.bits = new KineticatHalo.MemoryBit[3][];
			this.bits[0] = new KineticatHalo.MemoryBit[10];
			this.bits[1] = new KineticatHalo.MemoryBit[30];
			this.bits[2] = new KineticatHalo.MemoryBit[60];
			for (int i = 0; i < this.bits.Length; i++)
			{
				for (int j = 0; j < this.bits[i].Length; j++)
				{
					this.bits[i][j] = new KineticatHalo.MemoryBit(this, new IntVector2(i, j));
				}
			}
			this.totalSprites += 100;
			this.ringRotations = new float[10, 5];
			this.expand = 1f;
			this.getToExpand = 1f;
		}

		public override void Update(bool eu)
		{
			for (int i = 0; i < this.ringRotations.GetLength(0); i++)
			{
				this.ringRotations[i, 1] = this.ringRotations[i, 0];
				if (this.ringRotations[i, 0] != this.ringRotations[i, 3])
				{
					this.ringRotations[i, 4] += 1f / Mathf.Lerp(20f, Mathf.Abs(this.ringRotations[i, 2] - this.ringRotations[i, 3]), 0.5f);
					this.ringRotations[i, 0] = Mathf.Lerp(this.ringRotations[i, 2], this.ringRotations[i, 3], Custom.SCurve(this.ringRotations[i, 4], 0.5f));
					if (this.ringRotations[i, 4] > 1f)
					{
						this.ringRotations[i, 4] = 0f;
						this.ringRotations[i, 2] = this.ringRotations[i, 3];
						this.ringRotations[i, 0] = this.ringRotations[i, 3];
					}
				}
				else if (UnityEngine.Random.value < 0.033333335f)
				{
					this.ringRotations[i, 3] = this.ringRotations[i, 0] + ((UnityEngine.Random.value >= 0.5f) ? 1f : -1f) * Mathf.Lerp(15f, 150f, UnityEngine.Random.value);
				}
				else if (Kineticat.grabbedTimer[this.owner] > 0.99f)
				{
					this.ringRotations[i, 3] = this.ringRotations[i, 3] + 180f;
				}
			}
			for (int j = 0; j < this.bits.Length; j++)
			{
				for (int k = 0; k < this.bits[j].Length; k++)
				{
					this.bits[j][k].Update();
				}
			}
			if (UnityEngine.Random.value < 0.016666668f)
			{
				int num = UnityEngine.Random.Range(0, this.bits.Length);
				for (int l = 0; l < this.bits[num].Length; l++)
				{
					this.bits[num][l].SetToMax();
				}
			}
			this.lastExpand = this.expand;
			this.lastPush = this.push;
			this.lastWhite = this.white;
			this.expand = Custom.LerpAndTick(this.expand, this.getToExpand, 0.05f, 0.0125f);
			this.push = Custom.LerpAndTick(this.push, this.getToPush, 0.02f, 0.025f);
			this.white = Custom.LerpAndTick(this.white, this.getToWhite, 0.07f, 0.022727273f);
			bool flag = false;
			if (UnityEngine.Random.value < 0.00625f)
			{
				if (UnityEngine.Random.value < 0.125f)
				{
					flag = (this.getToWhite < 1f);
					this.getToWhite = 1f;
				}
				else
				{
					this.getToWhite = 0f;
				}
			}
			if (UnityEngine.Random.value < 0.00625f || flag)
			{
				this.getToExpand = ((UnityEngine.Random.value >= 0.5f || flag) ? Mathf.Lerp(0.8f, 2f, Mathf.Pow(UnityEngine.Random.value, 1.5f)) : 1f);
			}
			if (UnityEngine.Random.value < 0.00625f || flag)
			{
				this.getToPush = ((UnityEngine.Random.value >= 0.5f || flag) ? ((float)(-1 + UnityEngine.Random.Range(0, UnityEngine.Random.Range(1, 6)))) : 0f);
			}
		}

		public void ChangeAllRadi()
		{
			this.getToExpand = Mathf.Lerp(0.8f, 2f, Mathf.Pow(UnityEngine.Random.value, 1.5f));
			this.getToPush = (float)(-1 + UnityEngine.Random.Range(0, UnityEngine.Random.Range(1, 6)));
		}

		public float Radius(float ring, float timeStacker)
		{
			return (3f + ring + Mathf.Lerp(this.lastPush, this.push, timeStacker) - 0.5f * Kineticat.grabbedTimer[this.owner]) * Mathf.Lerp(this.lastExpand, this.expand, timeStacker) * 7f;
		}

		public float Rotation(int ring, float timeStacker)
		{
			return Mathf.Lerp(this.ringRotations[ring, 1], this.ringRotations[ring, 0], timeStacker);
		}

		public Vector2 Center(float timeStacker)
		{
			return Vector2.Lerp(this.pGraphics.head.lastPos, this.pGraphics.head.pos, timeStacker);
		}

		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = new FSprite[this.totalSprites];
			for (int i = 0; i < 2; i++)
			{
				sLeaser.sprites[this.firstSprite + i] = new FSprite("Futile_White", true);
				sLeaser.sprites[this.firstSprite + i].shader = rCam.game.rainWorld.Shaders["VectorCircle"];
				sLeaser.sprites[this.firstSprite + i].color = new Color(0.19607843f, 0f, 0.19607843f, 1f);
			}
			for (int j = 0; j < 100; j++)
			{
				sLeaser.sprites[this.firstBitSprite + j] = new FSprite("pixel", true);
				sLeaser.sprites[this.firstBitSprite + j].scaleX = 2f;
				sLeaser.sprites[this.firstBitSprite + j].color = new Color(0.19607843f, 0f, 0.19607843f, 1f);
			}
			this.AddToContainer(sLeaser, rCam, null);
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (this.room != rCam.room)
			{
				return;
			}
			Vector2 vector = this.Center(timeStacker);
			for (int i = 0; i < 2; i++)
			{
				sLeaser.sprites[this.firstSprite + i].x = vector.x - camPos.x;
				sLeaser.sprites[this.firstSprite + i].y = vector.y - camPos.y;
				sLeaser.sprites[this.firstSprite + i].scale = this.Radius((float)i, timeStacker) / 8f * Kineticat.grabbedTimer[this.owner];
			}
			sLeaser.sprites[this.firstSprite].alpha = Mathf.Lerp(3f / this.Radius(0f, timeStacker), 1f, Mathf.Lerp(this.lastWhite, this.white, timeStacker));
			sLeaser.sprites[this.firstSprite + 1].alpha = 3f / this.Radius(1f, timeStacker);
			int num = this.firstBitSprite;
			for (int j = 0; j < this.bits.Length; j++)
			{
				for (int k = 0; k < this.bits[j].Length; k++)
				{
					float num2 = (float)k / (float)this.bits[j].Length * 360f + this.Rotation(j, timeStacker);
					Vector2 vector2 = vector + Custom.DegToVec(num2) * (this.Radius((float)j + 0.5f, timeStacker) * Kineticat.grabbedTimer[this.owner]);
					sLeaser.sprites[num].scaleY = 8f * this.bits[j][k].Fill(timeStacker);
					sLeaser.sprites[num].x = vector2.x - camPos.x;
					sLeaser.sprites[num].y = vector2.y - camPos.y;
					sLeaser.sprites[num].rotation = num2;
					sLeaser.sprites[num].alpha = Kineticat.grabbedTimer[this.owner];
					num++;
				}
			}
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
		}

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			sLeaser.RemoveAllSpritesFromContainer();
			if (newContatiner == null)
			{
				newContatiner = rCam.ReturnFContainer("BackgroundShortcuts");
			}
			for (int i = 0; i < this.totalSprites; i++)
			{
				newContatiner.AddChild(sLeaser.sprites[i]);
			}
		}

		private Player owner;

		private PlayerGraphics pGraphics;

		public int firstSprite;

		public int totalSprites;

		private int firstBitSprite;

		public float white;

		public float lastWhite;

		public float getToWhite;

		public float push;

		public float lastPush;

		public float getToPush;

		public float expand;

		public float lastExpand;

		public float getToExpand;

		public float[,] ringRotations;

		public KineticatHalo.MemoryBit[][] bits;

		public class MemoryBit
		{
			public MemoryBit(KineticatHalo halo, IntVector2 position)
			{
				this.halo = halo;
				this.position = position;
				this.filled = UnityEngine.Random.value;
				this.lastFilled = this.filled;
				this.getToFilled = this.filled;
				this.fillSpeed = 0f;
			}

			public float Fill(float timeStacker)
			{
				if (this.blinkCounter % 4 > 1 && this.filled == this.getToFilled)
				{
					return 0f;
				}
				return Mathf.Lerp(this.lastFilled, this.filled, timeStacker);
			}

			public void SetToMax()
			{
				this.getToFilled = 1f;
				this.fillSpeed = Mathf.Lerp(this.fillSpeed, 0.25f, 0.25f);
				this.blinkCounter = 20;
			}

			public void Update()
			{
				this.lastFilled = this.filled;
				if (this.filled != this.getToFilled)
				{
					this.filled = Custom.LerpAndTick(this.filled, this.getToFilled, 0.03f, this.fillSpeed);
					return;
				}
				if (this.blinkCounter > 0)
				{
					this.blinkCounter--;
					return;
				}
				if (UnityEngine.Random.value < 0.016666668f)
				{
					this.getToFilled = UnityEngine.Random.value;
					this.fillSpeed = 1f / Mathf.Lerp(2f, 80f, UnityEngine.Random.value);
				}
			}

			public KineticatHalo halo;

			public IntVector2 position;

			private float filled;

			private float lastFilled;

			private float getToFilled;

			private float fillSpeed;

			public int blinkCounter;
		}
	}

}