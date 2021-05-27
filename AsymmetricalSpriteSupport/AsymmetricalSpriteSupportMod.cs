using MonoMod.RuntimeDetour;
using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;
using OptionalUI;
using CompletelyOptional;
using RWCustom;
using Menu;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace AsymmetricalSpriteSupport
{
    public class AsymmetricalSpriteSupport : PartialityMod
    {
        public AsymmetricalSpriteSupport()
        {
            this.ModID = "AsymmetricalSpriteSupport";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static AsymmetricalSpriteSupport instance;

        //public static OptionalUI.OptionInterface LoadOI()
        //{
        //    return new AsymmetricalSpriteSupportOI(instance);
        //}

//        public override void OnEnable()
//        {
//            base.OnEnable();
//            // Hooking code goose hre
//            Type fpg = Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats");
//            new Hook(fpg.GetMethod("ApplyPalette", BindingFlags.Public | BindingFlags.Instance), typeof(OnTopOfTerrainHandFix).GetMethod("PlayerGraphics_ApplyPalette_hk", BindingFlags.NonPublic | BindingFlags.Static));
//            new Hook(fpg.GetMethod("DrawSprites", BindingFlags.Public | BindingFlags.Instance), typeof(OnTopOfTerrainHandFix).GetMethod("PlayerGraphics_DrawSprites_hk", BindingFlags.NonPublic | BindingFlags.Static));
//        }

//        protected static void PlayerGraphics_DrawSprites_hk(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
//        {
//            orig(instance, sLeaser, rCam, timeStacker, camPos);
//            if (instance.player.playerState.slugcatCharacter < 0 || instance.player.playerState.slugcatCharacter > 3) return;
//            if (!AsymmetricalFace[instance.player.playerState.slugcatCharacter]) return;
//            sLeaser.sprites[2].isVisible = false;
//            sLeaser.sprites[4].isVisible = false;

//        }

//        protected static void PlayerGraphics_ApplyPalette_hk(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
//        {
//            orig(instance, sLeaser, rCam, palette);
//            if (instance.player.playerState.slugcatCharacter < 0 || instance.player.playerState.slugcatCharacter > 3) return;
//            if (!AsymmetricalHead[instance.player.playerState.slugcatCharacter]) return;
//            Color color = SlugcatHandsColor(instance.player.playerState.slugcatCharacter);
//            bool flag = instance.malnourished > 0f;
//            if (flag)
//            {
//                float num = (!instance.player.Malnourished) ? Mathf.Max(0f, instance.malnourished - 0.005f) : instance.malnourished;
//                color = Color.Lerp(color, Color.gray, 0.4f * num);
//            }
//            bool flag2 = !instance.player.glowing;
//            if (flag2)
//            {
//                color = Color.Lerp(color, palette.blackColor, Mathf.Clamp01(0.5f - (color.r * 0.3f + color.g * 0.59f + color.b * 0.11f)) * 0.3f);
//                color = Color.Lerp(color, palette.skyColor, Mathf.Clamp01(0.5f - (1f - (color.r * 0.3f + color.g * 0.59f + color.b * 0.11f))) * 0.3f);
//                color = Color.Lerp(color, palette.blackColor, Mathf.Lerp(0.08f, 0.04f, palette.darkness) * Mathf.Clamp01(0.7f - (color.r * 0.3f + color.g * 0.59f + color.b * 0.11f)));
//            }
//            sLeaser.sprites[2].color = color;
//            sLeaser.sprites[4].color = color;
//        }

//        //public static Color[] SlugcatHandsColors = new Color[4];
//        public static bool[] AsymmetricalHead = new bool[4];
//        public static bool[] AsymmetricalFace = new bool[4];

//        private class AsymmetricalSpriteSupportOI : OptionInterface
//        {
//            public AsymmetricalSpriteSupportOI(PartialityMod mod) : base(mod: mod)
//            { }

//            readonly string modDescription =
//@"Toggle support for assymetrical sprites. Leaving this on withouth the flippled sprites being present might cause Sprite-not-found-exceptions.

//When this is active, the game will expect MyHeadSprite#f to be present and use that when it would otherwise use MyHeadSprite# flipped around. Analogously for the face and legs.
//";

//            public override void Initialize()
//            {
//                base.Initialize();
//                Debug.Log("AsymmetricalSpriteSupportOI Initialize");


//                this.Tabs = new OpTab[1];
//                this.Tabs[0] = new OptionalUI.OpTab();
//                CompletelyOptional.GeneratedOI.AddBasicProfile(Tabs[0], rwMod);
//                Tabs[0].AddItems(new OpLabelLong(new Vector2(50f, 470f), new Vector2(500f, 0f), modDescription, alignment: FLabelAlignment.Center, autoWrap: true));

//                for (int i = 0; i < 4; i++)
//                {
//                    Tabs[0].AddItems(new OpLabel(new Vector2(0f, 400f - 50 * i), new Vector2(200, 24f), "Hand color for character " + i, alignment: FLabelAlignment.Right));
//                    Tabs[0].AddItems(new OpCheckBox(new Vector2(210f, 400f - 50 * i), "OnTopOfTerrainHandFixOI_enable_" + i, true) { description = "Enable overwrite player color on OnTopOfTerrainHand sprite for character " + i });
//                    OpTinyColorPicker picker = new OpTinyColorPicker(new Vector2(240f, 400f - 50 * i), "", "OnTopOfTerrainHandFixOI_color_" + i, OpColorPicker.ColorToHex(PlayerGraphics.SlugcatColor(i))) { description = "Color to apply to OnTopOfTerrainHand sprite for character " + i };
//                    picker.AddSelfAndChildrenToTab(Tabs[0]);

//                    Tabs[0].AddItems(new OpLabel(new Vector2(300f, 400f - 50 * i), new Vector2(200, 24f), "Hide the sprite instead", alignment: FLabelAlignment.Right));
//                    Tabs[0].AddItems(new OpCheckBox(new Vector2(510f, 400f - 50 * i), "OnTopOfTerrainHandFixOI_hide_" + i, false) { description = "Completely hide the OnTopOfTerrainHand sprite for character " + i });
//                }
//            }

//            public override void ConfigOnChange()
//            {
//                base.ConfigOnChange();
//                for (int i = 0; i < 4; i++)
//                {
//                    SlugcatHandsColors[i] = OpColorPicker.HexToColor(config["OnTopOfTerrainHandFixOI_color_" + i]);
//                    AsymmetricalHead[i] = bool.Parse(config["OnTopOfTerrainHandFixOI_enable_" + i]);
//                    AsymmetricalFace[i] = bool.Parse(config["OnTopOfTerrainHandFixOI_hide_" + i]);
//                }
//            }
//        }
    }
}