using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData_Patch;
            On.Room.Loaded += Room_Loaded_Patch;
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep_Patch;

            ManagedPlacedObjects.PlacedObjectsManager.RegisterFullyManagedObjectType(new ManagedPlacedObjects.PlacedObjectsManager.ManagedField[]
            {
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("rmin", 0, 1, 0.1f, 0.001f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("rmax", 0, 1, 0.3f, 0.001f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("gmin", 0, 1, 0.05f, 0.001f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("gmax", 0, 1, 0.2f, 0.001f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("bmin", 0, 1, 0.5f, 0.001f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("bmax", 0, 1, 0.25f, 0.001f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("stiff", 0, 1, 0.5f, 0.01f),
                new ManagedPlacedObjects.PlacedObjectsManager.IntegerField("ftc", 0, 400, 120, ManagedPlacedObjects.PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider),
            }, typeof(OrganicShelter.OrganicShelterCoordinator), "OrganicShelterCoordinator");

            ManagedPlacedObjects.PlacedObjectsManager.RegisterFullyManagedObjectType(new ManagedPlacedObjects.PlacedObjectsManager.ManagedField[]
            {
                new ManagedPlacedObjects.PlacedObjectsManager.Vector2Field("size", new UnityEngine.Vector2(40,40), ManagedPlacedObjects.PlacedObjectsManager.Vector2Field.VectorReprType.circle),
                new ManagedPlacedObjects.PlacedObjectsManager.Vector2Field("dest", new UnityEngine.Vector2(0,50), ManagedPlacedObjects.PlacedObjectsManager.Vector2Field.VectorReprType.line),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("stiff", 0, 1, 0.5f, 0.01f),
            }, null, "OrganicLockPart");

            ManagedPlacedObjects.PlacedObjectsManager.RegisterFullyManagedObjectType(new ManagedPlacedObjects.PlacedObjectsManager.ManagedField[]
            {
                new ManagedPlacedObjects.PlacedObjectsManager.Vector2Field("size", new UnityEngine.Vector2(-100,100), ManagedPlacedObjects.PlacedObjectsManager.Vector2Field.VectorReprType.circle),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("sizemin", 1, 200, 12f, 1f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("sizemax", 1, 200, 20f, 1f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("depth", -100, 100, 4f, 1f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("density", 0, 5, 0.5f, 0.01f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("stiff", 0, 1, 0.5f, 0.01f),
                new ManagedPlacedObjects.PlacedObjectsManager.FloatField("spread", 0, 20f, 2f, 0.1f),
                new ManagedPlacedObjects.PlacedObjectsManager.IntegerField("seed", 0, 9999, 0),
            }, null, "OrganicLining");


        }

        public static class EnumExt_ConcealedGarden
        {
            public static PlacedObject.Type CosmeticLeaves;
            public static PlacedObject.Type LifeSimProjectionSegment;
            //public static PlacedObject.Type LifeSimProjectionPulser;
            //public static PlacedObject.Type LifeSimProjectionKiller;


        }

        public static void PlacedObject_GenerateEmptyData_Patch(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject instance)
        {
            orig(instance);
            if (instance.type == EnumExt_ConcealedGarden.CosmeticLeaves)
            {
                instance.data = new CosmeticLeaves.CosmeticLeavesObjectData(instance);
            }
            if (instance.type == EnumExt_ConcealedGarden.LifeSimProjectionSegment)
            {
                instance.data = new PlacedObject.GridRectObjectData(instance);
            }
            //if (instance.type == EnumExt_ConcealedGarden.LifeSimProjectionPulser)
            //{
            //    instance.data = new LifeSimProjection.LifeSimProjectionPulserData(instance);
            //}
            //if (instance.type == EnumExt_ConcealedGarden.LifeSimProjectionKiller)
            //{
            //    instance.data = new LifeSimProjection.LifeSimProjectionKillerData(instance);
            //}


        }

        private static void ObjectsPage_CreateObjRep_Patch(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, DevInterface.ObjectsPage instance, PlacedObject.Type tp, PlacedObject pObj)
        {
            orig(instance, tp, pObj);
            if (tp == EnumExt_ConcealedGarden.CosmeticLeaves)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new CosmeticLeaves.CosmeticLeavesObjectData.CosmeticLeavesObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString());
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }
            if (tp == EnumExt_ConcealedGarden.LifeSimProjectionSegment)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new DevInterface.GridRectObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString());
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }
        }

        public static void Room_Loaded_Patch(On.Room.orig_Loaded orig, Room instance)
        {
            orig(instance);

            for (int l = 0; l < instance.roomSettings.placedObjects.Count; l++)
            {
                if (instance.roomSettings.placedObjects[l].active)
                {
                    if (instance.roomSettings.placedObjects[l].type == EnumExt_ConcealedGarden.CosmeticLeaves)
                        instance.AddObject(new CosmeticLeaves(instance.roomSettings.placedObjects[l], instance));

                    if (instance.roomSettings.placedObjects[l].type == EnumExt_ConcealedGarden.LifeSimProjectionSegment)
                    {
                        bool hasLifeSimProjection = false;
                        LifeSimProjection projection = null;
                        foreach (var item in instance.updateList)
                        {
                            if (item is LifeSimProjection)
                            {
                                hasLifeSimProjection = true;
                                projection = item as LifeSimProjection;
                                break;
                            }
                        }
                        if(!hasLifeSimProjection) instance.AddObject(projection = new LifeSimProjection(instance));
                        projection.places.Add(instance.roomSettings.placedObjects[l]);
                    }
                }
            }
        }
    }
}
