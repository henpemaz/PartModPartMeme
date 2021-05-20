using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal class SpawnCustomizations
    {
        internal static void Apply()
        {

            // ID spawndata support
            On.WorldLoader.ctor += WorldLoader_ctor;
            On.RainWorldGame.GetNewID_1 += RainWorldGame_GetNewID_1;

            // Assignable Trader support
            On.ScavengersWorldAI.Trader.ScavScore += Trader_ScavScore;

            // Personality Traits support
            On.AbstractCreature.ctor += AbstractCreature_ctor; ;
            On.CreatureState.LoadFromString += CreatureState_LoadFromString;

            // Innate RelationhShip support
            On.CreatureState.ctor += CreatureState_ctor;
            // On.CreatureState.LoadFromString += CreatureState_LoadFromString;

            // Friendable, follower
            On.AbstractCreature.InitiateAI += AbstractCreature_InitiateAI;
            On.CicadaAI.Update += CicadaAI_Update;
            On.CicadaAbstractAI.AbstractBehavior += CicadaAbstractAI_AbstractBehavior;
            On.ScavengerAI.Update += ScavengerAI_Update;
            On.ScavengerAbstractAI.AbstractBehavior += ScavengerAbstractAI_AbstractBehavior;
            On.ScavengerAI.LikeOfPlayer += ScavengerAI_LikeOfPlayer;
        }

        private static float ScavengerAI_LikeOfPlayer(On.ScavengerAI.orig_LikeOfPlayer orig, ScavengerAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            float val = orig(self, dRelation);
            if (self.friendTracker != null && self.friendTracker.friend.abstractCreature.ID == dRelation.trackerRep.representedCreature.ID)
            {
                val += self.friendTracker.Urgency;
            }
            return Mathf.Clamp(val, 0f, 1f);
        }
        private static void ScavengerAbstractAI_AbstractBehavior(On.ScavengerAbstractAI.orig_AbstractBehavior orig, ScavengerAbstractAI self, int time)
        {
            orig(self, time);
            //var AbstractCreatureA_AbstractBehaviorI = typeof(AbstractCreatureAI).GetMethod("AbstractBehavior").MethodHandle.GetFunctionPointer();
            //var baseBehavior = (Action<int>)Activator.CreateInstance(typeof(Action<int>), self, AbstractCreatureA_AbstractBehaviorI);
            self.MigrationBehavior(0); // time = zero => zero rolls of random migration, just followcreature logic
        }
        public static class EnumExt_SpawnCustomizations
        {
            public static ScavengerAI.Behavior FollowFriend;
        }
        private static void ScavengerAI_Update(On.ScavengerAI.orig_Update orig, ScavengerAI self)
        {
            orig(self);
            if (self.friendTracker == null || !self.friendTracker.followClosestFriend) return;
            AIModule aimodule = self.utilityComparer.HighestUtilityModule();
            self.currentUtility = self.utilityComparer.HighestUtility();
            if (aimodule != null)
            {
                if (aimodule is FriendTracker && self.currentUtility >= 0.1f)
                {
                    self.behavior = EnumExt_SpawnCustomizations.FollowFriend;
                }
            }
            if (self.behavior == EnumExt_SpawnCustomizations.FollowFriend)
            {
                if (self.friendTracker.friend == null)
                {
                    self.behavior = ScavengerAI.Behavior.Idle;
                }
                else
                {
                    self.creature.abstractAI.SetDestination(self.friendTracker.friendDest);
                    self.focusCreature = self.tracker.RepresentationForCreature(self.friendTracker.friend.abstractCreature, false);
                }
            }
            if (self.friendTracker.friend == null)
            {
                // Abstract follow-creature logic might do something about it
                if (UnityEngine.Random.value < 0.02)
                {
                    self.creature.abstractAI.AbstractBehavior(1);
                }
                if (self.creature.abstractAI.followCreature != null)
                {
                    self.friendTracker.friend = self.creature.abstractAI.followCreature.realizedCreature;
                    self.friendTracker.friendRel = self.creature.state.socialMemory.GetRelationship(self.creature.abstractAI.followCreature.ID);
                }
            }
        }
        private static void CicadaAI_Update(On.CicadaAI.orig_Update orig, CicadaAI self)
        {
            // reordered to prevent IDLE from taking precedence
            if (self.friendTracker != null && self.friendTracker.followClosestFriend)
            {
                AIModule aimodule = self.utilityComparer.HighestUtilityModule();
                self.currentUtility = self.utilityComparer.HighestUtility();
                if (aimodule != null)
                {
                    if (aimodule is FriendTracker && self.currentUtility >= 0.1f)
                    {
                        self.behavior = CicadaAI.Behavior.FollowFriend;
                    }
                }
                if (self.behavior == CicadaAI.Behavior.FollowFriend)
                {
                    if (self.friendTracker.friend == null)
                    {
                        self.behavior = CicadaAI.Behavior.Idle;
                    }
                    else
                    {
                        self.creature.abstractAI.SetDestination(self.friendTracker.friendDest);
                        self.focusCreature = self.tracker.RepresentationForCreature(self.friendTracker.friend.abstractCreature, false);
                    }
                }
                if (self.friendTracker.friend == null)
                {
                    // Abstract follow-creature logic might do something about it
                    if (UnityEngine.Random.value < 0.02)
                    {
                        self.creature.abstractAI.AbstractBehavior(1);
                    }
                    if (self.creature.abstractAI.followCreature != null)
                    {
                        self.friendTracker.friend = self.creature.abstractAI.followCreature.realizedCreature;
                        self.friendTracker.friendRel = self.creature.state.socialMemory.GetRelationship(self.creature.abstractAI.followCreature.ID);
                    }
                }
            }
            orig(self);
        }
        private static void CicadaAbstractAI_AbstractBehavior(On.CicadaAbstractAI.orig_AbstractBehavior orig, CicadaAbstractAI self, int time)
        {
            //if (self.followCreature == null)
            orig(self, time);
            //// I don't think I can call this early and store it because of JIT ?
            //var AbstractCreatureA_AbstractBehaviorI = typeof(AbstractCreatureAI).GetMethod("AbstractBehavior").MethodHandle.GetFunctionPointer();
            //var baseBehavior = (Action<int>)Activator.CreateInstance(typeof(Action<int>), self, AbstractCreatureA_AbstractBehaviorI);
            self.MigrationBehavior(0); // time = zero => zero rolls of random migration, just followcreature logic
        }
        private static void AbstractCreature_InitiateAI(On.AbstractCreature.orig_InitiateAI orig, AbstractCreature self)
        {
            orig(self);
            if (self.abstractAI == null || self.abstractAI.RealAI == null) return;
            string spawnData = self.spawnData;
            if (!string.IsNullOrEmpty(spawnData) && spawnData[0] == '{')
            {
                string[] array = spawnData.Substring(1, spawnData.Length - 2).Split(new char[]
                {
                    ','
                });
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Length > 0)
                    {
                        string[] array2 = array[i].Split(new char[] { ':' });
                        string text = array2[0].Trim().ToLowerInvariant();

                        if (text == "friend")
                        {
                            if (self.abstractAI.RealAI.friendTracker == null) self.abstractAI.RealAI.AddModule(new FriendTracker(self.abstractAI.RealAI));
                            Debug.LogError("Friend module added in " + self);
                        }

                        if (text == "follow")
                        {
                            if (self.abstractAI.RealAI.friendTracker != null && self.abstractAI.RealAI.utilityComparer != null)
                            {
                                self.abstractAI.RealAI.friendTracker.followClosestFriend = true;
                                bool tracked = false;
                                foreach (var tracker in self.abstractAI.RealAI.utilityComparer.uTrackers)
                                {
                                    if (tracker.module == self.abstractAI.RealAI.friendTracker) tracked = true;
                                }
                                if (!tracked) self.abstractAI.RealAI.utilityComparer.AddComparedModule(self.abstractAI.RealAI.friendTracker, null, array2.Length > 1 ? float.Parse(array2[1]) : 0.8f, 1.2f);
                                Debug.LogError("Follow configured in " + self);
                            }
                        }
                    }
                }
            }
        }
        private static WeakReference currentWorldLoader;
        private static void WorldLoader_ctor(On.WorldLoader.orig_ctor orig, WorldLoader self, RainWorldGame game, int playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            currentWorldLoader = new WeakReference(self);
            orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);
        }
        private static EntityID RainWorldGame_GetNewID_1(On.RainWorldGame.orig_GetNewID_1 orig, RainWorldGame self, int spawner)
        {
            EntityID id = orig(self, spawner);

            if (spawner >= 0 && self.IsStorySession) // called juuuust from WorldLoader.GeneratePopulation most likely, lets play safe though
            {
                int region = UnityEngine.Mathf.FloorToInt(spawner / 1000f);
                int inregionspawn = spawner - region * 1000;
                string spawnData = "";
                try
                {
                    // game.overWorld isn't set until the constructor is done so we can't use that reference
                    // Overworld.LoadWorld doesn't set a reference to worldloader anywhere while its doing its thing :/
                    WorldLoader worldLoader = currentWorldLoader.Target as WorldLoader;
                    if (worldLoader != null && !worldLoader.Finished && worldLoader.world.region != null && worldLoader.world.region.regionNumber == region)
                    {
                        if (worldLoader.world.spawners[inregionspawn] is World.SimpleSpawner simpleSpawner)
                        {
                            spawnData = simpleSpawner.spawnDataString;
                        }
                        else if (worldLoader.world.spawners[inregionspawn] is World.Lineage lineage)
                        {
                            spawnData = lineage.CurrentSpawnData((self.session as StoryGameSession).saveState);
                        }
                        if (!string.IsNullOrEmpty(spawnData) && spawnData[0] == '{')
                        {
                            string[] array = spawnData.Substring(1, spawnData.Length - 2).Split(new char[] { ',' });
                            for (int i = 0; i < array.Length; i++)
                            {
                                if (array[i].Length > 0)
                                {
                                    string[] array2 = array[i].Split(new char[] { ':' });
                                    string text = array2[0].Trim().ToLowerInvariant();
                                    if (text == "id")
                                    {
                                        id.number = int.Parse(array2[1].Trim());
                                    }
                                }
                            }
                        }
                    }
                }
                catch { UnityEngine.Debug.LogError("ConcealedGarden: Something terrible happened while trying to parse for a spawn ID for spawner " + spawner + " " + spawnData); }
            }
            // Cannot be properly done from arena since spawers are read and discarded withing a single function and spawnData/IDs are never actually used and everything is trash.
            // If you're feeling brave with IL editing give it a shot I suppose. I'm for the time being an oldschool partiality guy.
            // also arena uses the paramless ctor
            return id;
        }
        private static float Trader_ScavScore(On.ScavengersWorldAI.Trader.orig_ScavScore orig, ScavengersWorldAI.Trader self, ScavengerAbstractAI testScav)
        {
            float score = orig(self, testScav);
            bool specificallyAssigned = false;

            string spawnData = testScav.parent.spawnData;
            if (!string.IsNullOrEmpty(spawnData) && spawnData[0] == '{')
            {
                string[] array = spawnData.Substring(1, spawnData.Length - 2).Split(new char[]
                {
                    ','
                });
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].Length > 0)
                    {
                        string[] array2 = array[i].Split(new char[]
                        {
                            ':'
                        });
                        string text = array2[0].Trim().ToLowerInvariant();
                        if (text == "trader")
                        {
                            score += 1f;
                            if (array2.Length > 1 && array2[1].Trim().ToLowerInvariant() == self.worldAI.world.GetAbstractRoom(self.room).name.Trim().ToLowerInvariant())
                            {
                                score += 1f;
                                specificallyAssigned = true;
                                UnityEngine.Debug.Log("CG: Found trader for " + self.worldAI.world.GetAbstractRoom(self.room).name);
                            }
                            else
                            {
                                UnityEngine.Debug.Log("CG: Found trader");
                            }
                        }
                    }
                }
            }
            if (testScav.squad != null && testScav.squad.missionType == ScavengerAbstractAI.ScavengerSquad.MissionID.Trade && !specificallyAssigned) return 0f; // Already assigned
            return score;
        }
        private static void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            orig(self, world, creatureTemplate, realizedCreature, pos, ID);
            WorldLoader worldLoader = currentWorldLoader.Target as WorldLoader;

            if (worldLoader != null && ID.spawner >= 0 && worldLoader.game.IsStorySession) // called juuuust from WorldLoader.GeneratePopulation most likely, lets play safe though
            {
                int region = UnityEngine.Mathf.FloorToInt(ID.spawner / 1000f);
                int inregionspawn = ID.spawner - region * 1000;
                string spawnData = "";
                try
                {
                    if (worldLoader != null && !worldLoader.Finished && worldLoader.world.region != null && worldLoader.world.region.regionNumber == region)
                    {
                        if (worldLoader.world.spawners[inregionspawn] is World.SimpleSpawner simpleSpawner)
                        {
                            spawnData = simpleSpawner.spawnDataString;
                        }
                        else if (worldLoader.world.spawners[inregionspawn] is World.Lineage lineage)
                        {
                            spawnData = lineage.CurrentSpawnData((worldLoader.game.session as StoryGameSession).saveState);
                        }
                        if (!string.IsNullOrEmpty(spawnData) && spawnData[0] == '{')
                        {
                            string[] array = spawnData.Substring(1, spawnData.Length - 2).Split(new char[] { ',' });
                            for (int i = 0; i < array.Length; i++)
                            {
                                if (array[i].Length > 0)
                                {
                                    string[] array2 = array[i].Split(new char[] { ':' });
                                    string text = array2[0].Trim().ToLowerInvariant();
                                    if (text == "sympathy")
                                    {
                                        self.personality.sympathy = float.Parse(array2[1].Trim());
                                    }
                                    else if (text == "energy")
                                    {
                                        self.personality.energy = float.Parse(array2[1].Trim());
                                    }
                                    else if (text == "bravery")
                                    {
                                        self.personality.bravery = float.Parse(array2[1].Trim());
                                    }
                                    else if (text == "nervous")
                                    {
                                        self.personality.nervous = float.Parse(array2[1].Trim());
                                    }
                                    else if (text == "aggression")
                                    {
                                        self.personality.aggression = float.Parse(array2[1].Trim());
                                    }
                                    else if (text == "dominance")
                                    {
                                        self.personality.dominance = float.Parse(array2[1].Trim());
                                    }
                                }
                            }
                        }
                    }
                }
                catch { UnityEngine.Debug.LogError("ConcealedGarden: Something terrible happened while trying to parse traits for spawner " + ID.spawner + " " + spawnData); }
            }
        }
        private static void CreatureState_LoadFromString(On.CreatureState.orig_LoadFromString orig, CreatureState self, string[] s)
        {
            if (self.socialMemory != null && self.socialMemory.relationShips.Count > 0) self.socialMemory.relationShips.Clear();
            orig(self, s);
            string spawnData = self.creature.spawnData;
            try
            {
                if (!string.IsNullOrEmpty(spawnData) && spawnData[0] == '{')
                {
                    string[] array = spawnData.Substring(1, spawnData.Length - 2).Split(new char[] { ',' });
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (array[i].Length > 0)
                        {
                            string[] array2 = array[i].Split(new char[] { ':' });
                            string text = array2[0].Trim().ToLowerInvariant();
                            if (text == "sympathy")
                            {
                                self.creature.personality.sympathy = float.Parse(array2[1].Trim());
                            }
                            else if (text == "energy")
                            {
                                self.creature.personality.energy = float.Parse(array2[1].Trim());
                            }
                            else if (text == "bravery")
                            {
                                self.creature.personality.bravery = float.Parse(array2[1].Trim());
                            }
                            else if (text == "nervous")
                            {
                                self.creature.personality.nervous = float.Parse(array2[1].Trim());
                            }
                            else if (text == "aggression")
                            {
                                self.creature.personality.aggression = float.Parse(array2[1].Trim());
                            }
                            else if (text == "dominance")
                            {
                                self.creature.personality.dominance = float.Parse(array2[1].Trim());
                            }
                        }
                    }
                }
            }
            catch { UnityEngine.Debug.LogError("ConcealedGarden: Something terrible happened while trying to parse traits from state as " + self.creature.ID.spawner + " at CreatureState_LoadFromString " + spawnData); }
        }
        private static void CreatureState_ctor(On.CreatureState.orig_ctor orig, CreatureState self, AbstractCreature creature)
        {
            orig(self, creature);

            if (self.socialMemory != null)
            {
                WorldLoader worldLoader = currentWorldLoader.Target as WorldLoader;
                string spawnData = "";
                if (worldLoader != null && creature.ID.spawner >= 0 && worldLoader.game.IsStorySession) // called juuuust from WorldLoader.GeneratePopulation most likely, lets play safe though
                {
                    int region = UnityEngine.Mathf.FloorToInt(creature.ID.spawner / 1000f);
                    int inregionspawn = creature.ID.spawner - region * 1000;

                    try
                    {
                        if (worldLoader != null && !worldLoader.Finished && worldLoader.world.region != null && worldLoader.world.region.regionNumber == region)
                        {
                            if (worldLoader.world.spawners[inregionspawn] is World.SimpleSpawner simpleSpawner)
                            {
                                spawnData = simpleSpawner.spawnDataString;
                            }
                            else if (worldLoader.world.spawners[inregionspawn] is World.Lineage lineage)
                            {
                                spawnData = lineage.CurrentSpawnData((worldLoader.game.session as StoryGameSession).saveState);
                            }
                            if (!string.IsNullOrEmpty(spawnData) && spawnData[0] == '{')
                            {
                                string[] array = spawnData.Substring(1, spawnData.Length - 2).Split(new char[] { ',' });
                                for (int i = 0; i < array.Length; i++)
                                {
                                    if (array[i].Length > 0)
                                    {
                                        string[] array2 = array[i].Split(new char[] { ':' });
                                        string text = array2[0].Trim().ToLowerInvariant();
                                        if (text == "like")
                                        {
                                            float amount = array2.Length > 1 ? float.Parse(array2[1]) : 1f;
                                            foreach (var player in creature.world.game.Players)
                                            {
                                                var rel = self.socialMemory.GetOrInitiateRelationship(player.ID);
                                                rel.like = amount;
                                                rel.tempLike = amount;
                                            }
                                        }
                                        else if (text == "fear")
                                        {
                                            float amount = array2.Length > 1 ? float.Parse(array2[1]) : 1f;
                                            foreach (var player in creature.world.game.Players)
                                            {
                                                var rel = self.socialMemory.GetOrInitiateRelationship(player.ID);
                                                rel.fear = amount;
                                                rel.tempFear = amount;
                                            }
                                        }
                                        else if (text == "know")
                                        {
                                            float amount = array2.Length > 1 ? float.Parse(array2[1]) : 1f;
                                            foreach (var player in creature.world.game.Players)
                                            {
                                                var rel = self.socialMemory.GetOrInitiateRelationship(player.ID);
                                                rel.know = amount;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { UnityEngine.Debug.LogError("ConcealedGarden: Something terrible happened while trying to parse relationship for spawner " + creature.ID.spawner + " " + spawnData); }
                }
            }
        }
    }
}