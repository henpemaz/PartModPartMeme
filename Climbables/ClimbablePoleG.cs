using RWCustom;
using System;
using UnityEngine;


namespace Climbables
{
    abstract public class ClimbablePoleG : UpdatableAndDeletable, IDrawable, INotifyWhenRoomIsReady
    {
        protected PlacedObject placedObject;
        protected IntRect rect;
        protected IntRect lastRect;
        protected bool[] oldTiles;

        public ClimbablePoleG(PlacedObject placedObject, Room instance)
        {
            this.placedObject = placedObject;
            this.room = instance;

            rect = (this.placedObject.data as PlacedObject.GridRectObjectData).Rect;
            rect.right++;
            rect.top++;

            lastRect = rect;
            oldTiles = null;
        }


        public override void Update(bool eu)
        {
            base.Update(eu);
            rect = (this.placedObject.data as PlacedObject.GridRectObjectData).Rect;
            rect.right++;
            rect.top++;
            if (lastRect.bottom != rect.bottom || lastRect.left != rect.left || lastRect.right != rect.right || lastRect.top != rect.top)
            {

                updateTiles();
                queueAIRemapping();

                lastRect = rect;
            }

        }

        protected void queueAIRemapping()
        {
            // Not implemented for now :(
        }

        void IDrawable.InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMeshAtlased(1, false, true);

            (this as IDrawable).ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            (this as IDrawable).AddToContainer(sLeaser, rCam, null);
        }

        void IDrawable.AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.sprites[0].RemoveFromContainer();
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Items");
            }
            newContatiner.AddChild(sLeaser.sprites[0]);
        }

        void IDrawable.ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[0].color = palette.blackColor;
        }

        void IDrawable.DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 width = getWidth();
            Vector2 start = getStart();
            Vector2 end = getEnd();
            (sLeaser.sprites[0] as TriangleMesh).MoveVertice(0, start - width / 2 - camPos);
            (sLeaser.sprites[0] as TriangleMesh).MoveVertice(1, start + width / 2 - camPos);
            (sLeaser.sprites[0] as TriangleMesh).MoveVertice(2, end - width / 2 - camPos);
            (sLeaser.sprites[0] as TriangleMesh).MoveVertice(3, end + width / 2 - camPos);
        }

        void INotifyWhenRoomIsReady.AIMapReady()
        {
            // pass
        }

        void INotifyWhenRoomIsReady.ShortcutsReady()
        {
            updateTiles();
        }

        protected void updateTiles()
        {
            if (oldTiles != null)
            {
                for (int i = GetLastStartIndex(); i < GetLastStopIndex(); i++)
                {
                    Room.Tile tile = lastTile(i);
                    setPole(oldTiles[i - GetLastStartIndex()], tile);
                }

                oldTiles = null;
            }

            this.oldTiles = new bool[GetStopIndex() - GetStartIndex()];
            for (int i = GetStartIndex(); i < GetStopIndex(); i++)
            {
                Room.Tile tile = GetTile(i);
                oldTiles[i - GetStartIndex()] = getPole(tile);
                setPole(true, tile);
            }
        }

        protected abstract int GetStartIndex();

        protected abstract int GetStopIndex();

        protected abstract Room.Tile GetTile(int i);

        protected abstract bool getPole(Room.Tile tile);

        protected abstract void setPole(bool value, Room.Tile tile);

        protected abstract Room.Tile lastTile(int i);

        protected abstract int GetLastStopIndex();

        protected abstract int GetLastStartIndex();

        protected abstract Vector2 getWidth();

        protected abstract Vector2 getStart();

        protected abstract Vector2 getEnd();
    }
}