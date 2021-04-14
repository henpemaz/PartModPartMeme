﻿using UnityEngine;

namespace LizardSkin
{
    internal class GenericAxolotlGills : GenericLongBodyScales
	{
		CosmeticAxolotlGillsData cosmeticAxolotlGillsData => cosmeticData as CosmeticAxolotlGillsData;

		public GenericAxolotlGills(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
		{
			//this.rigor = UnityEngine.Random.value;
			float scale = longBodyScalesData.scale;// Mathf.Pow(UnityEngine.Random.value, 0.7f);// * iGraphics.lizard.lizardParams.headSize;
			//this.colored = true;
			//this.graphic = UnityEngine.Random.Range(0, 6);
			//if (this.graphic == 2)
			//{
			//	this.graphic = UnityEngine.Random.Range(0, 6);
			//}
			this.graphicHeight = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic).sourcePixelSize.y;
			int count = cosmeticAxolotlGillsData.count;// UnityEngine.Random.Range(2, 8);
			this.scalesPositions = new Vector2[count * 2];
			this.scaleObjects = new GenericLizardScale[this.scalesPositions.Length];
			this.backwardsFactors = new float[this.scalesPositions.Length];
			float thickness = longBodyScalesData.thickness; // UnityEngine.Random.value;
			float spread = cosmeticAxolotlGillsData.spread; //Mathf.Lerp(0.1f, 0.9f, UnityEngine.Random.value);
			for (int i = 0; i < count; i++)
			{
				float y = Mathf.Lerp(0f, 0.07f, Mathf.Pow(UnityEngine.Random.value, 1.3f));
				float num4 = Mathf.Lerp(0.5f, 1.5f, UnityEngine.Random.value);
				float num5 = Mathf.Lerp(0.2f, 1f, Mathf.Pow(UnityEngine.Random.value, 0.5f));
				float num6 = Mathf.Pow(UnityEngine.Random.value, 0.5f);
				for (int j = 0; j < 2; j++)
				{
					this.scalesPositions[i * 2 + j] = new Vector2((j != 0) ? num4 : (-num4), y);
					this.scaleObjects[i * 2 + j] = new GenericLizardScale(this);
					this.scaleObjects[i * 2 + j].length = Mathf.Lerp(5f, 35f, scale * num5);
					this.scaleObjects[i * 2 + j].width = Mathf.Lerp(0.65f, 1.2f, thickness * scale);
					this.backwardsFactors[i * 2 + j] = spread * num6;
				}
			}
			this.numberOfSprites = ((!this.colored) ? this.scalesPositions.Length : (this.scalesPositions.Length * 2));
		}

		// applying palette ?? not sure if this is really needed unles this was like shifting colors
		//// Token: 0x06001F56 RID: 8022 RVA: 0x001DB1F0 File Offset: 0x001D93F0
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
