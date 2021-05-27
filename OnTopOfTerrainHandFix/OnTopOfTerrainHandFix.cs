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


namespace OnTopOfTerrainHandFix
{
    public class OnTopOfTerrainHandFix : PartialityMod
    {
        public OnTopOfTerrainHandFix()
        {
            this.ModID = "OnTopOfTerrainHandFix";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static OnTopOfTerrainHandFix instance;

        public static OptionalUI.OptionInterface LoadOI()
        {
            return new OnTopOfTerrainHandFixOI(instance);
        }

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre
            Type fpg = Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats");
            new Hook(fpg.GetMethod("ApplyPalette", BindingFlags.Public | BindingFlags.Instance), typeof(OnTopOfTerrainHandFix).GetMethod("PlayerGraphics_ApplyPalette_hk", BindingFlags.NonPublic | BindingFlags.Static));
            new Hook(fpg.GetMethod("DrawSprites", BindingFlags.Public | BindingFlags.Instance), typeof(OnTopOfTerrainHandFix).GetMethod("PlayerGraphics_DrawSprites_hk", BindingFlags.NonPublic | BindingFlags.Static));
        }

        protected static void PlayerGraphics_DrawSprites_hk(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(instance, sLeaser, rCam, timeStacker, camPos);
            if (instance.player.playerState.slugcatCharacter < 0 || instance.player.playerState.slugcatCharacter > 3) return;
            if (!SlugcatHandsHide[instance.player.playerState.slugcatCharacter]) return;
            sLeaser.sprites[2].isVisible = false;
            sLeaser.sprites[4].isVisible = false;

        }

        protected static void PlayerGraphics_ApplyPalette_hk(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(instance, sLeaser, rCam, palette);
            if (instance.player.playerState.slugcatCharacter < 0 || instance.player.playerState.slugcatCharacter > 3) return;
            if (!SlugcatHandsOverwrite[instance.player.playerState.slugcatCharacter]) return;
            Color color = SlugcatHandsColor(instance.player.playerState.slugcatCharacter);
            bool flag = instance.malnourished > 0f;
            if (flag)
            {
                float num = (!instance.player.Malnourished) ? Mathf.Max(0f, instance.malnourished - 0.005f) : instance.malnourished;
                color = Color.Lerp(color, Color.gray, 0.4f * num);
            }
            bool flag2 = !instance.player.glowing;
            if (flag2)
            {
                color = Color.Lerp(color, palette.blackColor, Mathf.Clamp01(0.5f - (color.r * 0.3f + color.g * 0.59f + color.b * 0.11f)) * 0.3f);
                color = Color.Lerp(color, palette.skyColor, Mathf.Clamp01(0.5f - (1f - (color.r * 0.3f + color.g * 0.59f + color.b * 0.11f))) * 0.3f);
                color = Color.Lerp(color, palette.blackColor, Mathf.Lerp(0.08f, 0.04f, palette.darkness) * Mathf.Clamp01(0.7f - (color.r * 0.3f + color.g * 0.59f + color.b * 0.11f)));
            }
            sLeaser.sprites[2].color = color;
            sLeaser.sprites[4].color = color;
        }

        public static Color[] SlugcatHandsColors = new Color[4];
        public static bool[] SlugcatHandsOverwrite = new bool[4];
        public static bool[] SlugcatHandsHide = new bool[4];

        private static Color SlugcatHandsColor(int slugcatCharacter)
        {
            return SlugcatHandsColors[slugcatCharacter];
        }

        private class OnTopOfTerrainHandFixOI : OptionInterface
        {
            public OnTopOfTerrainHandFixOI(PartialityMod mod) : base(mod: mod)
            { }

            const string modDescription =
@"Because its just really annoying";

