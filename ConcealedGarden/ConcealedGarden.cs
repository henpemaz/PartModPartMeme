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
                instanceOI.SaveData();
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
                internal set { globalProgression["everBeaten"] = value;}
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
                if (progression != null)
                {
                    instanceOI.persData = Json.Serialize(progression.playerProgression);
                    instanceOI.data = Json.Serialize(progression.globalProgression);
                }
                Debug.Log("CG Progression saved with:");
                Debug.Log($"persData :{instanceOI.persData}");
                Debug.Log($"data : {instanceOI.data}");
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

            YellowTalk.Apply();

            // Screaming into the void
            Debug.Log("CG Fully Loaded");
        }
    }
}
