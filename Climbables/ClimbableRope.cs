using UnityEngine;

namespace Climbables
{
    public class ClimbableRope : UpdatableAndDeletable, IClimbableVine, IDrawable
    {
        private PlacedObject placedObject;
        private Room instance;

        public ClimbableRope(PlacedObject placedObject, Room instance)
        {
            this.placedObject = placedObject;
            this.instance = instance;
        }

        void IDrawable.AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            throw new System.NotImplementedException();
        }

        void IDrawable.ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            throw new System.NotImplementedException();
        }

        void IClimbableVine.BeingClimbedOn(Creature crit)
        {
            throw new System.NotImplementedException();
        }

        bool IClimbableVine.CurrentlyClimbable()
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

        float IClimbableVine.Mass(int index)
        {
            throw new System.NotImplementedException();
        }

        Vector2 IClimbableVine.Pos(int index)
        {
            throw new System.NotImplementedException();
        }

        void IClimbableVine.Push(int index, Vector2 movement)
        {
            throw new System.NotImplementedException();
        }

        float IClimbableVine.Rad(int index)
        {
            throw new System.NotImplementedException();
        }

        int IClimbableVine.TotalPositions()
        {
            throw new System.NotImplementedException();
        }
    }
}