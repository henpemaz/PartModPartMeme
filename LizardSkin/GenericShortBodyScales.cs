using RWCustom;
using UnityEngine;

namespace LizardSkin
{
    internal class GenericShortBodyScales : GenericBodyScales
	{
        public GenericShortBodyScales(ICosmeticsAdaptor iGraphics) : base(iGraphics)
		{
			int num = UnityEngine.Random.Range(0, 3);
			//if (iGraphics.lizard.Template.type == CreatureTemplate.Type.GreenLizard && UnityEngine.Random.value < 0.7f)
			//{
			//	num = 2;
			//}
			//else if (iGraphics.lizard.Template.type == CreatureTemplate.Type.BlueLizard && UnityEngine.Random.value < 0.93f)
			//{
			//	num = 1;
			//}
			switch (num)
			{
				case 0:
					base.GeneratePatchPattern(0.1f, UnityEngine.Random.Range(4, 15), 0.9f, 1.2f);
					break;
				case 1:
					base.GenerateTwoLines(0.1f, 1f, 1.5f, 1f);
					break;
				case 2:
					// base.GenerateSegments(0.1f, 0.9f, (iGraphics.lizard.Template.type != CreatureTemplate.Type.PinkLizard) ? 0.6f : 1.5f);
					base.GenerateSegments(0.1f, 0.9f, 1.5f);
					break;
			}
			this.numberOfSprites = this.scalesPositions.Length;
		}

		// Token: 0x06001F45 RID: 8005 RVA: 0x001D985C File Offset: 0x001D7A5C
		public override void Update()
		{
		}

		// Token: 0x06001F46 RID: 8006 RVA: 0x001D9860 File Offset: 0x001D7A60
		public override void InitiateSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam)
		{
			for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
			{
				sLeaser.sprites[i] = new FSprite("pixel", true);
				sLeaser.sprites[i].scaleX = 2f;
				sLeaser.sprites[i].scaleY = 3f;
			}
		}

		// Token: 0x06001F47 RID: 8007 RVA: 0x001D98CC File Offset: 0x001D7ACC
		public override void DrawSprites(LeaserAdaptor sLeaser, CameraAdaptor rCam, float timeStacker, Vector2 camPos)
		{
			for (int i = this.startSprite + this.scalesPositions.Length - 1; i >= this.startSprite; i--)
			{
				SpineData backPos = base.GetBackPos(i - this.startSprite, timeStacker, true);
				sLeaser.sprites[i].x = backPos.outerPos.x - camPos.x;
				sLeaser.sprites[i].y = backPos.outerPos.y - camPos.y;
				sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(backPos.dir, -backPos.dir);
				sLeaser.sprites[i].color = this.iGraphics.HeadColor(timeStacker);
			}
		}
	}
}
