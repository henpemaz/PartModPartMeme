using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Squiddy
{
    public class SquiddyBase : SlugBaseCharacter
	{

		public SquiddyBase() : base("hensquiddy", FormatVersion.V1, 0, true) {
			On.Player.ctor += Player_ctor;
		}

        public override string DisplayName => "Squiddy";
		public override string Description => @"Look at 'em go!";

		public override string StartRoom => "SU_A13";
		public override void StartNewGame(Room room)
		{
			if (room.game.IsStorySession)
			{
				if (room.abstractRoom.name == StartRoom)
				{
					for (int i = 0; i < room.game.Players.Count; i++)
					{
						room.game.Players[i].pos = new WorldCoordinate(room.abstractRoom.index, -1, -1, 3);
						Debug.Log("Squiddy: Player position set to " + room.game.Players[i].pos);
					}
				}
			}
		}

		protected override void Disable()
		{
			// its a me
			On.Cicada.ctor -= Cicada_ctor;

			// play
			On.Cicada.Update -= Cicada_Update;
			On.Cicada.Act -= Cicada_Act;
			On.Cicada.Swim -= Cicada_Swim;

			On.Cicada.GrabbedByPlayer -= Cicada_GrabbedByPlayer;
			On.Cicada.CarryObject -= Cicada_CarryObject;
			On.Cicada.Collide -= Cicada_Collide;

			On.Cicada.Die -= Cicada_Die;
			On.CicadaAI.Update -= CicadaAI_Update;

			On.AbstractCreatureAI.DoIwantToDropThisItemInDen -= AbstractCreatureAI_DoIwantToDropThisItemInDen;
			On.CicadaGraphics.Update -= CicadaGraphics_Update;

			// bugfix
            On.AbstractCreature.IsExitingDen -= AbstractCreature_IsExitingDen;
			IL.ShortcutHelper.Update -= ShortcutHelper_Update;
			On.SuperJumpInstruction.ctor -= SuperJumpInstruction_ctor;
			On.RegionState.AdaptRegionStateToWorld -= RegionState_AdaptRegionStateToWorld;

			// arena colors
			IL.CicadaGraphics.ApplyPalette -= CicadaGraphics_ApplyPalette;
            On.CicadaGraphics.ApplyPalette -= CicadaGraphics_ApplyPalette;
			On.Cicada.ShortCutColor -= Cicada_ShortCutColor;
		}

        protected override void Enable()
		{
            On.Cicada.ctor += Cicada_ctor;

			On.Cicada.Update += Cicada_Update;
			On.Cicada.Act += Cicada_Act;
			On.Cicada.Swim += Cicada_Swim;

			On.Cicada.GrabbedByPlayer += Cicada_GrabbedByPlayer;
            On.Cicada.CarryObject += Cicada_CarryObject;
            On.Cicada.Collide += Cicada_Collide;

            On.Cicada.Die += Cicada_Die;
			On.CicadaAI.Update += CicadaAI_Update;

			On.AbstractCreatureAI.DoIwantToDropThisItemInDen += AbstractCreatureAI_DoIwantToDropThisItemInDen;
            On.CicadaGraphics.Update += CicadaGraphics_Update;

			// silly silly game.
			On.AbstractCreature.IsExitingDen += AbstractCreature_IsExitingDen;
			IL.ShortcutHelper.Update += ShortcutHelper_Update;
            On.SuperJumpInstruction.ctor += SuperJumpInstruction_ctor;
            On.RegionState.AdaptRegionStateToWorld += RegionState_AdaptRegionStateToWorld;

			IL.CicadaGraphics.ApplyPalette += CicadaGraphics_ApplyPalette;
			On.CicadaGraphics.ApplyPalette += CicadaGraphics_ApplyPalette;
            On.Cicada.ShortCutColor += Cicada_ShortCutColor;
		}

        // Ties player and squit
        internal class SquiddyStick : AbstractPhysicalObject.AbstractObjectStick
		{
			public SquiddyStick(AbstractPhysicalObject A, AbstractPhysicalObject B) : base(A, B) { }
		}
		// Make squiddy
		private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
		{
			orig(self, abstractCreature, world);
			if (!IsMe(self)) return;

			var abscada = new AbstractCreature(world, StaticWorld.creatureTemplates[((int)CreatureTemplate.Type.CicadaA)], null, abstractCreature.pos, abstractCreature.ID);
			player[abscada] = self;
			new SquiddyStick(abscada, abstractCreature);
			Debug.Log("Squiddy: Abstract Squiddy created and attached");
		}

		private void Cicada_ctor(On.Cicada.orig_ctor orig, Cicada self, AbstractCreature abstractCreature, World world, bool gender)
		{
			orig(self, abstractCreature, world, gender);
			if (player.TryGet(self.abstractCreature, out var p))
			{
				Debug.Log("Squiddy: Realized!");
				// mycologist would be proud
				p.bodyChunks = self.bodyChunks.Reverse().ToArray();
				p.bodyChunkConnections = self.bodyChunkConnections;

				self.flying = false; // shh

				// no stuck in den randomly
				self.abstractCreature.remainInDenCounter = 120;

				if (world.game.IsArenaSession)
				{
					// hsl math was failing to lerp to survivor-white somehow, had a red tint. no hue weight compensation for low sat colors
					var col = RXColor.HSLFromColor(p.ShortCutColor());
					var del = Mathf.Abs(col.h * col.s - self.iVars.color.hue * self.iVars.color.saturation) * col.s;
					//Debug.Log("got del " + del + " for p " + p.playerState.playerNumber);
					// hunter was getting this ugly purple result from blending cyan and red, looks way better if you shift the other way around, through green
					// this solution is somewhat 'fragile' but I doubt we're getting that many more characters in arena
					if (del > 0.5f) // too divergent, blend towards
					{
						//col.h = Mathf.Lerp(col.h, 0.33f, 0.66f);
						col.h = self.iVars.color.hue; // huh just jumping to the opposite side looked great on hunter
													  //self.iVars.color.hue = (self.iVars.color.hue + 0.33f) / 2f;
					}
					self.iVars.color = HSLColor.Lerp(self.iVars.color, new HSLColor(col.h, col.s, col.l), 0.5f * col.s); // magical * sat so white doesnt blend smh
				}
			}
		}

		// Lock player and squiddy
		// player inconsious update
        private void Cicada_Update(On.Cicada.orig_Update orig, Cicada self, bool eu)
        {
            if (player.TryGet(self.abstractCreature, out var p))
            {
				// make so that that the player tags along
				// p has been "destroyed" which means it isn't in a room's update list
				p.room = self.room;
				p.abstractCreature.pos = self.abstractCreature.pos;
				p.abstractCreature.world = self.abstractCreature.world;

				// underwater cam checks these
				p.airInLungs = self.lungs;
				p.dead = self.dead;

				// Match some behaviors
				p.flipDirection = self.flipH; // influences playerpickup

				if (!p.slatedForDeletetion) // still havent gotten rid of that damned player
				{
					p.Destroy(); // when room update actually removes the player, SquiddyStick will break and will need to be added again.
					Debug.Log("Squiddy: Player removed");
					self.room.abstractRoom.AddEntity(self.abstractCreature); // game just loaded, hadnt been added yet, so do it here.
					
				}
				if (p.abstractCreature.stuckObjects.Count == 0) // this gets called after being added to room each time, including pipes
                {
					// because removefromroom breaks up sticks
					new SquiddyStick(self.abstractCreature, p.abstractCreature);
					self.AI.tracker.ForgetCreature(p.abstractCreature); // tracker stupid
					Debug.Log("Squiddy: Attached to player");
				}

				// Input
				p.checkInput(); // partial update (:
								//p.Update(eu); // this one acts hilarious

				if (p.wantToJump > 0) p.wantToJump--;
				if (p.wantToPickUp > 0) p.wantToPickUp--;

				// no input
				if (p.input[0].x == 0 && p.input[0].y == 0 && !p.input[0].jmp && !p.input[0].thrw && !p.input[0].pckp)
				{
					p.touchedNoInputCounter++;
				}
				else
				{
					p.touchedNoInputCounter = 0;
				}

				// shelter activation
				p.readyForWin = false;
				if (p.room.abstractRoom.shelter && p.room.game.IsStorySession && !p.dead && !p.Sleeping && p.room.shelterDoor != null && !p.room.shelterDoor.Broken)
				{
					if (!p.stillInStartShelter && p.FoodInRoom(p.room, false) >= ((!p.abstractCreature.world.game.GetStorySession.saveState.malnourished) ? p.slugcatStats.foodToHibernate : p.slugcatStats.maxFood))
					{
						p.readyForWin = true;
						p.forceSleepCounter = 0;
					}
					else if (p.room.world.rainCycle.timer > p.room.world.rainCycle.cycleLength)
					{
						p.readyForWin = true;
						p.forceSleepCounter = 0;
					}
					else if (p.input[0].y < 0 && !p.input[0].jmp && !p.input[0].thrw && !p.input[0].pckp && p.IsTileSolid(1, 0, -1) && !p.abstractCreature.world.game.GetStorySession.saveState.malnourished && p.FoodInRoom(p.room, false) > 0 && p.FoodInRoom(p.room, false) < p.slugcatStats.foodToHibernate && (p.input[0].x == 0 || ((!p.IsTileSolid(1, -1, -1) || !p.IsTileSolid(1, 1, -1)) && p.IsTileSolid(1, p.input[0].x, 0))))
					{
						p.forceSleepCounter++;
					}
					else
					{
						p.forceSleepCounter = 0;
					}
					if (Custom.ManhattanDistance(p.abstractCreature.pos.Tile, p.room.shortcuts[0].StartTile) > 6)
					{
						if (p.readyForWin && p.touchedNoInputCounter > 20)
						{
							p.room.shelterDoor.Close();
						}
						else if (p.forceSleepCounter > 260)
						{
							p.sleepCounter = -24;
							p.room.shelterDoor.Close();
						}
					}
				}

				// karma flower placement
				if (p.room.game.IsStorySession)
				{
					if (p.room.game.cameras[0].hud != null && !p.room.game.cameras[0].hud.textPrompt.gameOverMode)
					{
						p.SessionRecord.time++;
					}
					if (p.PlaceKarmaFlower && !p.dead && p.grabbedBy.Count == 0 && p.IsTileSolid(1, 0, -1) && !p.room.GetTile(p.bodyChunks[1].pos).DeepWater && !p.IsTileSolid(1, 0, 0) && !p.IsTileSolid(1, 0, 1) && !p.room.GetTile(p.bodyChunks[1].pos).wormGrass && (p.room == null || !p.room.readyForAI || !p.room.aimap.getAItile(p.room.GetTilePosition(p.bodyChunks[1].pos)).narrowSpace))
					{
						p.karmaFlowerGrowPos = new WorldCoordinate?(p.room.GetWorldCoordinate(p.bodyChunks[1].pos));
					}
				}

				// SHROOMIES
				if (p.mushroomCounter > 0)
				{
					if (!p.inShortcut)
					{
						p.mushroomCounter--;
					}
					p.mushroomEffect = Custom.LerpAndTick(p.mushroomEffect, 1f, 0.05f, 0.025f);
				}
				else
				{
					p.mushroomEffect = Custom.LerpAndTick(p.mushroomEffect, 0f, 0.025f, 0.014285714f);
				}
				if (p.Adrenaline > 0f)
				{
					if (p.adrenalineEffect == null)
					{
						p.adrenalineEffect = new AdrenalineEffect(p);
						p.room.AddObject(p.adrenalineEffect);
					}
					else if (p.adrenalineEffect.slatedForDeletetion)
					{
						p.adrenalineEffect = null;
					}
				}

				// death grasp
				// this is from vanilla but could be reworked into a more flexible system.
				if (p.dangerGrasp == null)
				{
					p.dangerGraspTime = 0;
					foreach( var grasp in self.grabbedBy)
                    {
						if (grasp.grabber is Lizard || grasp.grabber is Vulture || grasp.grabber is BigSpider || grasp.grabber is DropBug || grasp.pacifying) // cmon joarge
						{
							p.dangerGrasp = grasp;
						}
					}
				}
				else if (p.dangerGrasp.discontinued || (!p.dangerGrasp.pacifying && p.stun <= 0))
				{
					p.dangerGrasp = null;
					p.dangerGraspTime = 0;
				}
				else
				{
					p.dangerGraspTime++;
					if (p.dangerGraspTime == 60)
					{
						p.room.game.GameOver(p.dangerGrasp);
					}
				}
			}

			orig(self, eu);
		}

		// inputs and stuff
		// player consious update
		private void Cicada_Act(On.Cicada.orig_Act orig, Cicada self)
        {
			if(player.TryGet(self.abstractCreature, out var p))

            {
                var room = self.room;
                var chunks = self.bodyChunks;
                var nc = chunks.Length;

                // shroom things
                if (p.Adrenaline > 0f)
                {
                    if (self.waitToFlyCounter < 30) self.waitToFlyCounter = 30;
                    if (self.flying)
                    {
                        self.flyingPower = Mathf.Lerp(self.flyingPower, 1.4f, 0.03f * p.Adrenaline);
                    }
                }
                var stuccker = self.AI.stuckTracker; // used while in climbing/pipe mode
                stuccker.stuckCounter = (int)Mathf.Lerp(stuccker.minStuckCounter, stuccker.maxStuckCounter, p.Adrenaline);

                // faster takeoff
                if (self.waitToFlyCounter <= 15)
                    self.waitToFlyCounter = 15;

                var inputDir = p.input[0].analogueDir.magnitude > 0.2f ? p.input[0].analogueDir
                    : p.input[0].IntVec.ToVector2().magnitude > 0.2 ? p.input[0].IntVec.ToVector2().normalized
                    : Vector2.zero;

                var inputLastDir = p.input[1].analogueDir.magnitude > 0.2f ? p.input[1].analogueDir
                    : p.input[1].IntVec.ToVector2().magnitude > 0.2 ? p.input[1].IntVec.ToVector2().normalized
                    : Vector2.zero;

                bool preventStaminaRegen = false;
                if (p.input[0].thrw && !p.input[1].thrw) p.wantToJump = 5;
                if (p.wantToJump > 0) // dash charge
                {
                    if (self.flying && !self.Charging && self.chargeCounter == 0 && self.stamina > 0.2f)
                    {
                        self.Charge(self.mainBodyChunk.pos + (inputDir == Vector2.zero ? (chunks[0].pos - chunks[1].pos) : inputDir) * 100f);
                        p.wantToJump = 0;
                    }
                }

                if (self.chargeCounter > 0) // charge windup or midcharge
                {
                    self.stamina -= 0.008f;
                    preventStaminaRegen = true;
                    if (self.chargeCounter < 20)
                    {
                        if (self.stamina <= 0.2f || !p.input[0].thrw) // cancel out if unable to complete
                        {
                            self.chargeCounter = 0;
                        }
                    }
                    else
                    {
                        if (self.stamina <= 0f) // cancel out mid charge if out of stamina (happens in long bouncy charges)
                        {
                            self.chargeCounter = 0;
                        }
                    }
                    self.chargeDir = (self.chargeDir
                                                + 0.15f * inputDir
                                                + 0.03f * Custom.DirVec(self.bodyChunks[1].pos, self.mainBodyChunk.pos)).normalized;
                }

                // scoooot
                self.AI.swooshToPos = null;
                if (p.input[0].jmp)
                {
                    if (self.room.aimap.getAItile(self.mainBodyChunk.pos).terrainProximity > 1 && self.stamina > 0.5f) // cada.flying && 
                    {
                        self.AI.swooshToPos = self.mainBodyChunk.pos + inputDir * 40f + new Vector2(0, 4f);
                        self.flyingPower = Mathf.Lerp(self.flyingPower, 1f, 0.05f);
                        preventStaminaRegen = true;
                        self.stamina -= 0.6f * self.stamina * inputDir.magnitude / ((!self.gender) ? 120f : 190f);
                    }
                    else // easier takeoff
                    {
                        if (self.waitToFlyCounter < 30) self.waitToFlyCounter = 30;
                    }
                }

                // move
                var basepos = 0.5f * (self.firstChunk.pos + room.MiddleOfTile(self.abstractCreature.pos.Tile));
                if (inputDir != Vector2.zero || self.Charging)
                {
                    self.AI.pathFinder.AbortCurrentGenerationPathFinding(); // ignore previous dest
                    self.AI.behavior = CicadaAI.Behavior.GetUnstuck; // helps with sitting behavior
                    var dest = basepos + inputDir * 20f;
                    if (self.flying) dest.y -= 12f; // nose up goes funny
                    self.abstractCreature.abstractAI.SetDestination(self.room.GetWorldCoordinate(dest));
                }
                else
                {
                    self.AI.behavior = CicadaAI.Behavior.Idle;
                    if (inputDir == Vector2.zero && inputLastDir != Vector2.zero) // let go
                    {
                        self.abstractCreature.abstractAI.SetDestination(self.room.GetWorldCoordinate(basepos));
                    }
                }

                // Grab update was becoming too big just like player
                GrabUpdate(self, p, inputDir);

                // player direct into holes simplified equivalent
                if ((p.input[0].x == 0 || p.input[0].y == 0) && p.input[0].x != p.input[0].y) // a straight direction
                {
                    for (int n = 0; n < nc; n++)
                    {
                        if (room.GetTile(chunks[n].pos + p.input[0].IntVec.ToVector2() * 40f).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                        {
                            chunks[n].vel += (room.MiddleOfTile(chunks[n].pos + new Vector2(20f * (float)p.input[0].x, 20f * (float)p.input[0].y)) - chunks[n].pos) / 10f;
                            break;
                        }
                    }
                }

                // from player movementupdate code, entering a shortcut
                if (self.shortcutDelay < 1)
                {
					self.abstractCreature.remainInDenCounter = 40; // so can eat whatever whenever
                    for (int i = 0; i < nc; i++)
                    {
                        if (self.enteringShortCut == null && room.GetTile(chunks[i].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                        {
                            var sctype = room.shortcutData(room.GetTilePosition(chunks[i].pos)).shortCutType;
                            if (sctype != ShortcutData.Type.DeadEnd
                            && (sctype != ShortcutData.Type.CreatureHole || self.abstractCreature.abstractAI.HavePrey())
                            && sctype != ShortcutData.Type.NPCTransportation)
                            {
                                IntVector2 intVector = room.ShorcutEntranceHoleDirection(room.GetTilePosition(chunks[i].pos));
                                if (p.input[0].x == -intVector.x && p.input[0].y == -intVector.y)
                                {
                                    self.enteringShortCut = new IntVector2?(room.GetTilePosition(chunks[i].pos));
                                }
                            }

                        }
                    }
                }

                if (preventStaminaRegen) // opposite of what happens in orig
                {
                    if (self.grabbedBy.Count == 0 && self.stickyCling == null)
                    {
                        self.stamina -= 0.014285714f;
                    }
                }
                self.stamina = Mathf.Clamp01(self.stamina);
            }

            orig(self);
		}

        private void GrabUpdate(Cicada self, Player p, Vector2 inputDir)
        {
			var room = self.room;
            var grasps = self.grasps;
            bool holdingGrab = p.input[0].pckp;
            bool still = (inputDir == Vector2.zero && !p.input[0].thrw && !p.input[0].jmp && self.Submersion < 0.5f);
            bool eating = false;
            bool swallow = false;
            if (still)
            {
                Creature.Grasp edible = grasps.FirstOrDefault(g => g != null && (
                    (g.grabbed is IPlayerEdible ipe && ipe.Edible && ipe.FoodPoints == 0) // Edible with no food points (otherwise must carry to den)
                  || g.grabbed is InsectHolder                                                                      // or ??
                ));

                if (edible != null && (holdingGrab || p.eatCounter < 15))
                {
                    eating = true;
                    if (edible.grabbed is IPlayerEdible ipe) // im assuming ill have something different going on at some point here
                    {
                        if (ipe.FoodPoints <= 0 || p.FoodInStomach < p.MaxFoodInStomach) // can eat
                        {
                            if (p.eatCounter < 1)
                            {
                                Debug.Log("Squiddy: bit IPlayerEdible " + ipe);
                                p.eatCounter = 15;
                                edible.grabber = p;
                                p.grasps[0] = edible;
                                p.BiteEdibleObject(self.evenUpdate); // player code go
                                edible.grabber = self;
                                p.grasps[0] = null;
                                if (edible.discontinued) edible.Release();
                            }
                        }
                        else // no can eat
                        {
                            if (p.eatCounter < 20 && room.game.cameras[0].hud != null)
                            {
                                room.game.cameras[0].hud.foodMeter.RefuseFood();
                            }
                        }
                    }
                }

                if (holdingGrab)
                {
                    if (edible == null && ((p.objectInStomach == null && grasps.Any(g => g != null && p.CanBeSwallowed((g.grabbed)))) || p.objectInStomach != null))
                    {
                        swallow = true;
                    }
                }
            }

            if (eating && p.eatCounter > 0)
            {
                p.eatCounter--;
            }
            else if (!eating && p.eatCounter < 40)
            {
                p.eatCounter++;
            }

            if (swallow)
            {
                p.swallowAndRegurgitateCounter++;
                if (p.objectInStomach != null && p.swallowAndRegurgitateCounter > 110)
                {
                    p.Regurgitate();
                    var grabbed = p.grasps[0].grabbed;
                    p.grasps[0].Release();
                    self.TryToGrabPrey(grabbed);
                    p.swallowAndRegurgitateCounter = 0;
                }
                else if (p.objectInStomach == null && p.swallowAndRegurgitateCounter > 90)
                {
                    for (int j = 0; j < grasps.Length; j++)
                    {
                        if (grasps[j] != null && p.CanBeSwallowed(self.grasps[j].grabbed))
                        {
                            self.bodyChunks[0].pos += Custom.DirVec(grasps[j].grabbed.firstChunk.pos, self.bodyChunks[0].pos) * 2f;
                            var grabbed = grasps[j].grabbed;
                            self.ReleaseGrasp(j);
                            p.SlugcatGrab(grabbed, j);
                            p.SwallowObject(j);
                            p.swallowAndRegurgitateCounter = 0;
                            break;
                        }
                    }
                }
            }
            else
            {
                p.swallowAndRegurgitateCounter = 0;
            }

            // this was in vanilla might as well keep it
            foreach (var grasp in grasps) if (grasp != null && grasp.grabbed.slatedForDeletetion) self.ReleaseGrasp(grasp.graspUsed);

            // pickup updage
            if (p.input[0].pckp && !p.input[1].pckp) p.wantToPickUp = 5;

            PhysicalObject physicalObject = (p.dontGrabStuff >= 1) ? null : PickupCandidate(self, p);
            if (p.pickUpCandidate != physicalObject && physicalObject != null && physicalObject is PlayerCarryableItem)
            {
                (physicalObject as PlayerCarryableItem).Blink();
            }
            p.pickUpCandidate = physicalObject;



            if (p.wantToPickUp > 0) // pick up
            {
                var dropInstead = true; // grasps.Any(g => g != null);
                for (int i = 0; i < p.input.Length && i < 5; i++)
                {
                    if (p.input[i].y > -1) dropInstead = false;
                }
                if (dropInstead)
                {
                    for (int i = 0; i < grasps.Length; i++)
                    {
                        if (grasps[i] != null)
                        {
							Debug.Log("Squiddy: put item on ground!");
                            p.wantToPickUp = 0;
                            room.PlaySound((!(grasps[i].grabbed is Creature)) ? SoundID.Slugcat_Lay_Down_Object : SoundID.Slugcat_Lay_Down_Creature, grasps[i].grabbedChunk, false, 1f, 1f);
                            room.socialEventRecognizer.CreaturePutItemOnGround(grasps[i].grabbed, self);
                            if (grasps[i].grabbed is PlayerCarryableItem)
                            {
                                (grasps[i].grabbed as PlayerCarryableItem).Forbid();
                            }
                            self.ReleaseGrasp(i);
                            break;
                        }
                    }
                }
                else if (p.pickUpCandidate != null)
                {
                    for (int i = 0; i < grasps.Length; i++)
                    {
                        if (grasps[i] == null)
                        {
                            if (self.TryToGrabPrey(p.pickUpCandidate))
                            {
                                Debug.Log("Squiddy: grabbed " + p.pickUpCandidate);
                                p.pickUpCandidate = null;
                                p.wantToPickUp = 0;
                            }
                            break;
                        }
                    }
                }
            }
        }

        private class InsectHolder : PlayerCarryableItem, IPlayerEdible, IDrawable
        {
            public InsectHolder(CosmeticInsect insect, Player p, Room room) 
				: base(new AbstractPhysicalObject(room.world, AbstractPhysicalObject.AbstractObjectType.AttachedBee, null, room.GetWorldCoordinate(insect.pos), room.game.GetNewID())
				{ destroyOnAbstraction = true})
            {
                this.insect = insect;
                this.p = p;
                this.bodyChunks = new BodyChunk[] { new BodyChunk(this, 0, insect.pos, 2f, 0.01f) };
				this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
			}

            public override string ToString()
            {
				return "InsectHolder of " + insect.type.ToString();

			}

            public override void PickedUp(Creature upPicker)
            {
                base.PickedUp(upPicker);
				//insect. ??
            }

            public override void Update(bool eu)
            {
				if (this.grabbedBy.Count == 0 && p.pickUpCandidate != this) Destroy();
				if (this.insect.slatedForDeletetion) Destroy();
				base.Update(eu);
				if (slatedForDeletetion) return;
				if (this.grabbedBy.Count != 0)
				{
					insect.pos = this.firstChunk.pos;
					insect.vel = 0.8f * insect.vel + this.firstChunk.vel;
				}
                else
                {
					this.firstChunk.pos = insect.pos;
					this.firstChunk.vel = insect.vel;
				}
			}

            int bites = 1;
            public readonly CosmeticInsect insect;
            private readonly Player p;

            public int BitesLeft => bites;

            public int FoodPoints => 1;

            public bool Edible => true;

            public bool AutomaticPickUp => false;

            public void BitByPlayer(Creature.Grasp grasp, bool eu)
            {
				this.bites--;
				this.room.PlaySound(SoundID.Slugcat_Bite_Fly, base.firstChunk.pos);
				this.firstChunk.MoveFromOutsideMyUpdate(eu, grasp.grabber.mainBodyChunk.pos);
				if (this.bites < 1)
				{
					(grasp.grabber as Player).ObjectEaten(this);
					grasp.Release();
					insect.Destroy();
				}
			}

            public void ThrowByPlayer() { }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
				this.insect.InitiateSprites(sLeaser, rCam);
				foreach (var sprt in sLeaser.sprites)
				{
					sprt.color = base.blinkColor;
				}
			}

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
				this.insect.DrawSprites(sLeaser, rCam, timeStacker, camPos);
				if (this.blink > 0 && UnityEngine.Random.value < 0.5f)
				{
					foreach(var sprt in sLeaser.sprites)
                    {
						sprt.isVisible = true;
						sprt.color = base.blinkColor;
                    }
				}
				else
				{
					foreach (var sprt in sLeaser.sprites)
					{
						sprt.isVisible = false;
					}
				}
				if (base.slatedForDeletetion || this.room != rCam.room)
				{
					sLeaser.CleanSpritesAndRemove();
				}
			}

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) { }
        }

        private PhysicalObject PickupCandidate(Cicada self, Player p)
        {
			self.cantPickUpPlayer = p;
			self.cantPickUpCounter = 5;
			var candidate = p.PickupCandidate(0f);
			if (self.room?.insectCoordinator != null)
            {
				CosmeticInsect closestInsect = null;
				var ownpos = self.firstChunk.pos;
				float maxdist = candidate == null ? 30f : (ownpos - candidate.firstChunk.pos).magnitude;
				foreach(var insect in self.room.insectCoordinator.allInsects)
                {
					if(!insect.slatedForDeletetion && insect.alive && insect.inGround < 1f)
                    {
						var dist = (ownpos - insect.pos).magnitude;
						if(dist < maxdist)
                        {
							closestInsect = insect;
							maxdist = dist;
                        }
					}
                }

				if(closestInsect != null)
                {
					if(p.pickUpCandidate is InsectHolder hldr && hldr.insect == closestInsect)
                    {
						candidate = hldr;
                    }
                    else
                    {
						candidate = new InsectHolder(closestInsect, p, self.room);
						self.room.AddObject(candidate);
					}
                }
            }

			return candidate;
        }

        private void Cicada_Swim(On.Cicada.orig_Swim orig, Cicada self)
        {
			if (player.TryGet(self.abstractCreature, out var p))
			{
				if (self.Consious)
                {
					self.Act();
					if (self.Submersion == 1f)
					{
						self.flying = false;
						if (self.graphicsModule is CicadaGraphics cg)
						{
							cg.wingDeploymentGetTo = 0.2f;
						}
						self.waitToFlyCounter = 0; // so graphics uses wingdeployment
					}
				}
			}

			orig(self);
		}

        private void Cicada_GrabbedByPlayer(On.Cicada.orig_GrabbedByPlayer orig, Cicada self)
        {
			if (player.TryGet(self.abstractCreature, out var p))
			{
				if (self.Consious)
				{
					var oldflypower = self.flyingPower;
					self.flyingPower = self.flying ? 0.2f : self.flyingPower;
					self.Act();
				}
			}
			orig(self);
        }

		private void Cicada_CarryObject(On.Cicada.orig_CarryObject orig, Cicada self)
		{
			if (player.TryGet(self.abstractCreature, out var p))
			{
				// more realistic grab pos plz
				var oldpos = self.mainBodyChunk.pos;
				var owndir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
				self.mainBodyChunk.pos += 5f * owndir; 
				orig(self);
				self.mainBodyChunk.pos = oldpos;
				if (self.grasps[0] != null && self.grasps[0].grabbed is Spear speary)
                {
					var hangingdir = (speary.firstChunk.pos - oldpos).normalized;
					speary.setRotation = (self.chargeCounter > 0) ? owndir.normalized : Custom.PerpendicularVector(owndir);
					speary.rotationSpeed = 0f;
				}
				return;
			}
			orig(self);
		}

		private void Cicada_Collide(On.Cicada.orig_Collide orig, Cicada self, PhysicalObject otherObject, int myChunk, int otherChunk)
		{
			if (player.TryGet(self.abstractCreature, out var p))
			{
				if(self.Charging && self.grasps[0] != null && self.grasps[0].grabbed is Weapon we && myChunk == 0 && otherChunk >= 0)
                {
					var dir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
					var throwndir = new IntVector2(Mathf.Abs(dir.x) > 0.38 ? (int)Mathf.Sign(dir.x) : 0, Mathf.Abs(dir.y) > 0.38 ? (int)Mathf.Sign(dir.y) : 0);
					we.Thrown(self, self.mainBodyChunk.pos + dir * 30f, self.mainBodyChunk.pos, throwndir, 1f, self.evenUpdate);
					we.meleeHitChunk = otherObject.bodyChunks[otherChunk];
					if(we is Spear sp && !(otherObject is Player))
                    {
						sp.spearDamageBonus *= 0.6f;
                    }
					we.Forbid();
					self.ReleaseGrasp(0);
                }
				orig(self, otherObject, myChunk, otherChunk);
				return;
			}
			orig(self, otherObject, myChunk, otherChunk);
		}

		// Die! Squiddy
		private void Cicada_Die(On.Cicada.orig_Die orig, Cicada self)
		{
			if (player.TryGet(self.abstractCreature, out var p))
            {
				p.Die();
            }
			orig(self);
		}

		// dont let AI interfere on squiddy
		private void CicadaAI_Update(On.CicadaAI.orig_Update orig, CicadaAI self)
        {
			if (player.TryGet(self.creature, out var p))
			{
				if (self.cicada.room?.Tiles != null && !self.pathFinder.DoneMappingAccessibility)
					self.pathFinder.accessibilityStepsPerFrame = self.cicada.room.Tiles.Length; // faster, damn it. on entering a new room this needs to complete before it can pathfind
				else self.pathFinder.accessibilityStepsPerFrame = 10;
				self.pathFinder.Update(); // basic movement uses this
				self.tracker.Update(); // creature looker uses this
				self.tracker.ForgetCreature(p.abstractCreature);
				//self.preyTracker.Update(); // for grabbing prey
				//self.preyTracker.ForgetPrey(p.abstractCreature);
			}
            else
            {
				orig(self);
			}
		}

		// eat the thing you carried to a den
		private bool AbstractCreatureAI_DoIwantToDropThisItemInDen(On.AbstractCreatureAI.orig_DoIwantToDropThisItemInDen orig, AbstractCreatureAI self, AbstractPhysicalObject item)
		{
			if (player.TryGet(self.parent, out var p) && self.parent.InDen)
			{
				int amount = 0;
				int quarters = 0;
				bool eaten = false;
				if(item.realizedObject is IPlayerEdible ipe && ipe.Edible && ipe.FoodPoints > 0)
                {
					if (ipe is Creature) amount = ipe.FoodPoints;
					else quarters = ipe.FoodPoints;
                }

				// maybe switch to biting the thing until its done??
				// could break

				if (amount > 0)
                {
					p.AddFood(amount);
					eaten = true;
					self.parent.remainInDenCounter = Mathf.Min(self.parent.remainInDenCounter, 120 + amount * 80);
				}
                if (quarters > 0)
                {
					for (int i = 0; i < quarters; i++) p.AddQuarterFood();
					eaten = true;
					self.parent.remainInDenCounter = Mathf.Min(self.parent.remainInDenCounter, 60 + quarters * 20);
				}
                if (eaten)
                {
					Debug.Log("Squiddy: eaten in den " + item + " for " + amount + " + " + quarters);
				}

				return orig(self, item) && eaten;
			}
			else
			{
				return orig(self, item);
			}
		}

		// move tentacles properly
		private void CicadaGraphics_Update(On.CicadaGraphics.orig_Update orig, CicadaGraphics self)
		{
			orig(self);
			if (player.TryGet(self.cicada.abstractCreature, out var p))
			{
				for (int m = 0; m < 2; m++)
				{
					for (int n = 0; n < 2; n++)
					{
						Limb limb = self.tentacles[m, n];
						if (limb.mode == Limb.Mode.HuntAbsolutePosition)
						{
							// catch up properly like in relative hunt pos
							limb.pos += limb.connection.vel;
						}
					}
				}
			}
		}

		#region miscfixes
		// bugfix the gmae
		// 2 connected creatures leaving den = both added twice to the room.
		private void AbstractCreature_IsExitingDen(On.AbstractCreature.orig_IsExitingDen orig, AbstractCreature self)
		{
			if (player.TryGet(self, out var p) && self.realizedCreature != null) // if its us pulling things out of den, pull them in and prevent other creatures from being added to shortcuts
			{
				var room = self.Room;
				// from Abstractphisicalobjects but with changes so not to run isexitingden because that puts creatures in shortcuts
				// the alternative would be to intercept ALL calls to shortcuthandler.enteringfromabstractroom and that'd be a lot more code running
				foreach(var ent in self.GetAllConnectedObjects())
                {
					if(ent != self && ent.InDen)
                    {
						ent.InDen = false;
						room.entitiesInDens.Remove(ent);
						room.AddEntity(ent);
					}
                }
			}
			orig(self);
		}

		// skip me for playerpushbacks please
		private void ShortcutHelper_Update(ILContext il)
		{
			var c = new ILCursor(il);
			ILLabel innerloop = null;
			ILLabel afterloop = null;
			int itercount = 0;
			MethodReference itemGetCall = null;
			int playerloc = 0;
			if (c.TryGotoNext(MoveType.Before,
				i => i.MatchLdarg(0),
				i => i.MatchLdfld<ShortcutHelper>("pushers"),
				i => i.MatchLdloc(out itercount),
				i => i.MatchCallOrCallvirt(out itemGetCall),
				i => i.MatchLdflda<ShortcutHelper.ShortcutPusher>("shortcutDir"),
				i => i.MatchLdfld<RWCustom.IntVector2>("y"),
				i => i.MatchLdcI4(0),
				i => i.MatchBle(out innerloop),

				i => i.MatchLdloc(out playerloc),
				i => i.MatchLdfld<Player>("animation"),
				i => i.MatchLdcI4(19),
				i => i.MatchBeq(out afterloop),
				i => i.MatchLdloc(out _),
				i => i.MatchLdfld<Player>("animation"),
				i => i.MatchLdcI4(3),
				i => i.MatchBeq(out _)
				))
			{
				c.MoveAfterLabels();
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldarg_0);
				c.Emit<ShortcutHelper>(OpCodes.Ldfld, "pushers");
				c.Emit(OpCodes.Ldloc, itercount);
				c.Emit(OpCodes.Callvirt, itemGetCall);
				c.Emit(OpCodes.Ldloc, playerloc);
				c.EmitDelegate<Func<ShortcutHelper, ShortcutHelper.ShortcutPusher, Player, bool>>((helper, pusher, self) => // bypass me, pushbacks
				{
					return IsMe(self) && helper.room.shortcutData(pusher.shortCutPos).shortCutType == ShortcutData.Type.CreatureHole;
				});
				c.Emit(OpCodes.Brtrue, afterloop);
			}
			else Debug.LogException(new Exception("Couldn't IL-hook ShortcutHelper_Update from squiddy")); // deffendisve progrmanig
		}

		// what do you think i am stupid
		private void SuperJumpInstruction_ctor(On.SuperJumpInstruction.orig_ctor orig, SuperJumpInstruction self, Room room, PlacedObject placedObject)
		{
			orig(self, room, placedObject);
			if (IsMe(room.game)) self.Destroy();
		}

		// remove squiddy from save.
		private void RegionState_AdaptRegionStateToWorld(On.RegionState.orig_AdaptRegionStateToWorld orig, RegionState self, int playerShelter, int activeGate)
		{
			for (int i = 0; i < self.world.NumberOfRooms; i++)
			{
				AbstractRoom abstractRoom = self.world.GetAbstractRoom(self.world.firstRoomIndex + i);
				//if (abstractRoom.index == activeGate) continue;
				for (int j = abstractRoom.entities.Count - 1; j >= 0; j--)
				{
					if (abstractRoom.entities[j] is AbstractCreature ac && player.TryGet(ac, out var p))
					{
						abstractRoom.RemoveEntity(ac);
						player.Unset(ac);
						Debug.Log("Squiddy: removed from save");
					}
				}
				for (int j = abstractRoom.entitiesInDens.Count - 1; j >= 0; j--)
				{
					if (abstractRoom.entitiesInDens[j] is AbstractCreature ac && player.TryGet(ac, out var p))
					{
						abstractRoom.RemoveEntity(ac);
						player.Unset(ac);
						Debug.Log("Squiddy: removed from save");
					}
				}
			}

			orig(self, playerShelter, activeGate);
		}
		#endregion miscfixes

		#region arenacolors
		// arena colors

		// arena color mixing patchup
		private void CicadaGraphics_ApplyPalette(ILContext il)
		{
			var c = new ILCursor(il);
			if (c.TryGotoNext(MoveType.AfterLabel,
				i => i.MatchStloc(0) // I hope this one is stable
				))
			{
				c.MoveAfterLabels();
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<Color, CicadaGraphics, Color>>((cin, self) => // MORE tone of body
				{
					if (player.TryGet(self.cicada.abstractCreature, out var p))
					{
						if (self.cicada.room.world.game.IsArenaSession)
						{
							var col = RXColor.HSLFromColor(p.ShortCutColor());
							return HSLColor.Lerp(self.iVars.color, new HSLColor(col.h, col.s, col.l), 0.8f * col.s).rgb;
						}
					}
					return cin;
				});
			}
			else Debug.LogException(new Exception("Couldn't IL-hook CicadaGraphics_ApplyPalette from squiddy")); // deffendisve progrmanig
		}

		// Stronger color of body in arena
		private void CicadaGraphics_ApplyPalette(On.CicadaGraphics.orig_ApplyPalette orig, CicadaGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);

			if (player.TryGet(self.cicada.abstractCreature, out var p))
			{
				if (self.cicada.room.world.game.IsArenaSession)
				{
					// use 1:1 color no blending no filter
					Color bodyColor = p.ShortCutColor();
					var bodyHSL = RXColor.HSLFromColor(bodyColor);
					if (bodyHSL.l < 0.5) // stupig nighat color palete
					{
						if (self.cicada.gender)
						{
							self.cicada.gender = false;
							orig(self, sLeaser, rCam, palette); // what could go wrong
							self.cicada.gender = true;
						}
					}

					sLeaser.sprites[self.HighlightSprite].color = Color.Lerp(bodyColor, new Color(1f, 1f, 1f), 0.7f);
					sLeaser.sprites[self.BodySprite].color = bodyColor;
					sLeaser.sprites[self.HeadSprite].color = bodyColor;

					for (int side = 0; side < 2; side++)
						for (int wing = 0; wing < 2; wing++)
						{
							sLeaser.sprites[self.TentacleSprite(side, wing)].color = bodyColor;
						}
				}
			}
		}

		// correct shortcut color in arena
		// storymode remains squiddy-colored I guess
		private Color Cicada_ShortCutColor(On.Cicada.orig_ShortCutColor orig, Cicada self)
		{

			if (player.TryGet(self.abstractCreature, out var p))
			{
				if (self.abstractCreature.world.game.IsArenaSession)
				{
					return p.ShortCutColor(); // replaces ivar color which is a blend
				}
			}
			return orig(self);
		}
		#endregion arenacolors

		public static AttachedField<AbstractCreature, Player> player = new AttachedField<AbstractCreature, Player>();
    }
}