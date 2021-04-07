using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericTailFin : GenericCosmeticTemplate
    {
        public GenericTailFin(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
		{
			this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.BehindHead;
			float num = Mathf.Lerp(4f, 7f, Mathf.Pow(UnityEngine.Random.value, 0.7f));
			this.spineLength = Custom.ClampedRandomVariation(0.5f, 0.17f, 0.5f) * iGraphics.BodyAndTailLength;
			this.undersideSize = Mathf.Lerp(0.3f, 0.9f, UnityEngine.Random.value);
			this.sizeRangeMin = Mathf.Lerp(0.1f, 0.3f, Mathf.Pow(UnityEngine.Random.value, 2f));
			this.sizeRangeMax = Mathf.Lerp(this.sizeRangeMin, 0.6f, UnityEngine.Random.value);
			this.sizeSkewExponent = Mathf.Lerp(0.5f, 1.5f, UnityEngine.Random.value);
			this.graphic = UnityEngine.Random.Range(0, 6);
			//if (iGraphics.lizard.Template.type == CreatureTemplate.Type.RedLizard)
			//{
			//	this.graphic = 0;
			//	for (int i = 0; i < iGraphics.cosmetics.Count; i++)
			//	{
			//		if (iGraphics.cosmetics[i] is GenericLongBodyScales)
			//		{
			//			this.graphic = (iGraphics.cosmetics[i] as GenericLongBodyScales).graphic;
			//			break;
			//		}
			//	}
			//	this.sizeRangeMin *= 2f;
			//	this.sizeRangeMax *= 1.5f;
			//	this.spineLength = Custom.ClampedRandomVariation(0.3f, 0.17f, 0.5f) * iGraphics.BodyAndTailLength;
			//}
			this.bumps = (int)(this.spineLength / num);
			this.scaleX = Mathf.Lerp(1f, 2f, UnityEngine.Random.value);
			if (this.graphic == 3 && UnityEngine.Random.value < 0.5f)
			{
				this.scaleX = -this.scaleX;
			}
			else if (this.graphic != 0 && UnityEngine.Random.value < 0.06666667f)
			{
				this.scaleX = -this.scaleX;
			}
			this.colored = (UnityEngine.Random.value > 0.33333334f);
			this.numberOfSprites = ((!this.colored) ? this.bumps : (this.bumps * 2)) * 2;
		}

		// Token: 0x06001F58 RID: 8024 RVA: 0x001DB4BC File Offset: 0x001D96BC
		public override void Update()
		{
		}

		// Token: 0x06001F59 RID: 8025 RVA: 0x001DB4C0 File Offset: 0x001D96C0
		public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
		{
			for (int i = 0; i < 2; i++)
			{
				int num = i * ((!this.colored) ? this.bumps : (this.bumps * 2));
				for (int j = this.startSprite + this.bumps - 1; j >= this.startSprite; j--)
				{
					float num2 = Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.bumps - 1), (float)j);
					sLeaser.sprites[j + num] = new FSprite("LizardScaleA" + this.graphic, true);
					sLeaser.sprites[j + num].anchorY = 0.15f;
					if (this.colored)
					{
						sLeaser.sprites[j + this.bumps + num] = new FSprite("LizardScaleB" + this.graphic, true);
						sLeaser.sprites[j + this.bumps + num].anchorY = 0.15f;
					}
				}
			}
		}

		// Token: 0x06001F5A RID: 8026 RVA: 0x001DB5D0 File Offset: 0x001D97D0
		public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
		{
			for (int i = 0; i < 2; i++)
			{
				int num = i * ((!this.colored) ? this.bumps : (this.bumps * 2));
				for (int j = this.startSprite + this.bumps - 1; j >= this.startSprite; j--)
				{
					float num2 = Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.bumps - 1), (float)j);
					SpineData lizardSpineData = this.iGraphics.SpinePosition(Mathf.Lerp(1f - this.spineLength / this.iGraphics.BodyAndTailLength, 1f, num2), timeStacker);
					if (i == 0)
					{
						sLeaser.sprites[j + num].x = lizardSpineData.outerPos.x - camPos.x;
						sLeaser.sprites[j + num].y = lizardSpineData.outerPos.y - camPos.y;
					}
					else if (i == 1)
					{
						sLeaser.sprites[j + num].x = lizardSpineData.pos.x + (lizardSpineData.pos.x - lizardSpineData.outerPos.x) * 0.85f - camPos.x;
						sLeaser.sprites[j + num].y = lizardSpineData.pos.y + (lizardSpineData.pos.y - lizardSpineData.outerPos.y) * 0.85f - camPos.y;
					}
					sLeaser.sprites[j + num].rotation = Custom.VecToDeg(Vector2.Lerp(lizardSpineData.perp * lizardSpineData.depthRotation, lizardSpineData.dir * (float)((i != 1) ? 1 : -1), num2));
					float num3 = Mathf.Lerp(this.sizeRangeMin, this.sizeRangeMax, Mathf.Sin(Mathf.Pow(num2, this.sizeSkewExponent) * 3.1415927f));
					sLeaser.sprites[j + num].scaleX = Mathf.Sign(this.iGraphics.depthRotation) * this.scaleX * num3;
					sLeaser.sprites[j + num].scaleY = num3 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphics.depthRotation))) * ((i != 1) ? 1f : (-this.undersideSize));
					if (this.colored)
					{
						if (i == 0)
						{
							sLeaser.sprites[j + this.bumps + num].x = lizardSpineData.outerPos.x - camPos.x;
							sLeaser.sprites[j + this.bumps + num].y = lizardSpineData.outerPos.y - camPos.y;
						}
						else if (i == 1)
						{
							sLeaser.sprites[j + this.bumps + num].x = lizardSpineData.pos.x + (lizardSpineData.pos.x - lizardSpineData.outerPos.x) * 0.85f - camPos.x;
							sLeaser.sprites[j + this.bumps + num].y = lizardSpineData.pos.y + (lizardSpineData.pos.y - lizardSpineData.outerPos.y) * 0.85f - camPos.y;
						}
						sLeaser.sprites[j + this.bumps + num].rotation = Custom.VecToDeg(Vector2.Lerp(lizardSpineData.perp * lizardSpineData.depthRotation, lizardSpineData.dir * (float)((i != 1) ? 1 : -1), num2));
						sLeaser.sprites[j + this.bumps + num].scaleX = Mathf.Sign(this.iGraphics.depthRotation) * this.scaleX * num3;
						sLeaser.sprites[j + this.bumps + num].scaleY = num3 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphics.depthRotation))) * ((i != 1) ? 1f : (-this.undersideSize));
						if (i == 1)
						{
							sLeaser.sprites[j + this.bumps + num].alpha = Mathf.Pow(Mathf.InverseLerp(0.3f, 1f, Mathf.Abs(this.iGraphics.depthRotation)), 0.2f);
						}
					}
				}
			}
		}

		// Token: 0x06001F5B RID: 8027 RVA: 0x001DBA78 File Offset: 0x001D9C78
		public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
		{
			for (int i = 0; i < 2; i++)
			{
				int num = i * ((!this.colored) ? this.bumps : (this.bumps * 2));
				for (int j = this.startSprite; j < this.startSprite + this.bumps; j++)
				{
					float f = Mathf.Lerp(0.05f, this.spineLength / this.iGraphics.BodyAndTailLength, Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.bumps - 1), (float)j));
					sLeaser.sprites[j + num].color = this.cosmeticData.baseColor(iGraphics, f);
					if (this.colored)
					{
						sLeaser.sprites[j + this.bumps + num].color = this.cosmeticData.effectColor;
					}
				}
			}
		}

		// Token: 0x040021F7 RID: 8695
		public int bumps;

		// Token: 0x040021F8 RID: 8696
		public float spineLength;

		// Token: 0x040021F9 RID: 8697
		public float sizeSkewExponent;

		// Token: 0x040021FA RID: 8698
		public float sizeRangeMin;

		// Token: 0x040021FB RID: 8699
		public float sizeRangeMax;

		// Token: 0x040021FC RID: 8700
		public float undersideSize;

		// Token: 0x040021FD RID: 8701
		public int graphic;

		// Token: 0x040021FE RID: 8702
		public float scaleX;

		// Token: 0x040021FF RID: 8703
		public bool colored;
	}
}
