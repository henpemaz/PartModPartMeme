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
[assembly: System.Runtime.CompilerServices.SuppressIldasmAttribute()]
namespace ConcealedGarden
{
    public partial class ConcealedGarden : PartialityMod
    {
        public ConcealedGarden()
        {
            this.ModID = "Concealed Garden";
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

                if (!isOptionMenu) return;
                        
                CGAchievementManager.MakeAchievementsOi(this, Tabs[0]);
                Tabs[0].AddItems(new OpLabel(40, 150, "Concealed Garden was brought to you by:", true),
                    new OpLabelLong(new Vector2(40, 10), new Vector2(530, 140),
@"Henpemaz - Lead Dev, most of the stuff in the region unless otherwise noted!
Thalber - Assistant+ Dev, most of the underground rooms, shelter & some more, bug-hunting, productive discussions and psychological support in my DMs :flushed:
Mehri'Kairothep - Playtester, loved the region, helped me figure out what needed polishing.
LB Gamer - Playtester & Dev, helpful and resourceful, also helped with a couple rooms!
Wrayk - Almost playtester, made a connection room (:
DryCryCrystal - Colab, special tileset for LRU room.
Garrakx & Topicular - Makers of the awesome mods that help people make more mods!"
));
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

            CGElectricArcs.Register();

            CGOrganicShelter.Register();

            CGLifeSimProjection.Register();

            CGSongSFX.Register();

            CGCosmeticLeaves.Register();

            CGGateCustomization.Register();

            // Almost sure I ended up not using these
            CGSlipperySlope.Register();

            CGBunkerShelterParts.Register();

            CGQuestionableLizardBits.Apply();

            CGSpawnCustomizations.Apply();

            CGNoLurkArea.Register();

            CGGravityGradient.Register();

            // *sad quack*
            TremblingSeed.SeedHooks.Apply();

            CGProgressionFilter.Register();

            CGLRUPickup.Register();

            CGCameraZoomEffect.Apply();

            CGCutscenes.Apply();

            CGMenuScenes.Apply();

            CGSkyLine.Register();

            CGCosmeticWater.Register();

            CGFourthLayerFix.Apply();

            // CG progression
            CGYellowThoughtsAdaptor.Apply();
            CGLizardBehaviorChange.Apply();

            CGCameraEffects.Apply();

            CGAchievementManager.Apply();

            CGAmbienceFix.Apply();

            CGSlugFilter.Register();

            CGShelterRain.Register();

            CGQuickAndDirtyFix.Register();

            // Screaming into the void
            Debug.Log("CG Fully Loaded");
        }
    }
}
