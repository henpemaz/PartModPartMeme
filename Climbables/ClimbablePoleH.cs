using UnityEngine;

namespace Climbables
{
    public class ClimbablePoleH : UpdatableAndDeletable, IDrawable, INotifyWhenRoomIsReady
    {
        private PlacedObject placedObject;
        private Room instance;

        public ClimbablePoleH(PlacedObject placedObject, Room instance)
        {
            this.placedObject = placedObject;
            this.instance = instance;
        }

        void IDrawable.AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            throw new System.NotImplementedException();
        }

        void INotifyWhenRoomIsReady.AIMapReady()
        {
            throw new System.NotImplementedException();
        }

        void IDrawable.ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            throw new System.NotImplementedException();
        }

        void IDrawable.DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            throw new System.NotImplementedException();
        }

        void IDrawable.InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            throw new System.NotImplementedException();
        }

        void INotifyWhenRoomIsReady.ShortcutsReady()
        {
            throw new System.NotImplementedException();
        }
    }
}