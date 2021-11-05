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
using MonoMod.RuntimeDetour;

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
            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            new Hook(typeof(RainWorldGame).GetProperty("TimeSpeedFac").GetGetMethod(), typeof(MapExporter).GetMethod("RainWorldGame_ZeroProperty"), this);
            new Hook(typeof(RainWorldGame).GetProperty("InitialBlackSeconds").GetGetMethod(), typeof(MapExporter).GetMethod("RainWorldGame_ZeroProperty"), this);
            new Hook(typeof(RainWorldGame).GetProperty("FadeInTime").GetGetMethod(), typeof(MapExporter).GetMethod("RainWorldGame_ZeroProperty"), this);

            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded;

            On.Room.ReadyForAI += Room_ReadyForAI;
            On.Room.Loaded += Room_Loaded;
            On.Room.ScreenMovement += Room_ScreenMovement;

            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;

            On.VoidSpawnGraphics.DrawSprites += VoidSpawnGraphics_DrawSprites;

            On.AntiGravity.BrokenAntiGravity.ctor += BrokenAntiGravity_ctor;

            orig(self);
        }

        private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            if(self.room != null && self.room.shortcutsBlinking != null) self.room.shortcutsBlinking = new float[self.room.shortcuts.Length, 4];
            orig(self, timeStacker, timeSpeed);
        }

        private void Room_ScreenMovement(On.Room.orig_ScreenMovement orig, Room self, Vector2? pos, Vector2 bump, float shake)
        {
            return;
        }
        
        private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            self.myTimeStacker += 2f;
            orig(self, dt);
        }

        private void BrokenAntiGravity_ctor(On.AntiGravity.BrokenAntiGravity.orig_ctor orig, AntiGravity.BrokenAntiGravity self, int cycleMin, int cycleMax, RainWorldGame game)
        {
            orig(self, cycleMin, cycleMax, game);
            self.counter = 40000;
        }

        public delegate float orig_PropertyToZero(RainWorldGame self);
        public float RainWorldGame_ZeroProperty(orig_PropertyToZero orig, RainWorldGame self)
        {
            return 0f;
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
            // this broke CRS somehow smh
            //setup.loadProg = false;
            //setup.loadGame = false;

            setup.cycleTimeMax = 10000;
            setup.cycleTimeMin = 10000;

            setup.gravityFlickerCycleMin = 10000;
            setup.gravityFlickerCycleMax = 10000;


            setup.startScreen = false;
            setup.cycleStartUp = false;


            setup.player1 = false;
            setup.worldCreaturesSpawn = false;
            setup.singlePlayerChar = 0;


            return setup;
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
                if (self.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.VoidSea) self.roomSettings.effects.RemoveAt(i); // breaks with no player
            }
            orig(self);
        }

        private void Room_ReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
        {
            string oldname = self.abstractRoom.name;
            if (self.abstractRoom.name == "SS_AI" || self.abstractRoom.name == "SL_AI") self.abstractRoom.name = "XXX"; // oracle breaks w no player
            orig(self);
            self.abstractRoom.name = oldname;
        }

        private void OverWorld_WorldLoaded(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
        {
            return; // orig assumes a gate
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
            self.GetStorySession.saveState.deathPersistentSaveData.KarmaFlowerMessage = true;
            self.GetStorySession.saveState.deathPersistentSaveData.ScavMerchantMessage = true;
            self.GetStorySession.saveState.deathPersistentSaveData.ScavTollMessage = true;

            Debug.Log("RW Ctor done, starting capture task");
            //self.overWorld.activeWorld.activeRooms[0].abstractRoom.Abstractize();

            captureTask = CaptureTask(self);
            //captureTask.MoveNext();
        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            captureTask.MoveNext();
        }

        string PathOfRegion(string region)
        {
            string path = Custom.RootFolderDirectory() + "Export" + Path.DirectorySeparatorChar + region;
            Directory.CreateDirectory(path);
            return path;
        }

        string PathOfRoom(string region, string roomname)
        {
            return PathOfRegion(region) + Path.DirectorySeparatorChar + roomname;
        }

        string PathOfScreenshot(string region, string roomname, int num)
        {
            return PathOfRoom(region, roomname) + "_" + num.ToString() + ".png";
        }

        System.Collections.IEnumerator captureTask;
        private System.Collections.IEnumerator CaptureTask(RainWorldGame game)
        {
            Debug.Log("capture task start");
            // 1st camera transition is a bit whack ? give it a sec
            //while (game.cameras[0].www != null) yield return null;
            while (game.cameras[0].room == null || !game.cameras[0].room.ReadyForPlayer) yield return null;
            for (int i = 0; i < 30; i++) yield return null;

            bool skipped = true;
            foreach (var regionstr in Menu.FastTravelScreen.GetRegionOrder())
            {
                if(!skipped && regionstr != "LP")
                {
                    continue;
                } skipped = true;

                Debug.Log("capture task entering " + regionstr);
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
                    game.overWorld.activeWorld.loadingRooms.Clear();
                    game.overWorld.activeWorld.ActivateRoom(room);
                    if(game.overWorld.activeWorld.loadingRooms.Count > 0 && game.overWorld.activeWorld.loadingRooms[0].room == room.realizedRoom)
                    {
                        RoomPreparer loading = game.overWorld.activeWorld.loadingRooms[0];
                        yield return null;
                        while (!room.realizedRoom.ReadyForPlayer)
                        {
                            if (!loading.done)
                                for (int i = 0; i < 1000; i++)
                                {
                                    loading.Update();
                                    if (loading.done) break;
                                }
                            if (!room.realizedRoom.ReadyForPlayer)
                                Debug.Log("capture task still loading... ");
                            yield return null;
                        }
                    }
                    game.cameras[0].MoveCamera(room.realizedRoom, 0);
                    game.cameras[0].virtualMicrophone.AllQuiet();
                    while(game.cameras[0].loadingRoom != null) yield return null;
                    for (int i = 0; i < room.realizedRoom.cameraPositions.Length; i++)
                    {
                        Debug.Log("capture task camera " + i);
                        Debug.Log("capture task camera has " + room.realizedRoom.cameraPositions.Length + " positions");

                        game.cameras[0].MoveCamera(i);
                        while(game.cameras[0].www != null) yield return null;
                        yield return null;
                        //UnityEngine.Application.CaptureScreenshot(PathOfScreenshot(regionstr, room.name, i));
                    }
                    room.Abstractize();
                    yield return null;
                }
                Debug.Log("capture task done with " + regionstr);
            }
            Debug.Log("capture task done!");
            Application.Quit();
        }
    }
}
