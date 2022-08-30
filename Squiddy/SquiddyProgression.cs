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

		public override CustomSaveState CreateNewSave(PlayerProgression progression)
		{
			return new SquiddySaveState(progression, this);
		}

		class SquiddySaveState : CustomSaveState
		{
			public bool hitSurvivor = false; // survivor karma bonus applied
			public bool shownSurvivor = false; // bonus karma animation on speepscreen displayed

			public SquiddySaveState(PlayerProgression progression, SlugBaseCharacter character) : base(progression, character) { }

			public override void LoadPermanent(Dictionary<string, string> data)
			{
				hitSurvivor = data.TryGetValue("hitSurvivor", out string temp) ? bool.Parse(temp) : false;
				shownSurvivor = data.TryGetValue("shownSurvivor", out string temp2) ? bool.Parse(temp2) : false;
			}

			public override void SavePermanent(Dictionary<string, string> data, bool asDeath, bool asQuit)
			{
				data["hitSurvivor"] = hitSurvivor.ToString();
				data["shownSurvivor"] = shownSurvivor.ToString();
			}
		}

		private void WinState_CycleCompleted(On.WinState.orig_CycleCompleted orig, WinState self, RainWorldGame game)
		{
			orig(self, game);
			if (IsMe(game))
			{
				if (self.GetTracker(WinState.EndgameID.Survivor, false) is WinState.IntegerTracker survivor && survivor.GoalFullfilled)
				{
					Debug.Log("Squiddy: Survivor fullfiled");

					if (game.TryGetSave<SquiddySaveState>(out var save))
					{
						if (!save.hitSurvivor)
						{
							Debug.Log("Squiddy: Karma increase on hit survivor!!");
							save.IncreaseKarmaCapOneStep();
							save.hitSurvivor = true;
						}
					}
				}
			}
		}

		private void SaveState_IncreaseKarmaCapOneStep(On.SaveState.orig_IncreaseKarmaCapOneStep orig, SaveState self)
		{
			if (IsMe(self))
			{
				var dps = self.deathPersistentSaveData;
				int precap = dps.karmaCap;
				orig(self);
				int postcap = dps.karmaCap;
				if (precap == (postcap - 1) && postcap < 4) // only increased one step (under k5 upgrade)
				{
					dps.karmaCap++;
					if (dps.karma == postcap) dps.karma = dps.karmaCap;
				}
			}
			else
			{
				orig(self);
			}
		}

		private void KarmaLadder_ctor(On.Menu.KarmaLadder.orig_ctor orig, Menu.KarmaLadder self, Menu.Menu menu, Menu.MenuObject owner, Vector2 pos, HUD.HUD hud, IntVector2 displayKarma, bool reinforced)
		{
			orig(self, menu, owner, pos, hud, displayKarma, reinforced);

			if (menu is Menu.KarmaLadderScreen kls && IsMe(kls.saveState) && kls.saveState is SquiddySaveState sss)
			{
				if (sss.hitSurvivor && !sss.shownSurvivor && sss.deathPersistentSaveData.karmaCap < 5)
				{
					int startpoint = kls.karma.y - 2;
					kls.preGhostEncounterKarmaCap = startpoint;
					self.displayKarma.x = startpoint;
					self.moveToKarma = startpoint;
					self.scroll = startpoint;
					self.lastScroll = startpoint;

					for (int num6 = kls.preGhostEncounterKarmaCap + 1; num6 < self.karmaSymbols.Count; num6++)
					{
						self.karmaSymbols[num6].energy = 0f;
						self.karmaSymbols[num6].flickerCounter = int.MaxValue - 1000; // silence, but please no overflow lmao
					}
				}
			}
		}

		private void SleepAndDeathScreen_FoodCountDownDone(On.Menu.SleepAndDeathScreen.orig_FoodCountDownDone orig, Menu.SleepAndDeathScreen self)
		{
			orig(self);
			if (IsMe(self.saveState) && self.saveState is SquiddySaveState sss)
			{
				if (sss.hitSurvivor && !sss.shownSurvivor && sss.deathPersistentSaveData.karmaCap < 5)
				{

					self.karmaLadder.increaseKarmaCapMode = true;
					self.preGhostEncounterKarmaCap = self.karma.y - 2;
					self.karmaLadder.displayKarma.x = self.karma.y - 2;

					for (int num6 = self.preGhostEncounterKarmaCap + 1; num6 < self.karmaLadder.karmaSymbols.Count; num6++)
					{
						self.karmaLadder.karmaSymbols[num6].flickerCounter = UnityEngine.Random.Range(12, UnityEngine.Random.Range(40, 80));
					}

					sss.shownSurvivor = true;
					Debug.Log("Squiddy: Karma Ladder DANCE!");
					self.manager.rainWorld.progression.SaveDeathPersistentDataOfCurrentState(false, false);
				}
			}
		}

		private void PebblesConversation_AddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
		{
			if (IsMe(self.owner.oracle.room.game) && self.id == Conversation.ID.Pebbles_White)
			{
				//orig(self);
				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh you're still alive."), 10));
				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("That's a shame, my circuits must be miscalibrated."), 20));

				self.events.Add(new Conversation.WaitEvent(self, 40));

				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Tsc. How did you get in in the first place, stupid creature. You aren't worth my time, little bug."), 10));

				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Get out of my can before I turn you into a blue goop."), 0));
			}
			else
			{
				orig(self);
			}
		}

		private void SSOracleMeetWhite_Update(On.SSOracleBehavior.SSOracleMeetWhite.orig_Update orig, SSOracleBehavior.SSOracleMeetWhite self)
		{
			if (IsMe(self.oracle.room.game))
			{
				if (self.action == SSOracleBehavior.Action.MeetWhite_Shocked && self.inActionCounter > 40)
				{
					self.owner.NewAction(SSOracleBehavior.Action.General_GiveMark);
					self.owner.afterGiveMarkAction = SSOracleBehavior.Action.General_MarkTalk;
				}
			}
			orig(self);
		}

		private void ThrowOutBehavior_Update(On.SSOracleBehavior.ThrowOutBehavior.orig_Update orig, SSOracleBehavior.ThrowOutBehavior self)
		{
			if (cicada.TryGet(self.player?.abstractCreature, out var ac) && ac.realizedCreature is Cicada cada)
			{
				switch (self.action)
				{
					case SSOracleBehavior.Action.ThrowOut_ThrowOut:
						if (self.player.room == self.oracle.room)
						{
							self.owner.throwOutCounter++;
						}
						self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
						self.telekinThrowOut = (self.inActionCounter > 20);
						if (self.owner.throwOutCounter == 160)
						{
							self.dialogBox.Interrupt(self.Translate("I'm serious. I'll mess you up."), 0);
						}
						else if (self.owner.throwOutCounter == 280)
						{
							self.dialogBox.Interrupt(self.Translate("One blue goop coming right up."), 0);
						}
						else if (self.owner.throwOutCounter > 320)
						{
							self.owner.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
						}
						if (self.owner.playerOutOfRoomCounter > 100 && self.owner.throwOutCounter > 400)
						{
							self.owner.NewAction(SSOracleBehavior.Action.General_Idle);
							self.owner.getToWorking = 1f;
						}
						break;

					case SSOracleBehavior.Action.ThrowOut_SecondThrowOut:
						if (self.player.room == self.oracle.room)
						{
							self.owner.throwOutCounter++;
						}
						self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
						self.telekinThrowOut = (self.inActionCounter > 20);
						if (self.owner.throwOutCounter == 50)
						{
							self.dialogBox.Interrupt(self.Translate("You again? Not under my watch."), 0);
						}
						else if (self.owner.throwOutCounter == 120)
						{
							self.dialogBox.Interrupt(self.Translate("One blue goop coming right up."), 0);
						}
						else if (self.owner.throwOutCounter > 160)
						{
							self.owner.NewAction(SSOracleBehavior.Action.ThrowOut_KillOnSight);
						}
						if (self.owner.playerOutOfRoomCounter > 100 && self.owner.throwOutCounter > 400)
						{
							self.owner.NewAction(SSOracleBehavior.Action.General_Idle);
							self.owner.getToWorking = 1f;
						}
						break;

					case SSOracleBehavior.Action.ThrowOut_KillOnSight:
						if ((!self.player.dead || self.owner.killFac > 0.5f) && self.player.room == self.oracle.room)
						{
							self.owner.killFac += 0.025f;
							if (self.owner.killFac >= 1f)
							{
								self.player.mainBodyChunk.vel += Custom.RNV() * 12f;
								for (int i = 0; i < 20; i++)
								{
									self.oracle.room.AddObject(new Spark(self.player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
								}

								self.player.Die();
								cada.Destroy();

								var bluegoop = new AbstractCreature(self.oracle.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.DaddyLongLegs), null, cada.coord, new EntityID());
								self.oracle.room.abstractRoom.AddEntity(bluegoop);
								bluegoop.RealizeInRoom();

								self.owner.killFac = 0f;
							}
						}
						else
						{
							self.owner.killFac *= 0.8f;
							self.owner.getToWorking = 1f;
							self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
							self.owner.NewAction(SSOracleBehavior.Action.General_Idle);
						}
						break;
					default:
						orig(self);
						break;
				}
			}
			else
			{
				orig(self);
			}
		}

		private void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
		{
			if (cicada.TryGet(self.player?.abstractCreature, out _) && self.throwOutCounter > 0 && self.player.dead)
			{
				return;
			}
			orig(self);
		}
	}
}