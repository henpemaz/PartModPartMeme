using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using Partiality.Modloader;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShelterBehaviors
{
    public class ShelterBehaviorsMod : PartialityMod
    {
        public ShelterBehaviorsMod()

        {
            this.ModID = "ShelterBehaviorsMod";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;

        }

        public static ShelterBehaviorsMod instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData_Patch;
            On.Room.Loaded += Room_Loaded_Patch;
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep_Patch;

            ManagedPlacedObjects.ApplyHooks();
        }

        public static class EnumExt_ShelterBehaviorsMod
        {
            public static PlacedObject.Type ShelterBhvrNoVanillaDoor;
            public static PlacedObject.Type ShelterBhvrPlacedDoor;
            public static PlacedObject.Type ShelterBhvrHoldToTrigger;
            public static PlacedObject.Type ShelterBhvrHoldToTriggerTutorial;
            public static PlacedObject.Type ShelterBhvrTriggerZone;
            public static PlacedObject.Type ShelterBhvrNoTriggerZone;
            public static PlacedObject.Type ShelterBhvrConsumableShelter;
            public static PlacedObject.Type ShelterBhvrExtraLongTimer;
            public static PlacedObject.Type ShelterBhvrSpawnPosition;
        }

        public static void PlacedObject_GenerateEmptyData_Patch(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject instance)
        {
            orig(instance);

            // ShelterBhvrNoVanillaDoor;
            // no data
            // ShelterBhvrPlacedDoor;
            if (instance.type == EnumExt_ShelterBehaviorsMod.ShelterBhvrPlacedDoor)
            {
                instance.data = new PlacedObject.ResizableObjectData(instance);
            }
            // ShelterBhvrHoldToTrigger;
            // ShelterBhvrHoldToTriggerTutorial;
            // no data
            // ShelterBhvrTriggerZone;
            if (instance.type == EnumExt_ShelterBehaviorsMod.ShelterBhvrTriggerZone)
            {
                instance.data = new PlacedObject.GridRectObjectData(instance);
            }
            // ShelterBhvrNoTriggerZone;
            if (instance.type == EnumExt_ShelterBehaviorsMod.ShelterBhvrNoTriggerZone)
            {
                instance.data = new PlacedObject.GridRectObjectData(instance);
            }
            // ShelterBhvrConsumableShelter;
            if (instance.type == EnumExt_ShelterBehaviorsMod.ShelterBhvrConsumableShelter)
            {
                instance.data = new PlacedObject.ConsumableObjectData(instance);
                (instance.data as PlacedObject.ConsumableObjectData).minRegen = 3;
                (instance.data as PlacedObject.ConsumableObjectData).maxRegen = 4;
            }
            // ShelterBhvrExtraLongTimer
            // no data because I'm lazy AND clever
            // ShelterBhvrSpawnPosition
            // no data
        }

        private static void ObjectsPage_CreateObjRep_Patch(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, DevInterface.ObjectsPage instance, PlacedObject.Type tp, PlacedObject pObj)
        {
            orig(instance, tp, pObj);

            // ShelterBhvrNoVanillaDoor;
            // pass
            // ShelterBhvrPlacedDoor;
            if (tp == EnumExt_ShelterBehaviorsMod.ShelterBhvrPlacedDoor)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new DevInterface.ResizeableObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString(), false);
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }
            // ShelterBhvrHoldToTrigger;
            // pass
            // ShelterBhvrHoldToTriggerTutorial;
            // pass
            // ShelterBhvrTriggerZone;
            if (tp == EnumExt_ShelterBehaviorsMod.ShelterBhvrTriggerZone)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new DevInterface.GridRectObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString());
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }
            // ShelterBhvrNoTriggerZone;
            if (tp == EnumExt_ShelterBehaviorsMod.ShelterBhvrNoTriggerZone)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new DevInterface.GridRectObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString());
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }
            // ShelterBhvrConsumableShelter;
            if (tp == EnumExt_ShelterBehaviorsMod.ShelterBhvrConsumableShelter)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new DevInterface.ConsumableRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString());
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }
            // ShelterBhvrExtraLongTimer
            // pass
            // ShelterBhvrSpawnPosition
            // pass
        }

        public static void Room_Loaded_Patch(On.Room.orig_Loaded orig, Room instance)
        {
            orig(instance);
            ShelterBehaviorManager bhvrManager= null;
            for (int i = 0; i < instance.roomSettings.placedObjects.Count; i++)
            {
                if (instance.roomSettings.placedObjects[i].active)
                {
                    if (instance.roomSettings.placedObjects[i].type == EnumExt_ShelterBehaviorsMod.ShelterBhvrNoVanillaDoor)
                    {
                        if (bhvrManager is null) instance.AddObject(bhvrManager = new ShelterBehaviorManager(instance));
                        bhvrManager.RemoveVanillaDoors();
                    }
                    else if (instance.roomSettings.placedObjects[i].type == EnumExt_ShelterBehaviorsMod.ShelterBhvrPlacedDoor)
                    {
                        if (bhvrManager is null) instance.AddObject(bhvrManager = new ShelterBehaviorManager(instance));
                        bhvrManager.AddPlacedDoor(instance.roomSettings.placedObjects[i]);
                    }
                    else if (instance.roomSettings.placedObjects[i].type == EnumExt_ShelterBehaviorsMod.ShelterBhvrHoldToTrigger)
                    {
                        if (bhvrManager is null) instance.AddObject(bhvrManager = new ShelterBehaviorManager(instance));
                        bhvrManager.SetHoldToTrigger();
                    }
                    else if (instance.roomSettings.placedObjects[i].type == EnumExt_ShelterBehaviorsMod.ShelterBhvrHoldToTriggerTutorial)
                    {
                        if (bhvrManager is null) instance.AddObject(bhvrManager = new ShelterBehaviorManager(instance));
                        bhvrManager.HoldToTriggerTutorial(i);
                    }
                    else if (instance.roomSettings.placedObjects[i].type == EnumExt_ShelterBehaviorsMod.ShelterBhvrTriggerZone)
                    {
                        if (bhvrManager is null) instance.AddObject(bhvrManager = new ShelterBehaviorManager(instance));
                        bhvrManager.AddTriggerZone(instance.roomSettings.placedObjects[i]);
                    }
                    else if (instance.roomSettings.placedObjects[i].type == EnumExt_ShelterBehaviorsMod.ShelterBhvrNoTriggerZone)
                    {
                        if (bhvrManager is null) instance.AddObject(bhvrManager = new ShelterBehaviorManager(instance));
                        bhvrManager.AddNoTriggerZone(instance.roomSettings.placedObjects[i]);
                    }
                    else if (instance.roomSettings.placedObjects[i].type == EnumExt_ShelterBehaviorsMod.ShelterBhvrConsumableShelter)
                    {
                        if (bhvrManager is null) instance.AddObject(bhvrManager = new ShelterBehaviorManager(instance));
                        bhvrManager.ProcessConsumable(instance.roomSettings.placedObjects[i], i);
                    }
                    else if (instance.roomSettings.placedObjects[i].type == EnumExt_ShelterBehaviorsMod.ShelterBhvrExtraLongTimer)
                    {
                        if (bhvrManager is null) instance.AddObject(bhvrManager = new ShelterBehaviorManager(instance));
                        bhvrManager.IncreaseTimer();
                    }
                    else if (instance.roomSettings.placedObjects[i].type == EnumExt_ShelterBehaviorsMod.ShelterBhvrSpawnPosition)
                    {
                        if (bhvrManager is null) instance.AddObject(bhvrManager = new ShelterBehaviorManager(instance));
                        bhvrManager.AddSpawnPosition(instance.roomSettings.placedObjects[i]);
                    }
                }
            }
        }
    }
}
