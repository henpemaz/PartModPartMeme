using System;

namespace ConcealedGarden
{
    internal class CGRootShelterGhostFix : UpdatableAndDeletable
    {
        public CGRootShelterGhostFix(Room room)
        {
            if (room.world?.worldGhost?.GhostMode(room, 0) > 0.5f)
            {
                room.world.rainCycle.timer = UnityEngine.Mathf.Max(room.world.rainCycle.timer, 440);
            }
            
        }

        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("CGRootShelterGhostFix",
                typeof(CGRootShelterGhostFix), null, null));
        }
    }
}