using ManagedPlacedObjects;
using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal class CGQuickAndDirtyFix : UpdatableAndDeletable, IDrawable
    {
        public CGQuickAndDirtyFix(Room room)
        {
        }

        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("CGQuickAndDirtyFix", typeof(CGQuickAndDirtyFix), null, null));
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