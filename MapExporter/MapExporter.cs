using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using Partiality.Modloader;
using System.IO;
using RWCustom;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace MapExporter
{
    public class MapExporter : PartialityMod
    {
        public MapExporter()
        {
            this.ModID = "MapExporter";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static MapExporter instance;
        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            On.RainWorld.Start += RainWorld_Start; // FUCK compatibility just run my hooks
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            On.RainWorld.LoadSetupValues += RainWorld_LoadSetupValues;
            On.World.SpawnGhost += World_SpawnGhost;

            On.RainWorldGame.ctor += RainWorldGame_ctor;
            On.RainWorldGame.Update += RainWorldGame_Update;

            On.OverWorld.ctor += OverWorld_ctor;
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;

            On.Room.ReadyForAI += Room_ReadyForAI;
            On.Room.Loaded += Room_Loaded;

            On.VoidSpawnGraphics.DrawSprites += VoidSpawnGraphics_DrawSprites;

            orig(self);
        }

        private void VoidSpawnGraphics_DrawSprites(On.VoidSpawnGraphics.orig_DrawSprites orig, VoidSpawnGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            //youre code bad
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].isVisible = false;
            }
        }

        private void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            for (int i = self.roomSettings.effects.Count - 1; i >= 0; i--)
            {
                if (self.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.VoidSea) self.roomSettings.effects.RemoveAt(i);
            }
            orig(self);
        }

        private void Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
        {
            string oldname = self.abstractRoom.name;
            if (self.abstractRoom.name == "SS_AI" || self.abstractRoom.name == "SL_AI") self.abstractRoom.name = "XXX";
            orig(self);
            self.abstractRoom.name = oldname;
        }

        private void OverWorld_ctor(On.OverWorld.orig_ctor orig, OverWorld self, RainWorldGame game)
        {
            //game.startingRoom = "";
            orig(self, game);
        }

        private void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
        {
            return;
        }

        private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            self.session.Players[0].Room.RemoveEntity(self.session.Players[0]);
            self.session.Players.Clear();
            self.cameras[0].followAbstractCreature = null;
            self.roomRealizer.followCreature = null;
            self.roomRealizer = null;

            self.GetStorySession.saveState.theGlow = false;
            self.rainWorld.setup.playerGlowing = false;


            Debug.Log("RW Ctor done, starting capture task");
            //self.overWorld.activeWorld.activeRooms[0].abstractRoom.Abstractize();


            captureTask = CaptureTask(self);
            captureTask.MoveNext();
        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            captureTask.MoveNext();
        }

        private void World_SpawnGhost(On.World.orig_SpawnGhost orig, World self)
        {
            return;
        }

        private RainWorldGame.SetupValues RainWorld_LoadSetupValues(On.RainWorld.orig_LoadSetupValues orig, bool distributionBuild)
        {
            var setup = orig(false);

            setup.loadAllAmbientSounds = false;
            setup.playMusic = false;
            setup.loadProg = false;
            setup.loadGame = false;
            setup.startScreen = false;
            setup.cycleStartUp = false;
            setup.player1 = false;
            setup.worldCreaturesSpawn = false;
            setup.singlePlayerChar = 0;
            return setup;
        }

        string PathOfScreenshot(string region, string roomname, int num)
        {
            string path = Custom.RootFolderDirectory() + "Screenshots" + Path.DirectorySeparatorChar + region;
            Directory.CreateDirectory(path);
            return path + Path.DirectorySeparatorChar + roomname + "_" + num.ToString() + ".png";
        }

        System.Collections.IEnumerator captureTask;
        private System.Collections.IEnumerator CaptureTask(RainWorldGame game)
        {
            Debug.Log("capture task start");
            //while (game.cameras[0].www != null) yield return null;
            while (game.cameras[0].room == null || !game.cameras[0].room.ReadyForPlayer) yield return null;
            for (int i = 0; i < 30; i++) yield return null;

            foreach (var regionstr in Menu.FastTravelScreen.GetRegionOrder())
            {
                Debug.Log("capture task entering " + regionstr);
                // wrong region
                if (game.overWorld.activeWorld == null || game.overWorld.activeWorld.region.name != regionstr)
                {
                    Debug.Log("capture task loading " + regionstr);
                    game.overWorld.LoadWorld(regionstr, game.overWorld.PlayerCharacterNumber, false);
                    Debug.Log("capture task loaded " + regionstr);
                }

                foreach(var room in game.overWorld.activeWorld.abstractRooms)
                {
                    if (room.offScreenDen) continue;
                    Debug.Log("capture task room " + room.name);
                    game.overWorld.activeWorld.ActivateRoom(room);
                    while (!room.realizedRoom.ReadyForPlayer) yield return null;
                    game.cameras[0].MoveCamera(room.realizedRoom, 0);
                    yield return null;
                    for (int i = 0; i < room.realizedRoom.cameraPositions.Length; i++)
                    {
                        game.cameras[0].MoveCamera(i);
                        while(game.cameras[0].www != null) yield return null;
                        yield return null;
                        UnityEngine.Application.CaptureScreenshot(PathOfScreenshot(regionstr, room.name, i));
                    }
                    room.Abstractize();
                    yield return null;
                }

            }


        }
    }
}
