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
    public partial class SquiddyBase
    {
		// everyone will remember that
		public AttachedField<AbstractRoom, int[]> consumedInsects = new AttachedField<AbstractRoom, int[]>();

		// things were getting out of hand
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
				if(grasps.FirstOrDefault(g => g != null && (g.grabbed is Fly f && !f.dead))?.grabbed is Fly fly && UnityEngine.Random.value < 0.00625f) fly.Die();
				Creature.Grasp edible = grasps.FirstOrDefault(g => g != null && (
					(g.grabbed is IPlayerEdible ipe && ipe.Edible && ipe.FoodPoints == 0) // Edible with no food points (otherwise must carry to den)
				  || g.grabbed is InsectHolder
				  //|| g.grabbed is OracleSwarmer
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
								if (edible.discontinued)
                                {
									edible.Release();
								}
							}
						}
						else // no can eat
						{
							if (p.eatCounter < 20 && room.game.cameras[0].hud != null)
							{
								room.game.cameras[0].hud.foodMeter.RefuseFood();
							}
							edible = null;
						}
					}
				}

				if (holdingGrab)
				{
					if ((edible == null) && ((p.objectInStomach == null && grasps.Any(g => g != null && p.CanBeSwallowed((g.grabbed)))) || p.objectInStomach != null))
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

							if (grabbed is PuffBall) self.Die();
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

			if (p.FoodInStomach < p.MaxFoodInStomach)
			{
				foreach (var grasp in grasps) if (grasp != null)// && squiddyEatsInDen(grasp.grabbed))
					{
						if ((grasp.grabbed is IPlayerEdible ipe && ipe.FoodPoints > 0)
							|| (grasp.grabbed is Creature c && self.abstractCreature.abstractAI.RealAI.StaticRelationship(c.abstractCreature).type == CreatureTemplate.Relationship.Type.Eats))
						{
							densNeeded = true;
						}
						if (grasp.grabbed is SmallNeedleWorm snm && !snm.hasScreamed && self.enteringShortCut != null && room.shortcutData(self.enteringShortCut.Value).shortCutType == ShortcutData.Type.CreatureHole)
						{
							snm.Scream();
						}
					}
			}


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
							room.socialEventRecognizer.CreaturePutItemOnGround(grasps[i].grabbed, p);
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
					int freehands = 0;
					for (int i = 0; i < grasps.Length; i++)
					{
						if (grasps[i] == null)
						{
							freehands++;
						}
					}

					if (freehands == 0)// && !(p.pickUpCandidate is InsectHolder)) // let go of tiny bugs if trying to pickup something
					{
						for (int i = 0; i < grasps.Length; i++)
						{
							if (grasps[i] != null && grasps[i].grabbed is InsectHolder)
							{
								self.ReleaseGrasp(i);
								break;
							}
						}
					}

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

		private PhysicalObject PickupCandidate(Cicada self, Player p)
		{
			// initial physicalobject candidate
			var candidate = p.PickupCandidate(8f);

			// insect contender
			if (self.room?.insectCoordinator != null)
			{

				CosmeticInsect closestInsect = null;
				var ownpos = self.firstChunk.pos;
				float maxdist = candidate == null ? 40f : (ownpos - candidate.firstChunk.pos).magnitude;
				foreach (var insect in self.room.insectCoordinator.allInsects)
				{
					if (!insect.slatedForDeletetion && insect.alive && insect.inGround < 1f)
					{
						var dist = (ownpos - insect.pos).magnitude;
						if (dist < maxdist)
						{
							foreach (var g in self.grasps)
							{
								if (g != null && g.grabbed is InsectHolder hldr && hldr.insect == insect) goto skipped; // skip already grabbed, can't use a continue here
							}

							closestInsect = insect;
							maxdist = dist;
						skipped:;
						}
					}
				}

				if (closestInsect != null)
				{
					if (p.pickUpCandidate is InsectHolder hldr && hldr.insect == closestInsect) // already tracked!
					{
						candidate = hldr;
					}
					else
					{
						candidate = new InsectHolder(closestInsect, p, self.room); // new holder for tracking
						self.room.AddObject(candidate);
					}
				}
			}

			return candidate;
		}

		// player picks what squiddy wants
		private bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
		{
			if (IsMe(self))
			{
				if (obj is InsectHolder) return false;
				if (cicada.TryGet(self.abstractCreature, out var ac) && ac.realizedCreature is Cicada cada)
				{
					if (obj == cada) return false;
					foreach (var g in cada.grasps) if (g != null && g.grabbed == obj) return false;
					if (obj is Creature c && (
						cada.AI.StaticRelationship(c.abstractCreature).type == CreatureTemplate.Relationship.Type.Eats
						|| c.abstractCreature.creatureTemplate.smallCreature
						)) return true;
				}
			}
			return orig(self, obj);
		}

		// can always swallow a neuron
		private bool Player_CanBeSwallowed(On.Player.orig_CanBeSwallowed orig, Player self, PhysicalObject testObj)
		{
			if (IsMe(self))
			{
				return orig(self, testObj) || testObj is OracleSwarmer; // can eat this mans at any time
			}
			return orig(self, testObj);
		}

		// player eats what squiddy eats
		private void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
		{
			if (IsMe(self))
			{
				if (!(edible is Creature) && edible.FoodPoints > 0)
				{
					// not creatures are quarterpoints
					for (int i = 0; i < edible.FoodPoints; i++)
					{
						self.AddQuarterFood();
					}
					return;
				}
				if (edible is InsectHolder ih)
				{
					self.AddQuarterFood();
					var insects = consumedInsects[self.room.abstractRoom] ?? (consumedInsects[self.room.abstractRoom] = new int[Enum.GetValues(typeof(CosmeticInsect.Type)).Length]);
					if ((int)ih.insect.type < insects.Length)
					{
						insects[(int)ih.insect.type]++;
					}
					//return; // player eats it for zero points
				}
			}
			orig(self, edible);
		}

		// autoeating code weird
		private int Player_FoodInRoom_Room_bool(On.Player.orig_FoodInRoom_Room_bool orig, Player self, Room checkRoom, bool eatAndDestroy)
		{
			int num = self.FoodInStomach;
			int extra = 0;

			// now now here non-creature edibles will count for full food. How the heck do I prevent that ?
			// hunter took the easy route and just disabled auto-eating
			// attempted fix by disabling autoeat of stuff that'd be quarters using Player_ObjectCountsAsFood

			// pre vanilla eating, eat creatures squiddy can eat (smallCreature only?)
			if (IsMe(self) && cicada.TryGet(self.abstractCreature, out var abscada) && abscada.realizedCreature is Cicada cada)
			{
				for (int l = checkRoom.abstractRoom.entities.Count - 1; l >= 0 && num < self.slugcatStats.foodToHibernate; l--)
				{
					var item = checkRoom.abstractRoom.entities[l];
					if (item is AbstractPhysicalObject apo && !(self.ObjectCountsAsFood(apo.realizedObject)) && apo is AbstractCreature ac && ac.creatureTemplate.smallCreature && cada.AI.StaticRelationship(ac).type == CreatureTemplate.Relationship.Type.Eats)
					{
						num += 1;
						extra += 1;
						if (eatAndDestroy)
						{
							self.AddFood(1); // we add here instead of doing funny maths because player would still eat things to fill its own counter
							var realizedObject = ac.realizedObject;
							if (self.SessionRecord != null)
							{
								self.SessionRecord.AddEat(realizedObject);
							}
							ac.realizedObject.Destroy();
							checkRoom.RemoveObject(realizedObject);
							checkRoom.abstractRoom.RemoveEntity(apo);
						}
					}
				}
			}
			// if eaten, will be already accounted for in the original method
			return (eatAndDestroy ? 0 : extra) + orig(self, checkRoom, eatAndDestroy);
		}

		// there, no autoeating quater pip stuff because foodinroom code bad
		// obs the object here HAS to be an iplayeredible or player code will nullref
		private bool Player_ObjectCountsAsFood(On.Player.orig_ObjectCountsAsFood orig, Player self, PhysicalObject obj)
		{
			if (IsMe(self) && cicada.TryGet(self.abstractCreature, out var abscada) && abscada.realizedCreature is Cicada cada)
			{
				if (!(obj is Creature)) return false; // non-creatures are quarterpip and don't work with autoeat
			}
			return orig(self, obj);
		}

		// eat the thing you carried to a den
		private bool AbstractCreatureAI_DoIwantToDropThisItemInDen(On.AbstractCreatureAI.orig_DoIwantToDropThisItemInDen orig, AbstractCreatureAI self, AbstractPhysicalObject item)
		{
			if (player.TryGet(self.parent, out var ap) && ap.realizedCreature is Player p && self.parent.InDen)
			{
				bool eaten = false;
				if(p.FoodInStomach < p.MaxFoodInStomach)
                {
					// kind of wish I had a concise rule for both this and the grab one
					if (item.realizedObject is IPlayerEdible ipe && ipe.FoodPoints > 0)
					{
						if (p.SessionRecord != null)
						{
							p.SessionRecord.AddEat(item.realizedObject);
						}
						p.ObjectEaten(ipe);
						if (ipe is OracleSwarmer os)
						{
							if (self.world.game.session is StoryGameSession sgs)
							{
								sgs.saveState.theGlow = true;
							}
							p.glowing = true;
							if (os is SLOracleSwarmer slos && slos.oracle != null)
							{
								slos.oracle.GlowerEaten();
							}
						}
						eaten = true;
					}
					else if (item is AbstractCreature ac && self.parent.abstractAI.RealAI.StaticRelationship(ac).type == CreatureTemplate.Relationship.Type.Eats)
					{
						if (ac.creatureTemplate.smallCreature)
						{
							p.AddFood(1);
                        }
                        else
                        {
							p.AddFood(ac.state.meatLeft);
							ac.state.meatLeft = 0;
						}

						if (p.SessionRecord != null)
						{
							p.SessionRecord.AddEat(item.realizedObject);
						}
						eaten = true;
					}
				}

                if (eaten)
                {
					Debug.Log("Squiddy: eaten in den " + item);
				}

				return orig(self, item) && eaten;
			}
			else
			{
				return orig(self, item);
			}
		}

		// remove respawned insetcs on room reenter
		private void InsectCoordinator_NowViewed(On.InsectCoordinator.orig_NowViewed orig, InsectCoordinator self)
		{
			orig(self);
			var consumed = consumedInsects[self.room.abstractRoom];
			if (consumed != null)
			{
				for (int i = 0; i < consumed.Length; i++)
				{
					var t = (CosmeticInsect.Type)i;
					var amount = consumed[i];
					for (int j = 0; j < amount; j++)
					{
						foreach (var item in self.allInsects)
						{
							if (item.type == t && !item.slatedForDeletetion)
							{
								item.Destroy();
								break;
							}
						}
					}
				}
			}
		}
	}
}