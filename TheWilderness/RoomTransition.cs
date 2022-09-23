using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheWilderness
{
    internal class RoomTransition : UpdatableAndDeletable
    {
        class RoomTransitionData : PlacedObjectsManager.ManagedData
        {
            public RoomTransitionData(PlacedObject owner) : base(owner, null) { }

            [PlacedObjectsManager.Vector2Field("coord", 100,100, PlacedObjectsManager.Vector2Field.VectorReprType.rect)]
            public Vector2 coord;

            [PlacedObjectsManager.FloatField("margin", 0, 1000, 100)]
            public float margin;

            [PlacedObjectsManager.StringField("toRoom", "SU_C04")]
            public string toRoom;

            [PlacedObjectsManager.IntegerField("toNode", 0, int.MaxValue, 0)]
            public int toNode;
        }

        internal static void Apply()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("TWRoomTransition", typeof(RoomTransition), typeof(RoomTransitionData), typeof(PlacedObjectsManager.ManagedRepresentation)));
            IL.ShortcutHandler.FlyingCreatureArrivedInRealizedRoom += ShortcutHandler_FlyingCreatureArrivedInRealizedRoom;
            On.Creature.FlyIntoRoom += Creature_FlyIntoRoom;
        }


        // set camera properly for these entrances, they're -1 unset by default
        private static void Creature_FlyIntoRoom(On.Creature.orig_FlyIntoRoom orig, Creature self, WorldCoordinate entrancePos, Room newRoom)
        {
            orig(self, entrancePos, newRoom);

            if (entrancePos.NodeDefined)
            {
                Vector2 a = self.DangerPos;
                float num = float.MaxValue;
                int num2 = -1;
                for (int i = 0; i < newRoom.cameraPositions.Length; i++)
                {
                    if (Vector2.Distance(a, newRoom.cameraPositions[i] + new Vector2(700f, 402f)) < num)
                    {
                        num = Vector2.Distance(a, newRoom.cameraPositions[i] + new Vector2(700f, 402f));
                        num2 = i;
                    }
                }
                if (num2 == -1)
                {
                    num2 = 0;
                }

                newRoom.abstractRoom.nodes[entrancePos.abstractNode].viewedByCamera = num2;
            }
        }

        // fix nullref
        private static void ShortcutHandler_FlyingCreatureArrivedInRealizedRoom(MonoMod.Cil.ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel where = null;
            if(c.TryGotoNext(
                i=>i.MatchLdarg(1),
                i=>i.MatchLdfld<ShortcutHandler.Vessel>("creature"),
                i=>i.MatchCallOrCallvirt<Creature>("get_abstractCreature"),
                i=>i.MatchLdfld<AbstractCreature>("abstractAI"),
                i=>i.MatchLdfld<AbstractCreatureAI>("RealAI"),
                i=>i.MatchBrfalse(out where)
                ))
            {
                c.Emit(OpCodes.Ldarg_1);
                c.Emit<ShortcutHandler.Vessel>(OpCodes.Ldfld, "creature");
                c.Emit<Creature>(OpCodes.Callvirt, "get_abstractCreature");
                c.Emit<AbstractCreature>(OpCodes.Ldfld, "abstractAI");
                c.Emit(OpCodes.Brfalse, where);
            }
        }

        PlacedObject pobj;
        RoomTransitionData data => pobj.data as RoomTransitionData;

        public RoomTransition(PlacedObject pobj) : base()
        {
            this.pobj = pobj;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            var rect = new Rect(pobj.pos.x, pobj.pos.y, data.coord.x, data.coord.y);

            foreach (var ap in this.room.game.Players)
            {
                if (ap.realizedCreature is Player p && p.room == this.room)
                {
                    Vector2 pos2 = p.mainBodyChunk.pos;
                    var pos = rect.GetClosestInteriorPoint(pos2);

                    if (Custom.DistLess(pos2, pos, data.margin))
                    {
                        if (!room.ViewedByAnyCamera(pos2, 0))
                        {
                            Debug.Log("RoomTransition sucking in!!!");

                            var to = this.room.world.GetAbstractRoom(data.toRoom);
                            var tonode = this.data.toNode;
                            ShortcutHandler.BorderVessel bv =
                                new ShortcutHandler.BorderVessel(p, AbstractRoomNode.Type.BatHive,
                                new WorldCoordinate(to.index, -1, -1, tonode),
                                data.margin, to);
                            bv.entranceNode = tonode;
                            this.room.game.shortcuts.betweenRoomsWaitingLobby.Add(bv);

                            List<AbstractPhysicalObject> allConnectedObjects = p.abstractCreature.GetAllConnectedObjects();
                            Room room = this.room;
                            for (int i = 0; i < allConnectedObjects.Count; i++)
                            {
                                if (allConnectedObjects[i].realizedObject != null)
                                {
                                    if (allConnectedObjects[i].realizedObject is Creature)
                                    {
                                        (allConnectedObjects[i].realizedObject as Creature).inShortcut = true;
                                    }
                                    room.RemoveObject(allConnectedObjects[i].realizedObject);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}