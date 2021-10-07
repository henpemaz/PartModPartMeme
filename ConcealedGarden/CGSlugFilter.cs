using ManagedPlacedObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ConcealedGarden
{
    internal class CGSlugFilter
    {
        private static PlacedObjectsManager.ManagedObjectType slugcatFilterType;
        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(slugcatFilterType = new PlacedObjectsManager.ManagedObjectType("CGSlugFilter",
                null, typeof(CGSlugFilterData), typeof(PlacedObjectsManager.ManagedRepresentation)));

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
            List<PlacedObject> progressionFilters = self.placedObjects.Where(p => p.type == slugcatFilterType.GetObjectType()).ToList();
            for (int j = 0; j < self.placedObjects.Count; j++)
            {
                if (self.placedObjects[j].deactivattable && !(self.placedObjects[j].type == slugcatFilterType.GetObjectType()))
                {
                    for (int k = 0; k < progressionFilters.Count; k++)
                    {
                        if (RWCustom.Custom.DistLess(self.placedObjects[j].pos, progressionFilters[k].pos, (progressionFilters[k].data as CGSlugFilterData).handle.magnitude))
                        {
                            // bugfix: don't use playerchar mods pass a fake number to that
                            self.placedObjects[j].active &= (progressionFilters[k].data as CGSlugFilterData).ShouldActivate(room.game.StoryCharacter);
                            //break;
                        }
                    }
                }
            }
        }

        internal class CGSlugFilterData : PlacedObjectsManager.ManagedData
        {
            private static readonly PlacedObjectsManager.ManagedField[] customFields = new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.Vector2Field("h1", new Vector2(-50, -20), PlacedObjectsManager.Vector2Field.VectorReprType.circle),
            };
#pragma warning disable 0649
            [PlacedObjectsManager.StringField("01", "Slug", "Slugcat")]
            public string slugcat;
            [PlacedObjectsManager.BooleanField("02", false, displayName: "Exclude")]
            public bool exclude;

            [BackedByField("h1")]
            public Vector2 handle;
#pragma warning restore 0649

            public CGSlugFilterData(PlacedObject owner) : base(owner, customFields) { }

            internal bool ShouldActivate(int playerChar)
            {
                Debug.Log($"filter has slug:{slugcat} exc:{exclude} playernum:{playerChar} playername:{ConcealedGardenProgression.GetSlugcatName(playerChar)} ; should activate = {(!exclude) == (slugcat == ConcealedGardenProgression.GetSlugcatName(playerChar))}");
                return (!exclude) == (slugcat == ConcealedGardenProgression.GetSlugcatName(playerChar));
            }
        }
    }
}