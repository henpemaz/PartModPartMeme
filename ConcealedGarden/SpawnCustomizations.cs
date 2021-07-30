using MonoMod.RuntimeDetour;
using System;
using UnityEngine;
using System.Reflection;

namespace ConcealedGarden
{
    internal static class SpawnCustomizations
    {
        internal static void Apply()
        {
            // ID spawndata support
            //On.WorldLoader.ctor += WorldLoader_ctor;
            On.RainWorld.Start += RainWorld_Start; // Deferred hooks because of bad load order and mods that dont call orig
            On.RainWorldGame.GetNewID_1 += RainWorldGame_GetNewID_1;

            // Assignable Trader support
            On.ScavengersWorldAI.Trader.ScavScore += Trader_ScavScore;

            // Personality Traits support
            On.AbstractCreature.ctor += AbstractCreature_ctor;

            // Innate RelationhShip support
            On.CreatureState.ctor += CreatureState_ctor;
            On.CreatureState.LoadFromString += CreatureState_LoadFromString;

            // Friend module instalation 
            On.AbstractCreature.InitiateAI += AbstractCreature_InitiateAI;

            // Cicada Follow
            On.CicadaAI.Update += CicadaAI_Update;
            On.CicadaAbstractAI.AbstractBehavior += CicadaAbstractAI_AbstractBehavior;
            // Scav friend + follow
            On.ScavengerAI.Update += ScavengerAI_Update;
            On.ScavengerAbstractAI.AbstractBehavior += ScavengerAbstractAI_AbstractBehavior;
            //On.ScavengerGraphics.ctor += ScavengerGraphics_ctor;
            // Spider friend + follow
            On.BigSpiderAI.Update += BigSpiderAI_Update;
            new Hook(typeof(BigSpiderAI).GetMethod("IUseARelationshipTracker.UpdateDynamicRelationship", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), typeof(SpawnCustomizations).GetMethod(nameof(IUseARelationshipTracker_UpdateDynamicRelationship), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            //On.IUseARelationshipTracker.UpdateDynamicRelationship += IUseARelationshipTracker_UpdateDynamicRelationship;
            // Vulture friend + follow
            On.VultureAI.Update += VultureAI_Update;
            new Hook(typeof(VultureAI).GetMethod("IUseARelationshipTracker.UpdateDynamicRelationship", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), typeof(SpawnCustomizations).GetMethod(nameof(IUseARelationshipTracker_UpdateDynamicRelationship), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            On.VultureAbstractAI.AbstractBehavior += VultureAbstractAI_AbstractBehavior;

            // Support for gift offered for additional creatures
            On.SocialEventRecognizer.SocialEvent += SocialEventRecognizer_SocialEvent;
            On.SocialEventRecognizer.ItemOffered += SocialEventRecognizer_ItemOffered;
            On.FriendTracker.GiftRecieved += FriendTracker_GiftRecieved;
            // Aggression value fix for friendly creatures
            On.ArtificialIntelligence.CurrentPlayerAggression += ArtificialIntelligence_CurrentPlayerAggression;
        }

        // Defferred hoookks aaaaugh
        private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            On.WorldLoader.ctor += WorldLoader_ctor;
            orig(self);
        }

        private static Creature SocialEventRecognizer_ItemOffered(On.SocialEventRecognizer.orig_ItemOffered orig, SocialEventRecognizer self, Creature gifter, PhysicalObject item, Creature offeredTo)
        {
            var result = orig(self, gifter, item, offeredTo);
            // coppypasta with a twist
            if (gifter.Template.type == CreatureTemplate.Type.Slugcat)
            {
                for (int i = 0; i < self.stolenProperty.Count; i++)
                {
                    if (self.stolenProperty[i] == item.abstractPhysicalObject.ID)
                    {
                        return result;
                    }
                }
            }
            if (offeredTo == null)
            {
                float dst = result == null ? 500f : Vector2.Distance(item.firstChunk.pos, result.DangerPos); // closer than the original ?
                for (int j = 0; j < self.room.abstractRoom.creatures.Count; j++)
                {
                    if (self.room.abstractRoom.creatures[j].realizedCreature != null
                        && self.room.abstractRoom.creatures[j].realizedCreature != gifter
                        && !self.room.abstractRoom.creatures[j].realizedCreature.dead
                        && self.room.abstractRoom.creatures[j].abstractAI != null
                        && self.room.abstractRoom.creatures[j].abstractAI.RealAI != null
                        && !(self.room.abstractRoom.creatures[j].abstractAI.RealAI is IReactToSocialEvents) // Oho
                            && self.room.abstractRoom.creatures[j].abstractAI.RealAI.friendTracker != null // Aha
                            && self.room.abstractRoom.creatures[j].state.socialMemory != null // aha
                        && self.room.abstractRoom.creatures[j].abstractAI.RealAI.tracker != null
                        && RWCustom.Custom.DistLess(item.firstChunk.pos, self.room.abstractRoom.creatures[j].realizedCreature.DangerPos, dst))
                    {
                        Tracker.CreatureRepresentation creatureRepresentation = self.room.abstractRoom.creatures[j].abstractAI.RealAI.tracker.RepresentationForCreature(gifter.abstractCreature, false);
                        if (creatureRepresentation != null && creatureRepresentation.TicksSinceSeen < 120)
                        {
                            dst = Vector2.Distance(item.firstChunk.pos, self.room.abstractRoom.creatures[j].realizedCreature.DangerPos);
                            result = self.room.abstractRoom.creatures[j].realizedCreature;
                        }
                    }
                }
            }
            if (result != null)
            {
                self.SocialEvent(SocialEventRecognizer.EventID.ItemOffering, gifter, result, item);
            }
            return result;
        }

        public static class EnumExt_SpawnCustomizations
        {
#pragma warning disable 0649
            public static ScavengerAI.Behavior ScavengerFollowFriend;
            public static BigSpiderAI.Behavior SpiderFollowFriend;
            public static VultureAI.Behavior VultureFollowPlayer;
#pragma warning restore 0649
        }

        // generic playeraggression 
        // for creatures that don't override it
        // in use by spiders and vultures ; could affect other creatures that have social memory ?
        private static float ArtificialIntelligence_CurrentPlayerAggression(On.ArtificialIntelligence.orig_CurrentPlayerAggression orig, ArtificialIntelligence self, AbstractCreature player)
        {
            Tracker.CreatureRepresentation creatureRepresentation;;
            float result = orig(self, player);
            if (result != 1f|| self.tracker == null || self.creature.state.socialMemory == null || (creatureRepresentation = self.tracker.RepresentationForCreature(player, false)) == null || creatureRepresentation.dynamicRelationship == null)
            {
                return result;
            }
            return Mathf.InverseLerp(0.5f, 0f, self.creature.state.socialMemory.GetTempLike(player.ID));
        }

        // Dynamic relationship to be hooked to creatures for friendly behavior
        // handles gift, protection, passiveness
        // in use by spiders and vultures
        private static CreatureTemplate.Relationship IUseARelationshipTracker_UpdateDynamicRelationship(IUART_UDR orig, IUseARelationshipTracker self, RelationshipTracker.DynamicRelationship dRelation)
        {
            var result = orig(self, dRelation);
            if(self is ArtificialIntelligence ai && ai.friendTracker != null)
            {
                // from LizardAI
                if (ai.friendTracker.giftOfferedToMe != null && ai.friendTracker.giftOfferedToMe.active && ai.friendTracker.giftOfferedToMe.item == dRelation.trackerRep.representedCreature.realizedCreature)
                {
                    return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, (!dRelation.trackerRep.representedCreature.state.dead) ? 0.65f : 1f);
                }
                if (dRelation.trackerRep.representedCreature.creatureTemplate.type != CreatureTemplate.Type.Slugcat)
                {
                    if (ai.friendTracker.friend != null && ai.friendTracker.Urgency > 0.2f && dRelation.trackerRep.representedCreature.creatureTemplate.dangerousToPlayer > 0f && dRelation.trackerRep.representedCreature != ai.friendTracker.friend.abstractCreature && dRelation.trackerRep.representedCreature.state.alive && dRelation.trackerRep.representedCreature.realizedCreature != null && dRelation.trackerRep.representedCreature.abstractAI != null && dRelation.trackerRep.representedCreature.abstractAI.RealAI != null)
                    {
                        float num = Mathf.InverseLerp(0.2f, 0.8f, ai.friendTracker.Urgency) * Mathf.Pow(dRelation.trackerRep.representedCreature.creatureTemplate.dangerousToPlayer * ((!(ai.friendTracker.friend is Player)) ? 1f : dRelation.trackerRep.representedCreature.abstractAI.RealAI.CurrentPlayerAggression(ai.friendTracker.friend.abstractCreature)), 0.5f);
                        num *= Mathf.InverseLerp(30f, 7f, RWCustom.Custom.WorldCoordFloatDist(ai.friendTracker.friend.abstractCreature.pos, dRelation.trackerRep.BestGuessForPosition()));
                        if (!RWCustom.Custom.DistLess(ai.friendTracker.friend.abstractCreature.pos, dRelation.trackerRep.BestGuessForPosition(), RWCustom.Custom.WorldCoordFloatDist(ai.creature.pos, ai.friendTracker.friend.abstractCreature.pos)))
                        {
                            num *= 0.5f;
                        }
                        if (num > 0f && (ai.StaticRelationship(dRelation.trackerRep.representedCreature).type != CreatureTemplate.Relationship.Type.Eats || ai.StaticRelationship(dRelation.trackerRep.representedCreature).intensity < num))
                        {
                            return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Attacks, num);
                        }
                    }
                }
                else
                {
                    var player = dRelation.trackerRep;
                    float tempLike = ai.creature.state.socialMemory.GetTempLike(player.representedCreature.ID);
                    if (ai.friendTracker.giftOfferedToMe != null && ai.friendTracker.giftOfferedToMe.owner == player.representedCreature.realizedCreature)
                    {
                        tempLike = RWCustom.Custom.LerpMap(tempLike, -0.5f, 1f, 0f, 1f, 0.8f);
                    }
                    if (tempLike < 0.5f)
                    {
                        result = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Eats, Mathf.InverseLerp(0.5f, -1f, tempLike));
                    }
                    else
                    {
                        result = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
                    }
                }
            }
            return result;
        }

