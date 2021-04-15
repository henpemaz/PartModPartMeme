using UnityEngine;

namespace LizardSkin
{
    internal class GenericLongHeadScales : GenericLongBodyScales
	{
		CosmeticLongHeadScalesData longHeadScalesData => cosmeticData as CosmeticLongHeadScalesData;
		public GenericLongHeadScales(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
		{
			this.scalesPositions = new Vector2[2];
			this.graphicHeight = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic).sourcePixelSize.y;
			this.scaleObjects = new GenericLizardScale[this.scalesPositions.Length];
			this.backwardsFactors = new float[this.scalesPositions.Length];
			for (int i = 0; i < this.scalesPositions.Length; i++)
			{
				this.scaleObjects[i] = new GenericLizardScale(this);
			}
			this.numberOfSprites = ((!this.colored) ? this.scalesPositions.Length : (this.scalesPositions.Length * 2));
			PositionStuff();
		}

		private void PositionStuff()
        {
			// copypasted code to keep things inline with the settings
			float y = longHeadScalesData.spinePos; // Mathf.Lerp(0f, 0.07f, UnityEngine.Random.value);
			float num = longHeadScalesData.offset; // Mathf.Lerp(0.5f, 1.5f, UnityEngine.Random.value);
			float num2 = longBodyScalesData.scale; // Mathf.Pow(UnityEngine.Random.value, 0.7f);
			float value = longBodyScalesData.thickness; // UnityEngine.Random.value;
			float num3 = longHeadScalesData.angle; // Mathf.Pow(UnityEngine.Random.value, 0.85f);
			for (int i = 0; i < this.scalesPositions.Length; i++)
			{
				this.scalesPositions[i] = new Vector2((i != 0) ? num : (-num), y);
				this.scaleObjects[i].length = num2 * graphicHeight; // Mathf.Lerp(5f, 35f, num2);
				this.scaleObjects[i].width = value * num2; // Mathf.Lerp(0.65f, 1.2f, value * num2);
				this.backwardsFactors[i] = num3;
			}
		}

        public override void Update()
        {
			PositionStuff();
			base.Update();
        }

        // Only used in GenericLongHeadScales, moved to ctor
        // Token: 0x06001F53 RID: 8019 RVA: 0x001DAE98 File Offset: 0x001D9098
        //protected void GenerateTwoHorns()
        //{
        //	this.scalesPositions = new Vector2[2];
        //	float y = Mathf.Lerp(0f, 0.07f, UnityEngine.Random.value);
        //	float num = Mathf.Lerp(0.5f, 1.5f, UnityEngine.Random.value);
        //	for (int i = 0; i < this.scalesPositions.Length; i++)
        //	{
        //		this.scalesPositions[i] = new Vector2((i != 0) ? num : (-num), y);
        //	}
        //}

		// Apply palette takes care of that
        // Token: 0x06001F54 RID: 8020 RVA: 0x001DAF1C File Offset: 0x001D911C
		//public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
		//{
		//	base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		//	for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
		//	{
		//		sLeaser.sprites[i].color = this.cosmeticData.GetBaseColor(iGraphics, 0);
		//		if (this.colored)
		//		{
		//			sLeaser.sprites[i + this.scalesPositions.Length].color = this.cosmeticData.effectColor;
		//		}
		//	}
		//}
	}
}
