using UnityEngine;
using RWCustom;

namespace LizardSkin
{
    internal class GenericBumpHawk : GenericCosmeticTemplate
    {
        public GenericBumpHawk(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
		{
			this.coloredHawk = (UnityEngine.Random.value < 0.5f);
			this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.BehindHead;
			float num;
			if (this.coloredHawk)
			{
				num = Mathf.Lerp(3f, 8f, Mathf.Pow(UnityEngine.Random.value, 0.7f));
				this.spineLength = Mathf.Lerp(0.3f, 0.7f, UnityEngine.Random.value) * iGraphics.BodyAndTailLength;
				this.sizeRangeMin = Mathf.Lerp(0.1f, 0.2f, UnityEngine.Random.value);
				this.sizeRangeMax = Mathf.Lerp(this.sizeRangeMin, 0.35f, Mathf.Pow(UnityEngine.Random.value, 0.5f));
			}
			else
			{
				num = Mathf.Lerp(6f, 12f, Mathf.Pow(UnityEngine.Random.value, 0.5f));
				this.spineLength = Mathf.Lerp(0.3f, 0.9f, UnityEngine.Random.value) * iGraphics.BodyAndTailLength;
				this.sizeRangeMin = Mathf.Lerp(0.2f, 0.3f, Mathf.Pow(UnityEngine.Random.value, 0.5f));
				this.sizeRangeMax = Mathf.Lerp(this.sizeRangeMin, 0.5f, UnityEngine.Random.value);
			}
			this.sizeSkewExponent = Mathf.Lerp(0.1f, 0.7f, UnityEngine.Random.value);
			this.bumps = (int)(this.spineLength / num);
			this.numberOfSprites = this.bumps;
		}

		// Token: 0x06001F32 RID: 7986 RVA: 0x001D8884 File Offset: 0x001D6A84
		public override void Update()
		{
		}

		// Token: 0x06001F33 RID: 7987 RVA: 0x001D8888 File Offset: 0x001D6A88
		public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
		{
			for (int i = this.startSprite + this.numberOfSprites - 1; i >= this.startSprite; i--)
			{
				float num = Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.numberOfSprites - 1), (float)i);
				sLeaser.sprites[i] = new FSprite("Circle20", true);
				sLeaser.sprites[i].scale = Mathf.Lerp(this.sizeRangeMin, this.sizeRangeMax, Mathf.Lerp(Mathf.Sin(Mathf.Pow(num, this.sizeSkewExponent) * 3.1415927f), 1f, (num >= 0.5f) ? 0f : 0.5f));
			}
		}

		// Token: 0x06001F34 RID: 7988 RVA: 0x001D8948 File Offset: 0x001D6B48
		public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
		{
			for (int i = this.startSprite + this.numberOfSprites - 1; i >= this.startSprite; i--)
			{
				float num = Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.numberOfSprites - 1), (float)i);
				float num2 = Mathf.Lerp(0.05f, this.spineLength / this.iGraphics.BodyAndTailLength, num);
				SpineData lizardSpineData = this.iGraphics.SpinePosition(num2, timeStacker);
				sLeaser.sprites[i].x = lizardSpineData.outerPos.x - camPos.x;
				sLeaser.sprites[i].y = lizardSpineData.outerPos.y - camPos.y;
				//if (this.coloredHawk || this.iGraphics.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
				//{
					if (this.coloredHawk)
					{
					//sLeaser.sprites[i].color = Color.Lerp(this.iGraphics.HeadColor(timeStacker), this.iGraphics.BodyColor(num2), num);
					sLeaser.sprites[i].color = Color.Lerp(this.cosmeticData.effectColor, this.cosmeticData.GetBaseColor(iGraphics, num2), num);
				}
				//	else
				//	{
				//		sLeaser.sprites[i].color = this.iGraphics.DynamicBodyColor(num);
				//	}
				//}
			}
		}

		// Token: 0x06001F35 RID: 7989 RVA: 0x001D8A84 File Offset: 0x001D6C84
		public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
		{
			if (!this.coloredHawk)
			{
				for (int i = this.startSprite; i < this.startSprite + this.numberOfSprites; i++)
				{
					float f = Mathf.Lerp(0.05f, this.spineLength / this.iGraphics.BodyAndTailLength, Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.numberOfSprites - 1), (float)i));
					sLeaser.sprites[i].color = this.cosmeticData.GetBaseColor(iGraphics, f);
				}
			}
		}

		// Token: 0x040021E0 RID: 8672
		public int bumps;

		// Token: 0x040021E1 RID: 8673
		public float spineLength;

		// Token: 0x040021E2 RID: 8674
		public float sizeSkewExponent;

		// Token: 0x040021E3 RID: 8675
		public float sizeRangeMin;

		// Token: 0x040021E4 RID: 8676
		public float sizeRangeMax;

		// Token: 0x040021E5 RID: 8677
		public bool coloredHawk;
	}
}
