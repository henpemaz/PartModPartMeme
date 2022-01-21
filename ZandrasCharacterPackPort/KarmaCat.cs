using SlugBase;
using System;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	internal class KarmaCat : SlugBaseCharacter
	{
		public KarmaCat() : base("zcpkarmacat", FormatVersion.V1, 0, true) { }
		public override string DisplayName => "The Spirit";
		public override string Description => @"Ephemeral, fading. Without attunement, this slugcat might one day disappear from this plane.";

		protected override void Disable()
		{
			On.Player.Update -= Player_Update;
			On.Player.ctor -= Player_ctor;
			On.PlayerGraphics.ApplyPalette -= PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
			On.Player.ShortCutColor -= Player_ShortCutColor;
		}

        protected override void Enable()
		{
            On.Player.Update += Player_Update;
            On.Player.ctor += Player_ctor; ;
			On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
			On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
			On.Player.ShortCutColor += Player_ShortCutColor;
		}

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
				}
			}

			life[self] = world.game.IsStorySession ? maxlife : 2 * maxlife;
		}

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
			orig(self, eu);
			if (!IsMe(self)) return;
			if (self.room is null) return;

			if (self.Consious && !self.room.game.IsStorySession || self.room.game.GetStorySession.saveState.deathPersistentSaveData.karma == 0)
			{
				life[self]--;
				if (life[self] <= 0 && !self.dead)
				{
					self.Die();
				}
			}
		}


		private Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
		{
			bool isme = IsMe(self);
			if (isme) playerBeingUpdated.Target = self;
			Color c = orig(self);
			if (isme) playerBeingUpdated.Target = null;
			return c;
		}

		private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			if (!IsMe(self.owner as Player)) orig(self, sLeaser, rCam, palette);
			else
			{
				playerBeingUpdated.Target = self.owner;
				orig(self, sLeaser, rCam, palette);
				playerBeingUpdated.Target = null;
			}
		}

		public override Color? SlugcatColor(int slugcatCharacter, Color baseColor)
		{
			Color col = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
			Player p;
			if (playerBeingUpdated?.Target != null && IsMe(p = playerBeingUpdated?.Target<Player>()))
            {
				if (p.Karma == 0)
				{
					col = Color.Lerp(Color.black, Color.white, life[p] / maxlife);
				}else
				col = Color.Lerp(Color.black, Color.white, (float)p.Karma / (float)p.KarmaCap);

			}

			if (slugcatCharacter == -1)
				return col;
			else
				return Color.Lerp(baseColor, col, 0.75f);
		}

		private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (IsMe(self.player))self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
			orig(self, sLeaser, rCam, timeStacker, camPos);
		}

		public static AttachedField<Player, int> life = new AttachedField<Player, int>();
		public static int maxlife = 2400;
		public static WeakReference playerBeingUpdated = new WeakReference(null);
	}
}