        // Generic thinghie for "unintended" friendtracker owners
        // makes creechers like you; values might need tweaking
        // in use by spiders and vultures
        private static void FriendTracker_GiftRecieved(On.FriendTracker.orig_GiftRecieved orig, FriendTracker self)
        {
            var giftOfferedToMe = self.giftOfferedToMe;
            orig(self);
            if (!(self.AI is FriendTracker.IHaveFriendTracker) && self.creature.State.socialMemory != null && giftOfferedToMe.item is Creature food)
            {
                var foodRelationship = self.AI.DynamicRelationship(food.abstractCreature);
                if (foodRelationship.type != CreatureTemplate.Relationship.Type.Eats) return;
                SocialMemory.Relationship orInitiateRelationship = self.creature.State.socialMemory.GetOrInitiateRelationship(giftOfferedToMe.owner.abstractCreature.ID);
                float ownMass = self.creature.TotalMass;
                float otherMass = food.TotalMass;
                float amount = ((!food.dead) ? 0.6f : 1.2f) * foodRelationship.intensity * ((ownMass != 0f && otherMass != 0f) ? Mathf.Lerp(otherMass / ownMass, 1f, 0.5f) : 1f) / self.tamingDifficlty;

                if (orInitiateRelationship.like > -0.9f)
                {
                    orInitiateRelationship.InfluenceLike(amount);
                    orInitiateRelationship.InfluenceTempLike(amount * 1.5f);
                }
            }
        }

