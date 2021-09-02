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
            private Dictionary<string, object> playerProgression;
            private Dictionary<string, object> globalProgression;
            public ConcealedGardenProgression(Dictionary<string, object> ppDict, Dictionary<string, object> gDict) 
            {
                playerProgression = ppDict ?? new Dictionary<string, object>();
                globalProgression = gDict ?? new Dictionary<string, object>();
            }

            public bool transfurred // transformed
            {
                get { if (playerProgression.TryGetValue("transfurred", out object obj)) return (bool)obj; return false; }
                internal set { playerProgression["transfurred"] = value; globalProgression["everBeaten"] = true;}
            }

            public bool fishDream {
                get { if (playerProgression.TryGetValue("fishDream", out object obj)) return (bool)obj; return false; }
                internal set { playerProgression["fishDream"] = value;}
            }

            public bool everBeaten
            {
                get { if (globalProgression.TryGetValue("everBeaten", out object obj)) return (bool)obj; return false; }
                internal set { globalProgression["everBeaten"] = value; SaveGlobalData(); }
            }

            public bool achievementEcho
            {
                get { if (globalProgression.TryGetValue("achievementEcho", out object obj)) return (bool)obj; return false; }
                internal set { globalProgression["achievementEcho"] = value; SaveGlobalData(); }
            }

            public bool achievementTransfurred
            {
                get { if (globalProgression.TryGetValue("achievementTransfurred", out object obj)) return (bool)obj; return false; }
                internal set { globalProgression["achievementTransfurred"] = value; SaveGlobalData(); }
            }

            internal static void LoadProgression()
            {
                object storedPp;
                object storedg;
                Debug.Log("CG Progression loading with:");
                Debug.Log($"persData :{instanceOI.persData}");
                Debug.Log($"data : {instanceOI.data}");

                progression = new ConcealedGardenProgression(
                    (!string.IsNullOrEmpty(instanceOI.persData) && (storedPp = Json.Deserialize(instanceOI.persData)) != null && typeof(Dictionary<string, object>).IsAssignableFrom(storedPp.GetType())) ? (Dictionary<string, object>)storedPp
                    : null,
                    (!string.IsNullOrEmpty(instanceOI.data) && (storedg = Json.Deserialize(instanceOI.data)) != null && typeof(Dictionary<string, object>).IsAssignableFrom(storedg.GetType())) ? (Dictionary<string, object>)storedg
                    : null);
            }
            internal static void SaveProgression()
            {
                SaveGlobalData();
                instanceOI.persData = Json.Serialize(progression.playerProgression);
                Debug.Log("CG Progression saved with:");
                Debug.Log($"persData :{instanceOI.persData}");
                Debug.Log($"data : {instanceOI.data}");
            }

            internal static void SaveGlobalData()
            {
                instanceOI.data = Json.Serialize(progression.globalProgression);
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

            // Screaming into the void
            Debug.Log("CG Fully Loaded");
        }
    }
}
