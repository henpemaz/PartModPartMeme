using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericLongShoulderScales : GenericLongBodyScales
	{
        public GenericLongShoulderScales(ICosmeticsAdaptor iGraphics) : base(iGraphics)
		{
			this.rigor = 0f;
			int num = 0;
			//if (iGraphics.lizard.Template.type != CreatureTemplate.Type.PinkLizard || UnityEngine.Random.value < 0.33333334f)
			//{
			//	num = UnityEngine.Random.Range(0, 3);
			//}
			//else if (iGraphics.lizard.Template.type == CreatureTemplate.Type.GreenLizard || UnityEngine.Random.value < 0.5f)
			//{
			//	num = 2;
			//}
			num = UnityEngine.Random.Range(0, 3);
			switch (num)
			{
				case 0:
					base.GeneratePatchPattern(0.05f, UnityEngine.Random.Range(4, 15), 0.9f, 2f);
					break;
				case 1:
					base.GenerateTwoLines(0.07f, 1f, 1.5f, 3f);
					break;
				case 2:
					base.GenerateSegments(0.1f, 0.8f, 5f);
					break;
			}
			this.MoveScalesTowardsHead();
			float num2 = Mathf.Lerp(1f, 1f / Mathf.Lerp(1f, (float)this.scalesPositions.Length, Mathf.Pow(UnityEngine.Random.value, 2f)), 0.5f);
			float num3 = Mathf.Lerp(5f, 15f, UnityEngine.Random.value) * num2;
			float num4 = Mathf.Lerp(num3, 35f, Mathf.Pow(UnityEngine.Random.value, 0.5f)) * num2;
			//if (iGraphics.lizard.Template.type == CreatureTemplate.Type.RedLizard)
			//{
			//	if (this.scalesPositions.Length > 8)
			//	{
			//		this.StretchDownOnBack((num != 0) ? 0.5f : 0.3f);
			//	}
			//	num2 = Mathf.Max(0.5f, num2);
			//	num3 = Mathf.Max(10f, num3) * 1.2f;
			//	num4 = Mathf.Max(25f, num4) * 1.2f;
			//}

			//this.colored = (iGraphics.lizard.Template.type == CreatureTemplate.Type.GreenLizard || iGraphics.lizard.Template.type == CreatureTemplate.Type.RedLizard || UnityEngine.Random.value < 0.4f);
			this.colored = (UnityEngine.Random.value < 0.6f);
			if (UnityEngine.Random.value < 0.1f)
			{
				this.graphic = UnityEngine.Random.Range(0, 7);
			}
			else
			{
				this.graphic = UnityEngine.Random.Range(3, 6);
			}
			//if (iGraphics.lizard.Template.type == CreatureTemplate.Type.PinkLizard && UnityEngine.Random.value < 0.25f)
			//{
			//	this.graphic = 0;
			//}
			//if (iGraphics.lizard.Template.type == CreatureTemplate.Type.RedLizard)
			//{
			//	this.graphic = 0;
			//	if (UnityEngine.Random.value < 0.3f)
			//	{
			//		this.graphic = 3;
			//		this.scaleX = ((UnityEngine.Random.value >= 0.5f) ? 1f : -1f);
			//	}
			//}
			if (UnityEngine.Random.value < 0.25f)
            {
                this.graphic = 0;
            }

            this.graphicHeight = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic).sourcePixelSize.y;
			this.scaleObjects = new GenericLizardScale[this.scalesPositions.Length];
			this.backwardsFactors = new float[this.scalesPositions.Length];
			float num5 = 0f;
			float num6 = 1f;
			for (int i = 0; i < this.scalesPositions.Length; i++)
			{
				if (this.scalesPositions[i].y > num5)
				{
					num5 = this.scalesPositions[i].y;
				}
				if (this.scalesPositions[i].y < num6)
				{
					num6 = this.scalesPositions[i].y;
				}
			}
			float p = Mathf.Lerp(0.1f, 0.9f, UnityEngine.Random.value);
			for (int j = 0; j < this.scalesPositions.Length; j++)
			{
				this.scaleObjects[j] = new GenericLizardScale(this);
				float num7 = Mathf.Pow(Mathf.InverseLerp(num6, num5, this.scalesPositions[j].y), p);
				this.scaleObjects[j].length = Mathf.Lerp(num3, num4, Mathf.Lerp(Mathf.Sin(num7 * 3.1415927f), 1f, (num7 >= 0.5f) ? 0f : 0.5f));
				this.scaleObjects[j].width = Mathf.Lerp(0.8f, 1.2f, Mathf.Lerp(Mathf.Sin(num7 * 3.1415927f), 1f, (num7 >= 0.5f) ? 0f : 0.5f)) * num2;
				this.backwardsFactors[j] = this.scalesPositions[j].y * 0.7f;
			}
			this.numberOfSprites = ((!this.colored) ? this.scalesPositions.Length : (this.scalesPositions.Length * 2));
		}

		// Token: 0x06001F50 RID: 8016 RVA: 0x001DAAE8 File Offset: 0x001D8CE8
		private void MoveScalesTowardsHead()
		{
			float num = 1f;
			for (int i = 0; i < this.scalesPositions.Length; i++)
			{
				if (this.scalesPositions[i].y < num)
				{
					num = this.scalesPositions[i].y;
				}
			}
			if (num > 0.07f)
			{
				num -= 0.07f;
				for (int j = 0; j < this.scalesPositions.Length; j++)
				{
					Vector2[] scalesPositions = this.scalesPositions;
					int num2 = j;
					scalesPositions[num2].y = scalesPositions[num2].y - num;
				}
			}
		}

		// Token: 0x06001F51 RID: 8017 RVA: 0x001DAB84 File Offset: 0x001D8D84
		private void StretchDownOnBack(float stretchTo)
		{
			float num = 1f;
			float num2 = 0f;
			for (int i = 0; i < this.scalesPositions.Length; i++)
			{
				num = Mathf.Min(num, this.scalesPositions[i].y);
				num2 = Mathf.Max(num2, this.scalesPositions[i].y);
			}
			if (num2 > stretchTo)
			{
				return;
			}
			for (int j = 0; j < this.scalesPositions.Length; j++)
			{
				this.scalesPositions[j].y = Custom.LerpMap(this.scalesPositions[j].y, num, num2, num, stretchTo);
			}
		}
	}
}