        // Generic thinghie for "unintended" friendtracker owners
        // offers food to friendable creechers if creecher doesn't do IReactToSocialEvents
        // in use by spiders and vultures
        private static void SocialEventRecognizer_SocialEvent(On.SocialEventRecognizer.orig_SocialEvent orig, SocialEventRecognizer self, SocialEventRecognizer.EventID ID, Creature subjectCreature, Creature objectCreature, PhysicalObject involvedItem)
        {
            orig(self, ID, subjectCreature, objectCreature, involvedItem);
            if(ID == SocialEventRecognizer.EventID.ItemOffering && objectCreature.abstractCreature.abstractAI != null && objectCreature.abstractCreature.abstractAI.RealAI != null)
            {
                ArtificialIntelligence ai = objectCreature.abstractCreature.abstractAI.RealAI;
                if (!(ai is IReactToSocialEvents) && ai.tracker != null && ai.friendTracker != null)
                {
                    Tracker.CreatureRepresentation creatureRepresentation = ai.tracker.RepresentationForObject(subjectCreature, false);
                    if (creatureRepresentation != null)
                    {
                        ai.friendTracker.ItemOffered(creatureRepresentation, involvedItem);
                    }
                }
            }
        }

        // Vulture abstract ai
        // migration behavior for a chance to find its friend; while realized with friend prevent default
        private static void VultureAbstractAI_AbstractBehavior(On.VultureAbstractAI.orig_AbstractBehavior orig, VultureAbstractAI self, int time)
        {
            if (self.path.Count == 0 && self.parent.state.socialMemory != null && self.parent.state.socialMemory.relationShips.Count != 0 && self.parent.realizedCreature == null) // not doing anything else
                self.MigrationBehavior(0); // time = zero => zero rolls of random migration, just followcreature logic
            if (self.RealAI != null && self.RealAI is VultureAI ai && ai.utilityComparer.HighestUtilityModule() is FriendTracker ft && ai.behavior != VultureAI.Behavior.ReturnPrey)
            {
                if (ft.friend != null && ft.friend.abstractCreature.pos.room == self.parent.pos.room)
                    return; // prevent idle code
            }
            orig(self, time);
        }

