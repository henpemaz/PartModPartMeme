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
    [BepInPlugin("henpemaz.splitscreen", "SplitScreen", "0.1.0")]
    public partial class SplitScreenMod : BaseUnityPlugin
    {
        public enum SplitMode
        {
            NoSplit,
            SplitHorizontal, // top bottom screens
            SplitVertical // left right screens
        }

        SplitMode CurrentSplitMode;
        SplitMode preferedSplitMode = SplitMode.SplitVertical;

        int curCamera = -1;
        CameraListener[] cameraListeners = new CameraListener[2];
        private RoomRealizer realizer2;

        float offset;
        void Update() // debug thinghies
        {
            if (Input.GetKeyDown("0"))
            {
                if (preferedSplitMode == SplitMode.SplitHorizontal) preferedSplitMode = SplitMode.SplitVertical;
                else if (preferedSplitMode == SplitMode.SplitVertical) preferedSplitMode = SplitMode.SplitHorizontal;
                if(GameObject.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is RainWorldGame game) SetSplitMode(SplitMode.NoSplit, game);
            }

            if (Input.GetKeyDown("9"))
            {
                if (GameObject.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is RainWorldGame game) game.world.rainCycle.ArenaEndSessionRain();
            }

            if (Input.GetKeyDown("1"))
            {
                offset += 125f;
                Debug.Log(offset);
            }

            if (Input.GetKeyDown("2"))
            {
                offset -= 125f;
                Debug.Log(offset);
            }
        }

        public void OnEnable()
        {
            On.RainWorld.Start += RainWorld_Start; // ils and deguggable hooks and late compat

            On.Futile.Init += Futile_Init; // turn on splitscreen
            On.Futile.UpdateCameraPosition += Futile_UpdateCameraPosition; // handle custom switcheroos
            // game hook moved to rwstart for compat, lotsa things in it
            On.RoomCamera.ctor += RoomCamera_ctor1; // bind cam to camlistener
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess; // unbind camlistener
            On.RainWorldGame.Update += RainWorldGame_Update; // split unsplit

            // wrapped calls to store shader globals
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RoomCamera.Update += RoomCamera_Update;
            On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera_int;
            On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int; // can also colapse to single cam if one of the cams is dead 

            // fixes in fixes file
            On.OverWorld.WorldLoaded += OverWorld_WorldLoaded; // roomrealizer 2 
            On.RoomCamera.FireUpSinglePlayerHUD += RoomCamera_FireUpSinglePlayerHUD;// displace cam2 map
            On.Menu.PauseMenu.ctor += PauseMenu_ctor;// displace pause menu
            On.RainWorldGame.ContinuePaused += RainWorldGame_ContinuePaused; // kill dupe pause menu
            On.RoomCamera.SpriteLeaser.ctor += SpriteLeaser_ctor; // some sprotes initialize at 00 and are never moved, move
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            // jolly also hooks this to spawn players, hooked late for interop
            On.RainWorldGame.ctor += RainWorldGame_ctor; // make roomcam2, follow fix

            // fixes in fixes file
            IL.RoomCamera.ctor += RoomCamera_ctor; // create sprite with the right name
            IL.RoomCamera.Update += RoomCamera_Update1; // follow critter, clamp to proper values
            IL.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate1; // clamp to proper values
            IL.ShortcutHandler.Update += ShortcutHandler_Update; // activate room is followed, move cam1 if p moves

            // unity hooks
            new Hook(typeof(Shader).GetMethod("SetGlobalColor", new Type[] { typeof(string), typeof(Color) }),
                typeof(SplitScreenMod).GetMethod("Shader_SetGlobalColor"), this);
            new Hook(typeof(Shader).GetMethod("SetGlobalFloat", new Type[] { typeof(string), typeof(float) }),
                typeof(SplitScreenMod).GetMethod("Shader_SetGlobalFloat"), this);
            new Hook(typeof(Shader).GetMethod("SetGlobalTexture", new Type[] { typeof(string), typeof(Texture) }),
                typeof(SplitScreenMod).GetMethod("Shader_SetGlobalTexture"), this);

            // interop in interop files
            // jolly don't mess with the cameras PLEASE
            // currently only if pcount == 2
            if (Type.GetType("JollyCoop.PlayerHK, JollyCoop") != null) new Hook(Type.GetType("JollyCoop.PlayerHK, JollyCoop").GetMethod("HandleCoopCamera", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                 typeof(SplitScreenMod).GetMethod("fixHandleCoopCamera"), this);

            if (Type.GetType("SBCameraScroll.RoomCameraMod, SBCameraScroll") is Type sbcs)
            {
                HookEndpointManager.Modify(sbcs.GetMethod("RoomCamera_DrawUpdate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    (Action<ILContext>)fixsbcsDrawUpdate); // please do take in account that there might be more than one cam
                HookEndpointManager.Modify(sbcs.GetMethod("CheckBorders", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    (Action<ILContext>)fixsbcsCheckBorders); // please do offsets right
                // todo fix this mod calling resetpos all the time when 2 scrollers, causes a bit of a stuttew
                // it simply doesn't account for 2 cams as it stores a bunch of static vars
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
            }
            else if (CurrentSplitMode == SplitMode.SplitVertical)
            {
                self.camera2.enabled = true;
                self._camera.orthographicSize = Futile.screen.pixelHeight / 2f * Futile.displayScaleInverse * 1f;
                self._camera.rect = new Rect(0f, 0f, 0.5f, 1f);
                self._camera2.orthographicSize = Futile.screen.pixelHeight / 2f * Futile.displayScaleInverse * 1f;
                self._camera2.rect = new Rect(0.5f, 0f, 1f, 1f);
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
            orig(self, manager);

            var cams = self.cameras;
            Array.Resize(ref cams, 2);
            self.cameras = cams;

            cams[1] = new RoomCamera(self, 1);
            cams[1].MoveCamera(self.world.activeRooms[0], 0);

            cams[0].followAbstractCreature = self.session.Players[0];
            cams[1].followAbstractCreature = self.session.Players[1];

            realizer2 = new RoomRealizer(self.session.Players[0], self.world);
            realizer2.realizedRooms = self.roomRealizer.realizedRooms;
            realizer2.recentlyAbstractedRooms = self.roomRealizer.recentlyAbstractedRooms;
            realizer2.realizeNeighborCandidates = self.roomRealizer.realizeNeighborCandidates;

            // vanilla = horiz split only, we want MORE
            //foreach (RoomCamera c in cams) if (c != null) c.splitScreenMode = true;

            CurrentSplitMode = SplitMode.NoSplit;
            Futile.instance.UpdateCameraPosition();
        }

        // adds a listener for render events so shader globals can be set
        private void RoomCamera_ctor1(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
            orig(self, game, cameraNumber);
            if (cameraNumber == 0)
            {
                cameraListeners[0] = Futile.instance._cameraHolder.AddComponent<CameraListener>();
                cameraListeners[0].roomCamera = self;
            }
            else
            {
                cameraListeners[1] = Futile.instance._cameraHolder2.AddComponent<CameraListener>();
                cameraListeners[1].roomCamera = self;
            }
        }

        private void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig(self);
            CurrentSplitMode = SplitMode.NoSplit;
            Futile.instance.UpdateCameraPosition();
            for (int i = 0; i < cameraListeners.Length; i++)
            {
                cameraListeners[i].ShaderTextures.Clear();
                cameraListeners[i].roomCamera = null;
                Destroy(cameraListeners[i]);
                cameraListeners[i] = null;
            }
            realizer2 = null;
        }

        private void UpdateMapSize(RoomCamera cam)
        {
            var prev = curCamera;
            try
            {
                curCamera = cam.cameraNumber;
                if (cam.hud?.map != null)
                {
                    Shader.SetGlobalVector("_mapSize", cam.hud.map.mapSize);
                }
            }
            finally
            {
                curCamera = prev;
            }
        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);

            var main = self.cameras[0];
            var other = self.cameras[1];
            if (CurrentSplitMode == SplitMode.NoSplit && main.followAbstractCreature != other.followAbstractCreature && (other.room != main.room || other.currentCameraPosition != main.currentCameraPosition))
            {
                SetSplitMode(preferedSplitMode, self);
            }

            if (CurrentSplitMode != SplitMode.NoSplit && (other.room == main.room && other.currentCameraPosition == main.currentCameraPosition))
            {
                SetSplitMode(SplitMode.NoSplit, self);
            }

            if (realizer2 != null) realizer2.Update();
        }

        public void SetSplitMode(SplitMode split, RainWorldGame game)
        {
            var main = game.cameras[0];
            var other = game.cameras[1];
            CurrentSplitMode = split;
            Futile.instance.UpdateCameraPosition();
            OffsetHud(main);
            OffsetHud(other);
            //Shader.SetGlobalVector("_screenSize", self.rainWorld.screenSize);
            UpdateMapSize(main);
            UpdateMapSize(other);
        }

        private void ConsiderColapsing(RainWorldGame game)
        {
            foreach (var cam in game.cameras)
            {
                // if following dead critter, switch!
                if (cam.followAbstractCreature?.state?.dead ?? false)
                {
                    cam.followAbstractCreature = cam.game.cameras.FirstOrDefault(c => c.followAbstractCreature?.state?.alive ?? false)?.followAbstractCreature ?? cam.followAbstractCreature;
                }
            }
        }

        private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            var prev = curCamera;
            try
            {
                curCamera = self.cameraNumber;
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
                        if (curCamera == 1)
                        {
                            var rc = l.roomCamera; // - offset was being passed here, get rid of it
                            vec[0] += rc.offset.x / rc.sSize.x;
                            vec[2] += rc.offset.x / rc.sSize.x;
                            if (CurrentSplitMode == SplitMode.SplitVertical) // times 2!!
                            {
                                vec[0] += rc.offset.x / rc.sSize.x;
                                vec[2] += rc.offset.x / rc.sSize.x;
                            }
                        }
                    }
                    else if (propertyName == "_caminroomrect")
                    {
                        vec[CurrentSplitMode == SplitMode.SplitHorizontal ? 3 : 2] /= 2f;
                    }
                    //else if (propertyName == "_screenSize")
                    //{
                    //    vec[CurrentSplitMode == SplitMode.SplitHorizontal ? 1 : 0] /= 2f;
                    //}
                    //else if (propertyName == "_mapSize")
                    //{
                    //    vec[CurrentSplitMode == SplitMode.SplitHorizontal ? 1 : 0] /= 2f;
                    //}
                    else if (propertyName == "_RainSpriteRect" && curCamera == 1)
                    {
                        var rc = l.roomCamera; // - offset was being passed here, get rid of it
                        vec[0] -= rc.offset.x / (20f * (float)rc.room.TileWidth);
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

        private void ShortcutHandler_Update(ILContext il)
        {
            var c = new ILCursor(il);

            // this is loading room if creature followed by camera
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchCallvirt<AbstractCreature>("FollowedByCamera"),
                i => i.MatchBrfalse(out _),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld< ShortcutHandler>("betweenRoomsWaitingLobby")
                ))
            {
                c.Index++;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, 6);
                // was A && !B
                // becomes (A || A2) && !B
                // b param here is A
                c.EmitDelegate<Func<bool, ShortcutHandler, int, bool>>((b, sc, k) =>
                {
                    return b || sc.betweenRoomsWaitingLobby[k].creature.abstractCreature.FollowedByCamera(1);
                });

            }
            else Debug.LogException(new Exception("Couldn't IL-hook ShortcutHandler_Update part 1 from SplitScreenMod")); // deffendisve progrmanig
            
            
            // this is moving the camera if the creature is followed by camera
            ILLabel jump = null;
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchCallvirt<AbstractCreature>("FollowedByCamera"),
                i => i.MatchBrfalse(out jump),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<ShortcutHandler>("game")
                ))
            {
                c.GotoLabel(jump);

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, 6);
                c.EmitDelegate<Action<ShortcutHandler, int>>((sc, k) =>
                {
                    if (sc.betweenRoomsWaitingLobby[k].creature.abstractCreature.FollowedByCamera(1))
                        sc.game.cameras[1].MoveCamera(sc.betweenRoomsWaitingLobby[k].room.realizedRoom, sc.betweenRoomsWaitingLobby[k].room.nodes[sc.betweenRoomsWaitingLobby[k].entranceNode].viewedByCamera);
                });
            }
            else Debug.LogException(new Exception("Couldn't IL-hook ShortcutHandler_Update part 2 from SplitScreenMod")); // deffendisve progrmanig

        }

        private void RoomCamera_ctor(ILContext il)
        {
			var c = new ILCursor(il);
			if (c.TryGotoNext(MoveType.Before,
				i => i.MatchLdstr("LevelTexture"),
                i => i.MatchLdcI4(1),
                i => i.MatchNewobj<FSprite>()
				))
			{
                c.Index++;
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate<Func<string, int, string>>((name, camnum) => 
                {
                    return camnum > 0? name + camnum.ToString() : name;
                });
            }
			else Debug.LogException(new Exception("Couldn't IL-hook RoomCamera_ctor from SplitScreenMod")); // deffendisve progrmanig
		}

        private void RoomCamera_Update1(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel jump = null;
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<RoomCamera>("splitScreenMode"),
                i => i.MatchBrfalse(out jump),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<RoomCamera>("followAbstractCreature"),
                i => i.MatchCallvirt<AbstractCreature>("get_realizedCreature")
                ))
            {
                c.GotoLabel(jump);
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<RoomCamera>>((rc) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitHorizontal)
                    {
                        float pad = rc.sSize.y / 4f;
                        if(rc.followAbstractCreature != null && rc.followAbstractCreature.realizedCreature is Creature cr)
                        {
                            if(!cr.inShortcut) rc.pos.y = rc.followAbstractCreature.realizedCreature.mainBodyChunk.pos.y - 2 * pad;
                            else
                            {
                                Vector2? vector = rc.room.game.shortcuts.OnScreenPositionOfInShortCutCreature(rc.room, cr);
                                if (vector != null)
                                {
                                    rc.pos.y = vector.Value.y - 2 * pad;
                                }
                            }
                            rc.pos.y += rc.followCreatureInputForward.y * 2f;
                        }
                    }
                    else if (CurrentSplitMode == SplitMode.SplitVertical)
                    {
                        float pad = rc.sSize.x / 4f;
                        if (rc.followAbstractCreature != null && rc.followAbstractCreature.realizedCreature is Creature cr)
                        {
                            if (!cr.inShortcut) rc.pos.x = rc.followAbstractCreature.realizedCreature.mainBodyChunk.pos.x - 2 * pad;
                            else
                            {
                                Vector2? vector = rc.room.game.shortcuts.OnScreenPositionOfInShortCutCreature(rc.room, cr);
                                if (vector != null)
                                {
                                    rc.pos.x = vector.Value.x - 2 * pad;
                                }
                            }
                            rc.pos.x += rc.followCreatureInputForward.x * 2f;
                        }
                    }
                });

                try
                {
                    c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<RoomCamera>("get_hDisplace")); // IL_00A7

                    c.Emit(OpCodes.Ldarg_0); // RoomCamera
                    c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                    {
                        if (CurrentSplitMode == SplitMode.SplitVertical)
                        {
                            return v - rc.sSize.x / 4f;
                        }
                        return v;
                    });

                    c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<RoomCamera>("get_hDisplace")); // IL_00CF

                    c.Emit(OpCodes.Ldarg_0); // RoomCamera
                    c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                    {
                        if (CurrentSplitMode == SplitMode.SplitVertical)
                        {
                            return v + rc.sSize.x / 4f;
                        }
                        return v;
                    });

                    ILLabel hop = null;
                    c.GotoNext(MoveType.After, i => i.MatchLdfld<RoomCamera>("splitScreenMode"), i => i.MatchBrfalse(out hop)); // IL_0116
                    c.GotoLabel(hop, MoveType.After);
                    c.MoveAfterLabels();
                    c.Emit(OpCodes.Ldarg_0); // RoomCamera
                    c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                    {
                        if (CurrentSplitMode == SplitMode.SplitHorizontal)
                        {
                            return v + rc.sSize.y / 4f;
                        }
                        return v;
                    });


                    c.GotoNext(MoveType.After, i => i.MatchLdfld<RoomCamera>("splitScreenMode"), i => i.MatchBrfalse(out hop)); // IL_014C
                    c.GotoLabel(hop, MoveType.After);
                    c.MoveAfterLabels();
                    c.Emit(OpCodes.Ldarg_0); // RoomCamera
                    c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                    {
                        if (CurrentSplitMode == SplitMode.SplitHorizontal)
                        {
                            return v + rc.sSize.y / 4f;
                        }
                        return v;
                    });
                }
                catch (Exception e)
                {
                    Debug.LogException(new Exception("Couldn't IL-hook RoomCamera_Update1 from SplitScreenMod, inner spot", e)); // deffendisve progrmanig
                    throw;
                }
            }
            else Debug.LogException(new Exception("Couldn't IL-hook RoomCamera_Update1 from SplitScreenMod")); // deffendisve progrmanig
        }

        private void RoomCamera_DrawUpdate1(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel jump = null;
            ILLabel jump2 = null;
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<RoomCamera>("voidSeaMode"),
                i => i.MatchBrtrue(out jump),
                i => i.MatchLdloca(0)
                ))
            {
                var b = c.Index;
                c.GotoLabel(jump);
                if (c.Prev.MatchBr(out jump2))
                {
                    try
                    {
                        c.Index = b + 3; // NOT void Sea

                        c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<RoomCamera>("get_hDisplace")); // IL_00A7

                        c.Emit(OpCodes.Ldarg_0); // RoomCamera
                        c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                        {
                            if (CurrentSplitMode == SplitMode.SplitVertical)
                            {
                                return v - rc.sSize.x / 4f;
                            }
                            return v;
                        });

                        c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<RoomCamera>("get_hDisplace")); // IL_00CF

                        c.Emit(OpCodes.Ldarg_0); // RoomCamera
                        c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                        {
                            if (CurrentSplitMode == SplitMode.SplitVertical)
                            {
                                return v + rc.sSize.x / 4f;
                            }
                            return v;
                        });

                        ILLabel hop = null;
                        c.GotoNext(MoveType.After, i => i.MatchLdfld<RoomCamera>("splitScreenMode"), i => i.MatchBrfalse(out hop)); // IL_0116
                        c.GotoLabel(hop, MoveType.After);
                        c.MoveAfterLabels();
                        c.Emit(OpCodes.Ldarg_0); // RoomCamera
                        c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                        {
                            if (CurrentSplitMode == SplitMode.SplitHorizontal)
                            {
                                return v + rc.sSize.y / 4f;
                            }
                            return v;
                        });


                        c.GotoNext(MoveType.After, i => i.MatchLdfld<RoomCamera>("splitScreenMode"), i => i.MatchBrfalse(out hop)); // IL_014C
                        c.GotoLabel(hop, MoveType.After);
                        c.MoveAfterLabels();
                        c.Emit(OpCodes.Ldarg_0); // RoomCamera
                        c.EmitDelegate<Func<float, RoomCamera, float>>((v, rc) =>
                        {
                            if (CurrentSplitMode == SplitMode.SplitHorizontal)
                            {
                                return v + rc.sSize.y / 4f;
                            }
                            return v;
                        });
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(new Exception("Couldn't IL-hook RoomCamera_DrawUpdate1 from SplitScreenMod, inner inner spot", e)); // deffendisve progrmanig
                        throw;
                    }
                }
                else Debug.LogException(new Exception("Couldn't IL-hook RoomCamera_DrawUpdate1 from SplitScreenMod, inner spot")); // deffendisve progrmanig
            }
            else Debug.LogException(new Exception("Couldn't IL-hook RoomCamera_DrawUpdate1 from SplitScreenMod")); // deffendisve progrmanig
        }

        private class CameraListener: MonoBehaviour
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
        }
    }
}
