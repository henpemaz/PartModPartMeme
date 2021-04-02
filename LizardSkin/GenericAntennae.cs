using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericAntennae : GenericCosmeticTemplate
    {
        public GenericAntennae(ICosmeticsAdaptor iGraphics) : base(iGraphics)
		{
			this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.InFront;
			this.length = UnityEngine.Random.value;
			this.segments = Mathf.FloorToInt(Mathf.Lerp(3f, 8f, Mathf.Pow(this.length, Mathf.Lerp(1f, 6f, this.length))));
			this.alpha = this.length * 0.9f + UnityEngine.Random.value * 0.1f;
			this.antennae = new GenericBodyPartAdaptor[2, this.segments];
			for (int i = 0; i < this.segments; i++)
			{
				this.antennae[0, i] = new GenericBodyPartAdaptor(iGraphics, 1f, 0.6f, 0.9f);
				this.antennae[1, i] = new GenericBodyPartAdaptor(iGraphics, 1f, 0.6f, 0.9f);
			}
			this.redderTint = iGraphics.effectColor;
			this.redderTint.g *= 0.5f;
			this.redderTint.b *= 0.5f;
			this.redderTint.r = Mathf.Lerp(this.redderTint.r, 1f, 0.75f);
			this.numberOfSprites = 4;
		}

		// Token: 0x06001F65 RID: 8037 RVA: 0x001DC830 File Offset: 0x001DAA30
		private int Sprite(int side, int part)
		{
			return this.startSprite + part * 2 + side;
		}

		// Token: 0x06001F66 RID: 8038 RVA: 0x001DC840 File Offset: 0x001DAA40
		public override void Reset()
		{
			base.Reset();
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < this.segments; j++)
				{
					this.antennae[i, j].Reset(this.AnchorPoint(i, 1f));
				}
			}
		}

		// Token: 0x06001F67 RID: 8039 RVA: 0x001DC89C File Offset: 0x001DAA9C
		public override void Update()
		{
			float num = 0; //  this.iGraphics.lizard.AI.yellowAI.commFlicker;
			//if (!this.iGraphics.lizard.Consious)
			//{
			//	num = 0f;
			//}
			float num2 = Mathf.Lerp(10f, 7f, this.length);
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < this.segments; j++)
				{
					float num3 = (float)j / (float)(this.segments - 1);
					num3 = Mathf.Lerp(num3, Mathf.InverseLerp(0f, 5f, (float)j), 0.2f);
					this.antennae[i, j].vel += this.AntennaDir(i, 1f) * (1f - num3 + 0.6f * num);
					if (this.iGraphics.PointSubmerged(this.antennae[i, j].pos))
					{
						this.antennae[i, j].vel *= 0.8f;
					}
					else
					{
						GenericBodyPartAdaptor genericBodyPart = this.antennae[i, j];
						genericBodyPart.vel.y = genericBodyPart.vel.y - 0.4f * num3 * (1f - num);
					}
					this.antennae[i, j].Update();
					this.antennae[i, j].pos += Custom.RNV() * 3f * num;
					Vector2 p;
					if (j == 0)
					{
						this.antennae[i, j].vel += this.AntennaDir(i, 1f) * 5f;
						p = this.iGraphics.headPos;
						this.antennae[i, j].ConnectToPoint(this.AnchorPoint(i, 1f), num2, true, 0f, this.iGraphics.mainBodyChunkVel, 0f, 0f);
					}
					else
					{
						if (j == 1)
						{
							p = this.AnchorPoint(i, 1f);
						}
						else
						{
							p = this.antennae[i, j - 2].pos;
						}
						Vector2 a = Custom.DirVec(this.antennae[i, j].pos, this.antennae[i, j - 1].pos);
						float num4 = Vector2.Distance(this.antennae[i, j].pos, this.antennae[i, j - 1].pos);
						this.antennae[i, j].pos -= a * (num2 - num4) * 0.5f;
						this.antennae[i, j].vel -= a * (num2 - num4) * 0.5f;
						this.antennae[i, j - 1].pos += a * (num2 - num4) * 0.5f;
						this.antennae[i, j - 1].vel += a * (num2 - num4) * 0.5f;
					}
					this.antennae[i, j].vel += Custom.DirVec(p, this.antennae[i, j].pos) * 3f * Mathf.Pow(1f - num3, 0.3f);
					if (j > 1)
					{
						this.antennae[i, j - 2].vel += Custom.DirVec(this.antennae[i, j].pos, this.antennae[i, j - 2].pos) * 3f * Mathf.Pow(1f - num3, 0.3f);
					}
					if (!Custom.DistLess(this.iGraphics.headPos, this.antennae[i, j].pos, 200f))
					{
						this.antennae[i, j].pos = this.iGraphics.headPos;
					}
				}
			}
		}

		// Token: 0x06001F68 RID: 8040 RVA: 0x001DCD54 File Offset: 0x001DAF54
		private Vector2 AntennaDir(int side, float timeStacker)
		{
			float num = Mathf.Lerp(this.iGraphics.lastHeadDepthRotation, this.iGraphics.headDepthRotation, timeStacker);
			Vector2 vector = new Vector2(((side != 0) ? 1f : -1f) * (1f - Mathf.Abs(num)) * 1.5f + num * 3.5f, -1f);
			return Custom.RotateAroundOrigo(vector.normalized, Custom.AimFromOneVectorToAnother(Vector2.Lerp(this.iGraphics.mainBodyChunkLastPos, this.iGraphics.mainBodyChunkPos, timeStacker), Vector2.Lerp(this.iGraphics.headLastPos, this.iGraphics.headPos, timeStacker)));
		}

		// Token: 0x06001F69 RID: 8041 RVA: 0x001DCE1C File Offset: 0x001DB01C
		private Vector2 AnchorPoint(int side, float timeStacker)
		{
			return Vector2.Lerp(this.iGraphics.mainBodyChunkLastPos, this.iGraphics.mainBodyChunkPos, timeStacker) + this.AntennaDir(side, timeStacker) * 3f;
		}

		// Token: 0x06001F6A RID: 8042 RVA: 0x001DCE80 File Offset: 0x001DB080
		public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
		{
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					sLeaser.sprites[this.Sprite(i, j)] = TriangleMesh.MakeLongMesh(this.segments, true, true);
				}
			}
			for (int k = 0; k < 2; k++)
			{
				sLeaser.sprites[this.Sprite(k, 1)].shader = iGraphics.rainWorld.Shaders["LizardAntenna"];
			}
		}

		// Token: 0x06001F6B RID: 8043 RVA: 0x001DCF14 File Offset: 0x001DB114
		public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
		{
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			float flicker = 0; // Mathf.Pow(UnityEngine.Random.value, 1f - 0.5f * this.iGraphics.lizard.AI.yellowAI.commFlicker) * this.iGraphics.lizard.AI.yellowAI.commFlicker;
			//if (!this.iGraphics.lizard.Consious)
			//{
			//	flicker = 0f;
			//}
			Vector2 vector = Custom.DegToVec(this.iGraphics.HeadRotation(timeStacker));
			for (int i = 0; i < 2; i++)
			{
				sLeaser.sprites[this.startSprite + i].color = this.iGraphics.HeadColor(timeStacker);
				Vector2 vector2 = Vector2.Lerp(Vector2.Lerp(this.iGraphics.headLastPos, this.iGraphics.headPos, timeStacker), this.AnchorPoint(i, timeStacker), 0.5f);
				float num = 1f;
				float num2 = 0f;
				for (int j = 0; j < this.segments; j++)
				{
					float num3 = (float)j / (float)(this.segments - 1);
					Vector2 vector3 = Vector2.Lerp(this.antennae[i, j].lastPos, this.antennae[i, j].pos, timeStacker);
					Vector2 normalized = (vector3 - vector2).normalized;
					Vector2 a = Custom.PerpendicularVector(normalized);
					float d = Vector2.Distance(vector3, vector2) / 5f;
					float num4 = Mathf.Lerp(3f, 1f, Mathf.Pow(num3, 0.8f));
					for (int k = 0; k < 2; k++)
					{
						(sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).MoveVertice(j * 4, vector2 - a * (num + num4) * 0.5f + normalized * d - camPos);
						(sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).MoveVertice(j * 4 + 1, vector2 + a * (num + num4) * 0.5f + normalized * d - camPos);
						(sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4] = this.EffectColor(k, (num3 + num2) / 2f, timeStacker, flicker);
						(sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4 + 1] = this.EffectColor(k, (num3 + num2) / 2f, timeStacker, flicker);
						(sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4 + 2] = this.EffectColor(k, num3, timeStacker, flicker);
						if (j < this.segments - 1)
						{
							(sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).MoveVertice(j * 4 + 2, vector3 - a * num4 - normalized * d - camPos);
							(sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).MoveVertice(j * 4 + 3, vector3 + a * num4 - normalized * d - camPos);
							(sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).verticeColors[j * 4 + 3] = this.EffectColor(k, num3, timeStacker, flicker);
						}
						else
						{
							(sLeaser.sprites[this.Sprite(i, k)] as TriangleMesh).MoveVertice(j * 4 + 2, vector3 - camPos);
						}
					}
					num = num4;
					vector2 = vector3;
					num2 = num3;
				}
			}
		}

		// Token: 0x06001F6C RID: 8044 RVA: 0x001DD330 File Offset: 0x001DB530
		public Color EffectColor(int part, float tip, float timeStacker, float flicker)
		{
			tip = Mathf.Pow(Mathf.InverseLerp(0f, 0.6f, tip), 0.5f);
			if (part == 0)
			{
				return Color.Lerp(this.iGraphics.HeadColor(timeStacker), Color.Lerp(this.iGraphics.effectColor, this.iGraphics.palette.blackColor, flicker), tip);
			}
			return Color.Lerp(new Color(this.redderTint.r, this.redderTint.g, this.redderTint.b, this.alpha), new Color(1f, 1f, 1f, this.alpha), flicker);
		}

		// Token: 0x06001F6D RID: 8045 RVA: 0x001DD3E4 File Offset: 0x001DB5E4
		public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
		{
			base.ApplyPalette(sLeaser, rCam, palette);
		}

		// Token: 0x04002205 RID: 8709
		public GenericBodyPartAdaptor[,] antennae;

		// Token: 0x04002206 RID: 8710
		private Color redderTint;

		// Token: 0x04002207 RID: 8711
		private int segments;

		// Token: 0x04002208 RID: 8712
		private float length;

		// Token: 0x04002209 RID: 8713
		private float alpha;
	}
}
