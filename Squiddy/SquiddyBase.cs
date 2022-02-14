using SlugBase;
using System;
using System.Linq;
using UnityEngine;

namespace Squiddy
{
    public class SquiddyBase : SlugBaseCharacter
	{

		public SquiddyBase() : base("hensquiddy", FormatVersion.V1, 0, true) {
           
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

		}

		protected override void Enable()
		{
			On.Player.ctor += Player_ctor;
            On.Cicada.Update += Cicada_Update;
            On.CicadaAI.Update += CicadaAI_Update;
		}

        private void CicadaAI_Update(On.CicadaAI.orig_Update orig, CicadaAI self)
        {
			if (player.TryGet(self.cicada, out var p))
			{
				//limited update
				self.pathFinder.Update();

				// fixes
				if(self.cicada.waitToFlyCounter <= 12)
					self.cicada.waitToFlyCounter = 12;

				// Input
				var inputDir = p.input[0].analogueDir.magnitude > 0.2f ? p.input[0].analogueDir 
					: p.input[0].IntVec.ToVector2().magnitude > 0.2 ? p.input[0].IntVec.ToVector2().normalized 
					: Vector2.zero;

				bool preventStaminaRegen = false;
				self.swooshToPos = null;

				if (self.cicada.chargeCounter > 0) preventStaminaRegen = true;

				if (self.cicada.flying && p.input[0].thrw) // dash charge
                {
                    if (!self.cicada.Charging && inputDir != Vector2.zero)
                    {
						if(self.cicada.chargeCounter == 0)
                        {
							self.cicada.Charge(self.cicada.mainBodyChunk.pos + inputDir * 100f);
                        }

						self.cicada.chargeDir = (self.cicada.chargeDir 
												+ 0.2f * inputDir 
												+ 0.1f * RWCustom.Custom.DirVec(self.cicada.bodyChunks[1].pos, self.cicada.mainBodyChunk.pos)).normalized;
						preventStaminaRegen = true;
						self.cicada.stamina -= 0.3f / 20f;

						if(self.cicada.stamina <= 0f) // cancel out
                        {
							self.cicada.chargeCounter = 0;
                        }
					}
				}
                else
                {
					if(self.cicada.chargeCounter < 20)
                    {
						self.cicada.chargeCounter = 0;
                    }
                }

				if(inputDir != Vector2.zero)
                {
					if (p.input[0].jmp)
					{
						if (self.cicada.flying && self.cicada.room.aimap.getAItile(self.cicada.mainBodyChunk.pos).terrainProximity > 1)
						{
							self.swooshToPos = self.cicada.mainBodyChunk.pos + inputDir * 20f;
							preventStaminaRegen = true;
							self.cicada.stamina -= 1f / ((!self.cicada.gender) ? 120f : 190f);
							self.cicada.stamina = Mathf.Clamp(self.cicada.stamina, 0f, 1f);
						}
						else
						{
							if (self.cicada.waitToFlyCounter < 30) self.cicada.waitToFlyCounter = 30;
						}
					}
                    else
                    {
						var dest = self.cicada.mainBodyChunk.pos + inputDir * 20f;
						if (self.cicada.flying) dest.y -= 10f;
						self.creature.abstractAI.SetDestination(self.cicada.room.GetWorldCoordinate(dest));
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
            if (player.TryGet(self, out var p))
            {
				// make so that that the player tags along
				// p has been "destroyed" which means it isn't in a room's update list
				p.room = self.room;
				p.abstractCreature.pos = self.abstractCreature.pos;
				p.abstractCreature.world = self.abstractCreature.world;

				if(p.abstractCreature.stuckObjects.Count == 0)
                {
					new SquiddyStick(self.abstractCreature, p.abstractCreature);
				}

				p.checkInput(); // partial update (:
				//p.Update(eu); // this one acts hilarious
			}

			orig(self, eu);

			// orig calls AI update
		}

		internal class SquiddyStick : AbstractPhysicalObject.AbstractObjectStick
		{
			public SquiddyStick(AbstractPhysicalObject A, AbstractPhysicalObject B) : base(A, B)
			{
			}
		}

		private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
		{
			orig(self, abstractCreature, world);
			if (!IsMe(self)) return;

			//self.RemoveFromRoom();
			self.Destroy();

			var abscada = new AbstractCreature(world, StaticWorld.creatureTemplates[((int)CreatureTemplate.Type.CicadaA)], null, abstractCreature.pos, abstractCreature.ID);
			abscada.RealizeInRoom();
			var cada = cicada[self] = abscada.realizedCreature as Cicada;
			player[cada] = self;

			// Room removal calls loseallstuck which washes this out
			//self.grabbedBy.Add(new Creature.Grasp(cada, self, -1, 0, Creature.Grasp.Shareability.CanNotShare, 1, false));
			//new SquiddyStick(abscada, abstractCreature);
			//new SquiddyStick(abstractCreature, abscada);
			//cada.sticksRespawned = true;
			//self.sticksRespawned = true;

			self.bodyChunks = cada.bodyChunks.Reverse().ToArray();
			self.bodyChunkConnections = cada.bodyChunkConnections;

			self.touchedNoInputCounter = 999;
			self.sleepCounter = 0;
		}


		public static AttachedField<Player, Cicada> cicada = new AttachedField<Player, Cicada>();
		public static AttachedField<Cicada, Player> player = new AttachedField<Cicada, Player>();
	}
}