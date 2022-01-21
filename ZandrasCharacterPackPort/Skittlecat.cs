using System;
using On;
using SlugBase;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	public class Skittlecat : SlugBaseCharacter
	{
		const System.Reflection.BindingFlags any = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        private static Func<object, object> getIcon;
        private static Func<object, object> getPlayer;

        public Skittlecat() : base("zcpskittlecat", FormatVersion.V1, 0, true) {
            try
            {
				var playerLabelType = typeof(SlugBase.ArenaAdditions).GetNestedType("PlayerSelector", any).GetNestedType("PlayerLabel", any);

				getIcon = playerLabelType.GetField("icon", any | System.Reflection.BindingFlags.Instance).GetValue;
				getPlayer = playerLabelType.GetField("player", any | System.Reflection.BindingFlags.Instance).GetValue;

				new MonoMod.RuntimeDetour.Hook(playerLabelType.GetMethod("Update", any | System.Reflection.BindingFlags.Instance),
                typeof(Skittlecat).GetMethod("PlayerLabel_Update", any | System.Reflection.BindingFlags.Static));
            }
            catch { }
        }
		public override string DisplayName => "The Refractor";
		public override string Description => @"Shiny. Too shiny. Advert your eyes.";

        public static void PlayerLabel_Update(Action<object> orig, object self)
        {
            orig(self);
            try
            {
				var p = (getPlayer(self) as ArenaAdditions.PlayerDescriptor);
				if (p.player != null && p.player.Name == "zcpskittlecat")
					(getIcon(self) as CreatureSymbol).myColor = p.Color;
            }
            catch { }
        }

        protected override void Disable()
		{
			On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
			On.PlayerGraphics.ApplyPalette -= PlayerGraphics_ApplyPalette;
		}

		protected override void Enable()
		{
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
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