        // Vulture realized ai
        // if tracking and should follow, follow, unless in a different room in which case go to den
        private static void VultureAI_Update(On.VultureAI.orig_Update orig, VultureAI self)
        {
            orig(self);
            if (self.friendTracker != null && self.friendTracker.followClosestFriend) // {friend} switch
            {
                AIModule aimodule = self.utilityComparer.HighestUtilityModule();
                float currentUtility = self.utilityComparer.HighestUtility();
                if (aimodule != null)
                {
                    if (aimodule is FriendTracker && (currentUtility > 0.2f || (self.behavior == VultureAI.Behavior.Idle && currentUtility > 0.1f)))
                    {
                        if (self.friendTracker.friend.abstractCreature.pos.room == self.creature.pos.room && self.behavior != VultureAI.Behavior.ReturnPrey)
                        {
                            self.behavior = EnumExt_SpawnCustomizations.VultureFollowPlayer;
                        }
                    }
                }
                if (self.behavior == EnumExt_SpawnCustomizations.VultureFollowPlayer)
                {
                    if (self.friendTracker.friend != null && self.friendTracker.friend.abstractCreature.pos.room == self.creature.pos.room && self.friendTracker.friendDest.room == self.creature.pos.room)
                    {
                        self.creature.abstractAI.SetDestination(self.friendTracker.friendDest);
                    }
                    else
                    {
                        self.creature.abstractAI.GoToDen();
                    }
                }
                if (self.friendTracker.friend == null) // not found friend in room yet
                {
                    if (self.creature.abstractAI.followCreature != null) // but maybe abstai did
                    {
                        self.friendTracker.friend = self.creature.abstractAI.followCreature.realizedCreature;
                        self.friendTracker.friendRel = self.creature.state.socialMemory.GetRelationship(self.creature.abstractAI.followCreature.ID);
                    }
                }
            }
        }

        // spider real ai
        // if friend and should follow, follow unless reviving
        private static void BigSpiderAI_Update(On.BigSpiderAI.orig_Update orig, BigSpiderAI self)
        {
            // we're running a frame late but its best to have our code run first here and then let the AI overwrite behavior
            if (self.friendTracker != null && self.friendTracker.followClosestFriend) // {friend} switch
            {
                // could have a switch here to prevent reviving "enemy buddy", but ehhh
                AIModule aimodule = self.utilityComparer.HighestUtilityModule();
                self.currentUtility = self.utilityComparer.HighestUtility();
                if (aimodule != null)
                {
                    if (aimodule is FriendTracker && self.currentUtility > 0.2f)
                    {
                        if (self.behavior != BigSpiderAI.Behavior.ReturnPrey && self.behavior != BigSpiderAI.Behavior.ReviveBuddy)
                        {
                            self.behavior = EnumExt_SpawnCustomizations.SpiderFollowFriend;
                        }
                    }
                }
                if (self.behavior == EnumExt_SpawnCustomizations.SpiderFollowFriend)
                {
                    if (self.friendTracker.friend != null)
                    {
                        self.creature.abstractAI.SetDestination(self.friendTracker.friendDest);
                    }
                }
                if (self.friendTracker.friend == null) // not found friend in room yet
                {
                    if (self.creature.abstractAI.followCreature != null) // but maybe abstai did
                    {
                        self.friendTracker.friend = self.creature.abstractAI.followCreature.realizedCreature;
                        self.friendTracker.friendRel = self.creature.state.socialMemory.GetRelationship(self.creature.abstractAI.followCreature.ID);
                    }
                }
            }
            orig(self);
        }

