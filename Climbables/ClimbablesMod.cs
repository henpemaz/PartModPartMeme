using Partiality.Modloader;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace Climbables
{
    public class ClimbablesMod : PartialityMod
    {
        public ClimbablesMod()
        {
            this.ModID = "ClimbablesMod";
            this.Version = "1.1";
            this.author = "Henpemaz";

            instance = this;

            //ropeWatch = new System.Diagnostics.Stopwatch();
            //roomWatch = new System.Diagnostics.Stopwatch();
            //notRoomWatch = new System.Diagnostics.Stopwatch();
        }

        public static ClimbablesMod instance;

        //internal static System.Diagnostics.Stopwatch ropeWatch;
        //internal static System.Diagnostics.Stopwatch roomWatch;
        //internal static System.Diagnostics.Stopwatch notRoomWatch;
        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData_Patch;
            On.Room.Loaded += Room_Loaded_Patch;
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep_Patch;

            //On.Room.Update += Room_Update_dbg;

            On.ClimbableVinesSystem.VineSwitch += ClimbableVinesSystem_VineSwitch_hk;
        }

        private ClimbableVinesSystem.VinePosition ClimbableVinesSystem_VineSwitch_hk(On.ClimbableVinesSystem.orig_VineSwitch orig, ClimbableVinesSystem self, ClimbableVinesSystem.VinePosition vPos, UnityEngine.Vector2 goalPos, float rad)
        {
            ClimbableVinesSystem.VinePosition newPos = orig(self, vPos, goalPos, rad);

            if (self.vines[vPos.vine] is ClimbableArc && newPos == null && (vPos.floatPos == 0f || vPos.floatPos == 1f))
            {
                // Copypaste from orig but bypassing the dotprod check
                int num = self.PrevSegAtFloat(vPos.vine, vPos.floatPos);
                int num2 = Custom.IntClamp(num + 1, 0, self.vines[vPos.vine].TotalPositions() - 1);
                float t = Mathf.InverseLerp(self.FloatAtSegment(vPos.vine, num), self.FloatAtSegment(vPos.vine, num2), vPos.floatPos);
                Vector2 vector = Vector2.Lerp(self.vines[vPos.vine].Pos(num), self.vines[vPos.vine].Pos(num2), t);
                goalPos = vector + (vector - goalPos).normalized * 0.1f; // shorten that range a tiny bit.
                float f = Vector2.Dot((self.vines[vPos.vine].Pos(num) - self.vines[vPos.vine].Pos(num2)).normalized, (vector - goalPos).normalized);
                if (Mathf.Abs(f) > 0.5f)
                {
                    float num3 = float.MaxValue;
                    //ClimbableVinesSystem.VinePosition result = null;
                    for (int i = 0; i < self.vines.Count; i++)
                    {
                        for (int j = 0; j < self.vines[i].TotalPositions() - 1; j++)
                        {
                            if (self.OverlappingSegment(self.vines[i].Pos(j), self.vines[i].Rad(j), self.vines[i].Pos(j + 1), self.vines[i].Rad(j + 1), vector, rad))
                            {
                                Vector2 vector2 = self.ClosestPointOnSegment(self.vines[i].Pos(j), self.vines[i].Pos(j + 1), vector);
                                float num4 = Vector2.Distance(vector2, goalPos);
                                num4 *= 1f - 0.25f * Mathf.Abs(Vector2.Dot((self.vines[i].Pos(j) - self.vines[i].Pos(j + 1)).normalized, (vector - goalPos).normalized));
                                if (i == vPos.vine)
                                {
                                    float num5 = Mathf.Lerp(self.FloatAtSegment(i, j), self.FloatAtSegment(i, j + 1), Mathf.InverseLerp(0f, Vector2.Distance(self.vines[i].Pos(j), self.vines[i].Pos(j + 1)), Vector2.Distance(self.vines[i].Pos(j), vector2))) * self.TotalLength(i);
                                    if (Mathf.Abs(vPos.floatPos * self.TotalLength(vPos.vine) - num5) < 100f)
                                    {
                                        num4 = float.MaxValue;
                                    }
                                }
                                if (num4 < num3)
                                {
                                    num3 = num4;
                                    float t2 = Mathf.InverseLerp(0f, Vector2.Distance(self.vines[i].Pos(j), self.vines[i].Pos(j + 1)), Vector2.Distance(self.vines[i].Pos(j), vector2));
                                    newPos = new ClimbableVinesSystem.VinePosition(i, Mathf.Lerp(self.FloatAtSegment(i, j), self.FloatAtSegment(i, j + 1), t2));
                                }
                            }
                        }
                    }
                }
            }

            return newPos;
        }

        //private void Room_Update_dbg(On.Room.orig_Update orig, Room self)
        //{
        //    notRoomWatch.Stop();
        //    ropeWatch.Reset();
        //    roomWatch.Reset();
        //    roomWatch.Start();
        //    orig(self);
        //    roomWatch.Stop();

        //    if (UnityEngine.Input.GetKeyDown("x"))
        //    {
        //        DateTime date = new DateTime(roomWatch.ElapsedTicks);
        //        UnityEngine.Debug.Log($"roomWatch [{date.ToString("s.ffff")}s]");
        //        date = new DateTime(ropeWatch.ElapsedTicks);
        //        UnityEngine.Debug.Log($"ropeWatch [{date.ToString("s.ffff")}s]");
        //        date = new DateTime(notRoomWatch.ElapsedTicks);
        //        UnityEngine.Debug.Log($"notRoomWatch [{date.ToString("s.ffff")}s]");
        //    }
        //    notRoomWatch.Reset();
        //    notRoomWatch.Start();
        //}

        public static class EnumExt_ClimbablesMod
        {
            public static PlacedObject.Type ClimbablePoleV;
            public static PlacedObject.Type ClimbablePoleH;
            public static PlacedObject.Type ClimbableArc;
            public static PlacedObject.Type ClimbableRope;

        }

        public static void PlacedObject_GenerateEmptyData_Patch(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject instance)
        {
            orig(instance);
            if (instance.type == EnumExt_ClimbablesMod.ClimbablePoleV || instance.type == EnumExt_ClimbablesMod.ClimbablePoleH)
            {
                instance.data = new PlacedObject.GridRectObjectData(instance);
            }
            if (instance.type == EnumExt_ClimbablesMod.ClimbableArc)
            {
                instance.data = new PlacedObject.QuadObjectData(instance);
            }
            if (instance.type == EnumExt_ClimbablesMod.ClimbableRope)
            {
                instance.data = new PlacedObject.ResizableObjectData(instance);
            }
        }

        private static void ObjectsPage_CreateObjRep_Patch(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, DevInterface.ObjectsPage instance, PlacedObject.Type tp, PlacedObject pObj)
        {
            orig(instance, tp, pObj);
            if (tp == EnumExt_ClimbablesMod.ClimbablePoleV || tp == EnumExt_ClimbablesMod.ClimbablePoleH)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new DevInterface.GridRectObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString());
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }
            if (tp == EnumExt_ClimbablesMod.ClimbableArc)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new BezierObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString());
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }
            if (tp == EnumExt_ClimbablesMod.ClimbableRope)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)instance.tempNodes.Pop();
                instance.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = new DevInterface.ResizeableObjectRepresentation(instance.owner, tp.ToString() + "_Rep", instance, old.pObj, tp.ToString(), false);
                instance.tempNodes.Add(placedObjectRepresentation);
                instance.subNodes.Add(placedObjectRepresentation);
            }

        }

        public static void Room_Loaded_Patch(On.Room.orig_Loaded orig, Room instance)
        {
            orig(instance);
            if (instance.game == null) return;

            for (int l = 0; l < instance.roomSettings.placedObjects.Count; l++)
            {
                if (instance.roomSettings.placedObjects[l].active)
                {
                    if (instance.roomSettings.placedObjects[l].type == EnumExt_ClimbablesMod.ClimbablePoleV)
                        instance.AddObject(new ClimbablePoleV(instance.roomSettings.placedObjects[l], instance));
                    if (instance.roomSettings.placedObjects[l].type == EnumExt_ClimbablesMod.ClimbablePoleH)
                        instance.AddObject(new ClimbablePoleH(instance.roomSettings.placedObjects[l], instance));
                    if (instance.roomSettings.placedObjects[l].type == EnumExt_ClimbablesMod.ClimbableArc)
                        instance.AddObject(new ClimbableArc(instance.roomSettings.placedObjects[l], instance));
                    if (instance.roomSettings.placedObjects[l].type == EnumExt_ClimbablesMod.ClimbableRope)
                        instance.AddObject(new ClimbableRope(instance.roomSettings.placedObjects[l], instance));
                }
            }
        }
    }
}