            public override void Initialize()
            {
                base.Initialize();
                Debug.Log("OnTopOfTerrainHandFixOI Initialize");


                this.Tabs = new OpTab[1];
                this.Tabs[0] = new OptionalUI.OpTab();
                CompletelyOptional.GeneratedOI.AddBasicProfile(Tabs[0], rwMod);
                Tabs[0].AddItems(new OpLabelLong(new Vector2(50f, 470f), new Vector2(500f, 0f), modDescription, alignment: FLabelAlignment.Center, autoWrap: true));

                for (int i = 0; i < 4; i++)
                {
                    Tabs[0].AddItems(new OpLabel(new Vector2(0f, 400f - 50*i), new Vector2(200, 24f), "Hand color for character " + i, alignment: FLabelAlignment.Right));
                    Tabs[0].AddItems(new OpCheckBox(new Vector2(210f, 400f - 50 * i), "OnTopOfTerrainHandFixOI_enable_" + i, true) {description= "Enable overwrite player color on OnTopOfTerrainHand sprite for character " + i });
                    OpTinyColorPicker picker = new OpTinyColorPicker(new Vector2(240f, 400f - 50 * i), "", "OnTopOfTerrainHandFixOI_color_" + i, OpColorPicker.ColorToHex(PlayerGraphics.SlugcatColor(i))) { description = "Color to apply to OnTopOfTerrainHand sprite for character " + i };
                    picker.AddSelfAndChildrenToTab(Tabs[0]);

                    Tabs[0].AddItems(new OpLabel(new Vector2(300f, 400f - 50 * i), new Vector2(200, 24f), "Hide the sprite instead", alignment: FLabelAlignment.Right));
                    Tabs[0].AddItems(new OpCheckBox(new Vector2(510f, 400f - 50 * i), "OnTopOfTerrainHandFixOI_hide_" + i, false) { description = "Completely hide the OnTopOfTerrainHand sprite for character " + i});
                }
            }

            public override void ConfigOnChange()
            {
                base.ConfigOnChange();
                for (int i = 0; i < 4; i++)
                {
                    SlugcatHandsColors[i] = OpColorPicker.HexToColor(config["OnTopOfTerrainHandFixOI_color_" + i]);
                    SlugcatHandsOverwrite[i] = bool.Parse(config["OnTopOfTerrainHandFixOI_enable_" + i]);
                    SlugcatHandsHide[i] = bool.Parse(config["OnTopOfTerrainHandFixOI_hide_" + i]);
                }
            }

            internal class OpTinyColorPicker : OpSimpleButton
            {
                private OpColorPicker colorPicker;
                private bool currentlyPicking;
                const float mouseTimeout = 0.5f;
                float mouseOutCounter;

                public OpTinyColorPicker(Vector2 pos, string signal, string key, string defaultHex) : base(pos, new Vector2(24, 24), signal)
                {
                    this.colorPicker = new OpColorPicker(pos + new Vector2(-60, 24), key, defaultHex);
                    this.currentlyPicking = false;

                    if (!_init) return;

                    this.colorFill = colorPicker.valueColor;
                    this.rect.fillAlpha = 1f;
                }

                public void AddSelfAndChildrenToTab(OpTab tab)
                {
                    tab.AddItems(this,
                        colorPicker);
                    if (!_init) return;
                    this.colorPicker.Hide();
                }

                public void AddSelfAndChildrenToScroll(OpScrollBox scroll)
                {
                    //scroll.AddItems(this,
                    //    colorPicker);
                    scroll.AddItems(this);
                    this.tab.AddItems(colorPicker);
                    if (!_init) return;
                    this.colorPicker.Hide();
                }

                public void DestroySelfAndChildren()
                {
                    OpTab.DestroyItems(colorPicker, this);
                }

                public override void Signal()
                {
                    // base.Signal();
                    if (!currentlyPicking)
                    {
                        this.colorPicker.pos = (this.inScrollBox ? (this.GetPos() + scrollBox.GetPos()) : this.GetPos()) + new Vector2(-60, 24);
                        colorPicker.Show();
                        currentlyPicking = true;
                    }
                    else
                    {
                        currentlyPicking = false;
                        colorFill = colorPicker.valueColor;
                        colorPicker.Hide();
                        //if (!string.IsNullOrEmpty(signal)) base.Signal(); // uses eventful now, has to trigger event
                        base.Signal();
                    }
                }

                public Color valuecolor => colorPicker.valueColor;

                //public event OnFrozenUpdateHandler OnFrozenUpdate;

                public override void Update(float dt)
                {
                    // we do a little tricking
                    //if (currentlyPicking && !this.MouseOver) this.held = false;
                    base.Update(dt);
                    if (currentlyPicking && !this.MouseOver)
                    {
                        colorPicker.Update(dt);
                        this.held = true;
                    }

                    if (currentlyPicking && !this.MouseOver && !colorPicker.MouseOver)
                    {
                        mouseOutCounter += dt;
                    }
                    else
                    {
                        mouseOutCounter = 0f;
                    }
                    if (mouseOutCounter >= mouseTimeout)
                    {
                        Signal();
                    }
                }

                public override void GrafUpdate(float dt)
                {
                    base.GrafUpdate(dt);
                    this.colorFill = colorPicker.valueColor;
                    rect.fillAlpha = 1f;
                }

                public override void Show()
                {
                    base.Show();
                    colorPicker.Hide();
                }
            }
        }
    }
}
