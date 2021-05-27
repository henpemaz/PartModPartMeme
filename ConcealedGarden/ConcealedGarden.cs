using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedPlacedObjects;
using System.Security;
using System.Security.Permissions;
using System.Reflection;

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

        public override void OnEnable()
        {
            base.OnEnable();
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
        }

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
