﻿using ManagedPlacedObjects;
using System;
using System.Linq;
using UnityEngine;

namespace ConcealedGarden
{
    internal class CGShelterRain : UpdatableAndDeletable, IDrawable
    {
        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("CGShelterRain", typeof(CGShelterRain), null, null));
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
        }

        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);
            if (self.room.abstractRoom.shelter && self.room.updateList.Any(v => v is CGShelterRain))
            {
                float num = 0f;
                if (self.room.waterObject != null)
                {
                    num = self.room.waterObject.fWaterLevel + 100f;
                }
                else if (self.room.deathFallGraphic != null)
                {
                    num = self.room.deathFallGraphic.height + 180f;
                }
                Shader.SetGlobalFloat("_waterLevel", Mathf.InverseLerp(self.sSize.y, 0f, num - lastCamPos.y));
            }
        }

        private readonly PlacedObject pobj;
        private RoomRain roomRain;
        private static Vector2 lastCamPos;

        public CGShelterRain(PlacedObject pobj, Room rm)
        {
            this.pobj = pobj;
            this.room = rm;
            int shelterIndex = room.abstractRoom.shelterIndex;
            this.room.abstractRoom.shelterIndex = -1;
            this.roomRain = new RoomRain(room.game.globalRain, room);
            this.room.abstractRoom.shelterIndex = shelterIndex;
            this.room.roomRain = this.roomRain;
            this.roomRain.room = this.room;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            // Store
            float prevIntensity = roomRain.intensity;
            float globalintensity = room.game.globalRain.Intensity;
            // Fake
            roomRain.intensity = Mathf.Min(0.35f, roomRain.intensity);
            room.game.globalRain.Intensity = Mathf.Min(0.35f, room.game.globalRain.Intensity);
            roomRain.Update(eu);
            // Restore
            room.game.globalRain.Intensity = globalintensity;
            roomRain.intensity = prevIntensity;
            // Manipulate
            if (roomRain.dangerType == RoomRain.DangerType.Rain || roomRain.dangerType == RoomRain.DangerType.FloodAndRain)
            {
                roomRain.intensity = Mathf.Lerp(roomRain.intensity, roomRain.globalRain.Intensity, 0.2f);
            }
            roomRain.intensity = Mathf.Min(roomRain.intensity, roomRain.room.roomSettings.RainIntensity);
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            roomRain.InitiateSprites(sLeaser, rCam);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            roomRain.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            lastCamPos = camPos;
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            roomRain.ApplyPalette(sLeaser, rCam, palette);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            roomRain.AddToContainer(sLeaser, rCam, newContatiner);
        }
    }
}