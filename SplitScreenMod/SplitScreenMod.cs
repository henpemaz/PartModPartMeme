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
        public void OnEnable()
        {
            On.Futile.Init += Futile_Init;

            On.RainWorldGame.ctor += RainWorldGame_ctor;

            On.RainWorld.Start += RainWorld_Start;

            On.RoomCamera.ctor += RoomCamera_ctor1;

            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RoomCamera.Update += RoomCamera_Update;
            On.RoomCamera.MoveCamera_int += RoomCamera_MoveCamera_int;
            On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int;
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            IL.RoomCamera.ctor += RoomCamera_ctor;
            IL.ShortcutHandler.Update += ShortcutHandler_Update;

            new Hook(typeof(Shader).GetMethod("SetGlobalColor", new Type[] { typeof(string), typeof(Color) }),
                typeof(SplitScreenMod).GetMethod("Shader_SetGlobalColor"), this);
            new Hook(typeof(Shader).GetMethod("SetGlobalFloat", new Type[] { typeof(string), typeof(float) }),
                typeof(SplitScreenMod).GetMethod("Shader_SetGlobalFloat"), this);

            orig(self);
        }


        private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            var prev = currentOwner;
            currentOwner = self.cameraNumber;
            orig(self, timeStacker, timeSpeed);
            currentOwner = prev;
        }

        private void RoomCamera_MoveCamera_Room_int(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera self, Room newRoom, int camPos)
        {
            var prev = currentOwner;
            currentOwner = self.cameraNumber;
            orig(self, newRoom, camPos);
            currentOwner = prev;
        }

        private void RoomCamera_MoveCamera_int(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera self, int camPos)
        {
            var prev = currentOwner;
            currentOwner = self.cameraNumber;
            orig(self, camPos);
            currentOwner = prev;
        }

        private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            var prev = currentOwner;
            currentOwner = self.cameraNumber;
            orig(self);
            currentOwner = prev;
        }


        float amount = 0f;
        public delegate void delSetGlobalColor(string propertyName, Color vec);
        public void Shader_SetGlobalColor(delSetGlobalColor orig, string propertyName, Color vec)
        {
            orig(propertyName, vec);
            if (currentOwner >= 0)
            {
                if (propertyName == "_spriteRect")
                {
                    var rc = cameraListeners[currentOwner].roomCamera;
                    //float camy = vec[1] * rc.sSize.y - 0.5f - rc.CamPos(rc.currentCameraPosition).y;
                    float magicNumer = 0.25f * (rc.sSize.y - rc.levelGraphic.height) / rc.sSize.y;
                    vec[1] -= magicNumer;
                    vec[1] *= 2f;

                    vec[3] -= magicNumer;
                    vec[3] *= 2f;

                    if (currentOwner == 0)
                    {
                        vec[1] -= 0.5f * rc.levelGraphic.height / rc.sSize.y;
                        vec[3] -= 0.5f * rc.levelGraphic.height / rc.sSize.y;
                    }
                    if (currentOwner == 1)
                    {
                        vec[0] += rc.offset.x / rc.sSize.x;
                        vec[2] += rc.offset.x / rc.sSize.x;
                        vec[1] += amount * rc.levelGraphic.height / rc.sSize.y;
                        vec[3] += amount * rc.levelGraphic.height / rc.sSize.y;
                    }

                    if (Input.GetKeyDown("l"))
                    {
                        Debug.LogError("owner" + currentOwner);
                        Debug.LogError(vec);
                    }

                    if (Input.GetKeyDown("0"))
                    {
                        Debug.LogError("up");
                        amount += 0.125f;
                    }

                    if (Input.GetKeyDown("9"))
                    {
                        Debug.LogError("down");
                        amount -= 0.125f;
                    }
                }

                else if (propertyName == "_caminroomrect")
                {
                    vec[3] /= 2f;
                }
                else if (propertyName == "_screenSize")
                {
                    vec[1] /= 2f;
                }

                cameraListeners[currentOwner].ShaderColors[propertyName] = vec;
            }
        }

        public delegate void delSetGlobalFloat(string propertyName, float f);
        public void Shader_SetGlobalFloat(delSetGlobalFloat orig, string propertyName, float f)
        {
            orig(propertyName, f);
            if (currentOwner >= 0)
            {
                cameraListeners[currentOwner].ShaderFloats[propertyName] = f;
            }
        }


        int currentOwner = -1;
        CameraListener[] cameraListeners = new CameraListener[2];

        private void RoomCamera_ctor1(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
            orig(self, game, cameraNumber);
            if(cameraNumber == 0)
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

            foreach (RoomCamera c in cams) if (c != null) c.splitScreenMode = true;
        }

        private void Futile_Init(On.Futile.orig_Init orig, Futile self, FutileParams futileParams)
        {
            self.splitScreen = true;
            orig(self, futileParams);
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