        // Scavenger Fried / FollowPlayer support
        //private static void ScavengerGraphics_ctor(On.ScavengerGraphics.orig_ctor orig, ScavengerGraphics self, PhysicalObject ow)
        //{
        //    self.DEBUGLABELS = new DebugLabel[6];
        //    self.DEBUGLABELS[0] = new DebugLabel(ow, new Vector2(0f, 50f));
        //    self.DEBUGLABELS[1] = new DebugLabel(ow, new Vector2(0f, 40f));
        //    self.DEBUGLABELS[2] = new DebugLabel(ow, new Vector2(0f, 30f));
        //    self.DEBUGLABELS[3] = new DebugLabel(ow, new Vector2(0f, 20f));
        //    self.DEBUGLABELS[4] = new DebugLabel(ow, new Vector2(0f, 10f));
        //    self.DEBUGLABELS[5] = new DebugLabel(ow, new Vector2(0f, 0f));
        //    orig(self, ow);
        //    //throw new NotImplementedException();
        //}

        // scav abstr ai
        // if not busy and has relationship, roll migration behavior for a chance to find followcreature
        private static void ScavengerAbstractAI_AbstractBehavior(On.ScavengerAbstractAI.orig_AbstractBehavior orig, ScavengerAbstractAI self, int time)
        {
            orig(self, time);
            if (self.path.Count == 0 && self.parent.state.socialMemory.relationShips.Count != 0 && self.squad == null && !(self.bringPearlHome || !self.missionAppropriateGear)) // not doing anything else
                self.MigrationBehavior(0); // time = zero => zero rolls of random migration, just followcreature logic
        }

        // scav real ai
        // if friend module installed, if should follow friend do follow
        private static void ScavengerAI_Update(On.ScavengerAI.orig_Update orig, ScavengerAI self)
        {
            // we're running a frame late but its best to have our code run first here and then let the AI overwrite behavior
            if (self.friendTracker != null && self.friendTracker.followClosestFriend) // {friend} switch
            {
                AIModule aimodule = self.utilityComparer.HighestUtilityModule();
                self.currentUtility = self.utilityComparer.HighestUtility();
                if (aimodule != null)
                {
                    if (aimodule is FriendTracker && self.currentUtility > 0.2f)
                    {
                        if (!(self.scavenger.abstractCreature.abstractAI as ScavengerAbstractAI).bringPearlHome)
                        {
                            self.behavior = EnumExt_SpawnCustomizations.ScavengerFollowFriend;
                        }
                        else // got pearl, gotta dip
                        {
                            self.behavior = ScavengerAI.Behavior.Travel;
                            // this causes tracker urgency to dip when its called next during the actual AI code
                            self.friendTracker.friendRel = null; // Don't worry about me I'll be fine you go
                        }
                    }
                }
                if (self.behavior == EnumExt_SpawnCustomizations.ScavengerFollowFriend)
                {
                    if (self.friendTracker.friend != null)
                    {
                        self.creature.abstractAI.SetDestination(self.friendTracker.friendDest);
                        self.focusCreature = self.tracker.RepresentationForCreature(self.friendTracker.friend.abstractCreature, false);
                    }
                }
                if (self.friendTracker.friend == null) // not found friend in room yet
                {
                    if (self.creature.abstractAI.followCreature != null) // but maybe abstai did
                    {
                        self.friendTracker.friend = self.creature.abstractAI.followCreature.realizedCreature;
                        self.friendTracker.friendRel = self.creature.state.socialMemory.GetRelationship(self.creature.abstractAI.followCreature.ID);
                    }
                }
            }
            orig(self);
        }

        // Cicada abstr ai
        // if not pathin, roll for finding friend
        private static void CicadaAbstractAI_AbstractBehavior(On.CicadaAbstractAI.orig_AbstractBehavior orig, CicadaAbstractAI self, int time)
        {
            orig(self, time);
            if (self.path.Count == 0) // not currently going anywhere
                self.MigrationBehavior(0); // time = zero => zero rolls of random migration, just followcreature logic
        }

