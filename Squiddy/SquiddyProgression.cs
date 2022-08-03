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
				self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh you're still alive. That's a shame, my circuits must be miscalibrated."), 0));
			}
			else
			{
				orig(self);
			}
		}

	}
}