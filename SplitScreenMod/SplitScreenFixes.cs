using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SplitScreenMod
{
    public partial class SplitScreenMod : BaseUnityPlugin
    {
        private Vector2 DialogBox_DrawPos(On.HUD.DialogBox.orig_DrawPos orig, HUD.DialogBox self, float timeStacker)
        {
            if (CurrentSplitMode == SplitMode.SplitVertical && curCamera >= 0 && cameraListeners[curCamera] is CameraListener cl)
            {
                return orig(self, timeStacker) - new Vector2(cl.roomCamera.sSize.x / 4f, 0f);
            }
            return orig(self, timeStacker);
        }

        private void VirtualMicrophone_DrawUpdate(On.VirtualMicrophone.orig_DrawUpdate orig, VirtualMicrophone self, float timeStacker, float timeSpeed)
        {
            if (self.camera.cameraNumber > 0 && self.camera.room == self.camera.game.cameras[0].room)
            {
                self.volumeGroups[0] *= 1f;
                if(self.camera.game.cameras[0].virtualMicrophone is VirtualMicrophone other)
                {
                    self.volumeGroups[0] *= Mathf.InverseLerp(100f, 1000f, (self.listenerPoint - other.listenerPoint).magnitude);
                }
                self.volumeGroups[1] *= 0f;
                self.volumeGroups[2] *= 0f;
            }
            orig(self, timeStacker, timeSpeed);
        }

        private void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
        {
            orig(self);

            if (self.game.roomRealizer == null) return;
            realizer2 = new RoomRealizer(self.game.session.Players[1], self.game.world);
            realizer2.realizedRooms = self.game.roomRealizer.realizedRooms;
            realizer2.recentlyAbstractedRooms = self.game.roomRealizer.recentlyAbstractedRooms;
            realizer2.realizeNeighborCandidates = self.game.roomRealizer.realizeNeighborCandidates;
        }

        private void RainWorldGame_ContinuePaused(On.RainWorldGame.orig_ContinuePaused orig, RainWorldGame self)
        {
            orig(self);
            self.manager.StopSideProcess(self.manager.sideProcesses.FirstOrDefault(t => t is Menu.PauseMenu));
        }

        bool inpause; // non reentrant
        private void PauseMenu_ctor(On.Menu.PauseMenu.orig_ctor orig, Menu.PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            orig(self, manager, game);

            if (!inpause)
            {
                if (CurrentSplitMode == SplitMode.SplitVertical)
                {
                    self.container.SetPosition(manager.rainWorld.screenSize.x / 4f, 0);
                    inpause = true;
                    try
                    {
                        var otherpause = new Menu.PauseMenu(manager, game);
                        otherpause.container.SetPosition(-6000 - manager.rainWorld.screenSize.x / 4f, 0);
                        manager.sideProcesses.Add(otherpause);
                    }
                    finally
                    {
                        inpause = false;
                    }
                }
                else if (CurrentSplitMode == SplitMode.SplitHorizontal)
                {
                    self.container.SetPosition(0, -manager.rainWorld.screenSize.y / 4f);
                    inpause = true;
                    try
                    {
                        var otherpause = new Menu.PauseMenu(manager, game);
                        otherpause.container.SetPosition(-6000, + manager.rainWorld.screenSize.y / 4f);
                        manager.sideProcesses.Add(otherpause);
                    }
                    finally
                    {
                        inpause = false;
                    }
                }
            }
        }

        private void RoomCamera_FireUpSinglePlayerHUD(On.RoomCamera.orig_FireUpSinglePlayerHUD orig, RoomCamera self, Player player)
        {
            orig(self, player);
            OffsetHud(self);
        }

        private void OffsetHud(RoomCamera self)
        {
            if (self.hud != null)
            {
                if (CurrentSplitMode != SplitMode.NoSplit)
                {
                    Vector2 ex = CurrentSplitMode == SplitMode.SplitHorizontal ? new Vector2(0, self.sSize.y / 4f) : new Vector2(self.sSize.x / 4f, 0);
                    //if (self.cameraNumber == 1) ex = -ex;
                    self.ReturnFContainer("HUD").SetPosition(-self.offset + ex);
                    self.ReturnFContainer("HUD2").SetPosition(-self.offset + ex);
                    self.hud.map.inFrontContainer.SetPosition(-self.offset + ex);
                }
                else
                {
                    self.ReturnFContainer("HUD").SetPosition(-self.offset);
                    self.ReturnFContainer("HUD2").SetPosition(-self.offset);
                    self.hud.map.inFrontContainer.SetPosition(-self.offset);
                }
            }
        }

        // some sprites are initailized at 0/0 and never moved
        // moving 'all' sprites messes up trimeshes
        private void SpriteLeaser_ctor(On.RoomCamera.SpriteLeaser.orig_ctor orig, RoomCamera.SpriteLeaser self, IDrawable obj, RoomCamera rCam)
        {
            orig(self, obj, rCam);
            if (self.sprites != null && rCam.cameraNumber > 0) foreach (var s in self.sprites)
                {
                    if (s.GetType() == typeof(FSprite)) // move basic sprites only, don't move anything else because it could do vertex manip instead and that'll move it
                        s.SetPosition(s.GetPosition() + rCam.offset);
                }
        }

        // cull should account for more cams
        public delegate bool delget_ShouldBeCulled(GraphicsModule gm);
        public bool get_ShouldBeCulled(delget_ShouldBeCulled orig, GraphicsModule gm)
        {
            return orig(gm) && !gm.owner.room.game.cameras[1].PositionCurrentlyVisible(gm.owner.firstChunk.pos, gm.cullRange + ((!gm.culled) ? 100f : 0f), true) && !gm.owner.room.game.cameras[1].PositionVisibleInNextScreen(gm.owner.firstChunk.pos, (!gm.culled) ? 100f : 50f, true);
        }

        // stup' wa'er
        private void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (rCam.cameraNumber > 0)
            {
                camPos -= rCam.offset;
                orig(self, sLeaser, rCam, timeStacker, camPos);
                foreach (var s in sLeaser.sprites) if (s is WaterTriangleMesh) { s.SetPosition(-rCam.offset); };// if(s.isMatrixDirty) s.UpdateMatrix(); };
            }
            else
            {
                orig(self, sLeaser, rCam, timeStacker, camPos);
            }
        }
    }
}