        // Cicada FollowPlayer support
        // the enum was already there but unused
        private static void CicadaAI_Update(On.CicadaAI.orig_Update orig, CicadaAI self)
        {
            // reordered to prevent IDLE from taking precedence
            if (self.friendTracker != null && self.friendTracker.followClosestFriend) // {friend} switch
            {
                AIModule aimodule = self.utilityComparer.HighestUtilityModule();
                self.currentUtility = self.utilityComparer.HighestUtility();
                if (aimodule != null)
                {
                    if (aimodule is FriendTracker && self.currentUtility > 0.2f) // 0.2 = min, same room within 10 tiles
                    {
                        if (self.behavior != CicadaAI.Behavior.ReturnPrey || self.cicada.grasps[0] == null)
                        {
                            self.behavior = CicadaAI.Behavior.FollowFriend;
                        }
                    }
                }
                if (self.behavior == CicadaAI.Behavior.FollowFriend)
                {
                    if (self.friendTracker.friend != null)
                    {
                        self.creature.abstractAI.SetDestination(self.friendTracker.friendDest);
                        self.focusCreature = self.tracker.RepresentationForCreature(self.friendTracker.friend.abstractCreature, false);
                    }
                }
                if (self.friendTracker.friend == null)
                {
                    if (self.creature.abstractAI.followCreature != null)
                    {
                        self.friendTracker.friend = self.creature.abstractAI.followCreature.realizedCreature;
                        self.friendTracker.friendRel = self.creature.state.socialMemory.GetRelationship(self.creature.abstractAI.followCreature.ID);
                    }
                }
            }
            orig(self);
        }
        
