using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LizardSkin
{
    class FancyPlayerGraphicsCosmeticsAdaptor : PlayerGraphicsCosmeticsAdaptor
    {
        public FancyPlayerGraphicsCosmeticsAdaptor(PlayerGraphics pGraphics) : base(pGraphics){
            
        }


        public static void ApplyHooksToFancyPlayerGraphics()
        {
            //On.FancyPlayerGraphics.ctor += FancyPlayerGraphics_ctor_hk;
            //On.FancyPlayerGraphics.InitiateSprites += FancyPlayerGraphics_InitiateSprites_hk;
            //On.FancyPlayerGraphics.DrawSprites += FancyPlayerGraphics_DrawSprites_hk;
            //On.FancyPlayerGraphics.ApplyPalette += FancyPlayerGraphics_ApplyPalette_hk;
        }

        protected override FNode getOnTopNode(RoomCamera.SpriteLeaser sLeaser)
        {
            /// ??????
            return sLeaser.sprites[6];
        }

        protected override FNode getBehindHeadNode(RoomCamera.SpriteLeaser sLeaser)
        {
            //// ?????????
            return sLeaser.sprites[3];
        }

        protected override FSprite getBehindNode(RoomCamera.SpriteLeaser sLeaser)
        {
            //// ????????
            return sLeaser.sprites[0];
        }

        public override int getFirstSpriteImpl()
        {
            /////////// AAAAAAUUUUGH
            return 12;
        }
    }
}
