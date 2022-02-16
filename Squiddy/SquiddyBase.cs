using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase;
using System;
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

			IL.CicadaGraphics.ApplyPalette += CicadaGraphics_ApplyPalette;
			On.CicadaGraphics.ApplyPalette += CicadaGraphics_ApplyPalette;
            On.Cicada.ShortCutColor += Cicada_ShortCutColor;
		}

		private void Cicada_ctor(On.Cicada.orig_ctor orig, Cicada self, AbstractCreature abstractCreature, World world, bool gender)
		{
			orig(self, abstractCreature, world, gender);
			if (player.TryGet(self.abstractCreature, out var p))
			{
				// mycologist would be proud
				p.bodyChunks = self.bodyChunks.Reverse().ToArray();
				p.bodyChunkConnections = self.bodyChunkConnections;

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

			self.Destroy();

			var abscada = new AbstractCreature(world, StaticWorld.creatureTemplates[((int)CreatureTemplate.Type.CicadaA)], null, abstractCreature.pos, abstractCreature.ID);
			player[abscada] = self;

			if (world.game.IsStorySession)
			{
				// abscada cannot be added to room entities at this point because players are realized while iterating foreach in entities
				abscada.RealizeInRoom();
			}
			else // arena
			{
				// real cada cannot be instantiated in arena because room isn't ready for non-ai
				world.abstractRooms[0].AddEntity(abscada);
				// player starts in a shortcutvessel with its onw position unset, this teleports squiddy with it
				new SquiddyStick(abscada, abstractCreature);
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

				if(p.abstractCreature.stuckObjects.Count == 0) // new cada this gets called after being added to room each time, including pipes
                {
					Debug.Log("Squiddy: Attached to player");
					new SquiddyStick(self.abstractCreature, p.abstractCreature);
				}

				// underwater cam
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

        private void Cicada_Act(On.Cicada.orig_Act orig, Cicada self)
        {
			if(player.TryGet(self.abstractCreature, out var p))

			{
				var cada = self;
				var room = cada.room;
				var chunks = cada.bodyChunks;
				var nc = chunks.Length;

				if (wantToCharge[self] > 0) wantToCharge[self]--;

				// faster takeoff
				if (cada.waitToFlyCounter <= 15)
					cada.waitToFlyCounter = 15;

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
				if (throwPress || wantToCharge[self] > 0) // dash charge
				{
					if (cada.flying && !cada.Charging && cada.chargeCounter == 0 && cada.stamina > 0.2f)
					{
						cada.Charge(cada.mainBodyChunk.pos + (inputDir == Vector2.zero ? (chunks[0].pos - chunks[1].pos) : inputDir) * 100f);
						wantToCharge[self] = 0;
					}
					else if (throwPress)
					{
						wantToCharge[self] = 5;
					}
				}

				if (cada.chargeCounter > 0) // charge windup or midcharge
				{
					cada.stamina -= 0.008f;
					preventStaminaRegen = true;
					if (cada.chargeCounter < 20)
					{
						if (cada.stamina <= 0.2f || !p.input[0].thrw) // cancel out if unable to complete
						{
							cada.chargeCounter = 0;
						}
					}
					else
					{
						if (cada.stamina <= 0f) // cancel out mid charge if out of stamina (happens in long bouncy charges)
						{
							cada.chargeCounter = 0;
						}
					}

					cada.chargeDir = (cada.chargeDir
												+ 0.15f * inputDir
												+ 0.03f * RWCustom.Custom.DirVec(cada.bodyChunks[1].pos, cada.mainBodyChunk.pos)).normalized;
				}

				// scoooot
				self.AI.swooshToPos = null;
				if (p.input[0].jmp)
				{
					if (cada.room.aimap.getAItile(cada.mainBodyChunk.pos).terrainProximity > 1 && cada.stamina > 0.3f) // cada.flying && 
					{
						self.AI.swooshToPos = cada.mainBodyChunk.pos + inputDir * 60f + new Vector2(0, 4f);
						preventStaminaRegen = true;
						cada.stamina -= cada.stamina * inputDir.magnitude / ((!cada.gender) ? 120f : 190f);
					}
					else // easier takeoff
					{
						if (cada.waitToFlyCounter < 30) cada.waitToFlyCounter = 30;
					}
				}

				// move
				// this seems to have an issue with the pathfinder not keeping up
				self.AI.pathFinder.AbortCurrentGenerationPathFinding();
				if (inputDir != Vector2.zero || cada.Charging)
				{
					self.AI.behavior = CicadaAI.Behavior.GetUnstuck; // helps with sitting behavior
					var dest = cada.mainBodyChunk.pos + inputDir * 30f;
					if (cada.flying) dest.y -= 10f; // nose up goes funny
					self.abstractCreature.abstractAI.SetDestination(cada.room.GetWorldCoordinate(dest));
				}
				else
				{
					self.AI.behavior = CicadaAI.Behavior.Idle;
					if (inputDir == Vector2.zero && inputLastDir != Vector2.zero || UnityEngine.Random.value < 0.004f) // let go, or very rare update
					{
						self.abstractCreature.abstractAI.SetDestination(cada.room.GetWorldCoordinate(cada.mainBodyChunk.pos));
					}
				}

				// Grab update



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
				if (cada.shortcutDelay < 1)
				{
					for (int i = 0; i < nc; i++)
					{
						if (cada.enteringShortCut == null && room.GetTile(chunks[i].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance && room.shortcutData(room.GetTilePosition(chunks[i].pos)).shortCutType != ShortcutData.Type.DeadEnd && room.shortcutData(room.GetTilePosition(chunks[i].pos)).shortCutType != ShortcutData.Type.CreatureHole && room.shortcutData(room.GetTilePosition(chunks[i].pos)).shortCutType != ShortcutData.Type.NPCTransportation)
						{
							IntVector2 intVector = room.ShorcutEntranceHoleDirection(room.GetTilePosition(chunks[i].pos));
							if (p.input[0].x == -intVector.x && p.input[0].y == -intVector.y)
							{
								cada.enteringShortCut = new IntVector2?(room.GetTilePosition(chunks[i].pos));
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
					if (cada.grabbedBy.Count == 0 && cada.stickyCling == null)
					{
						cada.stamina -= 0.014285714f;
					}
				}

				cada.stamina = Mathf.Clamp01(cada.stamina);
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
				//limited to pathfinding
				self.pathFinder.stepsPerFrame = 15;
				self.pathFinder.accessibilityStepsPerFrame = 500; // faster, damn it
				self.pathFinder.Update();
			}
            else
            {
				orig(self);
			}
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
		public static AttachedField<Cicada, int> wantToCharge = new AttachedField<Cicada, int>();
    }
}