        // Add friendtracker and configure follow-ness for creatures
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
                            self.abstractAI.RealAI.friendTracker.followClosestFriend = true; // Cicadas have tracker but flag unset
                            if (self.abstractAI.RealAI.utilityComparer != null)
                            {
                                UtilityComparer.UtilityTracker utracker = self.abstractAI.RealAI.utilityComparer.GetUtilityTracker(self.abstractAI.RealAI.friendTracker);
                                if (utracker == null) self.abstractAI.RealAI.utilityComparer.AddComparedModule(self.abstractAI.RealAI.friendTracker, null, array2.Length > 1 ? float.Parse(array2[1]) : 0.8f, 1.2f);
                            }
                            self.abstractAI.RealAI.friendTracker.tamingDifficlty = array2.Length > 1 ? 1f / Mathf.Max(0.1f, float.Parse(array2[1])) : 1f;
                            if (self.state.socialMemory == null) self.state.socialMemory = new SocialMemory(); // required for friend-ness
                            Debug.Log("Friend module added in " + self);
                        }
                    }
                }
            }
        }

        // Store a reference to the current WorldLoader
        // because several things being created need a reference to their spawners before spawnData is attributed to the creature
        private static WeakReference currentWorldLoader;
        private static void WorldLoader_ctor(On.WorldLoader.orig_ctor orig, WorldLoader self, RainWorldGame game, int playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            currentWorldLoader = new WeakReference(self);
            orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);
        }

        // Load ID into EntityID right before AbstractCreature_ctor
        // uses spawndata from worldloader because its normally only added after AbstractCreature_ctor
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
                    if (currentWorldLoader?.Target is WorldLoader worldLoader && !worldLoader.Finished && worldLoader.world.region != null && worldLoader.world.region.regionNumber == region)
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
                                        Debug.Log("Configured ID.id in " + self);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e){ UnityEngine.Debug.LogError("ConcealedGarden: Something terrible happened while trying to parse for a spawn ID for spawner " + spawner + " " + spawnData + "\n" + e); }
            }
            // Cannot be properly done from arena since spawers are read and discarded withing a single function and spawnData/IDs are never actually used and everything is trash.
            // If you're feeling brave with IL editing give it a shot I suppose. I'm for the time being an oldschool partiality guy.
            // also arena uses the paramless ctor
            return id;
        }

        // uses spawndata from scavenger to decide on which trader to assign, called from scavworldAI way after everything initialized
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
        
        // Load Personality Traits into AbstractCreature during AbstractCreature_ctor
        // uses spawndata from worldloader because its normally only added after AbstractCreature_ctor
        // Should ideally be a hook to Personality ctor but hooking structs is bugged out in old monomod.
        private static void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            orig(self, world, creatureTemplate, realizedCreature, pos, ID);

            if (currentWorldLoader?.Target is WorldLoader worldLoader && ID.spawner >= 0 && worldLoader.game.IsStorySession) // called juuuust from WorldLoader.GeneratePopulation most likely, lets play safe though
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
                catch (Exception e) { UnityEngine.Debug.LogError("ConcealedGarden: Something terrible happened while trying to parse traits for spawner " + ID.spawner + " " + spawnData + "\n" + e); }
            }
        }
        
        // Load relationships into CreatureState during AbstractCreature_ctor
        // uses spawndata from worldloader because its normally only added after AbstractCreature_ctor
        private static void CreatureState_ctor(On.CreatureState.orig_ctor orig, CreatureState self, AbstractCreature creature)
        {
            orig(self, creature);
            string spawnData = "";
            if (currentWorldLoader?.Target is WorldLoader worldLoader && creature.ID.spawner >= 0 && worldLoader.game.IsStorySession) // called juuuust from WorldLoader.GeneratePopulation most likely, lets play safe though
            {
                int region = UnityEngine.Mathf.FloorToInt(creature.ID.spawner / 1000f);
                int inregionspawn = creature.ID.spawner - region * 1000;

                try
                {
                    if (worldLoader != null && !worldLoader.Finished && worldLoader.world.region != null && worldLoader.world.region.regionNumber == region)
                    {
                        //Debug.LogError(worldLoader.world.PrintSpawner(inregionspawn));
                        if (worldLoader.world.spawners[inregionspawn] is World.SimpleSpawner simpleSpawner)
                        {
                            spawnData = simpleSpawner.spawnDataString;
                        }
                        else if (worldLoader.world.spawners[inregionspawn] is World.Lineage lineage)
                        {
                            spawnData = lineage.CurrentSpawnData((worldLoader.game.session as StoryGameSession).saveState);
                        }
                        //Debug.LogError(spawnData);
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
                                        if (self.socialMemory == null) self.socialMemory = new SocialMemory();
                                        float amount = array2.Length > 1 ? float.Parse(array2[1]) : 1f;
                                        for (int p = 0; p < 4; p++) // foreach (var player in creature.world.game.Players)
                                            // this gets called before players are added to the game when waking up in a region :/
                                        {
                                            var rel = self.socialMemory.GetOrInitiateRelationship(new EntityID(-1, p));
                                            rel.like = amount;
                                            rel.tempLike = amount;
                                        }
                                        Debug.Log("Configured Like of "+amount+" in " + creature);
                                    }
                                    else if (text == "fear")
                                    {
                                        if (self.socialMemory == null) self.socialMemory = new SocialMemory();
                                        float amount = array2.Length > 1 ? float.Parse(array2[1]) : 1f;
                                        for (int p = 0; p < 4; p++) // foreach (var player in creature.world.game.Players)
                                        {
                                            var rel = self.socialMemory.GetOrInitiateRelationship(new EntityID(-1, p));
                                            rel.fear = amount;
                                            rel.tempFear = amount;
                                        }
                                        Debug.Log("Configured Fear in " + creature);
                                    }
                                    else if (text == "know")
                                    {
                                        if (self.socialMemory == null) self.socialMemory = new SocialMemory();
                                        float amount = array2.Length > 1 ? float.Parse(array2[1]) : 1f;
                                        for (int p = 0; p < 4; p++) // foreach (var player in creature.world.game.Players)
                                        {
                                            var rel = self.socialMemory.GetOrInitiateRelationship(new EntityID(-1, p));
                                            rel.know = amount;
                                        }
                                        Debug.Log("Configured Know in " + creature);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e) { UnityEngine.Debug.LogError("ConcealedGarden: Something terrible happened while trying to parse relationship for spawner " + creature.ID.spawner + " " + spawnData + "\n" + e); }
            }
        }
        
        // Clear loaded relationships if creature has a saved state
        // Prevents ctor-loaded rels from persisting if creature forgot about them
        private static void CreatureState_LoadFromString(On.CreatureState.orig_LoadFromString orig, CreatureState self, string[] s)
        {
            if (self.socialMemory != null && self.socialMemory.relationShips.Count > 0) self.socialMemory.relationShips.Clear(); // clear ctor relationship if loading from string
            // because if the creature lost all relationships it wouldnd save the block
            orig(self, s);
        }

        public delegate CreatureTemplate.Relationship IUART_UDR(IUseARelationshipTracker self, RelationshipTracker.DynamicRelationship drel);
    }
}