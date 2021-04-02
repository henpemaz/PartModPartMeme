using UnityEngine;

namespace LizardSkin
{
    internal class GenericAxolotlGills : GenericLongBodyScales

	{
		public GenericAxolotlGills(ICosmeticsAdaptor iGraphics) : base(iGraphics)
		{
			this.rigor = UnityEngine.Random.value;
			float num = Mathf.Pow(UnityEngine.Random.value, 0.7f);// * iGraphics.lizard.lizardParams.headSize;
			this.colored = true;
			this.graphic = UnityEngine.Random.Range(0, 6);
			if (this.graphic == 2)
			{
				this.graphic = UnityEngine.Random.Range(0, 6);
			}
			this.graphicHeight = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic).sourcePixelSize.y;
			int num2 = UnityEngine.Random.Range(2, 8);
			this.scalesPositions = new Vector2[num2 * 2];
			this.scaleObjects = new GenericLizardScale[this.scalesPositions.Length];
			this.backwardsFactors = new float[this.scalesPositions.Length];
			float value = UnityEngine.Random.value;
			float num3 = Mathf.Lerp(0.1f, 0.9f, UnityEngine.Random.value);
			for (int i = 0; i < num2; i++)
			{
				float y = Mathf.Lerp(0f, 0.07f, Mathf.Pow(UnityEngine.Random.value, 1.3f));
				float num4 = Mathf.Lerp(0.5f, 1.5f, UnityEngine.Random.value);
				float num5 = Mathf.Lerp(0.2f, 1f, Mathf.Pow(UnityEngine.Random.value, 0.5f));
				float num6 = Mathf.Pow(UnityEngine.Random.value, 0.5f);
				for (int j = 0; j < 2; j++)
				{
					this.scalesPositions[i * 2 + j] = new Vector2((j != 0) ? num4 : (-num4), y);
					this.scaleObjects[i * 2 + j] = new GenericLizardScale(this);
					this.scaleObjects[i * 2 + j].length = Mathf.Lerp(5f, 35f, num * num5);
					this.scaleObjects[i * 2 + j].width = Mathf.Lerp(0.65f, 1.2f, value * num);
					this.backwardsFactors[i * 2 + j] = num3 * num6;
				}
			}
			this.numberOfSprites = ((!this.colored) ? this.scalesPositions.Length : (this.scalesPositions.Length * 2));
		}

		// Token: 0x06001F56 RID: 8022 RVA: 0x001DB1F0 File Offset: 0x001D93F0
		public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
		{
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
			{
				sLeaser.sprites[i].color = this.iGraphics.HeadColor(timeStacker);
				if (this.colored)
				{
					sLeaser.sprites[i + this.scalesPositions.Length].color = this.iGraphics.effectColor;
				}
			}
		}
	}
}
