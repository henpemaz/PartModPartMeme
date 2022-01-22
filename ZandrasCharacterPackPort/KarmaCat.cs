using SlugBase;
using System;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	internal class KarmaCat : SlugBaseCharacter
	{
		public KarmaCat() : base("zcpkarmacat", FormatVersion.V1, 0, true) {
            On.Menu.KarmaLadderScreen.GetDataFromGame += KarmaLadderScreen_GetDataFromGame;
            On.Menu.SleepAndDeathScreen.FoodCountDownDone += SleepAndDeathScreen_FoodCountDownDone; ;
		}

		public override string DisplayName => "The Spirit";
		public override string Description => @"Ephemeral, fading.
Without attunement, this slugcat might one day disappear from this plane.";

		// fix  assumptions
		private void KarmaLadderScreen_GetDataFromGame(On.Menu.KarmaLadderScreen.orig_GetDataFromGame orig, Menu.KarmaLadderScreen self, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
		{
			orig(self, package);

			if (self.saveState != null && IsMe(self.saveState))
			{
				// would display the wrong karma because it assumed it was increased, but it was insead decreased
				if (self.karmaLadder != null && self.ID == ProcessManager.ProcessID.SleepScreen)
				{
					self.karma.x = RWCustom.Custom.IntClamp(package.karma.x + 1, 0, package.karma.y);
					self.karmaLadder.displayKarma = self.karma;
					self.karmaLadder.moveToKarma = self.karma.x;
					self.karmaLadder.scroll = (float)self.karma.x;
					self.karmaLadder.lastScroll = (float)self.karma.x;
				}
			}
		}

		private void SleepAndDeathScreen_FoodCountDownDone(On.Menu.SleepAndDeathScreen.orig_FoodCountDownDone orig, Menu.SleepAndDeathScreen self)
		{
			orig(self);

			if (self.saveState != null && IsMe(self.saveState))
			{
				// would display the wrong karma because it assumed it was increased, but it was insead decreased
				if (self.karmaLadder != null && self.IsSleepScreen)
				{
					self.karmaLadder.GoToKarma(self.karma.x - 1, true);
				}
			}
		}

		protected override void Disable()
		{
			On.Player.Update -= Player_Update;
			On.Player.ctor -= Player_ctor;
			On.PlayerGraphics.ApplyPalette -= PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
			On.Player.ShortCutColor -= Player_ShortCutColor;

			On.SaveState.SessionEnded -= SaveState_SessionEnded;

			On.World.SpawnGhost -= World_SpawnGhost;
			On.GhostWorldPresence.SpawnGhost -= GhostWorldPresence_SpawnGhost;
		}

        protected override void Enable()
		{
            On.Player.Update += Player_Update;
            On.Player.ctor += Player_ctor; ;
			On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
			On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
			On.Player.ShortCutColor += Player_ShortCutColor;

            On.SaveState.SessionEnded += SaveState_SessionEnded;

            On.World.SpawnGhost += World_SpawnGhost;
            On.GhostWorldPresence.SpawnGhost += GhostWorldPresence_SpawnGhost;
		}

		// boo
        private bool GhostWorldPresence_SpawnGhost(On.GhostWorldPresence.orig_SpawnGhost orig, GhostWorldPresence.GhostID ghostID, int karma, int karmaCap, int ghostPreviouslyEncountered, bool playingAsRed)
        {
			if (ghostly) return true;
			return orig(ghostID, karma, karmaCap, ghostPreviouslyEncountered, playingAsRed);
        }

		// ghostly inclined little slugcat
        private void World_SpawnGhost(On.World.orig_SpawnGhost orig, World self)
        {
			if (self.game.IsStorySession && IsMe((self.game.session as StoryGameSession).saveState)) ghostly = true;
			orig(self);
			ghostly = false;
        }

		// revert karma-up of win
        private void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            if (IsMe(self) && survived)
            {
				self.deathPersistentSaveData.karma-=2; // k goes down by 2 then up by 1
            }
			orig(self, game, survived, newMalnourished);
        }

		// On first cycle set up karma and Survivor
        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
			orig(self, abstractCreature, world);
			if (!IsMe(self)) return;

            if (world.game.IsStorySession)
            {
				var saveState = world.game.GetStorySession.saveState;
				if (saveState.cycleNumber == 0)
				{
					saveState.deathPersistentSaveData.karma = saveState.deathPersistentSaveData.karmaCap;
					var survivor = (saveState.deathPersistentSaveData.winState.GetTracker(WinState.EndgameID.Survivor, true) as WinState.IntegerTracker);
					survivor.SetProgress(survivor.max);
					survivor.lastShownProgress = survivor.progress;
				}
			}

			life[self] = world.game.IsStorySession ? maxlife : 2 * maxlife;
		}

		// if out of karma, slowly die
        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
			orig(self, eu);
			if (!IsMe(self)) return;
			if (self.room is null) return;

			if (self.Consious && (
				!self.room.game.IsStorySession || (
					self.room.game.GetStorySession.saveState.deathPersistentSaveData.karma <= 0)))
			{
				life[self]--;

				if (life[self] <= maxlife / 2f && !self.dead)
				{
					self.airInLungs = Mathf.Clamp01(Mathf.InverseLerp(0, maxlife / 2f, life[self]));
					self.aerobicLevel = Mathf.Max(self.aerobicLevel, 1 - self.airInLungs);
				}

				if (life[self] <= maxlife / 3f && !self.dead && !self.Malnourished)
				{
					self.SetMalnourished(true);
				}


				if (life[self] <= 0 && !self.dead)
				{
					self.SetMalnourished(false);
					self.Die();
				}
			}
		}

		// will color cat, set up reference
		private Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
		{
			bool isme = IsMe(self);
			if (isme) playerBeingUpdated.Target = self;
			Color c = orig(self);
			if (isme) playerBeingUpdated.Target = null;
			return c;
		}

		// will color cat, set up reference
		private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			if (!IsMe(self.owner as Player)) orig(self, sLeaser, rCam, palette);
			else
			{
				playerBeingUpdated.Target = self.owner;
				currPalette = palette;
				orig(self, sLeaser, rCam, palette);
				currPalette = null;
				playerBeingUpdated.Target = null;
			}
		}

		// color is based on karma or life left. polls an externar reference to player to get those, otherwise default
		public override Color? SlugcatColor(int slugcatCharacter, Color baseColor)
		{
			Color col = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
			Player p;
			if (playerBeingUpdated?.Target != null && IsMe(p = playerBeingUpdated?.Target<Player>()))
            {
				if (p.Karma <= 0)
				{
					col = Color.Lerp(currPalette != null ? currPalette.Value.blackColor : Color.black, Color.white, (float)life[p] / (float)maxlife);
                }
                else
                {
					col = Color.Lerp(currPalette != null ? currPalette.Value.blackColor : Color.black, Color.white, (float)p.Karma / (float)p.KarmaCap);
				}
			}

			if (slugcatCharacter == -1)
				return col;
			else
				return Color.Lerp(baseColor, col, 0.75f);
		}

		// color changes dynamically
		private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (IsMe(self.player))self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
			orig(self, sLeaser, rCam, timeStacker, camPos);
		}

		public static AttachedField<Player, int> life = new AttachedField<Player, int>();
		public static int maxlife = 2400;
		public static WeakReference playerBeingUpdated = new WeakReference(null);
        private bool ghostly;
        private RoomPalette? currPalette;
    }
}
