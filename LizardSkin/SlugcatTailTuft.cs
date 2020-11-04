﻿using UnityEngine;

namespace LizardSkin
{
    internal class SlugcatTailTuft : GenericLongBodyScales
    {
        public SlugcatTailTuft(ICosmeticsAdaptor iGraphics) : base(iGraphics)
		{
			this.rigor = 0f;
			//if (Random.value < 0.14285715f)// || (Random.value < 0.9f && pGraphics.lizard.Template.type == CreatureTemplate.Type.BlueLizard) || pGraphics.lizard.Template.type == CreatureTemplate.Type.RedLizard)
			//{
			//	//if (pGraphics.lizard.Template.type == CreatureTemplate.Type.BlueLizard || pGraphics.lizard.Template.type == CreatureTemplate.Type.RedLizard)
			//	//{
			//	//	base.GenerateTwoLines(0f, (pGraphics.lizard.Template.type != CreatureTemplate.Type.RedLizard) ? 0.7f : 0.3f, 1f, 3f);
			//	//}
			//	//else
			//	//{
			//	//	base.GenerateTwoLines(0f, 0.4f, 1.2f, 1.3f);
			//	//}
			//	base.GenerateTwoLines(0f, 0.4f, 1.2f, 1.3f);
			//}
			//else
			//{
			//	base.GeneratePatchPattern(0f, Random.Range(3, 7), 1.6f, 1.5f);
			//}
			base.GenerateTwoLines(0f, 0.5f, 1.2f, 1.3f);
			this.MoveScalesTowardsTail();
			float num = Mathf.Lerp(1f, 1f / Mathf.Lerp(1f, (float)this.scalesPositions.Length, Mathf.Pow(Random.value, 2f)), 0.5f);
			//if (pGraphics.lizard.Template.type == CreatureTemplate.Type.RedLizard)
			//{
			//	num = Mathf.Max(num, 0.4f) * 1.1f;
			//}
			float from = Mathf.Lerp(5f, 10f, Random.value) * num;
			float to = Mathf.Lerp(from, 25f, Mathf.Pow(Random.value, 0.5f)) * num;
			this.colored = (Random.value < 0.8f);
			this.graphic = Random.Range(3, 7);
			if (this.graphic == 3)
			{
				this.graphic = 1;
			}
			if (Random.value < 0.033333335f)
			{
				this.graphic = Random.Range(0, 7);
			}
			if (Random.value < 0.8f)// || pGraphics.lizard.Template.type == CreatureTemplate.Type.RedLizard)
			{
				for (int i = 0; i < iGraphics.cosmetics.Count; i++)
				{
					if (iGraphics.cosmetics[i] is GenericLongBodyScales)
					{
						this.graphic = (iGraphics.cosmetics[i] as GenericLongBodyScales).graphic;
						break;
					}
					//if (pGraphics.cosmetics[i] is SpineSpikes)
					//{
					//	this.graphic = (pGraphics.cosmetics[i] as SpineSpikes).graphic;
					//	break;
					//}
				}
			}
			this.graphicHeight = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic).sourcePixelSize.y;
			this.scaleObjects = new GenericLizardScale[this.scalesPositions.Length];
			this.backwardsFactors = new float[this.scalesPositions.Length];
			float num2 = 0f;
			float num3 = 1f;
			for (int j = 0; j < this.scalesPositions.Length; j++)
			{
				if (this.scalesPositions[j].y > num2)
				{
					num2 = this.scalesPositions[j].y;
				}
				if (this.scalesPositions[j].y < num3)
				{
					num3 = this.scalesPositions[j].y;
				}
			}
			float num4 = Mathf.Lerp(1f, 1.5f, Random.value);
			for (int k = 0; k < this.scalesPositions.Length; k++)
			{
				this.scaleObjects[k] = new GenericLizardScale(this);
				float t = Mathf.InverseLerp(num3, num2, this.scalesPositions[k].y);
				this.scaleObjects[k].length = Mathf.Lerp(from, to, t);
				this.scaleObjects[k].width = Mathf.Lerp(0.8f, 1.2f, t) * num;
				this.backwardsFactors[k] = 0.3f + 0.7f * Mathf.InverseLerp(0.75f, 1f, this.scalesPositions[k].y);
				Vector2[] scalesPositions = this.scalesPositions;
				int num5 = k;
				scalesPositions[num5].x = scalesPositions[num5].x * (Mathf.InverseLerp(1.05f, 0.85f, this.scalesPositions[k].y) * num4);
			}
			this.numberOfSprites = ((!this.colored) ? this.scalesPositions.Length : (this.scalesPositions.Length * 2));
		}

		// Token: 0x06001F4E RID: 8014 RVA: 0x001DA570 File Offset: 0x001D8770
		private void MoveScalesTowardsTail()
		{
			float num = 0f;
			for (int i = 0; i < this.scalesPositions.Length; i++)
			{
				if (this.scalesPositions[i].y > num)
				{
					num = this.scalesPositions[i].y;
				}
			}
			for (int j = 0; j < this.scalesPositions.Length; j++)
			{
				Vector2[] scalesPositions = this.scalesPositions;
				int num2 = j;
				scalesPositions[num2].y = scalesPositions[num2].y + (0.9f - num);
			}
		}
	}
}