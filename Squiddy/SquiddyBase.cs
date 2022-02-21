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
			On.Cicada.Die -= Cicada_Die;
			On.CicadaAI.Update -= CicadaAI_Update;

			On.AbstractCreatureAI.DoIwantToDropThisItemInDen -= AbstractCreatureAI_DoIwantToDropThisItemInDen;

			// bugfix
			On.ShortcutHandler.CreatureEnterFromAbstractRoom -= ShortcutHandler_CreatureEnterFromAbstractRoom;
			IL.ShortcutHelper.Update -= ShortcutHelper_Update;

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
            On.Cicada.Die += Cicada_Die;
			On.CicadaAI.Update += CicadaAI_Update;

			On.AbstractCreatureAI.DoIwantToDropThisItemInDen += AbstractCreatureAI_DoIwantToDropThisItemInDen;

			// silly silly game.
			On.ShortcutHandler.CreatureEnterFromAbstractRoom += ShortcutHandler_CreatureEnterFromAbstractRoom;
            IL.ShortcutHelper.Update += ShortcutHelper_Update;

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
        private void Cicada_Update(On.Cicada.orig_Update orig, Cicada self, bool eu)
        {
            if (player.TryGet(self.abstractCreature, out var p))
            {
				// make so that that the player tags along
				// p has been "destroyed" which means it isn't in a room's update list
				p.room = self.room;
				p.abstractCreature.pos = self.abstractCreature.pos;
				p.abstractCreature.world = self.abstractCreature.world;

				if (!p.slatedForDeletetion)
				{
					p.Destroy();
					Debug.Log("Squiddy: Player removed");
					self.room.abstractRoom.AddEntity(self.abstractCreature); // game just loaded, hadnt been added yet.
					
				}
				if (p.abstractCreature.stuckObjects.Count == 0) // this gets called after being added to room each time, including pipes
                {
					// because removefromroom breaks up sticks
					new SquiddyStick(self.abstractCreature, p.abstractCreature);
					Debug.Log("Squiddy: Attached to player");
				}

				// underwater cam checks these
				p.airInLungs = self.lungs;
				p.dead = self.dead;

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

				// death grasp
				// this is from vanilla but could be reworked into a more flexible system.
				if (p.dangerGrasp == null)
				{
					p.dangerGraspTime = 0;
					foreach( var grasp in self.grabbedBy)
                    {
						if (grasp.grabber is Lizard || grasp.grabber is Vulture || grasp.grabber is BigSpider || grasp.grabber is DropBug)
						{
							p.dangerGrasp = grasp;
						}
					}
				}
				else if (p.dangerGrasp.discontinued)
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
        private void Cicada_Act(On.Cicada.orig_Act orig, Cicada self)
        {
			if(player.TryGet(self.abstractCreature, out var p))

			{
                var room = self.room;
				var chunks = self.bodyChunks;
				var nc = chunks.Length;

				if (p.wantToJump > 0) p.wantToJump--;
				if (p.wantToPickUp > 0) p.wantToPickUp--;

				// faster takeoff
				if (self.waitToFlyCounter <= 15)
                    self.waitToFlyCounter = 15;

				// Input
				p.checkInput(); // partial update (:
								//p.Update(eu); // this one acts hilarious

				var inputDir = p.input[0].analogueDir.magnitude > 0.2f ? p.input[0].analogueDir
					: p.input[0].IntVec.ToVector2().magnitude > 0.2 ? p.input[0].IntVec.ToVector2().normalized
					: Vector2.zero;

				var inputLastDir = p.input[1].analogueDir.magnitude > 0.2f ? p.input[1].analogueDir
					: p.input[1].IntVec.ToVector2().magnitude > 0.2 ? p.input[1].IntVec.ToVector2().normalized
					: Vector2.zero;

				bool preventStaminaRegen = false;
				bool throwPress = (p.input[0].thrw && !p.input[1].thrw);
				if (throwPress || p.wantToJump > 0) // dash charge
				{
					if (self.flying && !self.Charging && self.chargeCounter == 0 && self.stamina > 0.2f)
					{
                        self.Charge(self.mainBodyChunk.pos + (inputDir == Vector2.zero ? (chunks[0].pos - chunks[1].pos) : inputDir) * 100f);
						p.wantToJump = 0;
					}
					else if (throwPress)
					{
						p.wantToJump = 5;
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
												+ 0.03f * RWCustom.Custom.DirVec(self.bodyChunks[1].pos, self.mainBodyChunk.pos)).normalized;
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

				// Grab update

				bool grabPress = (p.input[0].pckp && !p.input[1].pckp);
				if (grabPress || p.wantToPickUp > 0) // pick up
				{
					if (self.AI.preyTracker.MostAttractivePrey?.representedCreature is AbstractCreature ac && ac.realizedCreature != null
						&& Custom.DistLess(self.mainBodyChunk.pos, ac.realizedCreature.mainBodyChunk.pos, 25f) && self.TryToGrabPrey(ac.realizedCreature))
					{
						Debug.Log("Squiddy: grabbed " + ac.realizedCreature);
						p.wantToPickUp = 0;
					}
					else if (grabPress)
					{
						p.wantToPickUp = 5;
					}
				}



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
					for (int i = 0; i < nc; i++)
					{
						if (self.enteringShortCut == null && room.GetTile(chunks[i].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance )
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


				// From player update
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

				if (preventStaminaRegen)
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
            throw new NotImplementedException();
        }

		private void Cicada_Die(On.Cicada.orig_Die orig, Cicada self)
		{
			orig(self);
			if (player.TryGet(self.abstractCreature, out var p))
            {
				p.Die();
            }
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
				self.preyTracker.Update(); // for grabbing prey
			}
            else
            {
				orig(self);
			}
		}

		private bool AbstractCreatureAI_DoIwantToDropThisItemInDen(On.AbstractCreatureAI.orig_DoIwantToDropThisItemInDen orig, AbstractCreatureAI self, AbstractPhysicalObject item)
		{
			if (player.TryGet(self.parent, out var p) && self.parent.InDen)
			{
				bool eaten = false;
				int amount = 0;

				if (item is AbstractCreature ac && ac.creatureTemplate.type == CreatureTemplate.Type.Fly)
                {
					eaten = true;
					amount = 1;
				}

                if (eaten) { p.AddFood(amount); }

				return orig(self, item) && eaten;
			}
			else
			{
				return orig(self, item);
			}
		}

		// bugfix the gmae
		// 2 connected creatures leaving den = both added twice to the room.
		private void ShortcutHandler_CreatureEnterFromAbstractRoom(On.ShortcutHandler.orig_CreatureEnterFromAbstractRoom orig, ShortcutHandler self, Creature creature, AbstractRoom enterRoom, int enterNode)
		{
			if (creature is Player p && IsMe(p)) return; // carried by squiddy
			orig(self, creature, enterRoom, enterNode);
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
							orig(self, sLeaser, rCam, palette);
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