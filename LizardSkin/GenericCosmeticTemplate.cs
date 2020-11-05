using UnityEngine;

namespace LizardSkin
{
    public abstract partial class GenericCosmeticTemplate
	{
		// Token: 0x06001F2A RID: 7978 RVA: 0x001D869C File Offset: 0x001D689C
		public GenericCosmeticTemplate(ICosmeticsAdaptor iGraphics)
		{
			this.iGraphics = iGraphics;
		}

		// Token: 0x06001F2B RID: 7979 RVA: 0x001D86B4 File Offset: 0x001D68B4
		public virtual void Update()
		{
		}

		// Token: 0x06001F2C RID: 7980 RVA: 0x001D86B8 File Offset: 0x001D68B8
		public virtual void Reset()
		{
		}

		// Token: 0x06001F2D RID: 7981 RVA: 0x001D86BC File Offset: 0x001D68BC
		public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
		}

		// Token: 0x06001F2E RID: 7982 RVA: 0x001D86C0 File Offset: 0x001D68C0
		public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
		}

		// Token: 0x06001F2F RID: 7983 RVA: 0x001D86C4 File Offset: 0x001D68C4
		public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			this.palette = palette;
		}

		// Token: 0x06001F30 RID: 7984 RVA: 0x001D86D0 File Offset: 0x001D68D0
		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			for (int i = this.startSprite; i < this.startSprite + this.numberOfSprites; i++)
			{
				if(i < sLeaser.sprites.Length)
                {
					newContatiner.AddChild(sLeaser.sprites[i]);
                }
			}
		}

		// Token: 0x040021D7 RID: 8663
		public ICosmeticsAdaptor iGraphics;

		// Token: 0x040021D8 RID: 8664
		public int numberOfSprites;

		// Token: 0x040021D9 RID: 8665
		public int startSprite { get => this.iGraphics.firstSprite + this._startSprite; set => this._startSprite = value; }

		public int _startSprite;

		// Token: 0x040021DA RID: 8666
		public RoomPalette palette;

		// Token: 0x040021DB RID: 8667
		public GenericCosmeticTemplate.SpritesOverlap spritesOverlap;

		// Token: 0x020004BB RID: 1211
		public enum SpritesOverlap
		{
			// Token: 0x040021DD RID: 8669
			Behind,
			// Token: 0x040021DE RID: 8670
			BehindHead,
			// Token: 0x040021DF RID: 8671
			InFront
		}
	}
}