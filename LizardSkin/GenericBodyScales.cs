using RWCustom;
using UnityEngine;

namespace LizardSkin
{
	// Token: 0x020004BE RID: 1214
	public class GenericBodyScales : GenericCosmeticTemplate
	{
		// Token: 0x06001F3B RID: 7995 RVA: 0x001D9310 File Offset: 0x001D7510
		public GenericBodyScales(ICosmeticsAdaptor iGraphics) : base(iGraphics)
		{
			//this.spritesOverlap = ((!(this is SlugcatLongHeadScales)) ? SlugcatCosmeticsTemplate.SpritesOverlap.BehindHead : SlugcatCosmeticsTemplate.SpritesOverlap.InFront);
			this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.BehindHead;
		}

		// Token: 0x06001F3C RID: 7996 RVA: 0x001D9340 File Offset: 0x001D7540
		protected void GeneratePatchPattern(float startPoint, int numOfScales, float maxLength, float lengthExponent)
		{
			this.scalesPositions = new Vector2[numOfScales];
			float num = Mathf.Lerp(startPoint + 0.1f, Mathf.Max(startPoint + 0.2f, maxLength), Mathf.Pow(Random.value, lengthExponent));
			for (int i = 0; i < this.scalesPositions.Length; i++)
			{
				Vector2 vector = Custom.DegToVec(Random.value * 360f) * Random.value;
				this.scalesPositions[i].y = Mathf.Lerp(startPoint * this.iGraphics.bodyLength / this.iGraphics.BodyAndTailLength, num * this.iGraphics.bodyLength / this.iGraphics.BodyAndTailLength, (vector.y + 1f) / 2f);
				this.scalesPositions[i].x = vector.x;
			}
		}

		// Token: 0x06001F3D RID: 7997 RVA: 0x001D9428 File Offset: 0x001D7628
		protected void GenerateTwoLines(float startPoint, float maxLength, float lengthExponent, float spacingScale)
		{
			float num = Mathf.Lerp(startPoint + 0.1f, Mathf.Max(startPoint + 0.2f, maxLength), Mathf.Pow(Random.value, lengthExponent));
			float num2 = num * this.iGraphics.BodyAndTailLength;
			float num3 = Mathf.Lerp(2f, 9f, Random.value);
			//if (this.pGraphics.lizard.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.BlueLizard)
			//{
			//	num3 = 2f;
			//}
			num3 = 2f;
			num3 *= spacingScale;
			int num4 = (int)(num2 / num3);
			if (num4 < 3)
			{
				num4 = 3;
			}
			this.scalesPositions = new Vector2[num4 * 2];
			for (int i = 0; i < num4; i++)
			{
				float y = Mathf.Lerp(0f, num, (float)i / (float)(num4 - 1));
				float num5 = 0.6f + 0.4f * Mathf.Sin((float)i / (float)(num4 - 1) * 3.1415927f);
				this.scalesPositions[i * 2] = new Vector2(num5, y);
				this.scalesPositions[i * 2 + 1] = new Vector2(-num5, y);
			}
		}

		// Token: 0x06001F3E RID: 7998 RVA: 0x001D9550 File Offset: 0x001D7750
		protected void GenerateSegments(float startPoint, float maxLength, float lengthExponent)
		{
			float num = Mathf.Lerp(startPoint + 0.1f, Mathf.Max(startPoint + 0.2f, maxLength), Mathf.Pow(Random.value, lengthExponent));
			float num2 = num * this.iGraphics.BodyAndTailLength;
			float num3 = Mathf.Lerp(7f, 14f, Random.value);
			//if (this.pGraphics.lizard.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.RedLizard)
			//{
			//	num3 = Mathf.Min(num3, 11f) * 0.75f;
			//}
			int num4 = Mathf.Max(3, (int)(num2 / num3));
			int num5 = Random.Range(1, 4) * 2;
			this.scalesPositions = new Vector2[num4 * num5];
			for (int i = 0; i < num4; i++)
			{
				float y = Mathf.Lerp(0f, num, (float)i / (float)(num4 - 1));
				for (int j = 0; j < num5; j++)
				{
					float num6 = 0.6f + 0.6f * Mathf.Sin((float)i / (float)(num4 - 1) * 3.1415927f);
					num6 *= Mathf.Lerp(-1f, 1f, (float)j / (float)(num5 - 1));
					this.scalesPositions[i * num5 + j] = new Vector2(num6, y);
				}
			}
		}

		// Token: 0x06001F3F RID: 7999 RVA: 0x001D96A0 File Offset: 0x001D78A0
		protected LizardGraphics.LizardSpineData GetBackPos(int shoulderScale, float timeStacker, bool changeDepthRotation)
		{
			LizardGraphics.LizardSpineData result = this.iGraphics.SpinePosition(this.scalesPositions[shoulderScale].y, timeStacker);
			float num = Mathf.Clamp(this.scalesPositions[shoulderScale].x + result.depthRotation, -1f, 1f);
			result.outerPos = result.pos + result.perp * num * result.rad;
			if (changeDepthRotation)
			{
				result.depthRotation = num;
			}
			return result;
		}

		// Token: 0x06001F40 RID: 8000 RVA: 0x001D9730 File Offset: 0x001D7930
		public override void Update()
		{
		}

		// Token: 0x06001F41 RID: 8001 RVA: 0x001D9734 File Offset: 0x001D7934
		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
		}

		// Token: 0x06001F42 RID: 8002 RVA: 0x001D9738 File Offset: 0x001D7938
		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
		}

		// Token: 0x06001F43 RID: 8003 RVA: 0x001D973C File Offset: 0x001D793C
		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			base.ApplyPalette(sLeaser, rCam, palette);
		}

		// Token: 0x040021EE RID: 8686
		public int graphic;

		// Token: 0x040021EF RID: 8687
		public float scaleX;

		// Token: 0x040021F0 RID: 8688
		public bool colored;

		// Token: 0x040021F1 RID: 8689
		public Vector2[] scalesPositions;
	}
}