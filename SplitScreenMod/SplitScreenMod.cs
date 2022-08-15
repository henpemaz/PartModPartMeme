using BepInEx;
using BepInEx.Configuration;
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
    [BepInPlugin("henpemaz.splitscreen", "SplitScreen", "0.1.3")]
    public partial class SplitScreenMod : BaseUnityPlugin
    {

        private ConfigEntry<SplitMode> confPreferedSplitMode;
        private ConfigEntry<bool> confAlwaysSplit;

        public enum SplitMode
        {
            NoSplit,
            SplitHorizontal, // top bottom screens
            SplitVertical // left right screens
        }

        public static SplitMode CurrentSplitMode;
        public static SplitMode preferedSplitMode = SplitMode.SplitVertical;
        public static bool alwaysSplit;
        public static Vector2[] camOffsets = new Vector2[] { new Vector2(0,0), new Vector2(32000, 0), new Vector2(0, 32000), new Vector2(32000,32000) }; // one can dream

        int curCamera = -1;
        public static CameraListener[] cameraListeners = new CameraListener[2];
        public static RoomRealizer realizer2;

        //float offset;
        void Update() // debug thinghies
        {
            if (Input.GetKeyDown("f8"))
            {
                if (preferedSplitMode == SplitMode.NoSplit) return;
                if (preferedSplitMode == SplitMode.SplitHorizontal) preferedSplitMode = SplitMode.SplitVertical;
                else if (preferedSplitMode == SplitMode.SplitVertical) preferedSplitMode = SplitMode.SplitHorizontal;
                if(CurrentSplitMode != SplitMode.NoSplit && GameObject.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is RainWorldGame game) 
                    SetSplitMode(preferedSplitMode, game);
            }

            //if (Input.GetKeyDown("9"))
            //{
            //    if (GameObject.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is RainWorldGame game) game.world.rainCycle.ArenaEndSessionRain();
            //}

            //if (Input.GetKeyDown("1"))
            //{
            //    offset += 125f;
            //    Debug.Log(offset);
            //}

            //if (Input.GetKeyDown("2"))
            //{
            //    offset -= 125f;
            //    Debug.Log(offset);
            //}
        }

        public void OnEnable()
        {
            confPreferedSplitMode = Config.Bind("General",
                                         "preferedSplitMode",
                                         SplitMode.SplitVertical,
                                         "Preferred split mode");
            confAlwaysSplit = Config.Bind("General",
                                         "alwaysSplit",
                                         false,
                                         "Permanent split mode");


            On.RainWorld.Start += RainWorld_Start; // ils and deguggable hooks and late compat

            On.Futile.Init += Futile_Init; // turn on splitscreen
            On.Futile.UpdateCameraPosition += Futile_UpdateCameraPosition; // handle custom switcheroos

            // game hook moved to rwstart for compat, lotsa things in it
            On.RoomCamera.ctor += RoomCamera_ctor1; // bind cam to camlistener
            On.RainWorldGame.Update += RainWorldGame_Update; // split unsplit
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess; // unbind camlistener

            // wrapped calls to store shader globals
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RoomCamera.Update += RoomCamera_Update;
            On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera_int;
            On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int; // can also colapse to single cam if one of the cams is dead 

            // fixes in fixes file
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded; // roomrealizer 2 
            On.RoomCamera.FireUpSinglePlayerHUD += RoomCamera_FireUpSinglePlayerHUD;// displace cam2 map
            On.Menu.PauseMenu.ctor += PauseMenu_ctor;// displace pause menu
            On.Menu.PauseMenu.ShutDownProcess += PauseMenu_ShutDownProcess;// kill dupe pause menu
            On.Water.InitiateSprites += Water_InitiateSprites; // move water somewhere near final position
            On.VirtualMicrophone.DrawUpdate += VirtualMicrophone_DrawUpdate; // mic from 2nd cam should not pic up while on same cam
            On.HUD.DialogBox.DrawPos += DialogBox_DrawPos; // center dialog in half screen
            On.RoomRealizer.CanAbstractizeRoom += RoomRealizer_CanAbstractizeRoom;
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            // jolly also hooks this to spawn players, hooked late for interop
            On.RainWorldGame.ctor += RainWorldGame_ctor; // make roomcam2, follow fix

            // fixes in fixes file
            IL.RoomCamera.ctor += RoomCamera_ctor; // create sprite with the right name
            IL.RoomCamera.Update += RoomCamera_Update1; // follow critter, clamp to proper values
            IL.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate1; // clamp to proper values
            IL.ShortcutHandler.Update += ShortcutHandler_Update; // activate room if followed, move cam1 if p moves
            IL.ShortcutHandler.SuckInCreature += ShortcutHandler_SuckInCreature; // the vanilla room loading system is fragile af
            IL.PoleMimicGraphics.InitiateSprites += PoleMimicGraphics_InitiateSprites; // dont resize on re-init

            // creature culling has to take into account cam2
            new Hook(typeof(GraphicsModule).GetMethod("get_ShouldBeCulled", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic),
                typeof(SplitScreenMod).GetMethod("get_ShouldBeCulled"), this);

            // unity hooks
            // set shader variables into a dict so it can be set per-camera
            new Hook(typeof(Shader).GetMethod("SetGlobalColor", new Type[] { typeof(string), typeof(Color) }),
                typeof(SplitScreenMod).GetMethod("Shader_SetGlobalColor"), this);
            new Hook(typeof(Shader).GetMethod("SetGlobalFloat", new Type[] { typeof(string), typeof(float) }),
                typeof(SplitScreenMod).GetMethod("Shader_SetGlobalFloat"), this);
            new Hook(typeof(Shader).GetMethod("SetGlobalTexture", new Type[] { typeof(string), typeof(Texture) }),
                typeof(SplitScreenMod).GetMethod("Shader_SetGlobalTexture"), this);

            // interop in interop files
            if (Type.GetType("JollyCoop.PlayerHK, JollyCoop") is Type jol)
            {
                try
                {
                    new Hook(jol.GetMethod("HandleCoopCamera", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                        typeof(SplitScreenMod).GetMethod("fixHandleCoopCamera"), this); // jolly don't mess with the cameras PLEASE
                }
                catch (Exception e) { Debug.LogException(e); }
                try
                {
                    new Hook(Type.GetType("JollyCoop.PlayerMeter, JollyCoop").GetMethod("Draw", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                        typeof(SplitScreenMod).GetMethod("fixPlayerMeterDraw"), this); // if only there was a way to get which player the hud is following... oh
                }
                catch (Exception e) { Debug.LogException(e); }
                try
                {
                    HookEndpointManager.Modify(Type.GetType("JollyCoop.RoomScriptHK/VanillaEndingGhost, JollyCoop").GetMethod("Update", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance),
                        (Action<ILContext>)fixjollyBodyTransplant); // christ
                }
                catch (Exception e) { Debug.LogException(e); }
            }

            if (Type.GetType("SBCameraScroll.RoomCameraMod, SBCameraScroll") is Type sbcs)
            {
                try
                {
                    HookEndpointManager.Modify(sbcs.GetMethod("CheckBorders", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                        (Action<ILContext>)fixsbcsCheckBorders); // please do offsets right
                }
                catch (Exception e) { Debug.LogException(e); }
                try
                {
                    HookEndpointManager.Modify(sbcs.GetMethod("RoomCamera_ApplyPositionChange", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                        (Action<ILContext>)fixsbcsApplyPositionChange); // the right texture please
                }
                catch (Exception e) { Debug.Log(e); }
            }

            orig(self);
        }

        private void Futile_Init(On.Futile.orig_Init orig, Futile self, FutileParams futileParams)
        {
            self.splitScreen = true; // init 2 cams
            orig(self, futileParams);
            self.splitScreen = false; // keep only one working for now
            self.camera2.enabled = false;
            self.UpdateCameraPosition();
        }

        private void Futile_UpdateCameraPosition(On.Futile.orig_UpdateCameraPosition orig, Futile self)
        {
            orig(self);

            if (CurrentSplitMode == SplitMode.SplitHorizontal)
            {
                self.camera2.enabled = true;
                self._camera.orthographicSize = Futile.screen.pixelHeight / 2f * Futile.displayScaleInverse * 0.5f;
                self._camera.rect = new Rect(0f, 0.5f, 1f, 1f);
                self._camera2.orthographicSize = Futile.screen.pixelHeight / 2f * Futile.displayScaleInverse * 0.5f;
                self._camera2.rect = new Rect(0f, 0f, 1f, 0.5f);
                var offset = camOffsets[1];
                var x = (Futile.screen.originX - 0.5f) * -Futile.screen.pixelWidth * Futile.displayScaleInverse + Futile.screenPixelOffset + offset.x;
                var y = (Futile.screen.originY - 0.5f) * -Futile.screen.pixelHeight * Futile.displayScaleInverse - Futile.screenPixelOffset + offset.y;
                self._camera2.transform.position = new Vector3(x, y, -10f);
            }
            else if (CurrentSplitMode == SplitMode.SplitVertical)
            {
                self.camera2.enabled = true;
                self._camera.orthographicSize = Futile.screen.pixelHeight / 2f * Futile.displayScaleInverse * 1f;
                self._camera.rect = new Rect(0f, 0f, 0.5f, 1f);
                self._camera2.orthographicSize = Futile.screen.pixelHeight / 2f * Futile.displayScaleInverse * 1f;
                self._camera2.rect = new Rect(0.5f, 0f, 1f, 1f);
                var offset = camOffsets[1];
                var x = (Futile.screen.originX - 0.5f) * -Futile.screen.pixelWidth * Futile.displayScaleInverse + Futile.screenPixelOffset + offset.x;
                var y = (Futile.screen.originY - 0.5f) * -Futile.screen.pixelHeight * Futile.displayScaleInverse - Futile.screenPixelOffset + offset.y;
                self._camera2.transform.position = new Vector3(x, y, -10f);
            }
            else
            {
                self._camera.orthographicSize = Futile.screen.pixelHeight / 2f * Futile.displayScaleInverse * 1f;
                self._camera.rect = new Rect(0f, 0f, 1f, 1f);
                self.camera2.enabled = false;
            }
        }

        private void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            for (int i = 0; i < cameraListeners.Length; i++)
            {
                if (cameraListeners[i] != null) cameraListeners[i].Destroy();
                cameraListeners[i] = null;
            }
            realizer2 = null;
            // note, this could maybe be moved to manager.switchmainprocess, so that it's tied to game being the main process and wont fire if someone tries to instantiate a game for some reason
            // but then I'd have to rain checks for game == pm.mainprocess everywhere and thatd be ass
            orig(self, manager);
            preferedSplitMode = confPreferedSplitMode.Value;
            alwaysSplit = confAlwaysSplit.Value;
            
            if (self.session.Players.Count > 1 && preferedSplitMode != SplitMode.NoSplit)
            {
                var cams = self.cameras;
                Array.Resize(ref cams, 2);
                self.cameras = cams;

                cams[1] = new RoomCamera(self, 1);
                cams[1].MoveCamera(self.world.activeRooms[0], 0);

                cams[0].followAbstractCreature = self.session.Players[0];
                cams[1].followAbstractCreature = self.session.Players[1];

                realizer2 = new RoomRealizer(self.session.Players.First(p => p != self.roomRealizer.followCreature), self.world)
                {
                    realizedRooms = self.roomRealizer.realizedRooms,
                    recentlyAbstractedRooms = self.roomRealizer.recentlyAbstractedRooms,
                    realizeNeighborCandidates = self.roomRealizer.realizeNeighborCandidates
                };

                // vanilla splitScreenMode = horiz split only, we want MORE
                //foreach (RoomCamera c in cams) if (c != null) c.splitScreenMode = true;
                // cam.offset also dropped, too buggy as some idraws wouldnt use campos
            }

            CurrentSplitMode = SplitMode.NoSplit;
            Futile.instance.UpdateCameraPosition();

            SetSplitMode(alwaysSplit ? preferedSplitMode : SplitMode.NoSplit, self);
        }

        // adds a listener for render events so shader globals can be set
        private void RoomCamera_ctor1(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
            orig(self, game, cameraNumber);
            if (cameraNumber == 0)
            {
                if (cameraListeners[0] != null) cameraListeners[0].Destroy();
                cameraListeners[0] = Futile.instance._cameraHolder.AddComponent<CameraListener>();
                cameraListeners[0].roomCamera = self;
            }
            else
            {
                if (cameraListeners[1] != null) cameraListeners[1].Destroy();
                cameraListeners[1] = Futile.instance._cameraHolder2.AddComponent<CameraListener>();
                cameraListeners[1].roomCamera = self;
                foreach (var c in self.SpriteLayers) c.SetPosition(camOffsets[self.cameraNumber]);
                self.offset = Vector2.zero; // nulla zero niente don't use it
                // so many drawables don't ever fucking move or don't take into account the offset its infuriating
            }
        }

        private void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            CurrentSplitMode = SplitMode.NoSplit;
            Futile.instance.UpdateCameraPosition();
            for (int i = 0; i < cameraListeners.Length; i++)
            {
                if (cameraListeners[i] != null) cameraListeners[i].Destroy();
                cameraListeners[i] = null;
            }
            realizer2 = null;
            orig(self);
        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.cameras.Length > 1)
            {
                var main = self.cameras[0];
                var other = self.cameras[1];
                if (CurrentSplitMode == SplitMode.NoSplit && main.followAbstractCreature != other.followAbstractCreature && (other.room != main.room || other.currentCameraPosition != main.currentCameraPosition))
                {
                    SetSplitMode(preferedSplitMode, self);
                }

                if (CurrentSplitMode != SplitMode.NoSplit && (other.room == main.room && other.currentCameraPosition == main.currentCameraPosition) && !alwaysSplit)
                {
                    SetSplitMode(SplitMode.NoSplit, self);
                }

                if (CurrentSplitMode != SplitMode.NoSplit && main.room.abstractRoom.name == "SB_L01") // honestly jolly
                {
                    ConsiderColapsing(self);
                }
            }

            if (realizer2 != null) realizer2.Update();
        }

        public void SetSplitMode(SplitMode split, RainWorldGame game)
        {
            if(game.cameras.Length > 1 && split != CurrentSplitMode)
            {
                var main = game.cameras[0];
                var other = game.cameras[1];
                CurrentSplitMode = split;
                OffsetHud(main);
                OffsetHud(other);
                Futile.instance.UpdateCameraPosition();
            }
        }

        // following null or deleted or dead
        private bool IsCamDead(RoomCamera cam)
        {
            return IsCreatureDead(cam.followAbstractCreature);
        }

        // null or dead or deleted
        private bool IsCreatureDead(AbstractCreature critter)
        {
            return (critter?.state?.dead ?? true) || (critter?.realizedCreature?.slatedForDeletetion ?? true);
        }

        // consider changing if someones dead or deleted
        private void ConsiderColapsing(RainWorldGame game)
        {
            if(game.cameras.Length > 1)
            {
                if (game.Players.Count == 2 && alwaysSplit) return; // I guess
                foreach (var cam in game.cameras)
                {
                    // if following dead critter, switch!
                    if (IsCamDead(cam))
                    {
                        if(cam.game.Players.ToArray().Reverse().FirstOrDefault(cr=>!IsCreatureDead(cr))?.realizedCreature is Player p)
                            AssignCameraToPlayer(cam, p);
                        else if (cam.game.cameras.FirstOrDefault(c => !IsCamDead(c))?.followAbstractCreature?.realizedCreature is Player pp) 
                            AssignCameraToPlayer(cam, pp);
                    }
                }
            }
        }

        public void AssignCameraToPlayer(RoomCamera camera, Player player)
        {
            camera.followAbstractCreature = player.abstractCreature;
            var newroom = player.room ?? player.abstractCreature.Room.realizedRoom;
            if (newroom != null && camera.room != null && camera.room != newroom)
            {
                int node = player.abstractCreature.pos.abstractNode;
                camera.MoveCamera(newroom, newroom.CameraViewingNode(node != -1 ? node : 0));
            }
            if (camera.hud != null) camera.hud.owner = player;
        }

        private void OffsetHud(RoomCamera self)
        {
            Vector2 offset = camOffsets[self.cameraNumber];
            if (CurrentSplitMode != SplitMode.NoSplit)
            {
                offset += (CurrentSplitMode == SplitMode.SplitHorizontal ? new Vector2(0, self.sSize.y / 4f) : new Vector2(self.sSize.x / 4f, 0));
            }
            self.ReturnFContainer("HUD").SetPosition(offset);
            self.ReturnFContainer("HUD2").SetPosition(offset);
            self.hud?.map?.inFrontContainer?.SetPosition(offset);
        }


        private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            var prev = curCamera;
            try
            {
                curCamera = self.cameraNumber;
                if (self.cameraNumber > 0 && CurrentSplitMode == SplitMode.NoSplit) // optimized out
                {
                    self.virtualMicrophone.DrawUpdate(timeStacker, timeSpeed); // needs to remove old loops though
                    return;
                }
                orig(self, timeStacker, timeSpeed);
            }
            finally
            {
                curCamera = prev;
            }
        }

        private void RoomCamera_MoveCamera_Room_int(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera self, Room newRoom, int camPos)
        {
            ConsiderColapsing(self.game);

            var prev = curCamera;
            try
            {
                curCamera = self.cameraNumber;
                orig(self, newRoom, camPos);
            }
            finally
            {
                curCamera = prev;
            }
        }

        private void RoomCamera_MoveCamera_int(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera self, int camPos)
        {
            var prev = curCamera;
            try
            {
                curCamera = self.cameraNumber;
                orig(self, camPos);
            }
            finally
            {
                curCamera = prev;
            }
        }

        private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            var prev = curCamera;
            try
            {
                curCamera = self.cameraNumber;
                orig(self);
            }
            finally
            {
                curCamera = prev;
            }
        }

        public delegate void delSetGlobalColor(string propertyName, Color vec);
        public void Shader_SetGlobalColor(delSetGlobalColor orig, string propertyName, Color vec)
        {
            orig(propertyName, vec);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                if(CurrentSplitMode != SplitMode.NoSplit)
                {
                    if (propertyName == "_spriteRect")
                    {
                        int a = CurrentSplitMode == SplitMode.SplitHorizontal ? 1 : 0;
                        int b = CurrentSplitMode == SplitMode.SplitHorizontal ? 3 : 2;
                        vec[a] = vec[a] * 2f - 0.5f;
                        vec[b] = vec[b] * 2f - 0.5f;
                    }
                    else if (propertyName == "_caminroomrect")
                    {
                        vec[CurrentSplitMode == SplitMode.SplitHorizontal ? 3 : 2] /= 2f;
                    }
                }
                l.ShaderColors[propertyName] = vec;
            }
        }

        public delegate void delSetGlobalFloat(string propertyName, float f);
        public void Shader_SetGlobalFloat(delSetGlobalFloat orig, string propertyName, float f)
        {
            orig(propertyName, f);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderFloats[propertyName] = f;
            }
        }

        public delegate void delSetGlobalTexture(string propertyName, Texture t);
        public void Shader_SetGlobalTexture(delSetGlobalTexture orig, string propertyName, Texture t)
        {
            orig(propertyName, t);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderTextures[propertyName] = t;
            }
        }

        public class CameraListener: MonoBehaviour
        {
            public RoomCamera roomCamera;
            public Dictionary<string, Color> ShaderColors = new Dictionary<string, Color>();
            public Dictionary<string, float> ShaderFloats = new Dictionary<string, float>();
            public Dictionary<string, Texture> ShaderTextures = new Dictionary<string, Texture>();

            void OnPreRender()
            {
                foreach (var kv in ShaderColors) Shader.SetGlobalColor(kv.Key, kv.Value);
                foreach (var kv in ShaderFloats) Shader.SetGlobalFloat(kv.Key, kv.Value);
                foreach (var kv in ShaderTextures) Shader.SetGlobalTexture(kv.Key, kv.Value);
            }

            void OnDestroy()
            {
                ShaderTextures.Clear();
                roomCamera = null;
            }

            public void Destroy()
            {
                ShaderTextures.Clear();
                roomCamera = null;
                Destroy(this);
            }
        }
    }
}
