using RWCustom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace LizardSkin
{
	// Token: 0x020004C0 RID: 1216
	public class GenericLongBodyScales : GenericBodyScales
	{
		// Token: 0x06001F48 RID: 8008 RVA: 0x001D9994 File Offset: 0x001D7B94
		public GenericLongBodyScales(ICosmeticsAdaptor iGraphics) : base(iGraphics)
		{

		}

		// Token: 0x06001F49 RID: 8009 RVA: 0x001D99A0 File Offset: 0x001D7BA0
		public override void Update()
		{
			for (int i = 0; i < this.scaleObjects.Length; i++)
			{
				LizardGraphics.LizardSpineData backPos = base.GetBackPos(i, 1f, true);
				Vector2 vector = Vector2.Lerp(backPos.dir, Custom.DirVec(backPos.pos, backPos.outerPos), Mathf.Abs(backPos.depthRotation));
				if (this.scalesPositions[i].y < 0.2f)
				{
					vector -= Custom.DegToVec(this.iGraphics.HeadRotation(1f)) * Mathf.Pow(Mathf.InverseLerp(0.2f, 0f, this.scalesPositions[i].y), 2f) * 2f;
				}
				vector = Vector2.Lerp(vector, backPos.dir, Mathf.Pow(this.backwardsFactors[i], Mathf.Lerp(1f, 15f, this.iGraphics.showDominance))).normalized;
				Vector2 vector2 = backPos.outerPos + vector * this.scaleObjects[i].length;
				if (!Custom.DistLess(this.scaleObjects[i].pos, vector2, this.scaleObjects[i].length / 2f))
				{
					Vector2 a = Custom.DirVec(this.scaleObjects[i].pos, vector2);
					float num = Vector2.Distance(this.scaleObjects[i].pos, vector2);
					float num2 = this.scaleObjects[i].length / 2f;
					this.scaleObjects[i].pos += a * (num - num2);
					this.scaleObjects[i].vel += a * (num - num2);
				}
				this.scaleObjects[i].vel += Vector2.ClampMagnitude(vector2 - this.scaleObjects[i].pos, Mathf.Lerp(10f, 20f, this.iGraphics.showDominance)) / Mathf.Lerp(5f, 1.5f, this.rigor);
				this.scaleObjects[i].vel *= Mathf.Lerp(1f, 0.8f, this.rigor);
				if (this.iGraphics.showDominance > 0f)
				{
					this.scaleObjects[i].vel += Custom.DegToVec(Random.value * 360f) * Mathf.Lerp(0f, 6f, this.iGraphics.showDominance);
				}
				this.scaleObjects[i].ConnectToPoint(backPos.outerPos, this.scaleObjects[i].length, true, 0f, new Vector2(0f, 0f), 0f, 0f);
				this.scaleObjects[i].Update();
			}
		}

		// Token: 0x06001F4A RID: 8010 RVA: 0x001D9CBC File Offset: 0x001D7EBC
		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
			{
				sLeaser.sprites[i] = new FSprite("LizardScaleA" + this.graphic, true);
				sLeaser.sprites[i].scaleY = this.scaleObjects[i - this.startSprite].length / this.graphicHeight;
				sLeaser.sprites[i].anchorY = 0.1f;
				if (this.colored)
				{
					sLeaser.sprites[i + this.scalesPositions.Length] = new FSprite("LizardScaleB" + this.graphic, true);
					sLeaser.sprites[i + this.scalesPositions.Length].scaleY = this.scaleObjects[i - this.startSprite].length / this.graphicHeight;
					sLeaser.sprites[i + this.scalesPositions.Length].anchorY = 0.1f;
				}
			}
		}

		// Token: 0x06001F4B RID: 8011 RVA: 0x001D9DD0 File Offset: 0x001D7FD0
		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
			{
				LizardGraphics.LizardSpineData backPos = base.GetBackPos(i - this.startSprite, timeStacker, true);
				sLeaser.sprites[i].x = backPos.outerPos.x - camPos.x;
				sLeaser.sprites[i].y = backPos.outerPos.y - camPos.y;
				sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(backPos.outerPos, Vector2.Lerp(this.scaleObjects[i - this.startSprite].lastPos, this.scaleObjects[i - this.startSprite].pos, timeStacker));
				sLeaser.sprites[i].scaleX = this.scaleObjects[i - this.startSprite].width * Mathf.Sign(backPos.depthRotation);
				if (this.colored)
				{
					sLeaser.sprites[i + this.scalesPositions.Length].x = backPos.outerPos.x - camPos.x;
					sLeaser.sprites[i + this.scalesPositions.Length].y = backPos.outerPos.y - camPos.y;
					sLeaser.sprites[i + this.scalesPositions.Length].rotation = Custom.AimFromOneVectorToAnother(backPos.outerPos, Vector2.Lerp(this.scaleObjects[i - this.startSprite].lastPos, this.scaleObjects[i - this.startSprite].pos, timeStacker));
					sLeaser.sprites[i + this.scalesPositions.Length].scaleX = this.scaleObjects[i - this.startSprite].width * Mathf.Sign(backPos.depthRotation);
				}
			}
			//if (this.pGraphics.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
			//{
			//	this.ApplyPalette(sLeaser, rCam, this.palette);
			//}
		}

		// Token: 0x06001F4C RID: 8012 RVA: 0x001D9FDC File Offset: 0x001D81DC
		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
			{
				sLeaser.sprites[i].color = this.iGraphics.BodyColor(this.scalesPositions[i - this.startSprite].y);
				if (this.colored)
				{
					//if (this.pGraphics.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
					//{
					//	sLeaser.sprites[i + this.scalesPositions.Length].color = this.pGraphics.HeadColor(1f);
					//}
					//else
					//{
					//	sLeaser.sprites[i + this.scalesPositions.Length].color = this.pGraphics.effectColor;
					//}
					sLeaser.sprites[i + this.scalesPositions.Length].color = this.iGraphics.effectColor;
				}
			}
			base.ApplyPalette(sLeaser, rCam, palette);
		}

		// Token: 0x040021F2 RID: 8690
		public GenericLizardScale[] scaleObjects;

		// Token: 0x040021F3 RID: 8691
		public float[] backwardsFactors;

		// Token: 0x040021F4 RID: 8692
		public new int graphic;

		// Token: 0x040021F5 RID: 8693
		public float graphicHeight;

		// Token: 0x040021F6 RID: 8694
		public float rigor;
	}
}
