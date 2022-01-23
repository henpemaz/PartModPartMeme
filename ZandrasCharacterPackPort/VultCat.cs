using SlugBase;
using System;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	internal class VultCat : SlugBaseCharacter
	{
		public VultCat() : base("zcpvultcat", FormatVersion.V1, 0, true) {
			On.Player.ctor += Player_ctor;
		}
		public override string DisplayName => "Vult";
		public override string Description => @"Child of Vultures.
The one who hides their face both is feared and fears.";

		protected override void Disable()
		{
			On.Player.Update -= Player_Update;
			//On.Player.ctor -= Player_ctor; // moved
			On.PlayerGraphics.ApplyPalette -= PlayerGraphics_ApplyPalette;
			On.SaveState.SessionEnded -= SaveState_SessionEnded;
			On.Player.ShortCutColor -= Player_ShortCutColor;
		}

		protected override void Enable()
		{
			On.Player.Update += Player_Update;
            //On.Player.ctor += Player_ctor; // moved
			On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
			On.SaveState.SessionEnded += SaveState_SessionEnded;
            On.Player.ShortCutColor += Player_ShortCutColor;

			
		}

        private Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
        {
			bool isme = IsMe(self);
			if (isme) playerBeingUpdated.Target = self;
			Color c = orig(self);
			if (isme) playerBeingUpdated.Target = null;
			return c;
		}

        private void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
			for (int i = 0; i < game.world.NumberOfRooms; i++)
			{
				AbstractRoom abstractRoom = game.world.GetAbstractRoom(game.world.firstRoomIndex + i);
				for (int j = abstractRoom.entities.Count - 1; j >= 0; j--)
				{
					if (abstractRoom.entities[j] is VultureMask.AbstractVultureMask v && v.realizedObject != null && v.realizedObject is CustomVultMask)
					{
						abstractRoom.entities.RemoveAt(j);
					}
				}
			}
			orig(self, game, survived, newMalnourished);
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


		// the way this manages the mask assumes itll be called only once ever
		private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
			orig(self, abstractCreature, world);
			if (!IsMe(self)) return;

			int oldseed = UnityEngine.Random.seed;

			if (abstractCreature.world.game.IsStorySession)
            {
				UnityEngine.Random.seed = abstractCreature.world.game.GetStorySession.saveState.seed + self.playerState.playerNumber + 100 * self.playerState.slugcatCharacter;
            }
			catColor[self] = new HSLColor(Mathf.Lerp(0.9f, 1.6f, UnityEngine.Random.value), Mathf.Lerp(0.5f, 0.7f, UnityEngine.Random.value), Mathf.Lerp(0.7f, 0.8f, UnityEngine.Random.value)).rgb;
			VultureMask.AbstractVultureMask abstractVultureMask = new VultureMask.AbstractVultureMask(world, null, abstractCreature.pos, world.game.GetNewID(), UnityEngine.Random.seed, true);
			if (abstractCreature.world.game.IsStorySession)
			{
				UnityEngine.Random.seed = oldseed;
			}
			abstractVultureMask.realizedObject = new CustomVultMask(abstractVultureMask, world);
			mask[self] = new WeakReference(abstractVultureMask.realizedObject);
			self.grasps = new Creature.Grasp[3];
			self.Grab(abstractVultureMask.realizedObject, 2, 0, Creature.Grasp.Shareability.CanNotShare, 9999f, true, false);
		}

        public override Color? SlugcatColor(int slugcatCharacter, Color baseColor)
		{
			Color col = playerBeingUpdated?.Target != null && IsMe(playerBeingUpdated?.Target<Player>()) ? catColor[playerBeingUpdated?.Target<Player>()] : defaultColor;

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

			if (self.grasps[2] == null)
			{
				self.Grab(mask[self].Target<CustomVultMask>(), 2, 0, Creature.Grasp.Shareability.CanNotShare, 9999f, true, false);
			}
		}

		public static Color defaultColor = new HSLColor(1.25f, 0.6f, 0.75f).rgb;
		public static AttachedField<Player, Color> catColor = new AttachedField<Player, Color>();
		public static WeakReference playerBeingUpdated = new WeakReference(null);

		public static AttachedField<Player, WeakReference> mask = new AttachedField<Player, WeakReference>();
	}
}
