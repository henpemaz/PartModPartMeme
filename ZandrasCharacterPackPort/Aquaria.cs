using System;
using RWCustom;
using SlugBase;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	internal class Aquaria : SlugBaseCharacter
	{
		public Aquaria() : base("zcpaquaria", FormatVersion.V1, 0, true) { }
		public override string DisplayName => "Aquaria";
		public override string Description => @"Swim freely. Farewell, creatures of the surface.";

		protected override void Disable()
		{
			On.Player.Update -= Player_Update;
		}

		protected override void Enable()
		{
            On.Player.Update += Player_Update;
		}

        public override Color? SlugcatColor(int slugcatCharacter, Color baseColor)
		{
			Color col = new Color(0.5372549f, 0.92156863f, 1f);

			if (slugcatCharacter == -1)
				return col;
			else
				return Color.Lerp(baseColor, col, 0.75f);
		}


		public void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
		{
			orig(self, eu);
			if (!IsMe(self)) return;
			if (self.room is null) return;

			self.airInLungs = 1f; // mhh weird
			self.slugcatStats.lungsFac = 0f;
			if (self.submerged)
			{
				self.room.AddObject(new Bubble(self.firstChunk.pos, self.firstChunk.vel + Custom.DegToVec(UnityEngine.Random.value * 360f) * Mathf.Lerp(6f, 0f, self.airInLungs), false, false));
			}
		}
	}
}
