using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RopesAndDreamsMods
{
    public class RopesAndDreamsMod : PartialityMod
    {
        public RopesAndDreamsMod()
        {
            this.ModID = "RopesAndDreamsMod";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static RopesAndDreamsMod instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData_Patch;
            On.Room.Loaded += Room_Loaded_Patch;
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep_Patch;
        }

        public static class EnumExt_RopesAndDreamsMod
        {
            public static PlacedObject.Type ClimbableRope;
        }

        public static void PlacedObject_GenerateEmptyData_Patch(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject instance)
        {
            orig(instance);
            if(instance.type == EnumExt_RopesAndDreamsMod.ClimbableRope)
            {
                // For now, a resizable will do. More parameters someday maybe
                instance.data = new PlacedObject.ResizableObjectData(instance);
            }
        }

        private static void ObjectsPage_CreateObjRep_Patch(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, DevInterface.ObjectsPage instance, PlacedObject.Type tp, PlacedObject pObj)
        {
            orig(instance, tp, pObj);
            if (tp == EnumExt_RopesAndDreamsMod.ClimbableRope)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new DevInterface.ResizeableObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString(), false);
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }
        }

        public static void Room_Loaded_Patch(On.Room.orig_Loaded orig, Room instance)
        {
            orig(instance);

            for (int l = 0; l < instance.roomSettings.placedObjects.Count; l++)
            {
                if (instance.roomSettings.placedObjects[l].active && instance.roomSettings.placedObjects[l].type == EnumExt_RopesAndDreamsMod.ClimbableRope)
                {
                    instance.AddObject(new ClimbableRope(instance.roomSettings.placedObjects[l], instance));
                }
            }
        }
    }
}
