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
                progressData = true;
            }

            public override void Initialize()
            {
                base.Initialize();
                this.Tabs = new OpTab[1] { new OpTab() };
                CompletelyOptional.GeneratedOI.AddBasicProfile(Tabs[0], rwMod);
                LoadData();
            }

            public override void DataOnChange()
            {
                base.DataOnChange();
                ConcealedGardenProgression.LoadProgression();
                LizardSkin.LizardSkin.SetCGEverBeaten(progression.everBeaten);
                LizardSkin.LizardSkin.SetCGStoryProgression(progression.transfurred ? 1 : 0);
            }
        }

        public class ConcealedGardenProgression
        {
            private Dictionary<string, object> playerProgression;
            private Dictionary<string, object> miscProgression;
            public ConcealedGardenProgression(Dictionary<string, object> ppDict, Dictionary<string, object> miscDict) 
            {
                playerProgression = ppDict ?? new Dictionary<string, object>();
                miscProgression = miscDict ?? new Dictionary<string, object>();
            }

            public bool transfurred // transformed
            {
                get { if (playerProgression.TryGetValue("transfurred", out object obj)) return (bool)obj; return false; }
                internal set { playerProgression["transfurred"] = value; miscProgression["everBeaten"] = true; SaveProgression(); }
            }

            public bool fishDream {
                get { if (playerProgression.TryGetValue("fishDream", out object obj)) return (bool)obj; return false; }
                internal set { playerProgression["fishDream"] = value; SaveProgression(); }
            }

            public bool everBeaten
            {
                get { if (miscProgression.TryGetValue("everBeaten", out object obj)) return (bool)obj; return false; }
                internal set { miscProgression["everBeaten"] = value; SaveProgression(); }
            }

            internal static void LoadProgression()
            {
                object storedPp;
                object storedMisc;
                progression = new ConcealedGardenProgression(
                    (!string.IsNullOrEmpty(instanceOI.data) && (storedPp = Json.Deserialize(instanceOI.data)) != null && typeof(Dictionary<string, object>).IsAssignableFrom(storedPp.GetType())) ? (Dictionary<string, object>)storedPp
                    : null,
                    (!string.IsNullOrEmpty(instanceOI.miscdata) && (storedMisc = Json.Deserialize(instanceOI.miscdata)) != null && typeof(Dictionary<string, object>).IsAssignableFrom(storedMisc.GetType())) ? (Dictionary<string, object>)storedMisc
                    : null);
                
            }
            internal static void SaveProgression()
            {
                if (progression != null)
                {
                    var pp = progression.playerProgression;
                    var misc = progression.miscProgression;
                    // Causes CM to call OnDataChange which we use to read progression :/
                    // funny bug that wasted me an hour and some pizza
                    instanceOI.data = Json.Serialize(pp);
                    instanceOI.miscdata = Json.Serialize(misc);
                }
                else
                {
                    instanceOI.data = instanceOI.defaultData;
                    instanceOI.miscdata = instanceOI.defaultMiscData;
                }
                instanceOI.SaveData();
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

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

            ShaderTester.Register();

            NoLurkArea.Register();

            GravityGradient.Register();

            //quack
            TremblingSeed.SeedHooks.Apply();

            ProgressionFilter.Register();

            LRUPickup.Register();

            CameraZoomEffect.Apply();

            CGCutscenes.Apply();

            //On.Rock.ApplyPalette += Rock_ApplyPalette;
            //On.RainWorld.Start += RainWorld_Start;
        }

        //private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        //{
        //    orig(self);

        //    self.processManager.RequestMainProcessSwitch(ProcessManager.ProcessID.ConsoleOptionsMenu);
        //}

        //private void Rock_ApplyPalette(On.Rock.orig_ApplyPalette orig, Rock self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        //{
        //    orig(self, sLeaser, rCam, palette);
        //    self.color = UnityEngine.Color.white;
        //    sLeaser.sprites[0].color = UnityEngine.Color.white;
        //    sLeaser.sprites[0].element = Futile.atlasManager.GetElementWithName(UnityEngine.Random.value > 0.5f ? "pebble69" : "borget");
        //}

        public class ShaderTester : CosmeticSprite
        {
            public enum Shader { Basic, LevelColor, Background, WaterSurface, DeepWater, Shortcuts, DeathRain, LizardLaser, WaterLight, WaterFall, ShockWave, Smoke, Spores, Steam, ColoredSprite, ColoredSprite2, LightSource, LightBloom, SkyBloom, Adrenaline, CicadaWing, BulletRain, CustomDepth, UnderWaterLight, FlatLight, FlatLightBehindTerrain, VectorCircle, VectorCircleFadable, FlareBomb, Fog, WaterSplash, EelFin, EelBody, JaggedCircle, JaggedSquare, TubeWorm, LizardAntenna, TentaclePlant, LevelMelt, LevelMelt2, CoralCircuit, DeadCoralCircuit, CoralNeuron, Bloom, GravityDisruptor, GlyphProjection, BlackGoo, Map, MapAerial, MapShortcut, LightAndSkyBloom, SceneBlur, EdgeFade, HeatDistortion, Projection, SingleGlyph, DeepProcessing, Cloud, CloudDistant, DistantBkgObject, BkgFloor, House, DistantBkgObjectRepeatHorizontal, Dust, RoomTransition, VoidCeiling, FlatLightNoisy, VoidWormBody, VoidWormFin, VoidWormPincher, FlatWaterLight, WormLayerFade, OverseerZip, GhostSkin, GhostDistortion, GateHologram, OutPostAntler, WaterNut, Hologram, FireSmoke, HoldButtonCircle, GoldenGlow, ElectricDeath, VoidSpawnBody, SceneLighten, SceneBlurLightEdges, SceneRain, SceneOverlay, SceneSoftLight, HologramImage, HologramBehindTerrain, Decal, SpecificDepth, LocalBloom, MenuText, DeathFall, KingTusk, HoloGrid, SootMark, NewVultureSmoke, SmokeTrail, RedsIllness, HazerHaze, Rainbow, LightBeam }
            public enum Container { Shadows, BackgroundShortcuts, Background, Midground, Items, Foreground, ForegroundLights, Shortcuts, Water, GrabShaders, Bloom, HUD, HUD2 }

            private readonly PlacedObject pObj;
            PlacedObjectsManager.ManagedData data => pObj.data as PlacedObjectsManager.ManagedData;

            public ShaderTester(Room room, PlacedObject pObj)
            {
                this.room = room;
                this.pObj = pObj;
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                base.InitiateSprites(sLeaser, rCam);
                sLeaser.sprites = new FSprite[1] { new FSprite("Futile_White", true) };
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                rCam.ReturnFContainer(data.GetValue<Container>("container").ToString())
                    .AddChildAtIndex(sLeaser.sprites[0],
                        UnityEngine.Mathf.FloorToInt(data.GetValue<float>("depth") * rCam.ReturnFContainer(data.GetValue<Container>("container").ToString()).GetChildCount()));
                try
                {
                    sLeaser.sprites[0].SetElementByName(data.GetValue<string>("sprite"));
                }
                catch { }
                sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders[data.GetValue<Shader>("shader").ToString()];
                sLeaser.sprites[0].color = data.GetValue<UnityEngine.Color>("color");
                sLeaser.sprites[0].alpha = data.GetValue<float>("alpha");
                sLeaser.sprites[0].scale = data.GetValue<float>("scale");

                sLeaser.sprites[0].SetPosition(pObj.pos - camPos);
            }

            public static void Register()
            {
                PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]
                {
                    new PlacedObjectsManager.StringField("sprite", "Futile_White"),
                    new PlacedObjectsManager.FloatField("scale", 0.1f, 20f, 1f, 0.1f),
                    new PlacedObjectsManager.EnumField("shader", typeof(Shader), Shader.Basic),
                    new PlacedObjectsManager.EnumField("container", typeof(Container), Container.Shadows),
                    new PlacedObjectsManager.FloatField("depth", 0f, 1f, 0f, 0.01f),
                    new PlacedObjectsManager.ColorField("color", UnityEngine.Color.white, controlType: PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider),
                    new PlacedObjectsManager.FloatField("alpha", 0f, 1f, 0f, 0.01f),
                }, typeof(ShaderTester), "ShaderTester");
            }
        }
    }
}
