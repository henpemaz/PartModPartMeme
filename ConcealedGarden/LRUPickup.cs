using ManagedPlacedObjects;
using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal class LRUPickup : CosmeticSprite
    {
        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("LRUPickup",
                typeof(LRUPickup), null, null));
        }

        private readonly PlacedObject pObj;
        public LRUPickup(Room room, PlacedObject pObj)
        {
            this.room = room;
            this.pObj = pObj;
            pos = pObj.pos;
            if (!room.game.IsStorySession) Destroy();
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion) return;
            for (int i = 0; i < this.room.physicalObjects.Length; i++)
            {
                for (int j = 0; j < this.room.physicalObjects[i].Count; j++)
                {
                    if (this.room.physicalObjects[i][j] is Player player && !player.inShortcut)
                    {
                        (player.graphicsModule as PlayerGraphics).LookAtPoint(pos, 1000f);
                        if (player.controller == null)
                            player.controller = new ArenaGameSession.PlayerStopController();
                        Vector2 dir = (pos - player.mainBodyChunk.pos).normalized;
                        Vector2 vrel = player.mainBodyChunk.vel * (Vector2.Dot(player.mainBodyChunk.vel, dir) / player.mainBodyChunk.vel.magnitude);
                        if (vrel.magnitude < 1f) player.mainBodyChunk.vel += 0.2f * (dir - vrel);

                        if (RWCustom.Custom.DistLess(player.mainBodyChunk.pos, pos, 300f))
                        {
                            Activate();
                        }
                    }
                }
            }

        }

        bool activated = false;
        private void Activate()
        {
            if (activated) return;
            activated = true;
            CGCutscenes.ExitToCGSpiralSlideshow(room.game);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[]
            {
                new FSprite("Futile_White")
                {
                    shader = room.game.rainWorld.Shaders["Spores"],
                    color = new Color(0.9f,0.9f,0.9f),
                    alpha = 0.9f,
                    scale = 80f/16f
                },
                new FSprite("Futile_White")
                {
                    shader = room.game.rainWorld.Shaders["ShockWave"],
                    color = new Color(0.2f,1f,0.0f),
                    scale = 80f/16f
                }
            };
            this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Water"));
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[0].SetPosition(pos - camPos);
            sLeaser.sprites[1].SetPosition(pos - camPos);
        }
    }
}