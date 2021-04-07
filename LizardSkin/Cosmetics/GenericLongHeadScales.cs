using UnityEngine;

namespace LizardSkin
{
    internal class GenericLongHeadScales : GenericLongBodyScales
	{
        public GenericLongHeadScales(ICosmeticsAdaptor iGraphics, LizKinCosmeticData cosmeticData) : base(iGraphics, cosmeticData)
		{
			this.rigor = UnityEngine.Random.value;
			//if (iGraphics.lizard.Template.type != CreatureTemplate.Type.PinkLizard || UnityEngine.Random.value < 0.33333334f)
			//{
			//	int num = UnityEngine.Random.Range(0, 3);
			//}
			//else if (iGraphics.lizard.Template.type == CreatureTemplate.Type.GreenLizard || UnityEngine.Random.value < 0.5f)
			//{*
			//}
			this.GenerateTwoHorns();
			//float num2 = Mathf.Pow(UnityEngine.Random.value, 0.7f) * iGraphics.lizard.lizardParams.headSize;
			//this.colored = (UnityEngine.Random.value < 0.5f && iGraphics.lizard.Template.type != CreatureTemplate.Type.WhiteLizard);
			float num2 = Mathf.Pow(UnityEngine.Random.value, 0.7f);
			this.colored = (UnityEngine.Random.value < 0.5f); // && iGraphics.lizard.Template.type != CreatureTemplate.Type.WhiteLizard);
			this.graphic = UnityEngine.Random.Range(4, 6);
			if (num2 < 0.5f && UnityEngine.Random.value < 0.5f)
			{
				this.graphic = 6;
			}
			else if (num2 > 0.8f)
			{
				this.graphic = 5;
			}
			// if (num2 < 0.2f && iGraphics.lizard.Template.type != CreatureTemplate.Type.WhiteLizard)
			if (num2 < 0.2f)// && iGraphics.lizard.Template.type != CreatureTemplate.Type.WhiteLizard)
			{
				this.colored = true;
			}
			//if (iGraphics.lizard.Template.type == CreatureTemplate.Type.BlackLizard)
			//{
			//	this.colored = false;
			//}
			this.graphicHeight = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic).sourcePixelSize.y;
			this.scaleObjects = new GenericLizardScale[this.scalesPositions.Length];
			this.backwardsFactors = new float[this.scalesPositions.Length];
			float value = UnityEngine.Random.value;
			float num3 = Mathf.Pow(UnityEngine.Random.value, 0.85f);
			for (int i = 0; i < this.scalesPositions.Length; i++)
			{
				this.scaleObjects[i] = new GenericLizardScale(this);
				this.scaleObjects[i].length = Mathf.Lerp(5f, 35f, num2);
				this.scaleObjects[i].width = Mathf.Lerp(0.65f, 1.2f, value * num2);
				this.backwardsFactors[i] = num3;
			}
			this.numberOfSprites = ((!this.colored) ? this.scalesPositions.Length : (this.scalesPositions.Length * 2));
		}

		// Token: 0x06001F53 RID: 8019 RVA: 0x001DAE98 File Offset: 0x001D9098
		protected void GenerateTwoHorns()
		{
			this.scalesPositions = new Vector2[2];
			float y = Mathf.Lerp(0f, 0.07f, UnityEngine.Random.value);
			float num = Mathf.Lerp(0.5f, 1.5f, UnityEngine.Random.value);
			for (int i = 0; i < this.scalesPositions.Length; i++)
			{
				this.scalesPositions[i] = new Vector2((i != 0) ? num : (-num), y);
			}
		}

		// Token: 0x06001F54 RID: 8020 RVA: 0x001DAF1C File Offset: 0x001D911C
		public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
		{
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
			{
				sLeaser.sprites[i].color = this.cosmeticData.baseColor(iGraphics, 0);
				if (this.colored)
				{
					sLeaser.sprites[i + this.scalesPositions.Length].color = this.cosmeticData.effectColor;
				}
			}
		}
	}
}
