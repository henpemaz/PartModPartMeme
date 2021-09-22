using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoMod.RuntimeDetour;
using System.Text;
using UnityEngine;

namespace LizardSkin
{
    class FancyPlayerGraphicsCosmeticsAdaptor : PlayerGraphicsCosmeticsAdaptor
    {
        Type fpg_ref;
        public FancyPlayerGraphicsCosmeticsAdaptor(PlayerGraphics pGraphics) : base(pGraphics){
            fpg_ref = Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats");
        }


        public static void ApplyHooksToFancyPlayerGraphics()
        {
            Type fpg = Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats");

            new Hook(fpg.GetConstructor(new Type[] { typeof(PhysicalObject) }), typeof(FancyPlayerGraphicsCosmeticsAdaptor).GetMethod("FancyPlayerGraphics_ctor_hk", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            new Hook(fpg.GetMethod("InitiateSprites", BindingFlags.Public | BindingFlags.Instance), typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("PlayerGraphics_InitiateSprites_hk", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            new Hook(fpg.GetMethod("AddToContainer", BindingFlags.Public | BindingFlags.Instance), typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("PlayerGraphics_AddToContainer_hk", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            new Hook(fpg.GetMethod("DrawSprites", BindingFlags.Public | BindingFlags.Instance), typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("PlayerGraphics_DrawSprites_hk", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            new Hook(fpg.GetMethod("ApplyPalette", BindingFlags.Public | BindingFlags.Instance), typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("PlayerGraphics_ApplyPalette_hk", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            
            //MethodInfo fpg_ = fpg.GetMethod("InitiateSprites", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo fpg_hk = typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("PlayerGraphics_InitiateSprites_hk", BindingFlags.NonPublic |  BindingFlags.Static);

            //if (fpg_ == null)
            //{
            //    Debug.LogError("fpg_ NULL");
            //}
            //if (fpg_hk == null)
            //{
            //    Debug.LogError("fpg_hk NULL");
            //}

            //Hook hook = new Hook(fpg_, fpg_hk);

            //On.FancyPlayerGraphics.InitiateSprites += FancyPlayerGraphics_InitiateSprites_hk;
            //On.FancyPlayerGraphics.DrawSprites += FancyPlayerGraphics_DrawSprites_hk;
            //On.FancyPlayerGraphics.ApplyPalette += FancyPlayerGraphics_ApplyPalette_hk;
        }

        // static void FancyPlayerGraphics_ctor_hk(Action<PlayerGraphics, PhysicalObject> orig, PlayerGraphics instance, PhysicalObject ow)
        protected static void FancyPlayerGraphics_ctor_hk(On.PlayerGraphics.orig_ctor orig, PlayerGraphics instance, PhysicalObject ow)
        {
            // Debug.LogError("FancyPlayerGraphics HOOK WAS CALLED");
            orig(instance, ow);
            FancyPlayerGraphicsCosmeticsAdaptor adaptor = new FancyPlayerGraphicsCosmeticsAdaptor(instance);
            System.Array.Resize(ref instance.bodyParts, instance.bodyParts.Length + 1);
            instance.bodyParts[instance.bodyParts.Length - 1] = adaptor;
            AddAdaptor(adaptor);
            //addAdaptor(new FancyPlayerGraphicsCosmeticsAdaptor(instance));
        }

        protected override FNode getOnTopNode(RoomCamera.SpriteLeaser sLeaser)
        {
            return sLeaser.sprites[(int)fpg_ref.GetField("totalSprites", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this.pGraphics) -1];
        }

        protected override FNode getBehindHeadNode(RoomCamera.SpriteLeaser sLeaser)
        {
            return sLeaser.sprites[(int)fpg_ref.GetField("firstHeadSprite", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this.pGraphics)];
        }

        protected override FNode getBehindNode(RoomCamera.SpriteLeaser sLeaser)
        {
            return sLeaser.sprites[(int)fpg_ref.GetField("firstTailSprite", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this.pGraphics)];
        }

        public override Color BaseBodyColor()
        {
            // Nope, that's not how it works, Fancy will not use jolly's colors
            // Color color = base.BaseBodyColor(y);
            Color color = PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);
            if (pGraphics.malnourished > 0f)
            {
                float num = (!pGraphics.player.Malnourished) ? Mathf.Max(0f, pGraphics.malnourished - 0.005f) : pGraphics.malnourished;
                color = Color.Lerp(color, Color.gray, 0.4f * num);
            }
            bool flag2 = !pGraphics.player.glowing;
            if (flag2)
            {
                color = Color.Lerp(color, palette.blackColor, Mathf.Clamp01(0.5f - (color.r * 0.3f + color.g * 0.59f + color.b * 0.11f)) * 0.3f);
                color = Color.Lerp(color, palette.skyColor, Mathf.Clamp01(0.5f - (1f - (color.r * 0.3f + color.g * 0.59f + color.b * 0.11f))) * 0.3f);
                color = Color.Lerp(color, palette.blackColor, Mathf.Lerp(0.08f, 0.04f, palette.darkness) * Mathf.Clamp01(0.7f - (color.r * 0.3f + color.g * 0.59f + color.b * 0.11f)));
            }

            return color;
        }
    }
}
