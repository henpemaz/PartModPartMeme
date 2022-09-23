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
                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("That's a shame, my circuits must be miscalibrated."), 10));

                self.events.Add(new Conversation.WaitEvent(self, 40));

                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Tsc. How did you get in in the first place, stupid creature. " + (
                    (self.owner.oracle.room.game.IsStorySession && self.owner.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.memoryArraysFrolicked) ?
                    "My memory arrays are a maze not intended for the kinds of you." :
                    "The entrance you took is far above the clouds, you shouldn't be able to reach it.")), 10));

                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I suppose a bug will get through any maze if given enough time, but why did it have to be me?"), 10));

                self.events.Add(new Conversation.WaitEvent(self, 40));

                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You aren't worth my time, little bug. Get out of my can before I turn you into a blue goop."), 0));
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
					self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
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
						else if (self.owner.throwOutCounter > 340)
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
						else if (self.owner.throwOutCounter > 180)
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
			if (cicada.TryGet(self.player?.abstractCreature, out _) && self.player.dead)
			{
				return;
			}
			orig(self);
		}

		private void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self, SSOracleBehavior.Action nextAction)
		{
			if (cicada.TryGet(self.player?.abstractCreature, out var ac) && (nextAction == SSOracleBehavior.Action.General_GiveMark || nextAction == SSOracleBehavior.Action.ThrowOut_KillOnSight))
			{
				self.oracle.room.AddObject(new SquiddyCrosshair(self, ac.realizedCreature));
			}
			orig(self, nextAction);
		}

		private void SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
		{
			if (cicada.TryGet(self.player?.abstractCreature, out var ac) && ac.realizedCreature is Cicada cada)
			{
				var og = self.player.grasps;
				self.player.grasps = cada.grasps;
				orig(self, eu);
				self.player.grasps = og;
			}
			else
			{
				orig(self, eu);
			}
		}

		private class SquiddyCrosshair : CosmeticSprite
        {
            private readonly int circle = 0;
            private readonly int la = 1;
            private readonly int lb = 2;

			private float currentFactor;
            private float crossAlpha;
            float red;
            private float dieOut;
            private readonly SSOracleBehavior sob;
            private readonly Creature squiddy;

            public SquiddyCrosshair(SSOracleBehavior sob,Creature squiddy)
            {
                this.sob = sob;
                this.squiddy = squiddy;
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
				if (slatedForDeletetion) return;
				if ((sob.action == SSOracleBehavior.Action.General_GiveMark && sob.inActionCounter > 340) || (squiddy.dead))
				{ 
					dieOut+= squiddy.dead ? 0.05f : 0.025f;
				}

				if(dieOut > 1f) { Destroy(); return; }
			
				var towards = squiddy.firstChunk.pos - pos;

				if (sob.action == SSOracleBehavior.Action.General_GiveMark)
				{
					currentFactor = ((float)sob.inActionCounter) / 300;
				}
				if (sob.action == SSOracleBehavior.Action.ThrowOut_KillOnSight || squiddy.dead)
				{
					currentFactor = squiddy.dead ? 1f - dieOut : Mathf.Pow(sob.killFac, 0.3f);
				}

				crossAlpha = Mathf.Max(0, Mathf.InverseLerp(0.33f, 0.66f, currentFactor) - dieOut);

				if(currentFactor < 1f && dieOut == 0f)
                {
					var idealDist = Mathf.Lerp(400, 5, Mathf.Pow(currentFactor, 0.5f));
					if (towards.magnitude > idealDist)
					{
						vel += towards.normalized * (towards.magnitude - idealDist) / 4f;
					}

					if(currentFactor > 0.5f)
                    {
						red = Mathf.Pow(UnityEngine.Random.value, 10f - 5f * currentFactor);
					}
				}
                else
                {
					red = 1f;
                }
				this.vel *= 0.8f;
			}

			public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
			{
				sLeaser.sprites = new FSprite[3];
				sLeaser.sprites[circle] = new FSprite("Futile_White", true) { shader = rCam.game.rainWorld.Shaders["VectorCircle"] };
				sLeaser.sprites[la] = new FSprite("Futile_White", true) { rotation = 45, scaleY = 1f / 8f };
				sLeaser.sprites[lb] = new FSprite("Futile_White", true) { rotation = -45, scaleY = 1f / 8f };
				this.AddToContainer(sLeaser, rCam, null);
			}

			public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
			{
				Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker);
				var c = sLeaser.sprites[circle];
				c.x = vector.x - camPos.x;
				c.y = vector.y - camPos.y;
				c.scale = 10f - Mathf.Lerp(0, 5, Mathf.Pow(currentFactor, 0.5f));
				c.color = Color.Lerp(Color.white, Color.red, red);
				c.alpha = 0.01f + 0.1f * Mathf.Pow(currentFactor, 0.5f) - 0.011f * dieOut;

				var a = sLeaser.sprites[la];
				a.x = vector.x - camPos.x;
				a.y = vector.y - camPos.y;
				a.scaleX = 10f - Mathf.Lerp(0, 5, Mathf.Pow(currentFactor, 0.5f));
				a.color = Color.Lerp(Color.white, Color.red, red);
				a.alpha = crossAlpha;

				var b = sLeaser.sprites[lb];
				b.x = vector.x - camPos.x;
				b.y = vector.y - camPos.y;
				b.scaleX = 10f - Mathf.Lerp(0, 5, Mathf.Pow(currentFactor, 0.5f));
				b.color = Color.Lerp(Color.white, Color.red, red);
				b.alpha = crossAlpha;

				base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			}

            public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                base.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Shortcuts"));
            }
        }

		private void MoonConversation_AddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
		{
			if (IsMe(self.slOracleBehaviorHasMark.oracle.room.game))
			{
				if (self.id == Conversation.ID.MoonFirstPostMarkConversation)
				{
                    if (self.State.neuronsLeft == 4)
                    {
						self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Argh! It is you again."), 10));
						self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Pest! Come to munch on my neurons again? I don't have any to spare, go away!"), 5));
						self.events.Add(new Conversation.TextEvent(self, 20, self.Translate(". . ."), 30));
						self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("There will be no benefit if we are both filled with rage like that. I can sense it in you, too.<LINE>I know it's much to ask from a wild animal, but please stay civil, <PlayerName>."), 20));
						self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("We are both victims of the circunstances, you and I.<LINE>But right now, you seem more capable of fighting back than I do."), 30));
						self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Please put it to good use."), 15));
						self.events.Add(new Conversation.WaitEvent(self, 60));
						self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("I see that someone has given you the gift of communication. Was it Five Pebbles?<LINE>He's sick, you know. Being corrupted from the inside by old age and increasingly desperate experiments."), 30));
						self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("And to think he would help a wild creature like you. He must be just completely mad at this point."), 30));
						return;
					}
					if (self.State.neuronsLeft >= 5) {
						self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Hello <PlayerName>."), 10));
						self.events.Add(new Conversation.TextEvent(self, 25, self.Translate("What a facinating specimen... Are you a squidcicada?<LINE>You seem so oddly familiar too..."), 0));
						if (self.State.playerEncounters > 0 && self.State.playerEncountersWithMark == 0)
						{
							self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Perhaps... I saw you before? Or a relative of yours?<LINE>I have a vague memory..."), 20));
						}
                        else
                        {
							self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("No, really, you seem really, really familiar...<LINE>You... or something just like you, used to pay me visit?"), 20));
							self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("This intrigues me very much. More food for thought than what would be adequate for someone in my predicament."), 20));
						}
						self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("But anyways, <PlayerName>, what are you doing around these parts?<LINE>As you can see, I have nothing for you. Not even my memories."), 10));
						self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Or did I say that already?"), 5));
						self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("I see that someone has given you the gift of communication. Was it Five Pebbles? It must have been a really dangerous journey.<LINE>He's sick, you know. Being corrupted from the inside by old age and increasingly desperate experiments."), 30));
						self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I would have never imagined Five Pebbles being so benevolent to the wildlife like that, but here you are!<LINE>Maybe this will be his redemption arc? I highly doubt so, but there's no harm in wishful thinking."), 20));
						self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("We weren't made to cross out, you see, we were made to keep Iterating, and it has driven many of us a little mad."), 30));
						self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It is good to have someone to talk to after all this time!<LINE>I have had scavengers come by before, as you can tell from the paintings on the walls."), 0));
						self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("... hopefully the rain will wash out whatever markings you happen to leave, little creature."), 0));
						return;
					}
				}
				//else if (self.id == Conversation.ID.MoonSecondPostMarkConversation)
				//{

				//}
			}

			orig(self);
		}

		private void SLOracleBehaviorHasMark_Update(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
		{
			if (cicada.TryGet(self.player?.abstractCreature, out var ac) && ac.realizedCreature is Cicada cada)
			{
				var og = self.player.grasps;
				self.player.grasps = cada.grasps;
				orig(self, eu);
				self.player.grasps = og;
			}
			else
			{
				orig(self, eu);
			}
		}

		// Let me through dastards
		private float TempleGuardAI_ThrowOutScore(On.TempleGuardAI.orig_ThrowOutScore orig, TempleGuardAI self, Tracker.CreatureRepresentation crit)
		{
			if (player.TryGet(crit.representedCreature, out var ap) && ap.realizedCreature is Player p)
			{
				if (p.KarmaCap >= 9)
				{
					return 0f;
				}
			}
			return orig(self, crit);
		}

		private void Ghost_MoveCreature(On.VoidSea.PlayerGhosts.Ghost.orig_MoveCreature orig, VoidSea.PlayerGhosts.Ghost self, Vector2 movePos)
		{
			orig(self, movePos);
			if (cicada.TryGet(self.creature.abstractCreature, out var ac) && ac.realizedCreature is Cicada squiddy)
			{
				squiddy.graphicsModule.Reset();
			}
		}

		private void PlayerGhosts_AddGhost(On.VoidSea.PlayerGhosts.orig_AddGhost orig, VoidSea.PlayerGhosts self)
		{
			orig(self);
			var added = self.ghosts.Last();
			if (added != null && cicada.TryGet(added.creature.abstractCreature, out _)) self.voidSea.VoidSeaTreatment(added.creature, added.swimSpeed);
		}

		private void VoidSeaScene_VoidSeaTreatment(On.VoidSea.VoidSeaScene.orig_VoidSeaTreatment orig, VoidSea.VoidSeaScene self, Player player, float swimSpeed)
		{
			orig(self, player, swimSpeed);
			if (cicada.TryGet(player.abstractCreature, out var ac) && ac.realizedCreature is Cicada squiddy)
			{
				squiddy.lungs = 1f;
				squiddy.flyingPower = swimSpeed;
				squiddy.stamina = swimSpeed;
				squiddy.flying = true;
				if (squiddy.Submersion > 0.5f) squiddy.mainBodyChunk.vel.y -= 0.5f;
				squiddy.gravity = Mathf.Lerp(0.9f, 0f, squiddy.Submersion);
				squiddy.buoyancy = 0f;

				for (int i = 0; i < squiddy.bodyChunks.Length; i++)
				{
					squiddy.bodyChunks[i].restrictInRoomRange = float.MaxValue;
				}
			}
		}
	}
}