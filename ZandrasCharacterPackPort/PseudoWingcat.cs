using SlugBase;
using System;

namespace ZandrasCharacterPackPort
{
	public class PseudoWingcat : SlugBaseCharacter
	{
		public PseudoWingcat() : base("zcppseudowingcat", FormatVersion.V1, 0, true) { }
		public override string DisplayName => "The Winged";
		public override string Description => @"With just a little more time and preparation, this one would have conquered the skies...";

		protected override void Disable()
		{
			On.Player.InitiateGraphicsModule -= Player_InitiateGraphicsModule;
		}

		protected override void Enable()
		{
            On.Player.InitiateGraphicsModule += Player_InitiateGraphicsModule;
		}

        private void Player_InitiateGraphicsModule(On.Player.orig_InitiateGraphicsModule orig, Player self)
		{
            self.graphicsModule = new PseudoWingcatGraphics(self);
		}
	}
}
