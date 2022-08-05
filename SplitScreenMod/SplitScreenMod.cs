using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SplitScreenMod
{
    [BepInPlugin("henpemaz.splitscreen", "SplitScreen", "0.1.0")]
    public class SplitScreenMod : BaseUnityPlugin
    {
        public enum SplitMode
        {
            NoSplit,
            SplitHorizontal, // top bottom screens
            SplitVertical // left right screens
        }

        SplitMode CurrentSplitMode;

        int curCamera = -1;
        CameraListener[] cameraListeners = new CameraListener[2];

        public void OnEnable()
        {
            On.RainWorld.Start += RainWorld_Start; // ils and deguggable hooks

            On.Futile.Init += Futile_Init; // turn on splitscreen
            On.Futile.UpdateCameraPosition += Futile_UpdateCameraPosition;
            On.RainWorldGame.ctor += RainWorldGame_ctor; // make roomcam2, follow fix
            On.RoomCamera.ctor += RoomCamera_ctor1; // bind cam to camlistener

            // wrapped calls to store shader globals
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RoomCamera.Update += RoomCamera_Update;
            On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera_int;
            On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int;
        }

        void Update()
        {
            if (Input.GetKeyDown("0"))
            {
                if (CurrentSplitMode == SplitMode.SplitHorizontal) CurrentSplitMode = SplitMode.SplitVertical;
                else if (CurrentSplitMode == SplitMode.SplitVertical) CurrentSplitMode = SplitMode.SplitHorizontal;
                Futile.instance.UpdateCameraPosition();
            }
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            IL.RoomCamera.ctor += RoomCamera_ctor;
            IL.RoomCamera.Update += RoomCamera_Update1;
            IL.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate1;
            IL.ShortcutHandler.Update += ShortcutHandler_Update;

            new Hook(typeof(Shader).GetMethod("SetGlobalColor", new Type[] { typeof(string), typeof(Color) }),
                typeof(SplitScreenMod).GetMethod("Shader_SetGlobalColor"), this);
            new Hook(typeof(Shader).GetMethod("SetGlobalFloat", new Type[] { typeof(string), typeof(float) }),
                typeof(SplitScreenMod).GetMethod("Shader_SetGlobalFloat"), this);

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

            // vanilla = horiz split only, we want MORE
            //foreach (RoomCamera c in cams) if (c != null) c.splitScreenMode = true;

            CurrentSplitMode = SplitMode.NoSplit;
            Futile.instance.UpdateCameraPosition();
        }

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


            if(self.cameraNumber == 0)
            {
                var other = self.game.cameras[1];
                if (CurrentSplitMode == SplitMode.NoSplit && (other.room != self.room || other.currentCameraPosition != self.currentCameraPosition))
                {
                    CurrentSplitMode = SplitMode.SplitVertical;
                    Futile.instance.UpdateCameraPosition();
                }

                if (CurrentSplitMode != SplitMode.NoSplit && (other.room == self.room && other.currentCameraPosition == self.currentCameraPosition))
                {
                    CurrentSplitMode = SplitMode.NoSplit;
                    Futile.instance.UpdateCameraPosition();
                }
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
                            if (CurrentSplitMode == SplitMode.SplitVertical)
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
                    else if (propertyName == "_screenSize")
                    {
                        vec[CurrentSplitMode == SplitMode.SplitHorizontal ? 1 : 0] /= 2f;
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

            void OnPreRender()
            {
                foreach (var kv in ShaderColors) Shader.SetGlobalColor(kv.Key, kv.Value);
                foreach (var kv in ShaderFloats) Shader.SetGlobalFloat(kv.Key, kv.Value);
            }
        }
    }
}
