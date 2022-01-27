using System;
using On;
using SlugBase;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	public class Skittlecat : SlugBaseCharacter
	{
        public Skittlecat() : base("zcpskittlecat", FormatVersion.V1, 0, true) { }
		public override string DisplayName => "The Refractor";
		public override string Description => @"Shiny. Too shiny. Advert your eyes.";

        protected override void Disable()
		{
			On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
			On.PlayerGraphics.ApplyPalette -= PlayerGraphics_ApplyPalette;
		}

		protected override void Enable()
		{
			// self coloring
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;

			// Rainbow quest

		}

        private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
			orig(self, sLeaser, rCam, palette);
			if(IsMe(self.player))
            {
				if (self.lightSource != null) self.lightSource.color = Color.Lerp(new Color(1f, 1f, 1f), PlayerGraphics.SlugcatColor(self.player.playerState.slugcatCharacter), 0.5f);
			}
        }

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			if (IsMe(self.player)) self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);

			orig(self, sLeaser, rCam, timeStacker, camPos);
		}

		public override Color? SlugcatColor(int slugcatCharacter, Color baseColor)
		{
			Color col = new HSLColor(Time.realtimeSinceStartup, 1f, 0.5f).rgb;

			if (slugcatCharacter == -1)
				return col;
			else
				return Color.Lerp(baseColor, col, 0.75f);
		}
	}
}
