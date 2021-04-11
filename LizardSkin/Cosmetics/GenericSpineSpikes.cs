using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericSpineSpikes : GenericCosmeticTemplate
    {
        public GenericSpineSpikes(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
		{
			this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.BehindHead;
			float num = Mathf.Lerp(5f, 8f, Mathf.Pow(UnityEngine.Random.value, 0.7f));
			this.spineLength = Mathf.Lerp(0.2f, 0.95f, UnityEngine.Random.value) * iGraphics.BodyAndTailLength;
			this.sizeRangeMin = Mathf.Lerp(0.1f, 0.5f, Mathf.Pow(UnityEngine.Random.value, 2f));
			this.sizeRangeMax = Mathf.Lerp(this.sizeRangeMin, 1.1f, UnityEngine.Random.value);
			if (UnityEngine.Random.value < 0.5f)
			{
				this.sizeRangeMax = 1f;
			}
			//if (iGraphics.lizard.Template.type == CreatureTemplate.Type.BlueLizard)
			//{
			//	this.sizeRangeMin = Mathf.Min(this.sizeRangeMin, 0.3f);
			//	this.sizeRangeMax = Mathf.Min(this.sizeRangeMax, 0.6f);
			//}
			//else if (iGraphics.lizard.Template.type != CreatureTemplate.Type.GreenLizard && UnityEngine.Random.value < 0.7f)
			//{
			//	this.sizeRangeMin *= 0.7f;
			//	this.sizeRangeMax *= 0.7f;
			//}
			//else if (iGraphics.lizard.Template.type == CreatureTemplate.Type.GreenLizard && UnityEngine.Random.value < 0.7f)
			//{
			//	this.sizeRangeMin = Mathf.Lerp(this.sizeRangeMin, 1.1f, 0.1f);
			//	this.sizeRangeMax = Mathf.Lerp(this.sizeRangeMax, 1.1f, 0.4f);
			//}

			this.sizeRangeMin = Mathf.Min(this.sizeRangeMin, 0.3f);
			this.sizeRangeMax = Mathf.Min(this.sizeRangeMax, 0.6f);

			this.sizeSkewExponent = Mathf.Lerp(0.1f, 0.9f, UnityEngine.Random.value);
			this.bumps = (int)(this.spineLength / num);
			this.scaleX = 1f;
			this.graphic = UnityEngine.Random.Range(0, 5);
			if (this.graphic == 1)
			{
				this.graphic = 0;
			}
			if (this.graphic == 4)
			{
				this.graphic = 3;
			}
			else if (this.graphic == 3 && UnityEngine.Random.value < 0.5f)
			{
				this.scaleX = -1f;
			}
			else if (UnityEngine.Random.value < 0.06666667f)
			{
				this.scaleX = -1f;
			}
			//if (iGraphics.lizard.Template.type == CreatureTemplate.Type.PinkLizard && UnityEngine.Random.value < 0.7f)
			//{
			//	this.graphic = 0;
			//}
			//else if (iGraphics.lizard.Template.type == CreatureTemplate.Type.GreenLizard && UnityEngine.Random.value < 0.5f)
			//{
			//	this.graphic = 3;
			//}
			this.colored = UnityEngine.Random.Range(0, 3);
			//if (iGraphics.lizard.Template.type == CreatureTemplate.Type.PinkLizard && UnityEngine.Random.value < 0.5f)
			//{
			//	this.colored = 0;
			//}
			//else if (iGraphics.lizard.Template.type == CreatureTemplate.Type.GreenLizard && UnityEngine.Random.value < 0.5f)
			//{
			//	this.colored = 2;
			//}
			//else if (iGraphics.lizard.Template.type == CreatureTemplate.Type.GreenLizard && UnityEngine.Random.value < 0.5f)
			//{
			//	this.colored = 1;
			//}
			this.numberOfSprites = ((this.colored <= 0) ? this.bumps : (this.bumps * 2));
		}

		// Token: 0x06001F37 RID: 7991 RVA: 0x001D8E88 File Offset: 0x001D7088
		public override void Update()
		{
		}

		// Token: 0x06001F38 RID: 7992 RVA: 0x001D8E8C File Offset: 0x001D708C
		public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
		{
			for (int i = this.startSprite + this.bumps - 1; i >= this.startSprite; i--)
			{
				float num = Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.bumps - 1), (float)i);
				sLeaser.sprites[i] = new FSprite("LizardScaleA" + this.graphic, true);
				sLeaser.sprites[i].anchorY = 0.15f;
				if (this.colored > 0)
				{
					sLeaser.sprites[i + this.bumps] = new FSprite("LizardScaleB" + this.graphic, true);
					sLeaser.sprites[i + this.bumps].anchorY = 0.15f;
				}
			}
		}

		// Token: 0x06001F39 RID: 7993 RVA: 0x001D8F64 File Offset: 0x001D7164
		public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
		{
			for (int i = this.startSprite + this.bumps - 1; i >= this.startSprite; i--)
			{
				float num = Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.bumps - 1), (float)i);
				SpineData lizardSpineData = this.iGraphics.SpinePosition(Mathf.Lerp(0.05f, this.spineLength / this.iGraphics.BodyAndTailLength, num), timeStacker);
				sLeaser.sprites[i].x = lizardSpineData.outerPos.x - camPos.x;
				sLeaser.sprites[i].y = lizardSpineData.outerPos.y - camPos.y;
				sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(-lizardSpineData.perp * lizardSpineData.depthRotation, lizardSpineData.perp * lizardSpineData.depthRotation);
				float num2 = Mathf.Lerp(this.sizeRangeMin, this.sizeRangeMax, Mathf.Sin(Mathf.Pow(num, this.sizeSkewExponent) * 3.1415927f));
				sLeaser.sprites[i].scaleX = Mathf.Sign(this.iGraphics.depthRotation) * this.scaleX * num2;
				sLeaser.sprites[i].scaleY = num2 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphics.depthRotation)));
				if (this.colored > 0)
				{
					sLeaser.sprites[i + this.bumps].x = lizardSpineData.outerPos.x - camPos.x;
					sLeaser.sprites[i + this.bumps].y = lizardSpineData.outerPos.y - camPos.y;
					sLeaser.sprites[i + this.bumps].rotation = Custom.AimFromOneVectorToAnother(-lizardSpineData.perp * lizardSpineData.depthRotation, lizardSpineData.perp * lizardSpineData.depthRotation);
					sLeaser.sprites[i + this.bumps].scaleX = Mathf.Sign(this.iGraphics.depthRotation) * this.scaleX * num2;
					sLeaser.sprites[i + this.bumps].scaleY = num2 * Mathf.Max(0.2f, Mathf.InverseLerp(0f, 0.5f, Mathf.Abs(this.iGraphics.depthRotation)));
				}
			}
		}

		// Token: 0x06001F3A RID: 7994 RVA: 0x001D91F4 File Offset: 0x001D73F4
		public override void ApplyPalette(LeaserAdaptor sLeaser, CameraAdaptor rCam, PaletteAdaptor palette)
		{
			for (int i = this.startSprite; i < this.startSprite + this.bumps; i++)
			{
				float f = Mathf.Lerp(0.05f, this.spineLength / this.iGraphics.BodyAndTailLength, Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.bumps - 1), (float)i));
				sLeaser.sprites[i].color = this.cosmeticData.GetBaseColor(iGraphics, f);
				if (this.colored == 1)
				{
					sLeaser.sprites[i + this.bumps].color = this.cosmeticData.effectColor;
				}
				else if (this.colored == 2)
				{
					float f2 = Mathf.InverseLerp((float)this.startSprite, (float)(this.startSprite + this.bumps - 1), (float)i);
					sLeaser.sprites[i + this.bumps].color = Color.Lerp(this.cosmeticData.effectColor, this.cosmeticData.GetBaseColor(iGraphics, f), Mathf.Pow(f2, 0.5f));
				}
			}
		}

		// Token: 0x040021E6 RID: 8678
		public int bumps;

		// Token: 0x040021E7 RID: 8679
		public float spineLength;

		// Token: 0x040021E8 RID: 8680
		public float sizeSkewExponent;

		// Token: 0x040021E9 RID: 8681
		public float sizeRangeMin;

		// Token: 0x040021EA RID: 8682
		public float sizeRangeMax;

		// Token: 0x040021EB RID: 8683
		public int graphic;

		// Token: 0x040021EC RID: 8684
		public float scaleX;

		// Token: 0x040021ED RID: 8685
		public int colored;
	}
}
