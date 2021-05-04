using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ShelterBehaviors
{
    public static class ExtensionsForThingsIHateTypingOut
    {
        public static float Abs(this float f)
        {
            return Mathf.Abs(f);
        }
        public static float Sign(this float f)
        {
            return Mathf.Sign(f);
        }

        public static bool Contains(this IntRect rect, IntVector2 pos, bool incl = true) // Cmon joar
        {
            if(incl) return pos.x >= rect.left && pos.x <= rect.right && pos.y >= rect.bottom && pos.y <= rect.top;
            return pos.x > rect.left && pos.x < rect.right && pos.y > rect.bottom && pos.y < rect.top;
        }

        public static Vector2 ToCardinals(this Vector2 dir)
        {
            return new Vector2(Vector2.Dot(Vector2.right, dir).Abs() > 0.707 ? Vector2.Dot(Vector2.right, dir).Sign() : 0, Vector2.Dot(Vector2.up, dir).Abs() > 0.707 ? Vector2.Dot(Vector2.up, dir).Sign() : 0f);
        }
    }

    internal class ShelterBehaviorManager : UpdatableAndDeletable, INotifyWhenRoomIsReady
    {
        //private Room room;
        private bool noVanillaDoors;
        private IntVector2 vanillaSpawnPosition;
        private List<IntVector2> spawnPositions;
        private List<ShelterDoor> customDoors;
        private bool holdToTrigger;
        private bool holdToTriggerTutorial;
        private List<IntRect> triggers;
        private List<IntRect> noTriggers;
        private bool broken;
        private int extraTimer;
        private int extraTimerCounter;
        private bool closing;
        private ConsumableShelterObject consumableShelterObject;

        private AttachedField<Player, int> actualForceSleepCounter;


        bool _debug = true;
        private int noDoorCloseCount;
        private bool hasNoDoors;
        private ShelterDoor tempSpawnPosHackDoor;
        private bool deleteHackDoorNextFrame;

        private void ContitionalLog(string str)
        {
            if (_debug && UnityEngine.Input.GetKeyDown("l"))
            {
                Debug.LogError(str);
            }
        }

        public ShelterBehaviorManager(Room instance)
        {
            this.room = instance;
            spawnPositions = new List<IntVector2>();
            customDoors = new List<ShelterDoor>();
            triggers = new List<IntRect>();
            noTriggers = new List<IntRect>();

            this.broken = room.shelterDoor.Broken;
            this.vanillaSpawnPosition = room.shelterDoor.playerSpawnPos;

            actualForceSleepCounter = new AttachedField<Player, int>();
        }

        public void ShortcutsReady()
        {
            // housekeeping once all objects are placed
            this.hasNoDoors = noVanillaDoors && (customDoors.Count == 0);

            if (hasNoDoors)
            {
                this.tempSpawnPosHackDoor = new ShelterDoor(room);
                tempSpawnPosHackDoor.playerSpawnPos = GetSpawnPosition(0);
                room.AddObject(tempSpawnPosHackDoor);
            }
        }

        public void AIMapReady()
        {
            deleteHackDoorNextFrame = true;
        }


        public override void Update(bool eu)
        {
            if (deleteHackDoorNextFrame) {
                if (tempSpawnPosHackDoor != null)
                {
                    tempSpawnPosHackDoor.Destroy();
                    room.updateList.Remove(tempSpawnPosHackDoor);
                    room.drawableObjects.Remove(tempSpawnPosHackDoor);
                    // uuuuuh door has no slatedfordeletion protections and tries to acces room after deletion/removal
                    for (int i = 0; i < room.game.cameras.Length; i++)
                    {
                        for (int j = 0; j < room.game.cameras[i].spriteLeasers.Count; j++)
                        {
                            if (room.game.cameras[i].spriteLeasers[j].drawableObject == tempSpawnPosHackDoor)
                            {
                                room.game.cameras[i].spriteLeasers[j].CleanSpritesAndRemove();
                                goto DoneRemovingLeaser;
                            }
                        }
                    }
                DoneRemovingLeaser:
                    tempSpawnPosHackDoor = null;
                }
            }

            base.Update(eu);
            ContitionalLog("Update");
            if (noVanillaDoors)
            {
                ContitionalLog("Update no-vanilla-doors");
                // From HUD update
                for (int i = 0; i < room.game.cameras.Length; i++)
                {
                    if (room.game.cameras[i].room == room && room.game.cameras[i].hud != null)
                    {
                        ContitionalLog("Updated HUD");
                        HUD.HUD hud = room.game.cameras[i].hud;
                        hud.showKarmaFoodRain = (hud.owner.RevealMap ||
                            ((hud.owner as Player).room != null && (hud.owner as Player).room.abstractRoom.shelter && (hud.owner as Player).room.abstractRoom.realizedRoom != null && !this.broken));
                    }
                }
                // From Player update
                for (int i = 0; i < room.game.Players.Count; i++)
                {
                    if (room.game.Players[i].realizedCreature != null && room.game.Players[i].realizedCreature.room == room)
                    {
                        ContitionalLog("Updating player " + i);
                        Player p = room.game.Players[i].realizedCreature as Player;
                        if (p.room.abstractRoom.shelter && p.room.game.IsStorySession && !p.dead && !p.Sleeping && !broken)// && p.room.shelterDoor != null && !p.room.shelterDoor.Broken)
                        {
                            if (!p.stillInStartShelter && p.FoodInRoom(p.room, false) >= ((!p.abstractCreature.world.game.GetStorySession.saveState.malnourished) ? p.slugcatStats.foodToHibernate : p.slugcatStats.maxFood))
                            {
                                p.readyForWin = true;
                                p.forceSleepCounter = 0;
                                ContitionalLog("ready a");
                            }
                            else if (p.room.world.rainCycle.timer > p.room.world.rainCycle.cycleLength)
                            {
                                p.readyForWin = true;
                                p.forceSleepCounter = 0;
                                ContitionalLog("ready b");
                            }
                            else if (p.input[0].y < 0 && !p.input[0].jmp && !p.input[0].thrw && !p.input[0].pckp && p.IsTileSolid(1, 0, -1) && !p.abstractCreature.world.game.GetStorySession.saveState.malnourished && p.FoodInRoom(p.room, false) > 0 && p.FoodInRoom(p.room, false) < p.slugcatStats.foodToHibernate && (p.input[0].x == 0 || ((!p.IsTileSolid(1, -1, -1) || !p.IsTileSolid(1, 1, -1)) && p.IsTileSolid(1, p.input[0].x, 0))))
                            {
                                p.forceSleepCounter++;
                                ContitionalLog("force");
                            }
                            else
                            {
                                p.forceSleepCounter = 0;
                                ContitionalLog("not ready");
                            }
                            //if (Custom.ManhattanDistance(p.abstractCreature.pos.Tile, p.room.shortcuts[0].StartTile) > 6)
                            //{
                            //    if (p.readyForWin && p.touchedNoInputCounter > 20)
                            //    {
                            //        // AAAAAaaa
                            //        p.room.shelterDoor.Close();
                            //    }
                            //    else if (p.forceSleepCounter > 260)
                            //    {
                            //        // Aaaaaaa
                            //        p.sleepCounter = -24;
                            //        p.room.shelterDoor.Close();
                            //    }
                            //}
                        }
                    }
                }
            } // end noVanillaDoors


            if (!closing && !broken)
            {
                ContitionalLog("Update not-closing");
                PreventVanillaClose();
                // handle player sleep and triggers
                for (int i = 0; i < room.game.Players.Count; i++)
                {
                    if (room.game.Players[i].realizedCreature != null && room.game.Players[i].realizedCreature.room == room)
                    {
                        Player p = room.game.Players[i].realizedCreature as Player;
                        if (p.room.abstractRoom.shelter && p.room.game.IsStorySession && !p.dead)// && p.room.shelterDoor != null && !p.room.shelterDoor.Broken)
                        {
                            ContitionalLog("found player " + i);
                            if (holdToTrigger) p.readyForWin = false;
                            if (!PlayersInTriggerZone())
                            {
                                ContitionalLog("player NOT in trigger zone");
                                p.readyForWin = false;
                                p.forceSleepCounter = 0;
                                actualForceSleepCounter[p] = 0;
                                p.touchedNoInputCounter = Mathf.Min(p.touchedNoInputCounter, 19);
                                p.sleepCounter = 0;
                                this.extraTimerCounter = extraTimer;
                            }
                            else
                            {
                                ContitionalLog("player in trigger zone");
                            }

                            if (p.readyForWin && p.touchedNoInputCounter > 20)
                            {
                                ContitionalLog("ready not moving");
                                extraTimerCounter--;
                                if (extraTimerCounter <= 0)
                                {
                                    ContitionalLog("CLOSE due to ready");
                                    Close();
                                }
                            }
                            else if (p.readyForWin)
                            {
                                ContitionalLog("ready but moving");
                                extraTimerCounter = extraTimer;
                            }
                            else if (p.forceSleepCounter > 260 || actualForceSleepCounter[p] > 260)
                            {
                                ContitionalLog("CLOSE due to force sleep");
                                p.sleepCounter = -24;
                                Close();
                            }
                            else if (holdToTrigger && p.input[0].y < 0 && !p.input[0].jmp && !p.input[0].thrw && !p.input[0].pckp && p.IsTileSolid(1, 0, -1) && (p.input[0].x == 0 || ((!p.IsTileSolid(1, -1, -1) || !p.IsTileSolid(1, 1, -1)) && p.IsTileSolid(1, p.input[0].x, 0))))
                            {
                                ContitionalLog("force sleep hold to trigger");
                                // Might need something to preserve last counter through player update, zeroes if ready4win
                                actualForceSleepCounter[p] += 4; // gets uses default for int so this works
                                p.forceSleepCounter = actualForceSleepCounter[p];
                            }
                            //}
                        }
                    }
                }
            }
            
            if(closing && hasNoDoors)
            {
                // Manage no-door logic
                noDoorCloseCount--;

                if (noDoorCloseCount == 60)
                {
                    for (int j = 0; j < this.room.game.Players.Count; j++)
                    {
                        if (this.room.game.Players[j].realizedCreature != null && (this.room.game.Players[j].realizedCreature as Player).FoodInRoom(this.room, false) >= (this.room.game.Players[j].realizedCreature as Player).slugcatStats.foodToHibernate)
                        {
                            (this.room.game.Players[j].realizedCreature as Player).sleepWhenStill = true;
                        }
                    }
                }
                if (noDoorCloseCount == 20)
                {
                    for (int k = 0; k < this.room.game.Players.Count; k++)
                    {
                        if (this.room.game.Players[k].realizedCreature != null && (this.room.game.Players[k].realizedCreature as Player).FoodInRoom(this.room, false) < ((!this.room.game.GetStorySession.saveState.malnourished) ? 1 : (this.room.game.Players[k].realizedCreature as Player).slugcatStats.maxFood))
                        {
                            this.room.game.GoToStarveScreen();
                        }
                    }
                }
                if(noDoorCloseCount == 0)
                {
                    bool flag = true;
                    for (int i = 0; i < this.room.game.Players.Count; i++)
                    {
                        if (!this.room.game.Players[i].state.alive)
                        {
                            flag = false;
                        }
                    }
                    if (flag)
                    {
                        this.room.game.Win((this.room.game.Players[0].realizedCreature as Player).FoodInRoom(this.room, false) < (this.room.game.Players[0].realizedCreature as Player).slugcatStats.foodToHibernate);
                    }
                    else
                    {
                        this.room.game.GoToDeathScreen();
                    }
                }
            }
        }

        private void Close()
        {
            closing = true;
            noDoorCloseCount = 80;
            if (!noVanillaDoors) room.shelterDoor.Close();
            foreach (var door in customDoors)
            {
                door.Close();
            }
            if (consumableShelterObject != null) consumableShelterObject.Consume(room);
            ContitionalLog("CLOSE");
        }

        private bool PlayersInTriggerZone()
        {
            for (int i = 0; i < room.game.Players.Count; i++) // Any alive players missing ? Still in starting shelter ?
            {
                AbstractCreature ap = room.game.Players[i];
                if (!ap.state.dead && ap.realizedCreature != null && ap.realizedCreature.room != room) return false;
                if ((ap.realizedCreature as Player).stillInStartShelter) return false;
            }
            if (triggers.Count == 0 && noTriggers.Count == 0) // No trigges, possibly vanilla behavior
            {
                if (noVanillaDoors) return true;
                for (int i = 0; i < room.game.Players.Count; i++) // Any alive players missing ?
                {
                    AbstractCreature ap = room.game.Players[i];
                    if (Custom.ManhattanDistance(ap.pos.Tile, this.room.shortcuts[0].StartTile) > 6) return false;
                }
                return true;
            }
            else
            {
                for (int i = 0; i < room.game.Players.Count; i++)
                {
                    AbstractCreature ap = room.game.Players[i];
                    foreach (var rect in triggers)
                    {
                        if (!rect.Contains(ap.pos.Tile)) return false; // anyone out of a positive trigger
                    }
                    foreach (var rect in noTriggers)
                    {
                        if (rect.Contains(ap.pos.Tile)) return false; // anyone in a negative trigger
                    }
                }
            }

            return true;
        }

        private void PreventVanillaClose()
        {
            if (!noVanillaDoors) room.shelterDoor.closeSpeed = Mathf.Min(0f, room.shelterDoor.closeSpeed);
        }

        internal void RemoveVanillaDoors()
        {
            room.shelterDoor.Destroy();
            room.CleanOutObjectNotInThisRoom(room.shelterDoor);
            room.shelterDoor = null;
            this.noVanillaDoors = true;
        }

        internal IntVector2 GetSpawnPosition(int salt)
        {
            int oldseed = UnityEngine.Random.seed;
            try
            {
                if(room.game.IsStorySession)
                    UnityEngine.Random.seed = salt + room.game.clock + room.game.GetStorySession.saveState.seed + room.game.GetStorySession.saveState.cycleNumber + room.game.GetStorySession.saveState.deathPersistentSaveData.deaths + room.game.GetStorySession.saveState.deathPersistentSaveData.survives;
                if (noVanillaDoors)
                {
                    if (spawnPositions.Count > 0) return spawnPositions[UnityEngine.Random.Range(0, spawnPositions.Count)];
                    return vanillaSpawnPosition;
                }

                int roll = UnityEngine.Random.Range(0, spawnPositions.Count + 1);
                if (spawnPositions.Count < roll) return spawnPositions[roll];
                return vanillaSpawnPosition;
            }
            finally
            {
                UnityEngine.Random.seed = oldseed;
            }
        }

        internal void AddPlacedDoor(PlacedObject placedObject)
        {
            int preCounter = room.game.rainWorld.progression.miscProgressionData.starvationTutorialCounter; // Prevent starvation tutorial dupes
            if (room.game.IsStorySession)
                room.game.rainWorld.progression.miscProgressionData.starvationTutorialCounter = 0;
            ShelterDoor newDoor = new ShelterDoor(room);
            if (room.game.IsStorySession)
                room.game.rainWorld.progression.miscProgressionData.starvationTutorialCounter = preCounter;

            //Vector2 origin = placedObject.pos;
            IntVector2 originTile = room.GetTilePosition(placedObject.pos);
            Vector2 dir = (placedObject.data as PlacedObject.ResizableObjectData).handlePos.normalized;
            dir = dir.ToCardinals();

            newDoor.pZero = room.MiddleOfTile(originTile);
            newDoor.dir = dir;
            for (int n = 0; n < 4; n++)
            {
                newDoor.closeTiles[n] = originTile + IntVector2.FromVector2(dir) * (n + 2);
            }
            newDoor.pZero += newDoor.dir * 60f;
            newDoor.perp = Custom.PerpendicularVector(newDoor.dir);

            newDoor.playerSpawnPos = GetSpawnPosition(customDoors.Count);

            customDoors.Add(newDoor);
            room.AddObject(newDoor);
        }

        internal void IncreaseTimer()
        {
            this.extraTimer += 20; ;
        }

        internal void SetHoldToTrigger()
        {
            this.holdToTrigger = true;
        }

        internal void HoldToTriggerTutorial(int consumableIndex)
        {
            if (!holdToTriggerTutorial)
            {
                this.holdToTriggerTutorial = true;

                room.AddObject(new HoldToTriggerTutorialObject(room, consumableIndex));
            }
        }

        internal void AddSpawnPosition(PlacedObject placedObject)
        {
            this.spawnPositions.Add(room.GetTilePosition(placedObject.pos));
            // re-shuffle
            for (int i = 0; i < customDoors.Count; i++)
            {
                customDoors[i].playerSpawnPos = GetSpawnPosition(i);
            }
        }

        internal void AddTriggerZone(PlacedObject placedObject)
        {
            this.triggers.Add((placedObject.data as PlacedObject.GridRectObjectData).Rect);
        }

        internal void AddNoTriggerZone(PlacedObject placedObject)
        {
            this.noTriggers.Add((placedObject.data as PlacedObject.GridRectObjectData).Rect);
        }

        internal void ProcessConsumable(PlacedObject placedObject, int index)
        {
            this.consumableShelterObject = new ConsumableShelterObject(room, room.abstractRoom.index, index, placedObject.data as PlacedObject.ConsumableObjectData);
            if (this.consumableShelterObject.isConsumed)
            {
                this.broken = true;
                this.room.world.brokenShelters[this.room.abstractRoom.shelterIndex] = true;
            }
        }

        private class ConsumableShelterObject
        {
            private int originRoom;
            private int placedObjectIndex;
            private PlacedObject.ConsumableObjectData consumableObjectData;
            internal bool isConsumed;

            public ConsumableShelterObject(Room room, int roomIndex, int objectIndex, PlacedObject.ConsumableObjectData consumableObjectData)
            {
                this.originRoom = roomIndex;
                this.placedObjectIndex = objectIndex;
                this.consumableObjectData = consumableObjectData;

                if (room.game.session is StoryGameSession)
                {
                    this.isConsumed = (room.game.session as StoryGameSession).saveState.ItemConsumed(room.world, false, roomIndex, objectIndex);
                }
            }

            public void Consume(Room room)
            {
                if (this.isConsumed)
                {
                    return;
                }
                this.isConsumed = true;
                Debug.LogError("CONSUMED: ConsumableShelterObject ;)");
                if (room.world.game.session is StoryGameSession)
                {
                    (room.world.game.session as StoryGameSession).saveState.ReportConsumedItem(room.world, false, this.originRoom, this.placedObjectIndex, UnityEngine.Random.Range(consumableObjectData.minRegen, consumableObjectData.maxRegen + 1));
                }
            }
        }

        private class HoldToTriggerTutorialObject : UpdatableAndDeletable
        {
            public HoldToTriggerTutorialObject(Room room, int objectIndex)
            {
                this.room = room;
                placedObjectIndex = objectIndex;
                // player loaded in room
                foreach (var p in room.game.Players)
                {
                    if (p.pos.room == room.abstractRoom.index)
                    {
                        this.Destroy();
                    }
                }

                // recently displayed
                if (room.game.session is StoryGameSession)
                {
                    if((room.game.session as StoryGameSession).saveState.ItemConsumed(room.world, false, room.abstractRoom.index, objectIndex))
                    {
                        this.Destroy();
                    }
                }

            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (base.slatedForDeletetion) return;
                if (!room.BeingViewed) message = 0;
                else if (this.room.game.session.Players[0].realizedCreature != null && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.textPrompt != null && this.room.game.cameras[0].hud.textPrompt.messages.Count < 1)
                {
                    switch (this.message)
                    {
                        case 0:
                            this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("This place is safe from the rain and most predators"), 20, 160, true, true);
                            this.message++;
                            break;
                        case 1:
                            this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("With enough food, hold DOWN to hibernate"), 40, 160, false, true);
                            this.message++;
                            break;
                        default:
                            this.Consume();
                            break;
                    }
                }
            }
            public int message;
            private int placedObjectIndex;

            public void Consume()
            {
                Debug.Log("CONSUMED: HoldToTriggerTutorialObject ;)");
                if (room.world.game.session is StoryGameSession)
                {
                    (room.world.game.session as StoryGameSession).saveState.ReportConsumedItem(room.world, false, room.abstractRoom.index, this.placedObjectIndex, 3);
                }
                this.Destroy();
            }
        }
    }
}