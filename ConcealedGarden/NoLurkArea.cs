using ManagedPlacedObjects;
using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal static class NoLurkArea
    {
        private static PlacedObjectsManager.ManagedObjectType noLurkType;
        public static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(noLurkType = new PlacedObjectsManager.ManagedObjectType("NoLurkArea", null,
                dataType: typeof(NoLurkAreaData), typeof(PlacedObjectsManager.ManagedRepresentation)));

            On.LizardAI.LurkTracker.LurkPosScore += LurkPosScore_Hk;
        }

        private static float LurkPosScore_Hk(On.LizardAI.LurkTracker.orig_LurkPosScore orig, LizardAI.LurkTracker self, WorldCoordinate testLurkPos)
        {
            float retval = orig(self, testLurkPos);
            if (testLurkPos.room == self.lizard.abstractCreature.pos.room)
            {
                Vector2 lurkPos = self.lizard.room.MiddleOfTile(testLurkPos);
                PlacedObject.Type nolurktype = noLurkType.GetObjectType();
                foreach (var item in self.lizard.room.roomSettings.placedObjects)
                {
                    if(item.active && item.type == nolurktype)
                    {
                        if (RWCustom.Custom.DistLess(lurkPos, item.pos, (item.data as NoLurkAreaData).handle.magnitude))
                        {
                            //Debug.LogError("NO LURK");
                            return -100000f;
                        }
                    }
                }
            }
            return retval;
        }

        private class NoLurkAreaData : PlacedObjectsManager.ManagedData
        {
            private static PlacedObjectsManager.ManagedField[] paramFields = new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.Vector2Field("handle", new UnityEngine.Vector2(-100f, 40f), PlacedObjectsManager.Vector2Field.VectorReprType.circle)
            };
            [BackedByField("handle")]
            public Vector2 handle;
            public NoLurkAreaData(PlacedObject owner) : base(owner, paramFields) { }
        }

        //private PlacedObject pObj;

        //public NoLurkArea(Room room, PlacedObject pObj)
        //{
        //    this.room = room;
        //    this.pObj = pObj;
        //}
    }
}