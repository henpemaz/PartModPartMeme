using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Permissions;
using Partiality.Modloader;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace MapWarp
{
    public class MapWarp : PartialityMod
    {
        public static MapWarp instance;

        public MapWarp()
        {
            this.ModID = "MapWarp";
            this.Version = "1.0";
            this.author = "Henpemaz";
            instance = this;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            // Click
            On.DevInterface.RoomPanel.Update += RoomPanel_Update;

            // Teleportation!
            On.ShortcutHandler.TeleportingCreatureArrivedInRealizedRoom += ShortcutHandler_TeleportingCreatureArrivedInRealizedRoom;
            On.AbstractCreature.Realize += AbstractCreature_Realize;

            // Region browsing
            On.DevInterface.MapPage.NewMode += MapPage_NewMode;
            On.DevInterface.MapPage.Signal += MapPage_Signal;

            On.VirtualMicrophone.NewRoom += VirtualMicrophone_NewRoom;

        }

        /// <summary>
        /// Fix gate noises following the player through rooms
        /// copied over from ExtendedGates
        /// </summary>
        private void VirtualMicrophone_NewRoom(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
        {
            orig(self, room);
            for (int i = self.soundObjects.Count - 1; i >= 0; i--)
            {
                if (self.soundObjects[i] is VirtualMicrophone.PositionedSound) // Doesn't make sense that this carries over
                {
                    // I was going to do somehtin supercomplicated like test if controller as loop was in the same room but screw it
                    //VirtualMicrophone.ObjectSound obj = (self.soundObjects[i] as VirtualMicrophone.ObjectSound);
                    //if (obj.controller != null && )
                    self.soundObjects[i].Destroy();
                    self.soundObjects.RemoveAt(i);
                }
            }
        }

        private void MapPage_Signal(On.DevInterface.MapPage.orig_Signal orig, DevInterface.MapPage self, DevInterface.DevUISignalType type, DevInterface.DevUINode sender, string message)
        {
            orig(self, type, sender, message);

            if (type != DevInterface.DevUISignalType.ButtonClick) return;
            if (string.IsNullOrEmpty(sender.IDstring) || !sender.IDstring.StartsWith("region")) return;

            var target = sender.IDstring.Substring(7);

            if(target == self.map.world.name)
            {
                Debug.Log("MapWarp: staying in " + target);
                return;
            }
            Debug.Log("MapWarp: switching map to " + target);
            var oldWorld = self.owner.game.world;
            self.owner.game.overWorld.LoadWorld(target, self.owner.game.overWorld.PlayerCharacterNumber, false);
            var newWorld = self.owner.game.world;
            
            // from gate switching Overworld.worldloaded
            if (self.owner.game.roomRealizer != null)
            {
                self.owner.game.roomRealizer = new RoomRealizer(self.owner.game.roomRealizer.followCreature, newWorld);
            }

            for (int k = 0; k < self.owner.game.Players.Count; k++)
            {
                if (self.owner.game.Players[k].realizedCreature != null && (self.owner.game.Players[k].realizedCreature as Player).objectInStomach != null)
                {
                    (self.owner.game.Players[k].realizedCreature as Player).objectInStomach.world = newWorld;
                }
            }
            self.owner.game.shortcuts.transportVessels.Clear();
            self.owner.game.shortcuts.betweenRoomsWaitingLobby.Clear();
            self.owner.game.shortcuts.borderTravelVessels.Clear();

            for (int num = 0; num < newWorld.game.cameras.Length; num++)
            {
                newWorld.game.cameras[num].hud.ResetMap(new HUD.Map.MapData(newWorld, newWorld.game.rainWorld));
                if (newWorld.game.cameras[num].hud.textPrompt.subregionTracker != null)
                {
                    newWorld.game.cameras[num].hud.textPrompt.subregionTracker.lastShownRegion = 0;
                }
            }
            oldWorld.regionState.AdaptRegionStateToWorld(-1, -1);
            oldWorld.regionState.world = null;
            newWorld.rainCycle.cycleLength = oldWorld.rainCycle.cycleLength;
            newWorld.rainCycle.timer = oldWorld.rainCycle.timer;


            MovePlayers(newWorld.abstractRooms[0], 0, true);

            self.owner.ClearSprites();
            self.owner.game.devUI = null;
            // dirty fix
            newWorld.game.cameras[0].room.abstractRoom = newWorld.abstractRooms[1];
            self.owner.game.devUI = new DevInterface.DevUI(self.owner.game);
            self.owner.game.devUI.SwitchPage(3);
        }

        // Create ui in Dev view
        private void MapPage_NewMode(On.DevInterface.MapPage.orig_NewMode orig, DevInterface.MapPage self)
        {
            orig(self);

            if (!self.canonView)
            {
                var regions = Menu.FastTravelScreen.GetRegionOrder();
                Vector2 curpos = new Vector2(120f, 560f);
                self.modeSpecificNodes.Add(new DevInterface.DevUILabel(self.owner, "regions", self, curpos, 60f, "Regions:"));
                self.subNodes.Add(self.modeSpecificNodes[self.modeSpecificNodes.Count - 1]);
                curpos.x += 20;
                curpos.y -= 30;

                for (int i = 0; i < regions.Count; i++)
                {
                    string region = regions[i];
                    self.modeSpecificNodes.Add(new DevInterface.Button(self.owner, "region_"+region, self, curpos, 40f, region));
                    self.subNodes.Add(self.modeSpecificNodes[self.modeSpecificNodes.Count - 1]);
                    curpos.y -= 20;
                }
            }
        }

        // Fix camera not following player on realize (after warping to offscreen den lol)
        private void AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
        {
            bool alreadyRealized = self.realizedCreature != null;
            orig(self);
            if (!alreadyRealized && self.realizedCreature != null && self.realizedCreature.room != null && self.FollowedByCamera(0) && self.world.game.cameras[0].room != self.realizedCreature.room)
            {
                if (self.pos.TileDefined)
                {
                    self.world.game.cameras[0].MoveCamera(self.realizedCreature.room, self.realizedCreature.room.CameraViewingPoint(self.realizedCreature.room.MiddleOfTile(self.pos.Tile)));
                }
                else self.world.game.cameras[0].MoveCamera(self.realizedCreature.room, self.realizedCreature.room.abstractRoom.nodes[self.pos.abstractNode].viewedByCamera);
            }
        }

        private void ShortcutHandler_TeleportingCreatureArrivedInRealizedRoom(On.ShortcutHandler.orig_TeleportingCreatureArrivedInRealizedRoom orig, ShortcutHandler self, ShortcutHandler.TeleportationVessel tVessel)
        {
            try
            {
                orig(self, tVessel);
            }
            catch (System.NullReferenceException)
            {
                if (!(tVessel.creature is ITeleportingCreature))
                {
                    WorldCoordinate arrival = tVessel.room.realizedRoom.LocalCoordinateOfNode(tVessel.entranceNode);
                    arrival.abstractNode = tVessel.entranceNode; // silly isnt it
                    tVessel.creature.abstractCreature.pos = arrival;
                    tVessel.creature.SpitOutOfShortCut(arrival.Tile, tVessel.room.realizedRoom, true);
                    //tVessel.creature.PlaceInRoom(tVessel.room.realizedRoom);
                }
            }
        }

        private void RoomPanel_Update(On.DevInterface.RoomPanel.orig_Update orig, DevInterface.RoomPanel self)
        {
            orig(self);

            if (self.miniMap != null && !self.CanonView && self.owner.mouseClick && self.MouseOver)
            {
                Debug.Log("MapWarp clicked on room " + self.miniMap.roomRep.room.name);
                int nodeclicked = -1;
                Vector2 mousePos = self.owner.mousePos;
                for (int i = 0; i < self.miniMap.nodeSquarePositions.Length; i++)
                {
                    Vector2 delta = mousePos - (self.miniMap.absPos + self.miniMap.nodeSquarePositions[i]);
                    if (delta.x > -8 && delta.x < 8 && delta.y > -8 && delta.y < 8)
                    {
                        nodeclicked = i;
                    }
                }
                if(nodeclicked > -1)
                {
                    Debug.Log("MapWarp clicked on node " + nodeclicked);
                    AbstractRoom room = self.miniMap.roomRep.room;
                    MovePlayers(room, nodeclicked, false);
                }
            }
        }

        private void MovePlayers(AbstractRoom room, int nodeIndex, bool betweenWorlds)
        {
            WorldCoordinate dest = new WorldCoordinate(room.index, -1, -1, nodeIndex);
            if (room.realizedRoom != null)
            {
                dest = room.realizedRoom.LocalCoordinateOfNode(nodeIndex);
                dest.abstractNode = nodeIndex; // silly isnt it
            }

            if (betweenWorlds)
            {

            }

            foreach (var p in room.world.game.Players)
            {
                if (p.realizedCreature != null && p.realizedCreature.room != null)
                {
                    if (!p.realizedCreature.inShortcut || betweenWorlds)
                    {
                        // from: Creature.SuckedIntoShortCut
                        // cleans out connected objects *and self* from room.
                        Room realizedRoom = p.realizedCreature.room;
                        WorldCoordinate origin = p.pos;
                        List<AbstractPhysicalObject> allConnectedObjects = p.GetAllConnectedObjects(); // The catch: includes self
                        for (int i = 0; i < allConnectedObjects.Count; i++)
                        {
                            if (allConnectedObjects[i].realizedObject != null)
                            {
                                if (allConnectedObjects[i].realizedObject is Creature crit)
                                {
                                    crit.inShortcut = true;
                                }
                                realizedRoom.RemoveObject(allConnectedObjects[i].realizedObject);
                                realizedRoom.CleanOutObjectNotInThisRoom(allConnectedObjects[i].realizedObject);
                            }
                            if (betweenWorlds)
                            {
                                allConnectedObjects[i].world = room.world;
                                allConnectedObjects[i].pos = dest;
                                if (allConnectedObjects[i] is AbstractCreature abscre && abscre.creatureTemplate.AI)
                                {
                                    abscre.abstractAI.NewWorld(room.world);
                                    abscre.InitiateAI();
                                }
                            }
                        }
                        room.world.game.shortcuts.CreatureTeleportOutOfRoom(p.realizedCreature, origin, dest);
                    }
                }
                else // move abstract creetchere
                {
                    if (!betweenWorlds)
                    {
                        p.Move(dest); // nullrefs on rooms between worlds...
                    }
                    else
                    {
                        // todo
                    }
                }
            }
        }
    }
}