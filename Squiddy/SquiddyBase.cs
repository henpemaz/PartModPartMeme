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
			On.Cicada.Update -= Cicada_Update;
			On.CicadaAI.Update -= CicadaAI_Update;
		}

		protected override void Enable()
		{
            On.Cicada.Update += Cicada_Update;
            On.CicadaAI.Update += CicadaAI_Update;
		}

        private void CicadaAI_Update(On.CicadaAI.orig_Update orig, CicadaAI self)
        {
			if (player.TryGet(self.creature, out var p))
			{
				//limited update
				self.pathFinder.Update();
				self.pathFinder.stepsPerFrame = 30;

				if (wantToCharge[self] > 0) wantToCharge[self]--;

				// faster takeoff
				if (self.cicada.waitToFlyCounter <= 15)
					self.cicada.waitToFlyCounter = 15;

				// Input
				var inputDir = p.input[0].analogueDir.magnitude > 0.2f ? p.input[0].analogueDir
					: p.input[0].IntVec.ToVector2().magnitude > 0.2 ? p.input[0].IntVec.ToVector2().normalized
					: Vector2.zero;

				var inputLastDir = p.input[1].analogueDir.magnitude > 0.2f ? p.input[1].analogueDir 
					: p.input[1].IntVec.ToVector2().magnitude > 0.2 ? p.input[1].IntVec.ToVector2().normalized 
					: Vector2.zero;

				bool preventStaminaRegen = false;

				if (p.input[0].thrw || wantToCharge[self] > 0) // dash charge
                {
                    if (self.cicada.flying && !self.cicada.Charging && inputDir != Vector2.zero && !p.input[1].thrw)
                    {
						if(self.cicada.chargeCounter == 0 && self.cicada.stamina > 0.2f)
                        {
							self.cicada.Charge(self.cicada.mainBodyChunk.pos + inputDir * 100f);
                        }
					}
                    else
                    {
						wantToCharge[self] = 5;
					}
				}
                else
                {
					if(self.cicada.chargeCounter < 20) // cancel incomplete charge
                    {
						self.cicada.chargeCounter = 0;
                    }
                }

				if (self.cicada.chargeCounter > 0) // charge windup or midcharge
				{
					self.cicada.stamina -= 0.008f;
					preventStaminaRegen = true;
					if (self.cicada.chargeCounter < 20)
                    {
						if (self.cicada.stamina <= 0.2f) // cancel out if unable to complete
						{
							self.cicada.chargeCounter = 0;
							self.cicada.stamina += 0.1f;
						}
					}
                    else
                    {
						if (self.cicada.stamina <= 0f) // cancel out mid charge if out of stamina (happens in long bouncy charges)
						{
							self.cicada.chargeCounter = 0;
						}
					}

					self.cicada.chargeDir = (self.cicada.chargeDir
												+ 0.15f * inputDir
												+ 0.03f * RWCustom.Custom.DirVec(self.cicada.bodyChunks[1].pos, self.cicada.mainBodyChunk.pos)).normalized;
				}

				self.swooshToPos = null;
				if (p.input[0].jmp) // scoot
				{
					if (self.cicada.room.aimap.getAItile(self.cicada.mainBodyChunk.pos).terrainProximity > 1) // self.cicada.flying && 
					{
						self.swooshToPos = self.cicada.mainBodyChunk.pos + inputDir * 60f + new Vector2(0,4f);
						preventStaminaRegen = true;
						self.cicada.stamina = Mathf.Clamp01(self.cicada.stamina - self.cicada.stamina * inputDir.magnitude / ((!self.cicada.gender) ? 120f : 190f));
					}
					else // easier takeoff
					{
						if (self.cicada.waitToFlyCounter < 30) self.cicada.waitToFlyCounter = 30;
					}
				}

				// move
				if(inputDir != Vector2.zero || self.cicada.Charging)
                {
					var dest = self.cicada.mainBodyChunk.pos + inputDir * 30f;
					if (self.cicada.flying) dest.y -= 10f;
					self.creature.abstractAI.SetDestination(self.cicada.room.GetWorldCoordinate(dest));
					self.behavior = CicadaAI.Behavior.GetUnstuck;
				}
                else
                {
					self.behavior = CicadaAI.Behavior.Idle;
					if(inputDir == Vector2.zero && inputLastDir != Vector2.zero || UnityEngine.Random.value < 0.004f) // let go, or very rare update
                    {
						self.creature.abstractAI.SetDestination(self.cicada.room.GetWorldCoordinate(self.cicada.mainBodyChunk.pos));
					}
				}

				// player direct into holes equivalent
				var room = self.cicada.room;
				var nc = self.cicada.bodyChunks.Length;
				var chunks = self.cicada.bodyChunks;

				if((p.input[0].x == 0 || p.input[0].y == 0) && p.input[0].x != p.input[0].y) // a straight direction
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
				if (self.cicada.shortcutDelay < 1)
				{
					for (int i = 0; i < nc; i++)
					{
						if (self.cicada.enteringShortCut == null && room.GetTile(chunks[i].pos).Terrain == Room.Tile.TerrainType.ShortcutEntrance && room.shortcutData(room.GetTilePosition(chunks[i].pos)).shortCutType != ShortcutData.Type.DeadEnd && room.shortcutData(room.GetTilePosition(chunks[i].pos)).shortCutType != ShortcutData.Type.CreatureHole && room.shortcutData(room.GetTilePosition(chunks[i].pos)).shortCutType != ShortcutData.Type.NPCTransportation)
						{
							IntVector2 intVector = room.ShorcutEntranceHoleDirection(room.GetTilePosition(chunks[i].pos));
							if (p.input[0].x == -intVector.x && p.input[0].y == -intVector.y)
							{
								self.cicada.enteringShortCut = new IntVector2?(room.GetTilePosition(chunks[i].pos));
							}
						}
					}
				}

				if (preventStaminaRegen)
                {
					if (self.cicada.grabbedBy.Count == 0 && self.cicada.stickyCling == null)
					{
						self.cicada.stamina = Mathf.Max(self.cicada.stamina - 0.014285714f, 0f);
					}
				}
			}
            else
            {
				orig(self);
			}
		}

        private void Cicada_Update(On.Cicada.orig_Update orig, Cicada self, bool eu)
        {
            if (player.TryGet(self.abstractCreature, out var p))
            {
				// make so that that the player tags along
				// p has been "destroyed" which means it isn't in a room's update list
				p.room = self.room;
				p.abstractCreature.pos = self.abstractCreature.pos;
				p.abstractCreature.world = self.abstractCreature.world;

				if(p.abstractCreature.stuckObjects.Count == 0) // new cada (maybe this gets called after being added to room each time, including pipes?)
                {
					new SquiddyStick(self.abstractCreature, p.abstractCreature);
					p.bodyChunks = self.bodyChunks.Reverse().ToArray();
					p.bodyChunkConnections = self.bodyChunkConnections;

					p.touchedNoInputCounter = 999;
				}

				p.checkInput(); // partial update (:
				//p.Update(eu); // this one acts hilarious
			}

			orig(self, eu);
			// orig calls AI update into Act
		}

		internal class SquiddyStick : AbstractPhysicalObject.AbstractObjectStick
		{
			public SquiddyStick(AbstractPhysicalObject A, AbstractPhysicalObject B) : base(A, B) { }
		}

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

		public static AttachedField<AbstractCreature, Player> player = new AttachedField<AbstractCreature, Player>();
		public static AttachedField<CicadaAI, int> wantToCharge = new AttachedField<CicadaAI, int>();
    }
}