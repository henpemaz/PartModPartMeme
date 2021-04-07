using UnityEngine;
using RWCustom;

namespace LizardSkin
{
    internal class GenericTailGeckoScales : GenericCosmeticTemplate
    {
        public GenericTailGeckoScales(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
		{
			this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.BehindHead;
			this.rows = UnityEngine.Random.Range(7, 14);
			this.lines = UnityEngine.Random.Range(3, UnityEngine.Random.Range(3, 4));
			//if (iGraphics.iVars.tailColor > 0.1f && UnityEngine.Random.value < Mathf.Lerp(0.7f, 0.99f, iGraphics.iVars.tailColor))
			//{
			//	this.bigScales = true;
			//	for (int i = 0; i < iGraphics.cosmetics.Count; i++)
			//	{
			//		if (iGraphics.cosmetics[i] is WingScales)
			//		{
			//			if ((iGraphics.cosmetics[i] as WingScales).scaleLength > 10f)
			//			{
			//				this.bigScales = false;
			//			}
			//			break;
			//		}
			//	}
			//}
			this.bigScales = true; // ooopsie
			if (UnityEngine.Random.value < 0.5f)
			{
				this.rows += UnityEngine.Random.Range(0, UnityEngine.Random.Range(0, 7));
				this.lines += UnityEngine.Random.Range(0, UnityEngine.Random.Range(0, 3));
			}
			this.numberOfSprites = this.rows * this.lines;
		}

		// Token: 0x06001F75 RID: 8053 RVA: 0x001DDB50 File Offset: 0x001DBD50
		public override void Update()
		{
		}

		// Token: 0x06001F76 RID: 8054 RVA: 0x001DDB54 File Offset: 0x001DBD54
		public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
		{
			for (int i = 0; i < this.rows; i++)
			{
				for (int j = 0; j < this.lines; j++)
				{
					if (this.bigScales)
					{
						sLeaser.sprites[this.startSprite + i * this.lines + j] = new FSprite("Circle20", true);
						sLeaser.sprites[this.startSprite + i * this.lines + j].scaleY = 0.3f;
					}
					else
					{
						sLeaser.sprites[this.startSprite + i * this.lines + j] = new FSprite("tinyStar", true);
					}
				}
			}
		}

		// Token: 0x06001F77 RID: 8055 RVA: 0x001DDC08 File Offset: 0x001DBE08
		public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
		{
			if (this.bigScales)
			{
				SpineData lizardSpineData = this.iGraphics.SpinePosition(0.4f, timeStacker);
				for (int i = 0; i < this.rows; i++)
				{
					float num = Mathf.InverseLerp(0f, (float)(this.rows - 1), (float)i);
					float num2 = Mathf.Lerp(0.5f, 0.99f, Mathf.Pow(num, 0.8f));
					SpineData lizardSpineData2 = this.iGraphics.SpinePosition(num2, timeStacker);
					Color a = this.cosmeticData.baseColor(iGraphics, num2);
					for (int j = 0; j < this.lines; j++)
					{
						float num3 = ((float)j + ((i % 2 != 0) ? 0f : 0.5f)) / (float)(this.lines - 1);
						num3 = -1f + 2f * num3;
						num3 += Mathf.Lerp(this.iGraphics.lastDepthRotation, this.iGraphics.depthRotation, timeStacker);
						if (num3 < -1f)
						{
							num3 += 2f;
						}
						else if (num3 > 1f)
						{
							num3 -= 2f;
						}
						Vector2 vector = lizardSpineData.pos + lizardSpineData.perp * (lizardSpineData.rad + 0.5f) * num3;
						Vector2 vector2 = lizardSpineData2.pos + lizardSpineData2.perp * (lizardSpineData2.rad + 0.5f) * num3;
						sLeaser.sprites[this.startSprite + i * this.lines + j].x = (vector.x + vector2.x) * 0.5f - camPos.x;
						sLeaser.sprites[this.startSprite + i * this.lines + j].y = (vector.y + vector2.y) * 0.5f - camPos.y;
						sLeaser.sprites[this.startSprite + i * this.lines + j].rotation = Custom.AimFromOneVectorToAnother(vector, vector2);
						sLeaser.sprites[this.startSprite + i * this.lines + j].scaleX = Custom.LerpMap(Mathf.Abs(num3), 0.4f, 1f, lizardSpineData2.rad * 3.5f / (float)this.rows, 0f) / 10f;
						sLeaser.sprites[this.startSprite + i * this.lines + j].scaleY = Vector2.Distance(vector, vector2) * 1.1f / 20f;
						//if (this.iGraphics.iVars.tailColor > 0f)
						//{
							float num4 = Mathf.InverseLerp(0.5f, 1f, Mathf.Abs(Vector2.Dot(Custom.DirVec(vector2, vector), Custom.DegToVec(-45f + 120f * num3))));
						//num4 = Custom.LerpMap(Mathf.Abs(num3), 0.5f, 1f, 0.3f, 0f) + 0.7f * Mathf.Pow(num4 * Mathf.Pow(this.iGraphics.iVars.tailColor, 0.3f), Mathf.Lerp(2f, 0.5f, num));
						num4 = Custom.LerpMap(Mathf.Abs(num3), 0.5f, 1f, 0.3f, 0f) + 0.7f * Mathf.Pow(num4 * Mathf.Pow(0.5f, 0.3f), Mathf.Lerp(2f, 0.5f, num));
						if (num < 0.5f)
							{
								num4 *= Custom.LerpMap(num, 0f, 0.5f, 0.2f, 1f);
							}
							num4 = Mathf.Pow(num4, Mathf.Lerp(2f, 0.5f, num));
							if (num4 < 0.5f)
							{
								sLeaser.sprites[this.startSprite + i * this.lines + j].color = Color.Lerp(a, this.cosmeticData.effectColor, Mathf.InverseLerp(0f, 0.5f, num4));
							}
							else
							{
								sLeaser.sprites[this.startSprite + i * this.lines + j].color = Color.Lerp(this.cosmeticData.effectColor, Color.white, Mathf.InverseLerp(0.5f, 1f, num4));
							}
						//}
						//else
						//{
						//	sLeaser.sprites[this.startSprite + i * this.lines + j].color = Color.Lerp(a, this.iGraphics.effectColor, Custom.LerpMap(num, 0f, 0.8f, 0.2f, Custom.LerpMap(Mathf.Abs(num3), 0.5f, 1f, 0.8f, 0.4f), 0.8f));
						//}
					}
					lizardSpineData = lizardSpineData2;
				}
			}
			else
			{
				for (int k = 0; k < this.rows; k++)
				{
					float f = Mathf.InverseLerp(0f, (float)(this.rows - 1), (float)k);
					float num5 = Mathf.Lerp(0.4f, 0.95f, Mathf.Pow(f, 0.8f));
					SpineData lizardSpineData3 = this.iGraphics.SpinePosition(num5, timeStacker);
					Color color = Color.Lerp(this.cosmeticData.baseColor(iGraphics, num5), this.cosmeticData.effectColor, 0.2f + 0.8f * Mathf.Pow(f, 0.5f));
					for (int l = 0; l < this.lines; l++)
					{
						float num6 = ((float)l + ((k % 2 != 0) ? 0f : 0.5f)) / (float)(this.lines - 1);
						num6 = -1f + 2f * num6;
						num6 += Mathf.Lerp(this.iGraphics.lastDepthRotation, this.iGraphics.depthRotation, timeStacker);
						if (num6 < -1f)
						{
							num6 += 2f;
						}
						else if (num6 > 1f)
						{
							num6 -= 2f;
						}
						num6 = Mathf.Sign(num6) * Mathf.Pow(Mathf.Abs(num6), 0.6f);
						Vector2 vector3 = lizardSpineData3.pos + lizardSpineData3.perp * (lizardSpineData3.rad + 0.5f) * num6;
						sLeaser.sprites[this.startSprite + k * this.lines + l].x = vector3.x - camPos.x;
						sLeaser.sprites[this.startSprite + k * this.lines + l].y = vector3.y - camPos.y;
						sLeaser.sprites[this.startSprite + k * this.lines + l].color = new Color(1f, 0f, 0f);
						sLeaser.sprites[this.startSprite + k * this.lines + l].rotation = Custom.VecToDeg(lizardSpineData3.dir);
						sLeaser.sprites[this.startSprite + k * this.lines + l].scaleX = Custom.LerpMap(Mathf.Abs(num6), 0.4f, 1f, 1f, 0f);
						sLeaser.sprites[this.startSprite + k * this.lines + l].color = color;
					}
				}
			}
		}

		// Token: 0x06001F78 RID: 8056 RVA: 0x001DE358 File Offset: 0x001DC558
		public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
		{
		}

		// Token: 0x0400220A RID: 8714
		public int rows;

		// Token: 0x0400220B RID: 8715
		public int lines;

		// Token: 0x0400220C RID: 8716
		private bool bigScales;
	}
}
