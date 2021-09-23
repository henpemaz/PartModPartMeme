using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal class CGCutscenes
    {
        internal static void ExitToCGSpiralSlideshow(RainWorldGame game)
        {
            ConcealedGarden.progression.transfurred = true;
            game.GetStorySession.saveState.deathPersistentSaveData.karma = 0;
            game.ExitGame(false, false);
            game.manager.nextSlideshow = EnumExt_CGCutscenes.CGSpiral;
            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.SlideShow, 2.5f);
        }

        public static class EnumExt_CGCutscenes
        {
#pragma warning disable 0649
            public static DreamsState.DreamID CGDrowningDream;

            public static Menu.MenuScene.SceneID CGDrowningDreamScene;

            public static Menu.SlideShow.SlideShowID CGSpiral;
            public static Menu.MenuScene.SceneID CGSpiralScene1;
            public static Menu.MenuScene.SceneID CGSpiralScene2;
            public static Menu.MenuScene.SceneID CGSpiralScene3;
#pragma warning restore 0649
        }

        internal static void Apply()
        {
            On.SaveState.SessionEnded += SaveState_SessionEnded;
            On.Menu.DreamScreen.SceneFromDream += DreamScreen_SceneFromDream;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
            On.Menu.InteractiveMenuScene.Update += InteractiveMenuScene_Update;
            On.Menu.SlideShow.ctor += SlideShow_ctor;
            On.Menu.SlideShowMenuScene.ctor += SlideShowMenuScene_ctor;
            On.Menu.SlideShowMenuScene.ApplySceneSpecificAlphas += SlideShowMenuScene_ApplySceneSpecificAlphas;

            On.Menu.SlideShow.CommunicateWithUpcomingProcess += SlideShow_CommunicateWithUpcomingProcess;
        }

        private static void SlideShow_CommunicateWithUpcomingProcess(On.Menu.SlideShow.orig_CommunicateWithUpcomingProcess orig, Menu.SlideShow self, MainLoopProcess nextProcess)
        {
            orig(self, nextProcess);

            if (nextProcess is RainWorldGame && (self.slideShowID == EnumExt_CGCutscenes.CGSpiral))
            {
                self.manager.CueAchievement(CGAchievementManager.EnumExt_CGAchievementManager.CGTransfurred, 2f);
            }
        }

        private static void SlideShowMenuScene_ApplySceneSpecificAlphas(On.Menu.SlideShowMenuScene.orig_ApplySceneSpecificAlphas orig, Menu.SlideShowMenuScene self)
        {
            orig(self);
            if (self.sceneID == EnumExt_CGCutscenes.CGSpiralScene1)
            {
                // "7-bkge"
                // "6-bkg"
                // "5-reflection"
                // "4-circle2"
                // "3-circle1"
                // "2-slug"
                // "1-fuss"
                // center is new Vector2(-215f, -203f)
                self.depthIllustrations[5].pos.x = Mathf.Lerp(-350f, -215f, self.displayTime);
                self.depthIllustrations[5].pos.y = Mathf.Lerp(-180f, -203f, self.displayTime);
                self.depthIllustrations[4].sprite.SetPosition(-215f, -203f);
                self.depthIllustrations[4].sprite.rotation = 0f;
                self.depthIllustrations[4].sprite.RotateAroundPointAbsolute(self.depthIllustrations[4].sprite.GlobalToLocal(new Vector2(916f,364f)), 100 - 200f * RWCustom.Custom.SCurve(Mathf.InverseLerp(-1f, 1f, self.displayTime), 0.6f));
                self.depthIllustrations[4].pos = self.depthIllustrations[4].sprite.GetPosition();
                self.depthIllustrations[4].lastPos = self.depthIllustrations[4].sprite.GetPosition();
                self.depthIllustrations[3].sprite.SetPosition(-215f, -203f);
                self.depthIllustrations[3].sprite.rotation = 0f;
                self.depthIllustrations[3].sprite.RotateAroundPointAbsolute(self.depthIllustrations[3].sprite.GlobalToLocal(new Vector2(916f, 364f)), 135 - 180f * RWCustom.Custom.SCurve(Mathf.InverseLerp(-1f, 1f, self.displayTime), 0.6f));
                self.depthIllustrations[3].pos = self.depthIllustrations[3].sprite.GetPosition();
                self.depthIllustrations[3].lastPos = self.depthIllustrations[3].sprite.GetPosition();
                self.depthIllustrations[2].pos.x = Mathf.Lerp(-100f, -215f, Mathf.InverseLerp(0.3f, 1f, self.displayTime));
                self.depthIllustrations[2].pos.y = Mathf.Lerp(-225f, -203f, RWCustom.Custom.SCurve(Mathf.InverseLerp(0.3f, 1f, self.displayTime), 0.6f));
                self.depthIllustrations[2].setAlpha = new float?(RWCustom.Custom.SCurve(Mathf.InverseLerp(0.3f, 0.8f, self.displayTime), 0.6f));
                self.depthIllustrations[3].setAlpha = new float?(RWCustom.Custom.SCurve(Mathf.InverseLerp(-0.3f, 0.5f, self.displayTime), 0.8f));
            }
            else if (self.sceneID == EnumExt_CGCutscenes.CGSpiralScene2)
            {
                //"5-cat",
                //"4-aura"
                //"3-s2", 
                //"2-s1", 
                //"1-fuss"
                // Vector2(-320f, -442f)
                self.depthIllustrations[0].pos.y = Mathf.Lerp(-200f, -502f, self.displayTime);
                self.depthIllustrations[1].pos.y = Mathf.Lerp(-220f, -522f, self.displayTime);

                float scale = 1.0f - 0.35f * Mathf.InverseLerp(0f, 1f, self.displayTime);
                self.depthIllustrations[2].sprite.rotation = 0f;
                self.depthIllustrations[2].sprite.scaleX = 1f;
                self.depthIllustrations[2].sprite.scaleY = 0.5f;
                self.depthIllustrations[2].sprite.SetPosition(-320f, -442f);
                self.depthIllustrations[2].sprite.ScaleAroundPointAbsolute(self.depthIllustrations[2].sprite.GlobalToLocal(new Vector2(683f, 384f)), scale, scale);
                self.depthIllustrations[2].sprite.RotateAroundPointAbsolute(self.depthIllustrations[2].sprite.GlobalToLocal(new Vector2(683f, 384f)), 20 - 40f * Mathf.InverseLerp(0f, 1f, self.displayTime));
                self.depthIllustrations[2].pos = self.depthIllustrations[2].sprite.GetPosition();
                self.depthIllustrations[2].lastPos = self.depthIllustrations[2].sprite.GetPosition();
                self.depthIllustrations[2].setAlpha = new float?(scale);

                self.depthIllustrations[3].sprite.rotation = 0f;
                self.depthIllustrations[3].sprite.scaleX = 1f;
                self.depthIllustrations[3].sprite.scaleY = 0.5f;
                self.depthIllustrations[3].sprite.SetPosition(-320f, -442f);
                self.depthIllustrations[3].sprite.ScaleAroundPointAbsolute(self.depthIllustrations[3].sprite.GlobalToLocal(new Vector2(683f, 384f)), scale, scale);
                self.depthIllustrations[3].sprite.RotateAroundPointAbsolute(self.depthIllustrations[3].sprite.GlobalToLocal(new Vector2(683f, 384f)), 20 - 60f * Mathf.InverseLerp(0f, 1f, self.displayTime));
                self.depthIllustrations[3].pos = self.depthIllustrations[3].sprite.GetPosition();
                self.depthIllustrations[3].lastPos = self.depthIllustrations[3].sprite.GetPosition();
                self.depthIllustrations[3].setAlpha = new float?(scale);

            }
            else if (self.sceneID == EnumExt_CGCutscenes.CGSpiralScene3)
            {
                //"3-glow"
                //"2-scat"
                //"1-aura"
                // Vector2(-320f, -442f)

                self.depthIllustrations[0].pos.y = Mathf.Lerp(-802f, -462f, self.displayTime);
                self.depthIllustrations[1].pos.y = Mathf.Lerp(-782f, -462f, self.displayTime);

            }
        }

        private static void SlideShowMenuScene_ctor(On.Menu.SlideShowMenuScene.orig_ctor orig, Menu.SlideShowMenuScene self, Menu.Menu menu, Menu.MenuObject owner, Menu.MenuScene.SceneID sceneID)
        {
            orig(self, menu, owner, sceneID);
            if (sceneID == EnumExt_CGCutscenes.CGSpiralScene1)
            {
                self.cameraMovementPoints.Clear();
                self.cameraMovementPoints.Add(new Vector3(300, 0, 0.15f));
                self.cameraMovementPoints.Add(new Vector3(140, 0, 0.25f));
                self.cameraMovementPoints.Add(new Vector3(-20, 0, 0.4f));
            }
            else if (sceneID == EnumExt_CGCutscenes.CGSpiralScene2)
            {
                self.cameraMovementPoints.Clear();
                self.cameraMovementPoints.Add(new Vector3(0, -200, 0.6f));
                self.cameraMovementPoints.Add(new Vector3(0, 20, 0.3f));
            }
            else if (sceneID == EnumExt_CGCutscenes.CGSpiralScene3)
            {
                self.cameraMovementPoints.Clear();
                self.cameraMovementPoints.Add(new Vector3(0, 300, 0.3f));
                self.cameraMovementPoints.Add(new Vector3(0, -20, 0.6f));
            }
        }

        // Trigger drowning dream
        // called during Win, after dreamstate.EndOfCycleProgress, but has better params
        private static void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            orig(self, game, survived, newMalnourished);
            if (survived && self.dreamsState != null && !ConcealedGarden.progression.fishDream)
            {
                foreach (var item in game.world.GetAbstractRoom(game.Players[0].pos).entities)
                {
                    if(item is DataPearl.AbstractDataPearl porl)
                    {
                        string porlType = porl.dataPearlType.ToString();
                        if(porlType =="Concealed_Garden_programmer" || porlType == "Concealed_Garden_programmer_alt")
                        {
                            // slept with porl!
                            ConcealedGarden.progression.fishDream = true;
                            Debug.Log("CG Queued up fish dream");
                            self.dreamsState.InitiateEventDream(EnumExt_CGCutscenes.CGDrowningDream);
                            self.dreamsState.upcomingDream = self.dreamsState.eventDream;
                        }
                    }
                }
            }
        }

        // Dreams need scene
        private static Menu.MenuScene.SceneID DreamScreen_SceneFromDream(On.Menu.DreamScreen.orig_SceneFromDream orig, Menu.DreamScreen self, DreamsState.DreamID dreamID)
        {
            if (dreamID == EnumExt_CGCutscenes.CGDrowningDream)
            {
                return EnumExt_CGCutscenes.CGDrowningDreamScene;
            }
            return orig(self, dreamID);
        }

        // Load slideshows
        private static void SlideShow_ctor(On.Menu.SlideShow.orig_ctor orig, Menu.SlideShow self, ProcessManager manager, Menu.SlideShow.SlideShowID slideShowID)
        {
            FLabel loadingLabel = manager.loadingLabel;
            orig(self, manager, slideShowID);
            if(slideShowID == EnumExt_CGCutscenes.CGSpiral)
            {
                self.current = -1;
                // Undo RemoveLoadingLabel and NextScene thanks slimecubed
                if(loadingLabel != null)
                {
                    manager.loadingLabel = loadingLabel;
                    Futile.stage.AddChild(loadingLabel);
                }
                if (manager.musicPlayer != null && manager.musicPlayer.song is CGSongSFX songsfx) // LRU song is supposedly playing there
                {
                    songsfx.destroyCounter = 150 - 28 * 40;
                }
                self.playList.Add(new Menu.SlideShow.Scene(Menu.MenuScene.SceneID.Empty, 0f, 0f, 0f));
                self.playList.Add(new Menu.SlideShow.Scene(EnumExt_CGCutscenes.CGSpiralScene1, 0.4f, 3f, 8f));
                self.playList.Add(new Menu.SlideShow.Scene(EnumExt_CGCutscenes.CGSpiralScene2, 10f, 12f, 15f));
                self.playList.Add(new Menu.SlideShow.Scene(EnumExt_CGCutscenes.CGSpiralScene3, 20f, 25f, 28f));
                self.playList.Add(new Menu.SlideShow.Scene(Menu.MenuScene.SceneID.Empty, 30f, 0f, 0f));

                self.preloadedScenes = new Menu.SlideShowMenuScene[self.playList.Count];
                for (int k = 0; k < self.preloadedScenes.Length; k++)
                {
                    self.preloadedScenes[k] = new Menu.SlideShowMenuScene(self, self.pages[0], self.playList[k].sceneID);
                    self.preloadedScenes[k].Hide();
                }
                manager.RemoveLoadingLabel();
                self.nextProcess = ProcessManager.ProcessID.Game;
                self.NextScene();
            }
        }

        // Animate dreams
        private static void InteractiveMenuScene_Update(On.Menu.InteractiveMenuScene.orig_Update orig, Menu.InteractiveMenuScene self)
        {
            orig(self);
            if(self.sceneID == EnumExt_CGCutscenes.CGDrowningDreamScene)
            {
                if (self.timer < 0)
                {
                    self.timer++;
                }
                // (-306f, -416f)
                // lerp into place, funny S curves
                float factor = Mathf.InverseLerp(-100f, 240f, (float)self.timer);
                factor = -factor * (factor - 2); // magic quatratic easing
                self.depthIllustrations[0].pos.y = Mathf.Lerp(-616f, -416f, factor);
                self.depthIllustrations[1].pos.y = Mathf.Lerp(0f, -416f, factor);
                self.depthIllustrations[2].pos.x = Mathf.Lerp(-260f, -352f, 0.5f + 0.5f * Mathf.Sin((float)self.timer / 120f));
                self.depthIllustrations[3].pos.y = Mathf.Lerp(-816f, -416f, factor);
            }
        }

        // build custom scenes
        private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, Menu.MenuScene self)
        {
            if (self.sceneID == EnumExt_CGCutscenes.CGDrowningDreamScene)
            {
                self.sceneFolder = "Scenes" + System.IO.Path.DirectorySeparatorChar + "CGDrowningDream";
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "4-bg", new Vector2(-306f, -416f), 4f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "3-slug", new Vector2(-306f, -416f), 3f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "2-fishee", new Vector2(-306f, -416f), 2f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "1-fg", new Vector2(-306f, -416f), 1f, Menu.MenuDepthIllustration.MenuShader.Normal));
                (self as Menu.InteractiveMenuScene).idleDepths = new System.Collections.Generic.List<float>();
                (self as Menu.InteractiveMenuScene).idleDepths.Add(3.3f);
                (self as Menu.InteractiveMenuScene).idleDepths.Add(2.7f);
                (self as Menu.InteractiveMenuScene).idleDepths.Add(1.8f);
                (self as Menu.InteractiveMenuScene).idleDepths.Add(1.7f);
                (self as Menu.InteractiveMenuScene).idleDepths.Add(1.6f);
                (self as Menu.InteractiveMenuScene).idleDepths.Add(1.2f);
            }
            else if (self.sceneID == EnumExt_CGCutscenes.CGSpiralScene1)
            {
                self.sceneFolder = "Scenes" + System.IO.Path.DirectorySeparatorChar + "CGSpiral1";
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "7-bkge", new Vector2(-215f, -203f), 6f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "6-bkg", new Vector2(-215f, -203f), 4f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "5-reflection", new Vector2(-215f, -203f), 3.5f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "4-circle2", new Vector2(-215f, -203f), 3f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "3-circle1", new Vector2(-215f, -203f), 3f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "2-slug", new Vector2(-215f, -203f), 2f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "1-fuss", new Vector2(-215f, -203f), 1f, Menu.MenuDepthIllustration.MenuShader.Normal));
            }
            else if (self.sceneID == EnumExt_CGCutscenes.CGSpiralScene2)
            {
                self.sceneFolder = "Scenes" + System.IO.Path.DirectorySeparatorChar + "CGSpiral2";
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "5-cat", new Vector2(-320f, -442f), 4f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "4-aura", new Vector2(-320f, -442f), 4f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "3-s2", new Vector2(-320f, -442f), 3.5f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "2-s1", new Vector2(-320f, -442f), 3.3f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "1-fuss", new Vector2(-320f, -442f), 1f, Menu.MenuDepthIllustration.MenuShader.Normal));
            }
            else if (self.sceneID == EnumExt_CGCutscenes.CGSpiralScene3)
            {
                self.sceneFolder = "Scenes" + System.IO.Path.DirectorySeparatorChar + "CGSpiral3";
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "3-glow", new Vector2(-320f, -442f), 4f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "2-scat", new Vector2(-320f, -442f), 3f, Menu.MenuDepthIllustration.MenuShader.Normal));
                self.AddIllustration(new Menu.MenuDepthIllustration(self.menu, self, self.sceneFolder, "1-aura", new Vector2(-320f, -442f), 1f, Menu.MenuDepthIllustration.MenuShader.Normal));
            }
            else
                orig(self);
        }
    }
}