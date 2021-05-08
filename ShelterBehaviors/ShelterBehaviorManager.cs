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
        public static float Abs(this int f)
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


    public interface IReactToShelterClosing
    {
        void OnShelterClose();
    }

    internal class ShelterBehaviorManager : UpdatableAndDeletable, INotifyWhenRoomIsReady
    {
        //private Room room;
        private bool noVanillaDoors;
        private IntVector2 vanillaSpawnPosition;
        private List<IntVector2> spawnPositions;
        private List<ShelterDoor> customDoors;
        private bool holdToTrigger;
        private List<IntRect> triggers;
        private List<IntRect> noTriggers;
        private bool broken;
        private int noMovingCounter;
        private bool closing;

        private AttachedField<Player, int> actualForceSleepCounter;


        bool _debug = true;
        private int noDoorCloseCount;
        private bool hasNoDoors;
        private ShelterDoor tempSpawnPosHackDoor;
        private bool deleteHackDoorNextFrame;
        private bool isConsumed;
        private int placedObjectIndex;
        private List<IReactToShelterClosing> subscribers;
        private readonly PlacedObject pObj;
        private readonly ManagedPlacedObjects.PlacedObjectsManager.ManagedData data;

        private void ContitionalLog(string str)
        {
            if (_debug && UnityEngine.Input.GetKeyDown("l"))
            {
                Debug.LogError(str);
            }
        }

        public ShelterBehaviorManager(Room instance, PlacedObject pObj)
        {
            this.room = instance;
            this.pObj = pObj;
            this.data = pObj.data as ManagedPlacedObjects.PlacedObjectsManager.ManagedData;
            this.placedObjectIndex = room.roomSettings.placedObjects.IndexOf(pObj);
            spawnPositions = new List<IntVector2>();
            customDoors = new List<ShelterDoor>();
            triggers = new List<IntRect>();
            noTriggers = new List<IntRect>();
            subscribers = new List<IReactToShelterClosing>();

            noVanillaDoors = false;
            this.broken = room.shelterDoor.Broken;
            this.vanillaSpawnPosition = room.shelterDoor.playerSpawnPos;

            actualForceSleepCounter = new AttachedField<Player, int>();

            if (data.GetValue<bool>("nvd")) this.RemoveVanillaDoors();
            if (data.GetValue<bool>("htt")) holdToTrigger = true;

            if (room.game.session is StoryGameSession)
            {
                this.isConsumed = (room.game.session as StoryGameSession).saveState.ItemConsumed(room.world, false, this.room.abstractRoom.index, this.placedObjectIndex);
            }
            if (this.isConsumed)
            {
                this.broken = true;
                this.room.world.brokenShelters[this.room.abstractRoom.shelterIndex] = true;
            }

            for (int i = 0; i < instance.roomSettings.placedObjects.Count; i++)
            {
                if (instance.roomSettings.placedObjects[i].active)
                {
                    if (instance.roomSettings.placedObjects[i].type == ShelterBehaviorsMod.EnumExt_ShelterBehaviorsMod.ShelterBhvrPlacedDoor)
                    {
                        this.AddPlacedDoor(instance.roomSettings.placedObjects[i]);
                    }
                    else if (instance.roomSettings.placedObjects[i].type == ShelterBehaviorsMod.EnumExt_ShelterBehaviorsMod.ShelterBhvrTriggerZone)
                    {
                        this.AddTriggerZone(instance.roomSettings.placedObjects[i]);
                    }
                    else if (instance.roomSettings.placedObjects[i].type == ShelterBehaviorsMod.EnumExt_ShelterBehaviorsMod.ShelterBhvrNoTriggerZone)
                    {
                        this.AddNoTriggerZone(instance.roomSettings.placedObjects[i]);
                    }
                    else if (instance.roomSettings.placedObjects[i].type == ShelterBehaviorsMod.EnumExt_ShelterBehaviorsMod.ShelterBhvrSpawnPosition)
                    {
                        this.AddSpawnPosition(instance.roomSettings.placedObjects[i]);
                    }
                }
            }
        }

        public void Consume()
        {
            if (!data.GetValue<bool>("cs")) return;
            if (this.isConsumed) return;
            this.isConsumed = true;
            Debug.Log("CONSUMED: Consumable Shelter ;)");
            if (room.world.game.session is StoryGameSession)
            {
                int minCycles = data.GetValue<int>("csmin");
                (room.world.game.session as StoryGameSession).saveState.ReportConsumedItem(room.world, false, this.room.abstractRoom.index, this.placedObjectIndex,
                    (minCycles < 0) ? -1 : UnityEngine.Random.Range(minCycles, data.GetValue<int>("csmax") + 1));
            }
        }

        public void ShortcutsReady()
        {
            // housekeeping once all objects are placed
            this.hasNoDoors = noVanillaDoors && (customDoors.Count == 0);

            if (hasNoDoors)
            {
                this.tempSpawnPosHackDoor = new ShelterDoor(room);
                tempSpawnPosHackDoor.closeTiles = new IntVector2[0];
                tempSpawnPosHackDoor.playerSpawnPos = GetSpawnPosition(0);
                room.AddObject(tempSpawnPosHackDoor);
            }

            for (int i = 0; i < room.updateList.Count; i++)
            {
                if (room.updateList[i] is IReactToShelterClosing) this.subscribers.Add(room.updateList[i] as IReactToShelterClosing);
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
                deleteHackDoorNextFrame = false;
            }

            base.Update(eu);
            ContitionalLog("Update");
            if (noVanillaDoors)
            {
                ContitionalLog("Update no-vanilla-doors");
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
                        }
                    }
                }
                // From HUD update
                for (int i = 0; i < room.game.cameras.Length; i++)
                {
                    if (room.game.cameras[i].room == room && room.game.cameras[i].hud != null)
                    {
                        ContitionalLog("Updated HUD");
                        HUD.HUD hud = room.game.cameras[i].hud;
                        hud.showKarmaFoodRain = (hud.owner.RevealMap ||
                            ((hud.owner as Player).room != null && (hud.owner as Player).room.abstractRoom.shelter && (hud.owner as Player).room.abstractRoom.realizedRoom != null && !this.broken));
                        if (holdToTrigger && (hud.owner as Player).readyForWin) // trigger sleep
                        {
                            hud.foodMeter.forceSleep = 0;
                            hud.foodMeter.showSurvLim = (float)hud.foodMeter.survivalLimit;
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
                            // if (holdToTrigger) p.readyForWin = false; // lets make a better use of this flag shall we
                            if (!PlayersInTriggerZone())
                            {
                                ContitionalLog("player NOT in trigger zone");
                                p.readyForWin = false;
                                p.forceSleepCounter = 0;
                                actualForceSleepCounter[p] = 0;
                                p.touchedNoInputCounter = Mathf.Min(p.touchedNoInputCounter, 19);
                                p.sleepCounter = 0;
                                this.noMovingCounter = data.GetValue<int>("ftt");
                            }
                            else
                            {
                                ContitionalLog("player in trigger zone");
                            }

                            if(p.touchedNoInputCounter == 0)
                            {
                                noMovingCounter = data.GetValue<int>("ftt");
                            }
                            
                            if (!holdToTrigger && p.readyForWin && p.touchedNoInputCounter > 1)
                            {
                                ContitionalLog("ready not moving");
                                noMovingCounter--;
                                if (noMovingCounter <= 0)
                                {
                                    ContitionalLog("CLOSE due to ready");
                                    Close();
                                }
                            }
                            else if (p.forceSleepCounter > 260 || actualForceSleepCounter[p] > 260)
                            {
                                ContitionalLog("CLOSE due to force sleep");
                                p.sleepCounter = -24;
                                Close();
                            }
                            else if (p.readyForWin && holdToTrigger && p.input[0].y < 0 && !p.input[0].jmp && !p.input[0].thrw && !p.input[0].pckp && p.IsTileSolid(1, 0, -1) && (p.input[0].x == 0 || ((!p.IsTileSolid(1, -1, -1) || !p.IsTileSolid(1, 1, -1)) && p.IsTileSolid(1, p.input[0].x, 0))))
                            {
                                ContitionalLog("force sleep hold to trigger");
                                // need something to preserve last counter through player update, zeroes if ready4win
                                actualForceSleepCounter[p] += data.GetValue<int>("htts") - (noVanillaDoors ? 0 : 1); // gets uses default for int so this works
                                p.forceSleepCounter = actualForceSleepCounter[p];
                            } else if (noVanillaDoors && p.input[0].y < 0 && !p.input[0].jmp && !p.input[0].thrw && !p.input[0].pckp && p.IsTileSolid(1, 0, -1) && (p.input[0].x == 0 || ((!p.IsTileSolid(1, -1, -1) || !p.IsTileSolid(1, 1, -1)) && p.IsTileSolid(1, p.input[0].x, 0))))
                            {
                                // allow starve
                                actualForceSleepCounter[p] += 1;
                                p.forceSleepCounter = actualForceSleepCounter[p];
                            }
                            else
                            {
                                actualForceSleepCounter[p] = 0;
                            }
                        }
                    }
                }
            }
            
            if(closing && hasNoDoors)
            {
                // Manage no-door logic
                noDoorCloseCount++;

                if (noDoorCloseCount == data.GetValue<int>("fts"))
                {
                    for (int j = 0; j < this.room.game.Players.Count; j++)
                    {
                        if (this.room.game.Players[j].realizedCreature != null && (this.room.game.Players[j].realizedCreature as Player).FoodInRoom(this.room, false) >= (this.room.game.Players[j].realizedCreature as Player).slugcatStats.foodToHibernate)
                        {
                            (this.room.game.Players[j].realizedCreature as Player).sleepWhenStill = true;
                        }
                    }
                }
                if (noDoorCloseCount == data.GetValue<int>("ftsv"))
                {
                    for (int k = 0; k < this.room.game.Players.Count; k++)
                    {
                        if (this.room.game.Players[k].realizedCreature != null && (this.room.game.Players[k].realizedCreature as Player).FoodInRoom(this.room, false) < ((!this.room.game.GetStorySession.saveState.malnourished) ? 1 : (this.room.game.Players[k].realizedCreature as Player).slugcatStats.maxFood))
                        {
                            this.room.game.GoToStarveScreen();
                        }
                    }
                }
                if(noDoorCloseCount == data.GetValue<int>("ftw"))
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
            noDoorCloseCount = 0;
            if (!noVanillaDoors) room.shelterDoor.Close();
            foreach (var door in customDoors)
            {
                door.Close();
            }
            foreach (var sub in subscribers)
            {
                sub.OnShelterClose();
            }
            Consume();
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
                    if (Custom.ManhattanDistance(ap.pos.Tile, this.room.shortcuts[0].StartTile) <= 6) return false;
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
            IntVector2 dir = (placedObject.data as ManagedPlacedObjects.PlacedObjectsManager.ManagedData).GetValue<IntVector2>("dir");
            //dir = dir.ToCardinals();

            newDoor.pZero = room.MiddleOfTile(originTile);
            newDoor.dir = dir.ToVector2();
            for (int n = 0; n < 4; n++)
            {
                newDoor.closeTiles[n] = originTile + dir * (n + 2);
            }
            newDoor.pZero += newDoor.dir * 60f;
            newDoor.perp = Custom.PerpendicularVector(newDoor.dir);

            newDoor.playerSpawnPos = GetSpawnPosition(customDoors.Count);

            customDoors.Add(newDoor);
            room.AddObject(newDoor);
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

        public class HoldToTriggerTutorialObject : UpdatableAndDeletable
        {
            public HoldToTriggerTutorialObject(Room room, PlacedObject pObj)
            {
                this.room = room;
                placedObject = pObj;
                placedObjectIndex = room.roomSettings.placedObjects.IndexOf(pObj);
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
                    if((room.game.session as StoryGameSession).saveState.ItemConsumed(room.world, false, room.abstractRoom.index, placedObjectIndex))
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
            private PlacedObject placedObject;
            private int placedObjectIndex;

            public void Consume()
            {
                Debug.Log("CONSUMED: HoldToTriggerTutorialObject ;)");
                if (room.world.game.session is StoryGameSession)
                {
                    (room.world.game.session as StoryGameSession).saveState.ReportConsumedItem(room.world, false, room.abstractRoom.index, this.placedObjectIndex, (placedObject.data as ManagedPlacedObjects.PlacedObjectsManager.ManagedData).GetValue<int>("htttcd"));
                }
                this.Destroy();
            }
        }
    }
}