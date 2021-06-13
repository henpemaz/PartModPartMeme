using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using System.IO;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]


namespace BestestFriends
{
    public class BestestFriends : PartialityMod
    {
        public BestestFriends()
        {
            this.ModID = "BestestFriends";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public static class EnumExt_BestestFriends
        {
            public static DreamsState.DreamID bestestFriendsDream;

            public static Menu.MenuScene.SceneID bestestFriendsScene;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            On.SaveState.SessionEnded += SaveState_SessionEnded;
            On.Menu.DreamScreen.SceneFromDream += DreamScreen_SceneFromDream;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
            On.Menu.MenuIllustration.LoadFile_1 += MenuIllustration_LoadFile_1;
            On.Menu.MenuIllustration.Update += MenuIllustration_Update;
        }

        private void MenuIllustration_Update(On.Menu.MenuIllustration.orig_Update orig, Menu.MenuIllustration self)
        {
            if (self.fileName == "bestestfriends" && self.sprite?._atlas?._texture is MovieTexture movieTexture && movieTexture.isReadyToPlay && !movieTexture.isPlaying)
            {
                self.sprite.width = 1280;
                self.sprite.height = 720;
                self.sprite.shader = self.menu.manager.rainWorld.Shaders["Basic"];
                movieTexture.loop = true;
                movieTexture.Play();
            }
            orig(self);
        }

        private void MenuIllustration_LoadFile_1(On.Menu.MenuIllustration.orig_LoadFile_1 orig, Menu.MenuIllustration self, string folder)
        {
            if(self.fileName == "bestestfriends")
            {
                self.www = new WWW(string.Concat(new object[]
            {
                "file:///",
                RWCustom.Custom.RootFolderDirectory(),
                "cyan2out.ogv"
            }));
                self.texture = new Texture2D(1280, 720, TextureFormat.ARGB32, false); // uuuugh
                HeavyTexturesCache.LoadAndCacheAtlasFromTexture(self.fileName, self.www.movie);
                self.www = null;
            }
            else orig(self, folder);
        }

        private void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, Menu.MenuScene self)
        {
            orig(self);
            if(self.sceneID == EnumExt_BestestFriends.bestestFriendsScene)
            {
                self.AddIllustration(new Menu.MenuIllustration(self.menu, self, null, "bestestfriends", new Vector2(683f, 384f), false, true));
            }
        }

        private Menu.MenuScene.SceneID DreamScreen_SceneFromDream(On.Menu.DreamScreen.orig_SceneFromDream orig, Menu.DreamScreen self, DreamsState.DreamID dreamID)
        {
            if(dreamID == EnumExt_BestestFriends.bestestFriendsDream)
            {

                return EnumExt_BestestFriends.bestestFriendsScene;
            }
            return orig(self, dreamID);
        }

        private void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            orig(self, game, survived, newMalnourished); // Sets friend in den

            //if(survived && self.dreamsState && )
            bool needsDetour = false;
            if (self.dreamsState == null)
            {
                self.dreamsState = new DreamsState(); // doesnt get serialized
                needsDetour = true;
            }
            self.dreamsState.eventDream = new DreamsState.DreamID?(EnumExt_BestestFriends.bestestFriendsDream);
            self.dreamsState.upcomingDream = new DreamsState.DreamID?(EnumExt_BestestFriends.bestestFriendsDream);
            Debug.LogError("QUEUED DREAM " + self.dreamsState.eventDream.ToString());
            if (needsDetour)
            {
                game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Dream);
            }
        }


    }
}
