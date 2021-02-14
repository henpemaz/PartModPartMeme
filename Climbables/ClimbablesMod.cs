using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Climbables
{
    public class ClimbablesMod : PartialityMod
    {
        public ClimbablesMod()
        {
            this.ModID = "ClimbablesMod";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static ClimbablesMod instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData_Patch;
            On.Room.Loaded += Room_Loaded_Patch;
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep_Patch;
        }

        public static class EnumExt_ClimbablesMod
        {
            public static PlacedObject.Type ClimbablePoleV;
            public static PlacedObject.Type ClimbablePoleH;
            public static PlacedObject.Type ClimbableArc;
            public static PlacedObject.Type ClimbableRope;

        }

        public static void PlacedObject_GenerateEmptyData_Patch(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject instance)
        {
            orig(instance);
            if (instance.type == EnumExt_ClimbablesMod.ClimbablePoleV || instance.type == EnumExt_ClimbablesMod.ClimbablePoleH)
            {
                instance.data = new PlacedObject.GridRectObjectData(instance);
            }
            if (instance.type == EnumExt_ClimbablesMod.ClimbableArc || instance.type == EnumExt_ClimbablesMod.ClimbableRope)
            {
                instance.data = new PlacedObject.GridRectObjectData(instance);
            }
        }

        private static void ObjectsPage_CreateObjRep_Patch(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, DevInterface.ObjectsPage instance, PlacedObject.Type tp, PlacedObject pObj)
        {
            orig(instance, tp, pObj);
            if (tp == EnumExt_ClimbablesMod.ClimbablePoleV || tp == EnumExt_ClimbablesMod.ClimbablePoleH)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new DevInterface.GridRectObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString());
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }
            if (tp == EnumExt_ClimbablesMod.ClimbableArc || tp == EnumExt_ClimbablesMod.ClimbableRope)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new BezierObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString());
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
                    if (instance.roomSettings.placedObjects[l].type == EnumExt_ClimbablesMod.ClimbablePoleV)
                        instance.AddObject(new ClimbablePoleV(instance.roomSettings.placedObjects[l], instance));
                    if (instance.roomSettings.placedObjects[l].type == EnumExt_ClimbablesMod.ClimbablePoleH)
                        instance.AddObject(new ClimbablePoleH(instance.roomSettings.placedObjects[l], instance));
                    if (instance.roomSettings.placedObjects[l].type == EnumExt_ClimbablesMod.ClimbableArc)
                        instance.AddObject(new ClimbableArc(instance.roomSettings.placedObjects[l], instance));
                    if (instance.roomSettings.placedObjects[l].type == EnumExt_ClimbablesMod.ClimbableRope)
                        instance.AddObject(new ClimbableRope(instance.roomSettings.placedObjects[l], instance));
                }
            }
        }
    }
}
