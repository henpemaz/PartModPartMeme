using UnityEngine;
using RWCustom;

namespace LizardSkin
{
    internal class GenericJumpRings : GenericCosmeticTemplate
    {
        public GenericJumpRings(ICosmeticsAdaptor iGraphics) : base(iGraphics)
		{
			this.spritesOverlap = GenericCosmeticTemplate.SpritesOverlap.InFront;
			this.numberOfSprites = 8;
		}

		// Token: 0x06001F6F RID: 8047 RVA: 0x001DD408 File Offset: 0x001DB608
		public int RingSprite(int ring, int side, int part)
		{
			return this.startSprite + part * 4 + side * 2 + ring;
		}

		// Token: 0x06001F70 RID: 8048 RVA: 0x001DD41C File Offset: 0x001DB61C
		public override void Update()
		{
		}

		// Token: 0x06001F71 RID: 8049 RVA: 0x001DD420 File Offset: 0x001DB620
		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					for (int k = 0; k < 2; k++)
					{
						sLeaser.sprites[this.RingSprite(i, j, k)] = new FSprite("Circle20", true);
					}
				}
			}
		}

		// Token: 0x06001F72 RID: 8050 RVA: 0x001DD480 File Offset: 0x001DB680
		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			float to = Mathf.Lerp(this.iGraphics.lastDepthRotation, this.iGraphics.depthRotation, timeStacker);
			float from = Mathf.Lerp(this.iGraphics.lastHeadDepthRotation, this.iGraphics.headDepthRotation, timeStacker);
			//Color color = this.iGraphics.HeadColor(timeStacker);
			Color color = this.iGraphics.effectColor;
			float num = 1f;
			//if (this.iGraphics.lizard.animation == Lizard.Animation.PrepareToJump)
			//{
			//	num = 0.5f + 0.5f * Mathf.InverseLerp((float)this.iGraphics.lizard.timeToRemainInAnimation, 0f, (float)this.iGraphics.lizard.timeInAnimation);
			//	color = Color.Lerp(this.iGraphics.HeadColor(timeStacker), Color.Lerp(Color.white, this.iGraphics.effectColor, num), UnityEngine.Random.value);
			//}
			for (int i = 0; i < 2; i++)
			{
				float s = 0.06f + 0.12f * (float)i;
				LizardGraphics.LizardSpineData lizardSpineData = this.iGraphics.SpinePosition(s, timeStacker);
				Vector2 vector = lizardSpineData.dir;
				Vector2 pos = lizardSpineData.pos;

				/// UUUUUH WHAT IS GOING ONNNNN
				//if (i == 0)
				//{
				//	vector = (vector - Custom.DirVec(Vector2.Lerp(this.iGraphics.drawPositions[0, 1], this.iGraphics.drawPositions[0, 0], timeStacker), Vector2.Lerp(this.iGraphics.head.lastPos, this.iGraphics.head.pos, timeStacker))).normalized;
				//}
				Vector2 a = Custom.PerpendicularVector(vector);
				float num2 = 50f * Mathf.Lerp(from, to, (i != 0) ? 0.5f : 0.25f);
				for (int j = 0; j < 2; j++)
				{
					Vector2 vector2 = Custom.DegToVec(num2 + (((float)j != 0f) ? 40f : -40f));
					Vector2 vector3 = pos + a * lizardSpineData.rad * vector2.x;
					Vector2 vector4 = vector;

					/// FIX THIS IT LOOKS ALL SKEWED
					//if (i == 0)
					//{
					//	vector4 = (vector4 - 2f * Custom.DirVec(vector3, Vector2.Lerp(this.iGraphics.head.lastPos, this.iGraphics.head.pos, timeStacker)) * Mathf.Abs(vector2.y)).normalized;
					//}
					//else
					//{
					//	vector4 = (vector4 + 2f * Custom.DirVec(vector3, Vector2.Lerp(this.iGraphics.tail[0].lastPos, this.iGraphics.tail[0].pos, timeStacker)) * Mathf.Abs(vector2.y)).normalized;
					//}
					sLeaser.sprites[this.RingSprite(i, j, 0)].x = vector3.x - camPos.x;
					sLeaser.sprites[this.RingSprite(i, j, 0)].y = vector3.y - camPos.y;
					sLeaser.sprites[this.RingSprite(i, j, 0)].rotation = Custom.VecToDeg(vector4);
					vector3 = pos + a * (lizardSpineData.rad + 2f * Mathf.Pow(Mathf.Clamp01(Mathf.Abs(vector2.x) * Mathf.Abs(vector2.y)), 0.5f)) * vector2.x;
					vector3 -= vector4 * (1f - num) * 4f;
					sLeaser.sprites[this.RingSprite(i, j, 1)].x = vector3.x - camPos.x;
					sLeaser.sprites[this.RingSprite(i, j, 1)].y = vector3.y - camPos.y;
					sLeaser.sprites[this.RingSprite(i, j, 1)].rotation = Custom.VecToDeg(vector4);
					float t = Mathf.Pow(Mathf.Clamp01(Mathf.Abs(vector2.x)), 2f);
					sLeaser.sprites[this.RingSprite(i, j, 0)].scaleX = ((vector2.y <= 0f) ? 0f : Mathf.Lerp(0.45f, 0f, t));
					sLeaser.sprites[this.RingSprite(i, j, 0)].scaleY = 0.55f;
					sLeaser.sprites[this.RingSprite(i, j, 0)].color = new Color(1f, 0f, 0f);
					sLeaser.sprites[this.RingSprite(i, j, 0)].color = color;
					sLeaser.sprites[this.RingSprite(i, j, 1)].scaleX = ((vector2.y <= 0f) ? 0f : (Mathf.Lerp(0.27f, 0f, t) * num));
					sLeaser.sprites[this.RingSprite(i, j, 1)].scaleY = 0.33f * num;
				}
			}
		}

		// Token: 0x06001F73 RID: 8051 RVA: 0x001DD9CC File Offset: 0x001DBBCC
		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 2; j++)
				{
					sLeaser.sprites[this.RingSprite(i, j, 1)].color = palette.blackColor;
				}
			}
		}
	}
}
