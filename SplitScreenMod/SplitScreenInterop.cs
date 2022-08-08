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
        private void fixsbcsCheckBorders(ILContext il) // patch up cam scroll boundaries
        {
            var c = new ILCursor(il);
            // buncha gotos, faster like this
            try
            {
                // SplitVertical
                c.GotoNext(i => i.MatchCallOrCallvirt<RoomCamera>("get_sSize"));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitVertical)
                    {
                        return f - cam.sSize.x / 4f;
                    }
                    return f;
                });
                c.GotoNext(i => i.MatchCallOrCallvirt<RoomCamera>("get_sSize"));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitVertical)
                    {
                        return f - cam.sSize.x / 4f;
                    }
                    return f;
                });

                c.GotoNext(i => i.MatchLdarg(1));

                c.GotoNext(i => i.MatchLdloc(0));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitVertical)
                    {
                        return f - cam.sSize.x / 4f;
                    }
                    return f;
                });

                c.GotoNext(i => i.MatchLdloc(0));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitVertical)
                    {
                        return f - cam.sSize.x / 4f;
                    }
                    return f;
                });

                //// SplitHorizontal
                c.GotoNext(i => i.MatchCallOrCallvirt<RoomCamera>("get_sSize"));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitHorizontal)
                    {
                        return f - cam.sSize.y / 4f;
                    }
                    return f;
                });
                c.GotoNext(i => i.MatchCallOrCallvirt<RoomCamera>("get_sSize"));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitHorizontal)
                    {
                        return f - cam.sSize.y / 4f;
                    }
                    return f;
                });

                c.GotoNext(i => i.MatchLdarg(1));

                c.GotoNext(i => i.MatchLdloc(0));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitHorizontal)
                    {
                        return f - cam.sSize.y / 4f;
                    }
                    return f;
                });

                c.GotoNext(i => i.MatchLdloc(0));
                c.Index += 2;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, RoomCamera, float>>((f, cam) =>
                {
                    if (CurrentSplitMode == SplitMode.SplitHorizontal)
                    {
                        return f - cam.sSize.y / 4f;
                    }
                    return f;
                });

            }
            catch (Exception e)
            {
                Debug.LogException(new Exception("Couldn't IL-hook fixsbcsCheckBorders from SplitScreenMod", e)); // deffendisve progrmanig
            }
        }
        private void fixsbcsDrawUpdate(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchCallOrCallvirt<UnityEngine.Vector2>("Lerp")
                ))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Func<Vector2, RoomCamera, Vector2>>((vec, cam) => // please use the camera offset instead of ignoring it
                {
                    return vec + cam.offset;
                });
            }
            else Debug.LogException(new Exception("Couldn't IL-hook fixsbcsDrawUpdate from SplitScreenMod")); // deffendisve progrmanig
        }

        public delegate void delsbcsDrawUpdate(On.RoomCamera.orig_DrawUpdate orig, global::RoomCamera roomCamera, float timeStacker, float timeSpeed);

        public delegate void delHandleCoopCamera(Player self, int playerNumber);
        public void fixHandleCoopCamera(delHandleCoopCamera orig, Player self, int playerNumber)
        {
            if (self?.room?.game?.GetStorySession is StoryGameSession sgs && sgs.Players.Count == 2) return; // prevent cam rotate
            orig(self, playerNumber);
        }

    }
}
