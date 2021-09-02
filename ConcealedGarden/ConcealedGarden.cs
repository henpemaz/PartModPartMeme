using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedPlacedObjects;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using OptionalUI;
using UnityEngine;
using Menu;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ConcealedGarden
{
    public partial class ConcealedGarden : PartialityMod
    {
        public ConcealedGarden()
        {
            this.ModID = "ConcealedGarden";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static ConcealedGarden instance;
        public static ConcealedGardenOI instanceOI;
        public static ConcealedGardenProgression progression;
        public static OptionalUI.OptionInterface LoadOI()
        {
            return new ConcealedGardenOI();
        }

        public class ConcealedGardenOI : OptionalUI.OptionInterface
        {
            public ConcealedGardenOI() : base(mod:instance)
            {
                instanceOI = this;
                hasProgData = true;
            }

            public override void Initialize()
            {
                base.Initialize();
                this.Tabs = new OpTab[1] { new OpTab() };
                CompletelyOptional.GeneratedOI.AddBasicProfile(Tabs[0], rwMod);

               MakeAchievementsOi(Tabs[0]);
            }

            private void MakeAchievementsOi(OpTab opTab)
            {
                AchievementEntry ae = new AchievementEntry(new Vector2(100, 100), "Achievement (:");
                opTab.AddItems(ae);
            }

            private class AchievementEntry : UIelement
            {
                private FSprite bg;
                private MenuLabel label;

                public AchievementEntry(Vector2 pos, string testText) : base(pos, new Vector2(400, 60))
                {
                    if (!_init) { return; }
                    this.bg = new FSprite("Futile_White", true)
                    {
                        color = new Color(0.1f, 0.1f, 0.4f),
                        alpha = 0.25f,
                        width = 384f,
                        height = 44f,
                        anchorX = 0f, anchorY = 0f,
                        x = 8f, y = 8f,
                    };
                    this.myContainer.AddChild(this.bg);
                    this.label = new Menu.MenuLabel(menu, owner, testText, this.pos, this.size, false);
                    //this.label.label.color = this.color;
                    this.subObjects.Add(this.label);
                    OnChange();
                }
                public override void Hide()
                {
                    base.Hide();
                    this.bg.isVisible = false;
                    this.label.label.isVisible = false;
                }
                public override void Show()
                {
                    base.Show();
                    this.bg.isVisible = true;
                    this.label.label.isVisible = true;
                }
                public override void Unload()
                {
                    base.Unload();
                    this.bg.RemoveFromContainer();
                }
            }

            protected override void ProgressionLoaded()
            {
                base.ProgressionLoaded();
                LoadData();
                ConcealedGardenProgression.LoadProgression();
                LizardSkin.LizardSkin.SetCGEverBeaten(progression.everBeaten);
                LizardSkin.LizardSkin.SetCGStoryProgression(progression.transfurred ? 1 : 0);
            }

            protected override void ProgressionPreSave()
            {
                ConcealedGardenProgression.SaveProgression();
                base.ProgressionPreSave();
            }
        }

        public class ConcealedGardenProgression
        {
            private Dictionary<string, object> saveData;
            private Dictionary<string, object> persData;
            private Dictionary<string, object> miscData;
            private Dictionary<string, object> globalData;
            
            public ConcealedGardenProgression(Dictionary<string, object> saveData = null, Dictionary<string, object> persData = null, Dictionary<string, object> miscData = null, Dictionary<string, object> globalData = null)
            {
                saveData = saveData ?? ((!string.IsNullOrEmpty(instanceOI.saveData) && Json.Deserialize(instanceOI.saveData) is Dictionary<string, object> storedSd) ? storedSd : new Dictionary<string, object>());
                persData = persData ?? ((!string.IsNullOrEmpty(instanceOI.persData) && Json.Deserialize(instanceOI.persData) is Dictionary<string, object> storedPd) ? storedPd : new Dictionary<string, object>());
                miscData = miscData ?? ((!string.IsNullOrEmpty(instanceOI.miscData) && Json.Deserialize(instanceOI.miscData) is Dictionary<string, object> storedMd) ? storedMd : new Dictionary<string, object>());
                globalData = globalData ?? ((!string.IsNullOrEmpty(instanceOI.data) && Json.Deserialize(instanceOI.data) is Dictionary<string, object> storedData) ? storedData : new Dictionary<string, object>());
                this.saveData = saveData;
                this.persData = persData;
                this.miscData = miscData;
                this.globalData = globalData;
            }

            public bool transfurred // transformed
            {
                get { if (persData.TryGetValue("transfurred", out object obj)) return (bool)obj; return false; }
                internal set { persData["transfurred"] = value; everBeaten = true;}
            }

            public bool fishDream {
                get { if (persData.TryGetValue("fishDream", out object obj)) return (bool)obj; return false; }
                internal set { persData["fishDream"] = value;}
            }

            public bool everBeaten
            {
                get { if (globalData.TryGetValue("everBeaten", out object obj)) return (bool)obj; return false; }
                internal set { globalData["everBeaten"] = value; SaveGlobalData(); }
            }

            public bool achievementEcho
            {
                get { if (globalData.TryGetValue("achievementEcho", out object obj)) return (bool)obj; return false; }
                internal set { globalData["achievementEcho"] = value; SaveGlobalData(); }
            }

            public bool achievementTransfurred
            {
                get { if (globalData.TryGetValue("achievementTransfurred", out object obj)) return (bool)obj; return false; }
                internal set { globalData["achievementTransfurred"] = value; SaveGlobalData(); }
            }

            public long savestateFood {
                get { if (saveData.TryGetValue("savestateFood", out object obj)) return (long)obj; return 0; }
                internal set { saveData["savestateFood"] = value; }
            }
            public long deathpersFood {
                get { if (persData.TryGetValue("deathpersFood", out object obj)) return (long)obj; return 0; }
                internal set { persData["deathpersFood"] = value; }
            }
            public long progressionFood {
                get { if (miscData.TryGetValue("progressionFood", out object obj)) return (long)obj; return 0; }
                internal set { miscData["progressionFood"] = value; }
            }

            internal static void LoadProgression()
            {
                Debug.Log("CG Progression loading with:");
                Debug.Log($"saveData :{instanceOI.saveData}");
                Debug.Log($"persData :{instanceOI.persData}");
                Debug.Log($"miscData :{instanceOI.miscData}");
                Debug.Log($"data : {instanceOI.data}");

                progression = new ConcealedGardenProgression();
            }
            internal static void SaveProgression()
            {
                SaveGlobalData();
                instanceOI.saveData = Json.Serialize(progression.saveData);
                instanceOI.persData = Json.Serialize(progression.persData);
                instanceOI.miscData = Json.Serialize(progression.miscData);
                Debug.Log("CG Progression saved with:");
                Debug.Log($"saveData :{instanceOI.saveData}");
                Debug.Log($"persData :{instanceOI.persData}");
                Debug.Log($"miscData :{instanceOI.miscData}");
                Debug.Log($"data : {instanceOI.data}");
            }

            internal static void SaveGlobalData()
            {
                instanceOI.data = Json.Serialize(progression.globalData);
                instanceOI.SaveData();
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            Debug.Log("CG Loading Start");

            //throw new Exception("modding");

            LizardSkin.LizardSkin.SetCGStoryProgression(0); // CG Progression Mode

            // Hooking code goose hre

            ElectricArcs.Register();

            OrganicShelter.Register();

            LifeSimProjection.Register();

            SongSFX.Register();

            CosmeticLeaves.Register();

            CGGateFix.Register();

            SlipperySlope.Register();

            BunkerShelterParts.Register();

            QuestionableLizardBit.Apply();

            SpawnCustomizations.Apply();

            NoLurkArea.Register();

            GravityGradient.Register();

            //quack
            TremblingSeed.SeedHooks.Apply();

            ProgressionFilter.Register();

            LRUPickup.Register();

            CameraZoomEffect.Apply();

            CGCutscenes.Apply();

            CGMenuScenes.Apply();

            CGSkyLine.Register();

            CGCosmeticWater.Register();

            FourthLayerFix.Apply();

            // CG progression
            YellowThoughtsAdaptor.Apply();
            LizardBehaviorChange.Apply();

            CGCameraEffects.Apply();

            CGAchievementManager.Apply();


            FoodCounterTest.Apply();

            // Screaming into the void
            Debug.Log("CG Fully Loaded");
        }

        static class FoodCounterTest
        {
            public static void Apply()
            {
                On.PlayerGraphics.ctor += PlayerGraphics_ctor;
                On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
                On.Player.AddFood += Player_AddFood;
            }

            private static void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
            {
                orig(self, add);
                progression.savestateFood += add;
                progression.deathpersFood += add;
                progression.progressionFood += add;
            }

            private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                orig(self, sLeaser, rCam, timeStacker, camPos);
                self.DEBUGLABELS[0].label.text = "Savestate Food:" + progression.savestateFood;
                self.DEBUGLABELS[1].label.text = "Deathpers Food:" + progression.deathpersFood;
                self.DEBUGLABELS[2].label.text = "Misc Food:" + progression.progressionFood;
            }

            private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
            {
                orig(self, ow);
                self.DEBUGLABELS = new DebugLabel[3];
                self.DEBUGLABELS[0] = new DebugLabel(ow, new Vector2(10f, 20f));
                self.DEBUGLABELS[1] = new DebugLabel(ow, new Vector2(10f, 10f));
                self.DEBUGLABELS[2] = new DebugLabel(ow, new Vector2(10f, 0f));
            }
        }
    }
}
