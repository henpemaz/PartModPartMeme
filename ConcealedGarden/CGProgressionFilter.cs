using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConcealedGarden
{
    internal class CGProgressionFilter
    {
        private static PlacedObjectsManager.ManagedObjectType progressionFilterType;
        public enum ProgressionRequirement { No, Yes, Either } // int value important for yes/no, bool'd
        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(progressionFilterType = new PlacedObjectsManager.ManagedObjectType("CGProgressionFilter",
                null, typeof(CGProgressionFilterData), typeof(PlacedObjectsManager.ManagedRepresentation)));

            On.Room.ctor += Room_ctor;
            On.RoomSettings.LoadPlacedObjects += RoomSettings_LoadPlacedObjects;
        }

        private static WeakReference currentRoomLoading;
        private static void Room_ctor(On.Room.orig_ctor orig, Room self, RainWorldGame game, World world, AbstractRoom abstractRoom)
        {
            currentRoomLoading = new WeakReference(self);
            orig(self, game, world, abstractRoom);
        }

        private static void RoomSettings_LoadPlacedObjects(On.RoomSettings.orig_LoadPlacedObjects orig, RoomSettings self, string[] s, int playerChar)
        {
            orig(self, s, playerChar);
            if (playerChar == -1 || currentRoomLoading == null || !currentRoomLoading.IsAlive) return;
            Room room = currentRoomLoading.Target as Room;
            if (room.roomSettings != null || room.game == null || !room.game.IsStorySession || room.game.GetStorySession.saveState == null || room.game.GetStorySession.saveState.progression == null) return;
            List<PlacedObject> progressionFilters = self.placedObjects.Where(p => p.type == progressionFilterType.GetObjectType()).ToList();
            for (int j = 0; j < self.placedObjects.Count; j++)
            {
                if (self.placedObjects[j].deactivattable && !(self.placedObjects[j].type == progressionFilterType.GetObjectType()))
                {
                    for (int k = 0; k < progressionFilters.Count; k++)
                    {
                        if (RWCustom.Custom.DistLess(self.placedObjects[j].pos, progressionFilters[k].pos, (progressionFilters[k].data as CGProgressionFilterData).handle.magnitude))
                        {
                            self.placedObjects[j].active &= (progressionFilters[k].data as CGProgressionFilterData).ShouldActivate(room.game.GetStorySession.saveState);
                            //break;
                        }
                    }
                }
            }
        }

        internal class CGProgressionFilterData : PlacedObjectsManager.ManagedData
        {
            private static readonly PlacedObjectsManager.ManagedField[] customFields = new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.EnumField("e1", typeof(ProgressionRequirement), ProgressionRequirement.Either, displayName:"TheGlow"),
                new PlacedObjectsManager.EnumField("e2", typeof(ProgressionRequirement), ProgressionRequirement.Either, displayName:"TheMark"),
                new PlacedObjectsManager.EnumField("e3", typeof(ProgressionRequirement), ProgressionRequirement.Either, displayName:"Ascended"),
                new PlacedObjectsManager.EnumField("e4", typeof(ProgressionRequirement), ProgressionRequirement.Either, displayName:"CG - Transf"),
                new PlacedObjectsManager.Vector2Field("h1", new Vector2(-50, -20), PlacedObjectsManager.Vector2Field.VectorReprType.circle),
            };
#pragma warning disable 0649
            [BackedByField("e1")]
            public ProgressionRequirement theglow;
            [BackedByField("e2")]
            public ProgressionRequirement themark;
            [BackedByField("e3")]
            public ProgressionRequirement ascended;
            [BackedByField("e4")]
            public ProgressionRequirement transfurred;
            [BackedByField("h1")]
            public Vector2 handle;
#pragma warning restore 0649

            public CGProgressionFilterData(PlacedObject owner) : base(owner, customFields) { }

            internal bool ShouldActivate(SaveState saveState)
            {
                if (theglow != ProgressionRequirement.Either && saveState.theGlow != Convert.ToBoolean((int)theglow)) return false;
                if (themark != ProgressionRequirement.Either && saveState.deathPersistentSaveData.theMark != Convert.ToBoolean((int)themark)) return false;
                if (ascended != ProgressionRequirement.Either && saveState.deathPersistentSaveData.ascended != Convert.ToBoolean((int)ascended)) return false;
                if (transfurred != ProgressionRequirement.Either && ConcealedGarden.progression.transfurred != Convert.ToBoolean((int)transfurred)) return false;
                return true;
            }
        }
    }
}