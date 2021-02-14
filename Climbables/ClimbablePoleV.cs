using RWCustom;
using System;
using UnityEngine;

namespace Climbables
{
    public class ClimbablePoleV : UpdatableAndDeletable, IDrawable, INotifyWhenRoomIsReady
    {
        private PlacedObject placedObject;
        private Room instance;
        private Vector2 start;
        private Vector2 end;
        private IntRect lastRect;
        private Vector2 width;
        private bool[] oldTiles;

        public ClimbablePoleV(PlacedObject placedObject, Room instance)
        {
            this.placedObject = placedObject;
            this.instance = instance;

            RWCustom.IntRect rect = (this.placedObject.data as PlacedObject.GridRectObjectData).Rect;
            //rect.right++;
            rect.top++;
            width = new Vector2(4, 0);
            start = new Vector2((float)rect.left * 20f + 10f, (float)rect.bottom * 20f);
            end = new Vector2((float)rect.left * 20f + 10f, (float)rect.top * 20f);

        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            RWCustom.IntRect rect = (this.placedObject.data as PlacedObject.GridRectObjectData).Rect;
            //rect.right++;
            rect.top++;
            if (lastRect.bottom != rect.bottom || lastRect.left != rect.left || lastRect.top != rect.top)
            {
                start = new Vector2((float)rect.left * 20f + 10f, (float)rect.bottom * 20f);
                end = new Vector2((float)rect.left * 20f + 10f, (float)rect.top * 20f);

                updateTiles();
                queueAIRemapping();

                lastRect = rect;
            }
            
        }

        private void queueAIRemapping()
        {
            // Nothing for now :(
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

        private void updateTiles()
        {
            if (oldTiles != null)
            {
                for (int i = lastRect.bottom; i < lastRect.top; i++)
                {
                    Room.Tile tile = room.GetTile(lastRect.left, i);
                    tile.verticalBeam = oldTiles[i - lastRect.bottom];
                }

                oldTiles = null;
            }
            RWCustom.IntRect rect = (this.placedObject.data as PlacedObject.GridRectObjectData).Rect;
            rect.top++;
            this.oldTiles = new bool[rect.top - rect.bottom];
            for (int i = rect.bottom; i < rect.top; i++)
            {
                Room.Tile tile = room.GetTile(rect.left, i);
                oldTiles[i - rect.bottom] = tile.verticalBeam;
                tile.verticalBeam = true;
            }
        }
    }
}