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
    public partial class SquiddyBase : SlugBaseCharacter
	{
		public SquiddyBase() : base("hensquiddy", FormatVersion.V1, 0, true) {
			// pre-enable and menu hooks

			// its a me
            On.PlayerState.ctor += PlayerState_ctor;
			On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;

			// starts at low karma, up on survivor
            On.WinState.CycleCompleted += WinState_CycleCompleted;
            On.SaveState.IncreaseKarmaCapOneStep += SaveState_IncreaseKarmaCapOneStep;

			// display karma uppening on survivor event
            On.Menu.KarmaLadder.ctor += KarmaLadder_ctor;
            On.Menu.SleepAndDeathScreen.FoodCountDownDone += SleepAndDeathScreen_FoodCountDownDone;
		}

		public AttachedField<AbstractCreature, AbstractCreature> player = new AttachedField<AbstractCreature, AbstractCreature>();
		public AttachedField<AbstractCreature, AbstractCreature> cicada = new AttachedField<AbstractCreature, AbstractCreature>();

		// Make squiddy
		// this used to be in playerctor which was a mess because it was mid-room-realize
		// moved to session.addplayers but then that doesn't account for ghosts
		// now its here in playerstate ctor which is... where the important stuff is set I suppose
		private void PlayerState_ctor(On.PlayerState.orig_ctor orig, PlayerState self, AbstractCreature crit, int playerNumber, int slugcatCharacter, bool isGhost)
		{
			orig(self, crit, playerNumber, slugcatCharacter, isGhost);

			if ((crit.world.game.IsStorySession && slugcatCharacter == this.SlugcatIndex)
				|| (crit.world.game.IsArenaSession && ArenaAdditions.GetSelectedArenaCharacter(crit.world.game.rainWorld.processManager.arenaSetup, playerNumber).player == this))
			{
				var abscada = new AbstractCreature(crit.world, StaticWorld.creatureTemplates[((int)CreatureTemplate.Type.CicadaA)], null, crit.pos, crit.ID);
				new SquiddyStick(abscada, crit);

				if (crit.world.game.IsStorySession)
				{
					crit.Room.AddEntity(abscada); // Actually pretty darn important,
												  // prevent player from being realized in shortcutsready since there's a squit (uses AI) in there too
				}
				// in arenamode stuff spawns in the shortcuts systems and plays out nicely

				abscada.remainInDenCounter = 120; // maybe move this to story start?
				this.player[abscada] = crit;
				cicada[crit] = abscada;

				Debug.Log("Squiddy: Abstract Squiddy created and attached for player no:" + playerNumber);
				//Debug.Log("Squiddy: room is "  + player.Room.name);
			}
		}

		private void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
		{
			orig(self);
			// maybe only cull where player.world.game is self ? can't do that with these, shame
			player.Clear();
			cicada.Clear();
		}

		public override string DisplayName => "Squiddy";
		public override string Description => @"Look at 'em go!";

		//override color

		public override string StartRoom => "SU_A13";
		public override void StartNewGame(Room room)
		{
			if (room.game.IsStorySession)
			{
                if (IsMe(room.game))
                {
					room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap = 2; // starts campaign at k3
                }
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

        protected override void Enable()
		{
			On.Cicada.Update += Cicada_Update; // input, sync, player things
			On.Cicada.Act += Cicada_Act; // movement
			On.Cicada.Swim += Cicada_Swim; // prevent loss of control

			On.Cicada.GrabbedByPlayer += Cicada_GrabbedByPlayer; // prevent loss of control
            On.Cicada.CarryObject += Cicada_CarryObject;
            On.Cicada.Collide += Cicada_Collide;

            On.Cicada.Die += Cicada_Die;
			On.CicadaAI.Update += CicadaAI_Update;
            On.AbstractCreature.WantToStayInDenUntilEndOfCycle += AbstractCreature_WantToStayInDenUntilEndOfCycle;
            On.AbstractCreature.Abstractize += AbstractCreature_Abstractize;
            On.ShortcutHandler.SuckInCreature += ShortcutHandler_SuckInCreature;
            On.ShortcutHandler.OnScreenPositionOfInShortCutCreature += ShortcutHandler_OnScreenPositionOfInShortCutCreature;
            On.Player.ShortCutColor += Player_ShortCutColor;
            On.RoomCamera.MoveCamera_Room_int += RoomCamera_MoveCamera_Room_int;

            On.Player.CanIPickThisUp += Player_CanIPickThisUp;
            On.Player.ObjectEaten += Player_ObjectEaten;
            On.Player.FoodInRoom_Room_bool += Player_FoodInRoom_Room_bool;
            On.Player.ObjectCountsAsFood += Player_ObjectCountsAsFood;
			On.AbstractCreatureAI.DoIwantToDropThisItemInDen += AbstractCreatureAI_DoIwantToDropThisItemInDen;
            On.CicadaGraphics.Update += CicadaGraphics_Update;

            On.ShortcutGraphics.GenerateSprites += ShortcutGraphics_GenerateSprites;
			On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;
            On.ShortcutGraphics.Update += ShortcutGraphics_Update;

			On.AbstractCreature.IsExitingDen += AbstractCreature_IsExitingDen;
			IL.ShortcutHelper.Update += ShortcutHelper_Update;
            On.SuperJumpInstruction.ctor += SuperJumpInstruction_ctor;
            On.RegionState.AdaptRegionStateToWorld += RegionState_AdaptRegionStateToWorld;

			On.Cicada.InitiateGraphicsModule += Cicada_InitiateGraphicsModule;
			IL.CicadaGraphics.ApplyPalette += CicadaGraphics_ApplyPalette;
			On.CicadaGraphics.ApplyPalette += CicadaGraphics_ApplyPalette;

            On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversation_AddEvents;

			densNeededAmount = 0f;
			densNeeded = false;
		}

        protected override void Disable()
		{
			On.Cicada.Update -= Cicada_Update;
			On.Cicada.Act -= Cicada_Act;
			On.Cicada.Swim -= Cicada_Swim;

			On.Cicada.GrabbedByPlayer -= Cicada_GrabbedByPlayer;
			On.Cicada.CarryObject -= Cicada_CarryObject;
			On.Cicada.Collide -= Cicada_Collide;

			On.Cicada.Die -= Cicada_Die;
			On.CicadaAI.Update -= CicadaAI_Update;
			On.AbstractCreature.WantToStayInDenUntilEndOfCycle -= AbstractCreature_WantToStayInDenUntilEndOfCycle;
			On.AbstractCreature.Abstractize -= AbstractCreature_Abstractize;
			On.ShortcutHandler.SuckInCreature -= ShortcutHandler_SuckInCreature;
			On.ShortcutHandler.OnScreenPositionOfInShortCutCreature -= ShortcutHandler_OnScreenPositionOfInShortCutCreature;
			On.Player.ShortCutColor -= Player_ShortCutColor;
			On.RoomCamera.MoveCamera_Room_int -= RoomCamera_MoveCamera_Room_int;

			On.Player.CanIPickThisUp -= Player_CanIPickThisUp;
			On.Player.ObjectEaten -= Player_ObjectEaten;
			On.Player.FoodInRoom_Room_bool -= Player_FoodInRoom_Room_bool;
			On.Player.ObjectCountsAsFood -= Player_ObjectCountsAsFood;
			On.AbstractCreatureAI.DoIwantToDropThisItemInDen -= AbstractCreatureAI_DoIwantToDropThisItemInDen;
			On.CicadaGraphics.Update -= CicadaGraphics_Update;

			On.ShortcutGraphics.GenerateSprites -= ShortcutGraphics_GenerateSprites;
			On.ShortcutGraphics.Draw -= ShortcutGraphics_Draw;
			On.ShortcutGraphics.Update -= ShortcutGraphics_Update;

			On.AbstractCreature.IsExitingDen -= AbstractCreature_IsExitingDen;
			IL.ShortcutHelper.Update -= ShortcutHelper_Update;
			On.SuperJumpInstruction.ctor -= SuperJumpInstruction_ctor;
			On.RegionState.AdaptRegionStateToWorld -= RegionState_AdaptRegionStateToWorld;

			On.Cicada.InitiateGraphicsModule -= Cicada_InitiateGraphicsModule;
			IL.CicadaGraphics.ApplyPalette -= CicadaGraphics_ApplyPalette;
			On.CicadaGraphics.ApplyPalette -= CicadaGraphics_ApplyPalette;


			On.SSOracleBehavior.PebblesConversation.AddEvents -= PebblesConversation_AddEvents;
		}

		// Ties player and squit (not a grasp, so not easily undone by game code)
		internal class SquiddyStick : AbstractPhysicalObject.AbstractObjectStick
		{
			public SquiddyStick(AbstractPhysicalObject A, AbstractPhysicalObject B) : base(A, B) { }
		}
		
		// Lock player and squiddy
		// player inconsious update
        private void Cicada_Update(On.Cicada.orig_Update orig, Cicada self, bool eu)
        {
            if (player.TryGet(self.abstractCreature, out var ap) && ap.realizedCreature is Player p)
            {
				// make so that that the player tags along
				// p has been "destroyed" which means it isn't in a room's update list
				p.room = self.room;
				p.abstractCreature.pos = self.abstractCreature.pos;
				p.abstractCreature.world = self.abstractCreature.world;

				// underwater cam checks these
				p.airInLungs = self.lungs;
				if (p.dead && !self.dead) self.Die();
				p.dead = self.dead;

				// Match some behaviors
				p.flipDirection = self.flipH; // influences playerpickup
				p.stun = self.stun;

                if (!p.slatedForDeletetion)
                {
					// mycologist would be proud
					p.bodyChunks = self.bodyChunks.Reverse().ToArray();
					p.bodyChunkConnections = self.bodyChunkConnections;
					p.Destroy(); // when room update actually removes the player, SquiddyStick will break and will need to be added again.
					Debug.Log("Squiddy: Player removed");
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

				// cheats
				if (self.room != null && self.room.game.devToolsActive)
				{
					if (Input.GetKey("q") && !p.FLYEATBUTTON)
					{
						p.AddFood(1);
					}
					p.FLYEATBUTTON = Input.GetKey("q");

					if (Input.GetKey("v"))
					{
						for (int m = 0; m < 2; m++)
						{
							self.bodyChunks[m].vel = Custom.DegToVec(UnityEngine.Random.value * 360f) * 12f;
							self.bodyChunks[m].pos = (Vector2)Input.mousePosition + self.room.game.cameras[0].pos;
							self.bodyChunks[m].lastPos = (Vector2)Input.mousePosition + self.room.game.cameras[0].pos;
						}
					}
					else if (Input.GetKey("w"))
					{
						self.bodyChunks[1].vel += Custom.DirVec(self.bodyChunks[1].pos, Input.mousePosition) * 7f;
					}
				}
			}

			orig(self, eu);
		}

		// inputs and stuff
		// player consious update
		private void Cicada_Act(On.Cicada.orig_Act orig, Cicada self)
        {
			if(player.TryGet(self.abstractCreature, out var ap) && ap.realizedCreature is Player p)

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

					if (self.Charging && self.grasps[0] != null && self.grasps[0].grabbed is Weapon w)
					{
						SharedPhysics.CollisionResult result = SharedPhysics.TraceProjectileAgainstBodyChunks(null, self.room, w.firstChunk.lastPos, ref w.firstChunk.pos, w.firstChunk.rad + 5f, 1, self, true);
                        if (result.hitSomething)
                        {
							var dir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
							var throwndir = new IntVector2(Mathf.Abs(dir.x) > 0.38 ? (int)Mathf.Sign(dir.x) : 0, Mathf.Abs(dir.y) > 0.38 ? (int)Mathf.Sign(dir.y) : 0);
							w.Thrown(self, self.mainBodyChunk.pos, self.mainBodyChunk.pos - dir * 30f, throwndir, 1f, self.evenUpdate);
							if (w is Spear sp && !(result.obj is Player))
							{
								sp.spearDamageBonus *= 0.6f;
							}
							w.Forbid();
							self.ReleaseGrasp(0);
						}
					}
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
					self.abstractCreature.remainInDenCounter = 200; // so can eat whatever whenever
                    for (int i = 0; i < nc; i++)
                    {
                        if (self.enteringShortCut == null && room.GetTile(chunks[i].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance)
                        {
                            var sctype = room.shortcutData(room.GetTilePosition(chunks[i].pos)).shortCutType;
                            if (sctype != ShortcutData.Type.DeadEnd
                            //&& (sctype != ShortcutData.Type.CreatureHole || self.abstractCreature.abstractAI.HavePrey())
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

        private void Cicada_Swim(On.Cicada.orig_Swim orig, Cicada self)
        {
			if (player.TryGet(self.abstractCreature, out var ap) && ap.realizedCreature is Player p)
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

		private void Cicada_CarryObject(On.Cicada.orig_CarryObject orig, Cicada self)
		{
			if (player.TryGet(self.abstractCreature, out var ap) && ap.realizedCreature is Player p)
			{
				// more realistic grab pos plz
				var oldpos = self.mainBodyChunk.pos;
				var owndir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
				self.mainBodyChunk.pos += 5f * owndir; 
				orig(self); // this thing drops creatures cada doesn't eat. it's a bit weird but its ok I guess
				self.mainBodyChunk.pos = oldpos;
				if (self.grasps[0] != null && self.grasps[0].grabbed is Spear speary)
                {
					var hangingdir = (speary.firstChunk.pos - oldpos).normalized;
					speary.setRotation = Vector2.Lerp(
						Vector2.Lerp(Custom.PerpendicularVector(owndir), owndir.normalized, ((float)self.chargeCounter) / 20f),
						speary.rotation, 0.25f);
					speary.rotationSpeed = 0f;
				}
				return;
			}
			orig(self);
		}

		private void Cicada_Collide(On.Cicada.orig_Collide orig, Cicada self, PhysicalObject otherObject, int myChunk, int otherChunk)
		{
			if (player.TryGet(self.abstractCreature, out var ap) && ap.realizedCreature is Player p)
			{
				if (self.Charging && self.grasps[0] != null && self.grasps[0].grabbed is Weapon we && myChunk == 0 && otherChunk >= 0)
				{
					var dir = (self.bodyChunks[0].pos - self.bodyChunks[1].pos).normalized;
					var throwndir = new IntVector2(Mathf.Abs(dir.x) > 0.38 ? (int)Mathf.Sign(dir.x) : 0, Mathf.Abs(dir.y) > 0.38 ? (int)Mathf.Sign(dir.y) : 0);
					we.Thrown(self, self.mainBodyChunk.pos + dir * 30f, self.mainBodyChunk.pos, throwndir, 1f, self.evenUpdate);
					we.meleeHitChunk = otherObject.bodyChunks[otherChunk];
					if (we is Spear sp && !(otherObject is Player))
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

		// dont let AI interfere on squiddy
		private void CicadaAI_Update(On.CicadaAI.orig_Update orig, CicadaAI self)
        {
			if (player.TryGet(self.creature, out var ap) && ap.realizedCreature is Player p)
			{
				if (self.cicada.room?.Tiles != null && !self.pathFinder.DoneMappingAccessibility)
					self.pathFinder.accessibilityStepsPerFrame = self.cicada.room.Tiles.Length; // faster, damn it. on entering a new room this needs to complete before it can pathfind
				else self.pathFinder.accessibilityStepsPerFrame = 10;
				self.pathFinder.Update(); // basic movement uses this
				self.tracker.Update(); // creature looker uses this
				self.tracker.ForgetCreature(p.abstractCreature);
			}
            else
            {
				orig(self);
			}
		}

		private void CicadaGraphics_Update(On.CicadaGraphics.orig_Update orig, CicadaGraphics self)
		{
			orig(self);
			if (player.TryGet(self.cicada.abstractCreature, out var ap) && ap.realizedCreature is Player p)
			{
				// move tentacles properly
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
		private void Cicada_GrabbedByPlayer(On.Cicada.orig_GrabbedByPlayer orig, Cicada self)
		{
			if (player.TryGet(self.abstractCreature, out var ap) && ap.realizedCreature is Player p)
			{
				if (self.Consious)
				{
					var oldflypower = self.flyingPower;
					self.flyingPower *= 0.6f;
					self.Act();
					self.flyingPower = oldflypower;
				}
			}
			orig(self);
		}
	}
}