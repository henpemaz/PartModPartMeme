namespace LizardSkin
{
	public class GenericLizardScale : BodyPart
	{
		// Token: 0x06001FCE RID: 8142 RVA: 0x001EAA18 File Offset: 0x001E8C18
		public GenericLizardScale(GenericCosmeticsTemplate gCosmetics) : base(gCosmetics.iGraphics.graphics)
		{
			this.iCosmetics = gCosmetics;
		}

		// Token: 0x06001FCF RID: 8143 RVA: 0x001EAA30 File Offset: 0x001E8C30
		public override void Update()
		{
			base.Update();
			if (this.owner.owner.room.PointSubmerged(this.pos))
			{
				this.vel *= 0.5f;
			}
			else
			{
				this.vel *= 0.9f;
			}
			this.lastPos = this.pos;
			this.pos += this.vel;
		}

		// Token: 0x0400229C RID: 8860
		public GenericCosmeticsTemplate iCosmetics;

		// Token: 0x0400229D RID: 8861
		public float length;

		// Token: 0x0400229E RID: 8862
		public float width;
	}
}