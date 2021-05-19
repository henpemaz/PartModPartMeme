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

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.FloatField("rmin", 0, 1, 0.1f, 0.001f),
                new PlacedObjectsManager.FloatField("rmax", 0, 1, 0.3f, 0.001f),
                new PlacedObjectsManager.FloatField("gmin", 0, 1, 0.05f, 0.001f),
                new PlacedObjectsManager.FloatField("gmax", 0, 1, 0.2f, 0.001f),
                new PlacedObjectsManager.FloatField("bmin", 0, 1, 0.5f, 0.001f),
                new PlacedObjectsManager.FloatField("bmax", 0, 1, 0.25f, 0.001f),
                new PlacedObjectsManager.FloatField("stiff", 0, 1, 0.5f, 0.01f),
                new PlacedObjectsManager.IntegerField("ftc", 0, 400, 120, PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider),
            }, typeof(OrganicShelter.OrganicShelterCoordinator), "OrganicShelterCoordinator");

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.Vector2Field("size", new UnityEngine.Vector2(40,40), PlacedObjectsManager.Vector2Field.VectorReprType.circle),
                new PlacedObjectsManager.Vector2Field("dest", new UnityEngine.Vector2(0,50), PlacedObjectsManager.Vector2Field.VectorReprType.line),
                new PlacedObjectsManager.FloatField("stiff", 0, 1, 0.5f, 0.01f),
            }, null, "OrganicLockPart");

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.Vector2Field("size", new UnityEngine.Vector2(-100,100), PlacedObjectsManager.Vector2Field.VectorReprType.circle),
                new PlacedObjectsManager.FloatField("sizemin", 1, 200, 12f, 1f),
                new PlacedObjectsManager.FloatField("sizemax", 1, 200, 20f, 1f),
                new PlacedObjectsManager.FloatField("depth", -100, 100, 4f, 1f),
                new PlacedObjectsManager.FloatField("density", 0, 5, 0.5f, 0.01f),
                new PlacedObjectsManager.FloatField("stiff", 0, 1, 0.5f, 0.01f),
                new PlacedObjectsManager.FloatField("spread", 0, 20f, 2f, 0.1f),
                new PlacedObjectsManager.IntegerField("seed", 0, 9999, 0),
            }, null, "OrganicLining");

            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("LifeSimProjectionSegment",
                typeof(LifeSimProjection), typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation), singleInstance: true));
            //PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("LifeSimProjectionPulser",
            //    typeof(LifeSimProjection), typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation), singleInstance: true));
            //PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("LifeSimProjectionKiller",
            //    typeof(LifeSimProjection), typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation), singleInstance: true));

            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("CosmeticLeaves",
                typeof(CosmeticLeaves), typeof(CosmeticLeaves.CosmeticLeavesObjectData), typeof(PlacedObjectsManager.ManagedRepresentation)));

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.BooleanField("noleft", false, displayName:"No Left Door"),
                new PlacedObjectsManager.BooleanField("noright", false, displayName:"No Right Door"),
                new PlacedObjectsManager.BooleanField("nowater", false, displayName:"No Water, stoopid"),
            }, typeof(CGGateFix), "CGGateFix");


            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "NudeMod")
                {
                    QuestionableLizardBit.Apply();
                }
            }

            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("SlipperySlope",
                typeof(SlipperySlope), typeof(SlipperySlope.SlipperySlopeData), typeof(PlacedObjectsManager.ManagedRepresentation)));


            // ID spawndata support
            On.RainWorldGame.GetNewID_1 += RainWorldGame_GetNewID_1;
            On.WorldLoader.ctor += WorldLoader_ctor;
        }

        private WeakReference currentWorldLoader;
        private void WorldLoader_ctor(On.WorldLoader.orig_ctor orig, WorldLoader self, RainWorldGame game, int playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            currentWorldLoader = new WeakReference(self);
            orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);
        }

        private EntityID RainWorldGame_GetNewID_1(On.RainWorldGame.orig_GetNewID_1 orig, RainWorldGame self, int spawner)
        {
            EntityID id = orig(self, spawner);

            if(spawner > 0 && self.IsStorySession) // called juuuust from WorldLoader.GeneratePopulation most likely, lets play safe though
            {
                int region = UnityEngine.Mathf.FloorToInt(spawner / 1000f);
                int inregionspawn = spawner - region * 1000;

                try
                {
                    // game.overWorld isn't set until the constructor is done
                    // Overworld.LoadWorld doesn't set a reference to worldloader anywhere while its doing its thing :/
                    WorldLoader worldLoader = currentWorldLoader.Target as WorldLoader;
                    if (worldLoader != null && !worldLoader.Finished && worldLoader.world.region != null && worldLoader.world.region.regionNumber == region)
                    {
                        string spawnData = "";
                        if (worldLoader.world.spawners[inregionspawn] is World.SimpleSpawner simpleSpawner)
                        {
                            spawnData = simpleSpawner.spawnDataString;
                        }
                        else if (worldLoader.world.spawners[inregionspawn] is World.Lineage lineage)
                        {
                            spawnData = lineage.CurrentSpawnData((self.session as StoryGameSession).saveState);
                        }
                        if (!string.IsNullOrEmpty(spawnData) && spawnData[0] == '{')
                        {
                            string[] array = spawnData.Substring(1, spawnData.Length - 2).Split(new char[] { ',' });
                            for (int i = 0; i < array.Length; i++)
                            {
                                if (array[i].Length > 0)
                                {
                                    string[] array2 = array[i].Split(new char[] { ':' });
                                    string text = array2[0].Trim().ToLowerInvariant();
                                    if (text == "id")
                                    {
                                        id.number = int.Parse(array2[1].Trim());
                                    }
                                }
                            }
                        }
                    }
                }
                catch { UnityEngine.Debug.LogError("ConcealedGarden: Something terrible happened while trying to parse for a spawn ID for spawner " + spawner); }
            }
            return id;
        }
    }
}
