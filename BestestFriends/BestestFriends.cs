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
        public static string currentMovieName;
        public static bool anyCat = true;

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
            if (self.folderName == "bestestfriends" && self.sprite._atlas._texture is MovieTexture movieTexture && movieTexture.isReadyToPlay && !movieTexture.isPlaying)
            {
                self.sprite.width = 1366;
                self.sprite.height = 768;
                self.sprite.shader = self.menu.manager.rainWorld.Shaders["Basic"];
                movieTexture.loop = true;
                movieTexture.Play();
            }
            orig(self);
        }

        private void MenuIllustration_LoadFile_1(On.Menu.MenuIllustration.orig_LoadFile_1 orig, Menu.MenuIllustration self, string folder)
        {
            if(self.folderName == "bestestfriends")
            {

                string fileName = Path.GetTempPath() + "bestestfriends.ogv";
                File.Delete(fileName); // safe ? its a specific enough filename
                var input = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(BestestFriends).Namespace + ".Resources." + self.fileName + ".ogv");
                var output = File.OpenWrite(fileName);
                try{
                    byte[] buffer = new byte[81920];
                    int read;
                    while ((read = input.Read(buffer, 0, 81920)) > 0)
                    {
                        output.Write(buffer, 0, read);
                    }
                }
                finally
                {
                    input.Close();
                    output.Close();
                }
                self.www = new WWW("file:///" + fileName);
                self.texture = new Texture2D(1, 1, TextureFormat.ARGB32, false); // uuuugh
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
                // no reference to anything I needed to either set a static of spam enumextends
                // set with static for now
                self.AddIllustration(new Menu.MenuIllustration(self.menu, self, "bestestfriends", currentMovieName, new Vector2(683f, 384f), false, true));
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
            orig(self, game, survived, newMalnourished); // Sets friend in den, one per player, but we need all the lizards <3

            if(survived)
            {
                List<KeyValuePair<int, CreatureTemplate.Type>> pairings = new List<KeyValuePair<int, CreatureTemplate.Type>>();
                //List<AbstractCreature> lizardsInDen = new List<AbstractCreature>();
                for (int k = 0; k < game.Players.Count; k++)
                {
                    if (game.Players[k] != null)
                    {
                        for (int l = 0; l < game.world.GetAbstractRoom(game.Players[k].pos).creatures.Count; l++)
                        {
                            if (game.world.GetAbstractRoom(game.Players[k].pos).creatures[l].state.alive 
                                && game.world.GetAbstractRoom(game.Players[k].pos).creatures[l].state.socialMemory != null 
                                && game.world.GetAbstractRoom(game.Players[k].pos).creatures[l].realizedCreature != null 
                                && game.world.GetAbstractRoom(game.Players[k].pos).creatures[l].abstractAI != null 
                                && game.world.GetAbstractRoom(game.Players[k].pos).creatures[l].abstractAI.RealAI != null 
                                && game.world.GetAbstractRoom(game.Players[k].pos).creatures[l].abstractAI.RealAI.friendTracker != null 
                                && game.world.GetAbstractRoom(game.Players[k].pos).creatures[l].abstractAI.RealAI.friendTracker.friend != null 
                                && game.world.GetAbstractRoom(game.Players[k].pos).creatures[l].abstractAI.RealAI.friendTracker.friend == game.Players[k].realizedCreature 
                                && game.world.GetAbstractRoom(game.Players[k].pos).creatures[l].state.socialMemory.GetLike(game.Players[k].ID) > 0f)
                            {
                                pairings.Add(new KeyValuePair<int, CreatureTemplate.Type>(
                                    (game.Players[k].state as PlayerState).slugcatCharacter, 
                                    game.world.GetAbstractRoom(game.Players[k].pos).creatures[l].creatureTemplate.type));
                            }
                        }
                    }
                }
                if (pairings.Count == 0) return;
                currentMovieName = MovieForList(pairings);
                if (string.IsNullOrEmpty(currentMovieName)) return;
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

        public static string MovieForList(List<KeyValuePair<int, CreatureTemplate.Type>> pairings)
        {
            string movie = null;
            movie = GetOneOnOneScene(pairings, false);
            if(movie == null && anyCat) movie = GetOneOnOneScene(pairings, true);
            return movie;
        }

        public static string GetOneOnOneScene(List<KeyValuePair<int, CreatureTemplate.Type>> pairings, bool fallback)
        {
            foreach (var item in pairings)
            {
                if (item.Value == CreatureTemplate.Type.CyanLizard && (item.Key == 2 || fallback))
                {
                    return "hunterxcyan";
                }
            }
            return null;
        }
    }
}
