﻿using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal class FourthLayerFix
    {
        internal static void Apply()
        {
            On.PersistentData.ctor += PersistentData_ctor;
            On.RoomCamera.MoveCamera_1 += RoomCamera_MoveCamera_1;
        }

        static private void RoomCamera_MoveCamera_1(On.RoomCamera.orig_MoveCamera_1 orig, RoomCamera self, Room newRoom, int camPos)
        {
            orig(self, newRoom, camPos);

            if (self.bkgwww == null)
            {
                string text = string.Concat(new object[]
                {
                    WorldLoader.FindRoomFileDirectory(newRoom.abstractRoom.name, true),
                    "_",
                    camPos + 1,
                    "_bkg.png"
                });
                Uri uri = new Uri(text);
                if (uri.IsFile && System.IO.File.Exists(uri.LocalPath))
                {
                    Debug.Log("RoomCamera_MoveCamera loading bkg img from: " + text);
                    self.bkgwww = new WWW(text);
                }
                //Debug.Log("RoomCamera_MoveCamera_1 would load from:" + text);
                //Debug.Log("RoomCamera_MoveCamera_1 would load :" + System.IO.File.Exists(text));
                //Debug.Log("RoomCamera_MoveCamera_1 bkgwww real " + (self.bkgwww != null));
            }
        }

        static private void PersistentData_ctor(On.PersistentData.orig_ctor orig, PersistentData self, RainWorld rainWorld)
        {
            self.cameraTextures = new Texture2D[2, 2];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    self.cameraTextures[i, j] = new Texture2D(1400, 800, TextureFormat.ARGB32, false);
                    self.cameraTextures[i, j].anisoLevel = 0;
                    self.cameraTextures[i, j].filterMode = FilterMode.Point;
                    self.cameraTextures[i, j].wrapMode = TextureWrapMode.Clamp;
                    // This part originally loaded the same texture into both atlases
                    // In the normal game, this had no effect, but if it remained, the background
                    // Would always be a copy of the foreground
                    if (j == 0)
                        Futile.atlasManager.LoadAtlasFromTexture("LevelTexture" + ((i != 0) ? i.ToString() : string.Empty), self.cameraTextures[i, j]);
                    else
                        Futile.atlasManager.LoadAtlasFromTexture("BackgroundTexture" + ((i != 0) ? i.ToString() : string.Empty), self.cameraTextures[i, j]);
                }
            }
        }
    }
}