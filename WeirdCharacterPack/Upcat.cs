using MonoMod.RuntimeDetour;
using SlugBase;

namespace WeirdCharacterPack
{
    internal class Upcat : VVVVVCat
    {
        public Upcat() : base ("zpcupcat") {
            On.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage; ;
		}

        private void SlugcatPage_AddImage(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_AddImage orig, Menu.SlugcatSelectMenu.SlugcatPage self, bool ascended)
        {
			orig(self, ascended);

			if (PlayerManager.GetCustomPlayer(self.slugcatNumber) == this)
			{
                foreach (var item in self.slugcatImage.depthIllustrations)
                {
					item.sprite.ScaleAroundPointRelative(UnityEngine.Vector2.zero, 1f, -1f);
					item.pos.y = self.menu.manager.rainWorld.screenSize.y - item.pos.y;
					item.lastPos.y = self.menu.manager.rainWorld.screenSize.y - item.lastPos.y;
				}
			}
		}

        public override string DisplayName => "Upcat";
        public override string Description => @"What is that?
ps. this mode probaby isn't beatable, but good luck trying!";

		// Todo maaaaaybe just maybe make it easier to get into ceiling pipes
		// that and spawn a grapple or two in outskirts

        protected override void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            base.Player_ctor(orig, self, abstractCreature, world);
			if (!IsMe(self)) return;

			reverseGravity[self] = true; //permanently
		}

        protected override void Disable()
		{
			// Basic swithched behavior
			On.Player.Update -= Player_Update;
			//On.Player.Jump -= Player_Jump;
			//On.Player.UpdateAnimation -= Player_UpdateAnimation1;
			//On.Player.WallJump -= Player_WallJump;
			On.PlayerGraphics.Update -= PlayerGraphics_Update;
			Utils.FancyPlayerGraphics.Update -= PlayerGraphics_Update;
			On.Player.GraphicsModuleUpdated -= Player_GraphicsModuleUpdated;

			// Inverted drawing
			On.PlayerGraphics.InitiateSprites -= PlayerGraphics_InitiateSprites;
			Utils.FancyPlayerGraphics.InitiateSprites -= PlayerGraphics_InitiateSprites;
			On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
			Utils.FancyPlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;

			// Edge cases
			On.PlayerGraphics.Reset -= PlayerGraphics_Reset;
			On.Creature.SuckedIntoShortCut -= Creature_SuckedIntoShortCut;

			// Items
			On.Player.PickupCandidate -= Player_PickupCandidate;
			On.Player.SlugcatGrab -= Player_SlugcatGrab;
			IL.Player.Update -= Player_Update1;
			On.TubeWorm.Tongue.ProperAutoAim -= Tongue_ProperAutoAim;

			// Water fixes
			IL.Player.UpdateAnimation -= Player_UpdateAnimation;
			IL.Player.MovementUpdate -= Player_MovementUpdate;
			IL.BodyChunk.Update -= BodyChunk_Update;

			chunkDetour.Free();
			chunkDetour = null;

			// If you change these, don't forget to update Upcat too, uses the same hooks minus jumps
		}

		protected override void Enable()
		{
			// Basic swithched behavior
			// Switch behavior, start inverted processing
			On.Player.Update += Player_Update;
			// proper jump detection
			//On.Player.Jump += Player_Jump;
			// jump behavior changes
			//On.Player.UpdateAnimation += Player_UpdateAnimation1;
			// no op
			//On.Player.WallJump += Player_WallJump;
			// used to reverse, now just catch exceptions to avoid invalid state
			On.PlayerGraphics.Update += PlayerGraphics_Update;
			Utils.FancyPlayerGraphics.Update += PlayerGraphics_Update;
			// end of inverted processing
			On.Player.GraphicsModuleUpdated += Player_GraphicsModuleUpdated;

			// Inverted drawing
			// reset previousDraw coordinates
			On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
			Utils.FancyPlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
			// draw things in the mirrored room!!!
			On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
			Utils.FancyPlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

			// Edge cases
			// reset called from outside of update, apply reversed coordinates
			On.PlayerGraphics.Reset += PlayerGraphics_Reset;
			// deverse on room leave mid-update, fix wrong tile data during room activation
			On.Creature.SuckedIntoShortCut += Creature_SuckedIntoShortCut;

			// Items
			// player picks up things considering its real position
			On.Player.PickupCandidate += Player_PickupCandidate;
			// Picked up things move to inverted space
			On.Player.SlugcatGrab += Player_SlugcatGrab;
			// player colides with flies considering its real position
			IL.Player.Update += Player_Update1;
			// fix grapple dir
			On.TubeWorm.Tongue.ProperAutoAim += Tongue_ProperAutoAim;

			// Water fixes
			// fix clinging to surface of water while surfaceswim
			IL.Player.UpdateAnimation += Player_UpdateAnimation;
			// Determine deep-swim vs surface swim
			IL.Player.MovementUpdate += Player_MovementUpdate;
			// Bodychunk float when submerged logic
			IL.BodyChunk.Update += BodyChunk_Update;

			// Chunk 'submerged' inverted (water on top), hook applied during player update, reflection done here.
			chunkDetour = new Hook(typeof(BodyChunk).GetProperty("submersion").GetGetMethod(), typeof(VVVVVCat).GetMethod("Flipped_submersion"), this);
			chunkDetour.Undo();
		}
	}
}