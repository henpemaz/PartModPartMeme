using System;
using UnityEngine;

namespace ConcealedGarden
{
    public class CGCameraZoomEffect
    {
        public static class EnumExt_CameraZoomEffect
        {
#pragma warning disable 0649
            public static RoomSettings.RoomEffect.Type CGCameraZoom;
#pragma warning restore 0649
        }
        
        internal static void Apply()
        {
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
        }

        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
			float zoom = 1f;
			bool zoomed = false;
			Vector2 offset = Vector2.zero;
			if (self.room != null && self.room.roomSettings.GetEffectAmount(EnumExt_CameraZoomEffect.CGCameraZoom) > 0f)
			{
				//zoom = 1f;// self.room.roomSettings.GetEffectAmount(EnumExt_CameraZoomEffect.CameraZoom) * 10f;
				zoom = self.room.roomSettings.GetEffectAmount(EnumExt_CameraZoomEffect.CGCameraZoom) * 20f;
				zoomed = true;
				Creature creature = (self.followAbstractCreature == null) ? null : self.followAbstractCreature.realizedCreature;
				if (creature != null)
				{
					//Vector2 testPos = creature.bodyChunks[0].pos + creature.bodyChunks[0].vel + self.followCreatureInputForward * 2f;
					Vector2 value = Vector2.Lerp(creature.bodyChunks[0].lastPos, creature.bodyChunks[0].pos, timeStacker);
					if (creature.inShortcut)
					{
						Vector2? vector = self.room.game.shortcuts.OnScreenPositionOfInShortCutCreature(self.room, creature);
						if (vector != null)
						{
							//testPos = vector.Value;
							value = vector.Value;
						}
					}
					offset = (value - (self.pos + self.sSize/2f));
				}

			}
			for (int i = 0; i < 11; i++)
			{
				self.SpriteLayers[i].scale = 1f;
				self.SpriteLayers[i].SetPosition(Vector2.zero);
				if(zoomed)
					self.SpriteLayers[i].ScaleAroundPointRelative(self.sSize/2f, zoom, zoom);
				
				//self.SpriteLayers[i].SetPosition(-offset);
			}
			self.offset = offset;
			//self.levelGraphic.scale = zoom;
			int theseed = 0; ;
			if (zoomed)
			{
				theseed = UnityEngine.Random.seed;
				UnityEngine.Random.seed = theseed;
			}
			orig(self, timeStacker, timeSpeed);
            if (zoomed)
            {
				UnityEngine.Random.seed = theseed;
				// coppypasta I just need the same exact viewport
				Vector2 vector = Vector2.Lerp(self.lastPos, self.pos, timeStacker);
                if (self.microShake > 0f)
                {
                    vector += RWCustom.Custom.RNV() * 8f * self.microShake * UnityEngine.Random.value;
                }
                if (!self.voidSeaMode)
                {
                    vector.x = Mathf.Clamp(vector.x, self.CamPos(self.currentCameraPosition).x + self.hDisplace + 8f - 20f, self.CamPos(self.currentCameraPosition).x + self.hDisplace + 8f + 20f);
                    vector.y = Mathf.Clamp(vector.y, self.CamPos(self.currentCameraPosition).y + 8f - 7f, self.CamPos(self.currentCameraPosition).y + 33f);
                }
                else
                {
                    vector.y = Mathf.Min(vector.y, -528f);
                }
                vector = new Vector2(Mathf.Floor(vector.x), Mathf.Floor(vector.y));
                vector.x -= 0.02f;
                vector.y -= 0.02f;
				// THIS CRAP BUGS OUT ON SCREEN TRANSITIONS AND i DON'T UNDESTAND WHYYYYYY
                Vector2 magicOffset = self.CamPos(self.currentCameraPosition) - vector;
				//Debug.LogError("magic offset is " + magicOffset);
				//Vector4 center = new Vector4(
				//	(-vector.x - 0.5f + self.levelGraphic.width / 2f + self.CamPos(self.currentCameraPosition).x) / self.sSize.x,
				//	(-vector.y + 0.5f + self.levelGraphic.height / 2f + self.CamPos(self.currentCameraPosition).y) / self.sSize.y,
				//	(-vector.x - 0.5f + self.levelGraphic.width / 2f + self.CamPos(self.currentCameraPosition).x) / self.sSize.x,
				//	(-vector.y + 0.5f + self.levelGraphic.height / 2f + self.CamPos(self.currentCameraPosition).y) / self.sSize.y);
				Vector4 center = new Vector4(
					(magicOffset.x + self.levelGraphic.width / 2f) / self.sSize.x,
					(magicOffset.y +2f + self.levelGraphic.height / 2f) / self.sSize.y,
					(magicOffset.x + self.levelGraphic.width / 2f) / self.sSize.x,
					(magicOffset.y +2f + self.levelGraphic.height / 2f) / self.sSize.y);
				vector += self.offset;
                Vector4 sprpos = new Vector4(
                    (-vector.x + self.CamPos(self.currentCameraPosition).x) / self.sSize.x,
                    (-vector.y + self.CamPos(self.currentCameraPosition).y) / self.sSize.y,
                    (-vector.x + self.levelGraphic.width + self.CamPos(self.currentCameraPosition).x) / self.sSize.x,
                    (-vector.y + self.levelGraphic.height + self.CamPos(self.currentCameraPosition).y) / self.sSize.y);

                //sprpos -= new Vector4(17f / self.sSize.x, 18f / self.sSize.y, 17f / self.sSize.x, 18f / self.sSize.y) * (1f - 1f / zoom);
                sprpos -= center;
				sprpos *= zoom;
				sprpos += center;
				Shader.SetGlobalVector("_spriteRect", sprpos);
				Vector2 zooming = (1f - 1f / zoom) * new Vector2(self.sSize.x / self.room.PixelWidth, self.sSize.y / self.room.PixelHeight);
				Shader.SetGlobalVector("_camInRoomRect", new Vector4(vector.x / self.room.PixelWidth + zooming.x/2f, vector.y / self.room.PixelHeight + zooming.y/2f,
					self.sSize.x / self.room.PixelWidth - zooming.x, self.sSize.y / self.room.PixelHeight - zooming.y));
				Shader.SetGlobalVector("_screenSize", self.sSize);
			}

		}

	}
}