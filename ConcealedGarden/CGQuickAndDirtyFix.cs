using System;
using UnityEngine;

namespace ConcealedGarden
{
    /// <summary>
    /// Fixes weird things from Climbables, temporary solution until I decide how I want to handle an update for it :monke:
    /// </summary>
    internal class CGQuickAndDirtyFix : UpdatableAndDeletable, IDrawable
    {
        public CGQuickAndDirtyFix(Room room)
        {
        }

        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("CGQuickAndDirtyFix", typeof(CGQuickAndDirtyFix), null, null));
            On.Player.MovementUpdate += Player_MovementUpdate;
        }

        private static void Player_MovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            Player.BodyModeIndex prevMode = self.bodyMode;
            orig(self, eu);
            if (prevMode == Player.BodyModeIndex.CorridorClimb && self.bodyMode == Player.BodyModeIndex.Default && self.room.climbableVines != null)
            {
                ClimbableVinesSystem.VinePosition vinePosition2 = self.room.climbableVines.VineOverlap(self.mainBodyChunk.pos, self.mainBodyChunk.rad);
                if (vinePosition2 != null)
                {
                    self.wantToGrab = 1; // let grab naturally next frame
                }
            }
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            foreach (var item in rCam.spriteLeasers)
            {
                if (item.drawableObject is Climbables.ClimbableArc)
                {
                    for (int i = 0; i < item.sprites.Length; i++)
                    {
                        item.sprites[i].isVisible = false;
                    }
                }
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[0];
        }
    }
}