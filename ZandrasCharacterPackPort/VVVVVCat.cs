using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using SlugBase;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	internal class VVVVVCat : SlugBaseCharacter
	{
		public VVVVVCat() : base("zcpVVVVVcat", FormatVersion.V1, 0, true) {
			// Initialize variables
			On.Player.ctor += Player_ctor;
		}
		public override string DisplayName => "The VVVVV";
		public override string Description => @"An unstable prototype.
Created for reaching places no other slugcat ever reached.";

		private Hook chunkDetour;
		// this was a lot more complicated than it should have been.
		protected override void Disable()
		{
			On.Player.Update -= Player_Update;
            On.Player.GraphicsModuleUpdated -= Player_GraphicsModuleUpdated;
			On.Creature.SuckedIntoShortCut -= Creature_SuckedIntoShortCut;
			On.PlayerGraphics.Update -= PlayerGraphics_Update;
			On.PlayerGraphics.InitiateSprites -= PlayerGraphics_InitiateSprites;
			On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
            On.PlayerGraphics.Reset -= PlayerGraphics_Reset;

			IL.Player.MovementUpdate -= Player_MovementUpdate;
            IL.BodyChunk.Update -= BodyChunk_Update;
            IL.Player.UpdateAnimation -= Player_UpdateAnimation;
			chunkDetour.Free();
			chunkDetour = null;
			On.Player.PickupCandidate -= Player_PickupCandidate;
			IL.Player.Update -= Player_Update1;
		}

        protected override void Enable()
		{
			// Switch behavior, start inverted processing
			On.Player.Update += Player_Update;
			// end of inverted processing
			On.Player.GraphicsModuleUpdated += Player_GraphicsModuleUpdated;
			// deverse on room leave mid-update, fix wrong tile data during room activation
			On.Creature.SuckedIntoShortCut += Creature_SuckedIntoShortCut;

			// used to reverse, now just catch exceptions to avoid invalid state
			On.PlayerGraphics.Update += PlayerGraphics_Update;
			// reset previousDraw coordinates
			On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
			// draw things in the mirrored room!!!
			On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
			// reset called from outside of update, apply reversed coordinates
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;

			// Determine deep-swim vs surface swim
			IL.Player.MovementUpdate += Player_MovementUpdate;
			// Bodychunk float when submerged logic
			IL.BodyChunk.Update += BodyChunk_Update;

			// fix clinging to surface of water while surfaceswim
			IL.Player.UpdateAnimation += Player_UpdateAnimation;
			// Chunk 'submerged' inverted (water on top), hook applied during player update, reflection done here.
			chunkDetour = new Hook(typeof(BodyChunk).GetProperty("submersion").GetGetMethod(), typeof(VVVVVCat).GetMethod("Flipped_submersion"), this);
			chunkDetour.Undo();
			// player picks up things considering its real position
            On.Player.PickupCandidate += Player_PickupCandidate;
			// player colides with flies considering its real position
			IL.Player.Update += Player_Update1;

            // fix grapple dir
            On.TubeWorm.Tongue.ProperAutoAim += Tongue_ProperAutoAim;
		}

        private Vector2 Tongue_ProperAutoAim(On.TubeWorm.Tongue.orig_ProperAutoAim orig, TubeWorm.Tongue self, Vector2 originalDir)
        {
			if (self.worm.grabbedBy.Count > 0 && self.worm.grabbedBy[0].grabber is Player p && IsMe(p) && reverseGravity[p])
			{
				originalDir.y *= -1f;
			}
			return orig(self, originalDir);
		}

        // player colides with flies considering its real position
        // player lines 1000 through 1012 envelopped in flipping player y
        private void Player_Update1(ILContext il)
        {
			var c = new ILCursor(il);

			ILLabel dest1 = null;
			if (c.TryGotoNext(MoveType.Before,
				i => i.MatchLdarg(0),
				i => i.MatchCall<Creature>("get_grasps"),
				i => i.MatchLdcI4(0),
				i => i.MatchLdelemRef(),
				i => i.MatchBrfalse(out _),

				i => i.MatchLdarg(0),
				i => i.MatchCall<Creature>("get_grasps"),
				i => i.MatchLdcI4(1),
				i => i.MatchLdelemRef(),
				i => i.MatchBrtrue(out dest1),

				i => i.MatchLdarg(0),
				i => i.MatchLdfld<UpdatableAndDeletable>("room"),
				i => i.MatchLdfld<Room>("fliesRoomAi"),
				i => i.MatchBrfalse(out _)
				))
			{
				c.MoveAfterLabels();

				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Action<Player>>((p) =>
				{
					if (reversedPlayer[p])
					{
						p.bodyChunks[0].pos.y = p.room.PixelHeight - p.bodyChunks[0].pos.y; // upsidown
					}
				});

				c.GotoLabel(dest1);
				c.Index++; // the game was mysteriously crashing without this

				c.MoveAfterLabels();
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Action<Player>>((p) =>
				{
					if (reversedPlayer[p])
					{
						p.bodyChunks[0].pos.y = p.room.PixelHeight - p.bodyChunks[0].pos.y; // upsidown
					}
				});
			}
			else Debug.LogException(new Exception("Couldn't IL-hook Player_Update from VVVVVV cat")); // deffendisve progrmanig
		}

		// Determine deep-swim vs surface swim
		// player line 5453 patched in 2 spots
		private void Player_MovementUpdate(MonoMod.Cil.ILContext il)
		{
			var c = new ILCursor(il);
			ILLabel dest1 = null;
			if (c.TryGotoNext(MoveType.After,
				i => i.MatchLdarg(0),
				i => i.MatchCall<PhysicalObject>("get_bodyChunks"),
				i => i.MatchLdcI4(0),
				i => i.MatchLdelemRef(),
				i => i.MatchLdflda<BodyChunk>("pos"),
				i => i.MatchLdfld<Vector2>("y"),

				i => i.MatchLdarg(0),
				i => i.MatchLdfld<UpdatableAndDeletable>("room"),

				i => i.MatchLdarg(0),
				i => i.MatchCall<PhysicalObject>("get_bodyChunks"),
				i => i.MatchLdcI4(0),
				i => i.MatchLdelemRef(),
				i => i.MatchLdflda<BodyChunk>("pos"),
				i => i.MatchLdfld<Vector2>("x"),

				i => i.MatchCallOrCallvirt<Room>("FloatWaterLevel"),
				i => i.MatchLdcR4(out _), // a value

				i => i.MatchSub(),
				i => i.MatchBgeUn(out dest1) // fail out
				))
			{
				c.Index--;
				c.Index--;
				c.MoveAfterLabels();

				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<float, float, float, Player, bool>>((y, l, t, p) => // Test wether should NOT deep swim (shortcircuit out logic)
				{
					if (reverseGravity[p])
					{
						return y <= (l + t); // upsidown
					}
					else return y >= (l - t);
				});
				c.Emit(OpCodes.Brtrue, dest1);
				c.RemoveRange(2);

				ILLabel dest2 = null;
				if (c.TryGotoNext(MoveType.After,
					i => i.MatchLdarg(0),
					i => i.MatchCall<PhysicalObject>("get_bodyChunks"),
					i => i.MatchLdcI4(0),
					i => i.MatchLdelemRef(),
					i => i.MatchLdflda<BodyChunk>("pos"),
					i => i.MatchLdfld<Vector2>("y"),

					i => i.MatchLdarg(0),
					i => i.MatchLdfld<UpdatableAndDeletable>("room"),

					i => i.MatchLdarg(0),
					i => i.MatchCall<PhysicalObject>("get_bodyChunks"),
					i => i.MatchLdcI4(0),
					i => i.MatchLdelemRef(),
					i => i.MatchLdflda<BodyChunk>("pos"),
					i => i.MatchLdfld<Vector2>("x"),

					i => i.MatchCallOrCallvirt<Room>("FloatWaterLevel"),

					i => i.MatchLdloc(9), // check flag
					i => i.MatchBrfalse(out _),
					i => i.MatchLdcR4(out _), // true value
					i => i.MatchBr(out _), // path
					i => i.MatchLdcR4(out _), // value

					// I want to erase these two
					i => i.MatchSub(),
					i => i.MatchBgeUn(out dest2) // fail out
					))
				{
					c.Index--;
					c.Index--;
					c.MoveAfterLabels();

					c.Emit(OpCodes.Ldarg_0);
					c.EmitDelegate<Func<float, float, float, Player, bool>>((y, l, t, p) => // Test wether should NOT deep swim (shortcircuit out logic)
					{
						if (reverseGravity[p])
						{
							return y <= (l + t); // upsidown
						}
						else return y >= (l - t);
					});
					c.Emit(OpCodes.Brtrue, dest2);
					c.RemoveRange(2);
				}
				else Debug.LogException(new Exception("Couldn't IL-hook Player_MovementUpdate from VVVVVV cat")); // deffendisve progrmanig
			}
			else Debug.LogException(new Exception("Couldn't IL-hook Player_MovementUpdate from VVVVVV cat")); // deffendisve progrmanig
		}

		// Bodychunk float when submerged logic
		//BodyChunk line 104
		private void BodyChunk_Update(ILContext il)
		{
			var c = new ILCursor(il);
			ILLabel dest1 = null;
			if (c.TryGotoNext(MoveType.After,
				i => i.MatchLdarg(0),
				i => i.MatchLdflda<BodyChunk>("pos"),
				i => i.MatchLdfld<Vector2>("y"),

				i => i.MatchLdarg(0),
				i => i.MatchLdfld<BodyChunk>("rad"),

				i => i.MatchSub(),

				i => i.MatchLdarg(0),
				i => i.MatchCall<BodyChunk>("get_owner"),
				i => i.MatchLdfld<UpdatableAndDeletable>("room"),

				i => i.MatchLdarg(0),
				i => i.MatchLdflda<BodyChunk>("pos"),
				i => i.MatchLdfld<Vector2>("x"),

				i => i.MatchCallOrCallvirt<Room>("FloatWaterLevel"),

				i => i.MatchBgtUn(out dest1) // fail out
				))
			{
				c.Index--;

				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<float, float, float, BodyChunk, bool>>((y, r, l, b) => // Test wether should NOT float up (shortcircuit out logic)
				{
					if (b.owner is Player p && reversedPlayer[p])
					{
						return (y + r) <= l; // upsidown
					}
					else return (y - r) >= l;
				});
				c.Emit(OpCodes.Brtrue, dest1);
				c.Remove();
				c.GotoPrev(MoveType.Before, i => i.MatchSub());
				c.Remove();
			}
			else Debug.LogException(new Exception("Couldn't IL-hook BodyChunk_Update from VVVVVV cat")); // deffendisve progrmanig
		}

		// fix clinging to surface of water while surfaceswim
		// player line 2429
		private void Player_UpdateAnimation(ILContext il)
		{
			var c = new ILCursor(il);
			float mulfac = 0f;
			if (c.TryGotoNext(MoveType.After,
				i => i.MatchLdarg(0),
				i => i.MatchCall<PhysicalObject>("get_bodyChunks"),
				i => i.MatchLdcI4(0),
				i => i.MatchLdelemRef(),
				i => i.MatchLdflda<BodyChunk>("pos"),
				i => i.MatchLdfld<Vector2>("y"),

				i => i.MatchLdarg(0),
				i => i.MatchLdfld<UpdatableAndDeletable>("room"),

				i => i.MatchLdarg(0),
				i => i.MatchCall<PhysicalObject>("get_bodyChunks"),
				i => i.MatchLdcI4(0),
				i => i.MatchLdelemRef(),
				i => i.MatchLdflda<BodyChunk>("pos"),
				i => i.MatchLdfld<Vector2>("x"),

				i => i.MatchCallOrCallvirt<Room>("FloatWaterLevel"),
				i => i.MatchLdcR4(out _), // a value

				i => i.MatchAdd(),
				i => i.MatchSub(),
				i => i.MatchLdcR4(out mulfac), // a value
				i => i.MatchMul()
				))
			{
				c.Index -= 4;
				c.MoveAfterLabels();

				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<float, float, float, Player, float>>((f1, f2, f3, p) => // distance offset from surface times gain
				{
					if (reverseGravity[p])
					{
						return (f2 - f3) - f1; // upsidown
					}
					else return f1 - (f2 + f3);
				});
				c.RemoveRange(2);
			}
			else Debug.LogException(new Exception("Couldn't IL-hook Player_UpdateAnimation from VVVVVV cat")); // deffendisve progrmanig
		}

		// player picks up things considering its real position
		private PhysicalObject Player_PickupCandidate(On.Player.orig_PickupCandidate orig, Player self, float favorSpears)
        {
			
			PhysicalObject retval = null;
			if (IsMe(self) && reversedPlayer[self] && self.room != null)
			{
				Room room = self.room;
				// simpler switch
				self.bodyChunks[0].pos.y = room.PixelHeight - self.bodyChunks[0].pos.y;
				try
				{
					retval = orig(self, favorSpears);
				}
				catch (Exception e) { Debug.LogException(e); }
				self.bodyChunks[0].pos.y = room.PixelHeight - self.bodyChunks[0].pos.y;
			}
			else
			{
				retval = orig(self, favorSpears);
			}
			return retval;
		}

        // fix wrong tile data during room activation from player entering shortcut
        private void Creature_SuckedIntoShortCut(On.Creature.orig_SuckedIntoShortCut orig, Creature self, RWCustom.IntVector2 entrancePos, bool carriedByOther)
        {
			if (self is Player p && IsMe(p) && reversedPlayer[p] && p.room != null)
            {
				Room room = p.room;
				DeversePlayer(p, room);
				try
				{
					orig(self, p.enteringShortCut.Value, carriedByOther);
				}
				catch (Exception e) { Debug.LogException(e); }
				ReversePlayer(p, room);
			}
            else
            {
				orig(self, entrancePos, carriedByOther);
            }
        }

		// reset previousDraw coordinates
		private void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
			if (IsMe(self.player))
			{
				previousDraw.Unset(self);
			}
			orig(self, sLeaser, rCam);
		}

		// end of reversed update cycle
        private void Player_GraphicsModuleUpdated(On.Player.orig_GraphicsModuleUpdated orig, Player self, bool actuallyViewed, bool eu)
		{
			if (!IsMe(self))
			{
				orig(self, actuallyViewed, eu);
				return;
			}
			// switched behavior
			if (reversedPlayer[self] && self.room != null)
			{
				Room room = self.room;
				//ReversePlayer(self, room);
				try
				{
					orig(self, actuallyViewed, eu);
				}
				catch (Exception e) { Debug.LogException(e); }
				DeversePlayer(self, room);
			}
			else
			{
				orig(self, actuallyViewed, eu);
			}
		}

		// reset called from outside of update, apply reversed coordinates if needed
		private void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
		{
			if (!IsMe(self.player))
			{
				orig(self);
				return;
			}
			// switched behavior
			if (reverseGravity[self.player] && self.owner.room != null && !reversedPlayer[self.player])
			{
				Room room = self.owner.room;
				ReversePlayer(self.player, room);
				try
				{
					orig(self);
				}
				catch (Exception e) { Debug.LogException(e); }
				DeversePlayer(self.player, room);
			}
			else
			{
				orig(self);
			}
		}

		// used to reverse, now just catch exceptions to avoid invalid state
		private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
			if (!IsMe(self.player))
			{
				orig(self);
				return;
			}
			// switched behavior
			if (reverseGravity[self.player] && self.owner.room != null)// && !alreadyReversedPlayer[self.player])
			{
				//Room room = self.owner.room;
				// already reversed by player.update
				//ReversePlayer(self.player, room);
				try
				{
					orig(self);
				}
				catch (Exception e) { Debug.LogException(e); }
				//GraphicsModuleUpdated deverses it
				//DeversePlayer(self.player, room);
			}
			else
			{
				orig(self);
			}
		}

		// not initialized per instance, tryget,
        public static AttachedField<PlayerGraphics, Vector2> previousDraw = new AttachedField<PlayerGraphics, Vector2>();
		// draw things in the mirrored room!!!
		private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
			if (!self.owner.slatedForDeletetion && IsMe(self.player))
            {

                Vector2 center;
                if (previousDraw.TryGet(self, out Vector2 prevCam))
                {
					//deverse
                    center = new Vector2(prevCam.x / 2f, rCam.room.PixelHeight / 2 - prevCam.y);
					foreach (var s in sLeaser.sprites)
					{
						var rot = s.rotation;
						s.rotation = 0f;
						s.ScaleAroundPointRelative(s.ScreenToLocal(center), 1, -1);
						s.rotation -= rot;
					}
				}

				var reversed = reverseGravity[self.player];
				var pheight = rCam.room.PixelHeight;

				if (reversed)
				{
					foreach (var bp in self.bodyParts)
					{
						bp.pos = new Vector2(bp.pos.x, pheight - bp.pos.y);
						bp.lastPos = new Vector2(bp.lastPos.x, pheight - bp.lastPos.y);
						bp.vel.y *= -1;
					}
				}
				orig(self, sLeaser, rCam, timeStacker, camPos);
				if (reversed)
                {
					foreach (var bp in self.bodyParts)
					{
						bp.pos = new Vector2(bp.pos.x, pheight - bp.pos.y);
						bp.lastPos = new Vector2(bp.lastPos.x, pheight - bp.lastPos.y);
						bp.vel.y *= -1;
					}
				}
					

				if (reversed)
                {
                    center = new Vector2(camPos.x / 2f, rCam.room.PixelHeight / 2 - camPos.y);
                    foreach (var s in sLeaser.sprites)
                    {
                        var rot = s.rotation;
                        s.rotation = 0f;
                        s.ScaleAroundPointRelative(s.ScreenToLocal(center), 1, -1);
                        s.rotation -= rot;
                    }
					previousDraw[self] = camPos;
                }
                else
                {
					previousDraw.Unset(self);
				}
            }
            else
            {
				orig(self, sLeaser, rCam, timeStacker, camPos);
			}
		}

		// Initialize variables
        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
			orig(self, abstractCreature, world);
			if (!IsMe(self)) return;

			reverseGravity[self] = false;
			reversedPlayer[self] = false;
			forceStanding[self] = 0;
		}

		// Switch behavior, start inverted processing
		private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
			if (!IsMe(self))
            {
				orig(self, eu);
				return;
			}

			// switch trigger
			if (self.input[0].jmp && !self.input[1].jmp && self.canJump > 0 && self.room != null)
			{
				reverseGravity[self] = !reverseGravity[self];
				(self.graphicsModule as PlayerGraphics).Reset();
				self.canJump = 0;
				if (self.standing) forceStanding[self] = 5;
			}
			if (reverseGravity[self] && (self.dangerGrasp != null || (self.room != null && self.room.gravity == 0f)))
            {
				reverseGravity[self] = false;
			}

			// switched behavior
			if (reverseGravity[self] && self.room != null)
			{
				// could collapse this but would change exception behavior on normal run....
				Room room = self.room;
				float pheight = room.PixelHeight;
				ReversePlayer(self, room);
				try
				{
					orig(self, eu);
				}
				catch (Exception e){ Debug.LogException(e); }
				// die if too far oob upwards too
				// normally rooms with water would ignore this check (water bottom) but we still need to
				// coordinates still reversed here
				if (self.room != null && self.bodyChunks[0].pos.y  < -self.bodyChunks[0].restrictInRoomRange + 1f)
				{
					self.Die();
					self.Destroy();
				}

				if (self.slatedForDeletetion || self.room != room)
					DeversePlayer(self, room);
				// else un-needed because graphics will be updated and deverse happens on graphicsupdated
			}
			else
			{
				orig(self, eu);
			}

			// stand back up after swith+hitting the ceiling
			if(forceStanding[self] > 0)
            {
				self.standing = true;
				forceStanding[self]--;
			}

			
		}

		// flip player's perspective of room.
        private void ReversePlayer(Player self, Room room)
        {
			if (!reverseGravity[self] || reversedPlayer[self]) return;
			List<PhysicalObject> objs;
			reversedObjects[self] = objs = new List<PhysicalObject>();
			float pheight = room.PixelHeight;
			On.Room.GetTile_int_int += Flipped_GetTile;
			On.Room.shortcutData_IntVector2 += Flipped_shortcutData;
            On.Room.FloatWaterLevel += Room_FloatWaterLevel;
            On.Room.AddObject += Room_AddObject;
			chunkDetour.Apply();
			room.defaultWaterLevel = room.Height - 1 - room.defaultWaterLevel;
			//room.floatWaterLevel = room.PixelHeight - room.floatWaterLevel; // redundant and bad

			//self.buoyancy *= -1f;
			foreach (var c in self.bodyChunks)
			{
				c.pos = new Vector2(c.pos.x, pheight - c.pos.y);
				c.lastPos = new Vector2(c.lastPos.x, pheight - c.lastPos.y);
				c.lastLastPos = new Vector2(c.lastLastPos.x, pheight - c.lastLastPos.y);
				c.contactPoint.y *= -1;
				c.vel.y *= -1;
			}
			foreach (var g in self.grasps)
            {
                if (g != null && g.grabbed != null)
                {
                    foreach (var c in g.grabbed.bodyChunks)
                    {
                        c.pos = new Vector2(c.pos.x, pheight - c.pos.y);
                        c.lastPos = new Vector2(c.lastPos.x, pheight - c.lastPos.y);
                        c.lastLastPos = new Vector2(c.lastLastPos.x, pheight - c.lastLastPos.y);
                        c.contactPoint.y *= -1;
						c.vel.y *= -1;
						if (c.setPos != null) c.setPos = new Vector2(c.setPos.Value.x, pheight - c.setPos.Value.y);
					}
					//g.grabbed.gravity *= -1f;
					objs.Add(g.grabbed);
				}
            }
			if(self.graphicsModule is PlayerGraphics pg)
            {
                foreach (var bp in pg.bodyParts)
                {
					bp.pos = new Vector2(bp.pos.x, pheight - bp.pos.y);
					bp.lastPos = new Vector2(bp.lastPos.x, pheight - bp.lastPos.y);
					bp.vel.y *= -1;
				}
            }

			if (self.enteringShortCut != null) self.enteringShortCut = new RWCustom.IntVector2(self.enteringShortCut.Value.x, room.Height - 1 - self.enteringShortCut.Value.y);
			reversedPlayer[self] = true;
		}

        // ReversePlayer undo
        private void DeversePlayer(Player self, Room room)
		{
			if (!reverseGravity[self] || !reversedPlayer[self]) return;
			List<PhysicalObject> objs = reversedObjects[self];
			float pheight = room.PixelHeight;

			//self.buoyancy *= -1f;
			foreach (var c in self.bodyChunks)
			{
				c.pos = new Vector2(c.pos.x, pheight - c.pos.y);
				c.lastPos = new Vector2(c.lastPos.x, pheight - c.lastPos.y);
				c.lastLastPos = new Vector2(c.lastLastPos.x, pheight - c.lastLastPos.y);
				c.contactPoint.y *= -1;
				c.vel.y *= -1;
			}
			//foreach (var g in self.grasps)
   //         {
			//	if (g != null && g.grabbed != null && objs.Contains(g.grabbed))
   //             {
   //                 foreach (var c in g.grabbed.bodyChunks)
   //                 {
   //                     c.pos = new Vector2(c.pos.x, pheight - c.pos.y);
   //                     c.lastPos = new Vector2(c.lastPos.x, pheight - c.lastPos.y);
   //                     c.lastLastPos = new Vector2(c.lastLastPos.x, pheight - c.lastLastPos.y);
   //                     c.contactPoint.y *= -1;
			//			if (c.setPos != null) c.setPos = new Vector2(c.setPos.Value.x, pheight - c.setPos.Value.y);
			//		}
			//		g.grabbed.gravity *= -1f;
			//		objs.Remove(g.grabbed);
   //             }
   //         }
			foreach(var o in objs)
            {
				foreach (var c in o.bodyChunks)
				{
					c.pos = new Vector2(c.pos.x, pheight - c.pos.y);
					c.lastPos = new Vector2(c.lastPos.x, pheight - c.lastPos.y);
					c.lastLastPos = new Vector2(c.lastLastPos.x, pheight - c.lastLastPos.y);
					c.contactPoint.y *= -1;
					c.vel.y *= -1;
					if (c.setPos != null) c.setPos = new Vector2(c.setPos.Value.x, pheight - c.setPos.Value.y);
				}
				//o.gravity *= -1f;
			}

			if (self.graphicsModule is PlayerGraphics pg)
			{
				foreach (var bp in pg.bodyParts)
				{
					bp.pos = new Vector2(bp.pos.x, pheight - bp.pos.y);
					bp.lastPos = new Vector2(bp.lastPos.x, pheight - bp.lastPos.y);
					bp.vel.y *= -1;
				}
			}

			if (self.enteringShortCut != null) self.enteringShortCut = new RWCustom.IntVector2(self.enteringShortCut.Value.x, room.Height - 1 - self.enteringShortCut.Value.y);
			On.Room.GetTile_int_int -= Flipped_GetTile;
			On.Room.shortcutData_IntVector2 -= Flipped_shortcutData;
			On.Room.FloatWaterLevel -= Room_FloatWaterLevel;
			On.Room.AddObject -= Room_AddObject;
			chunkDetour.Undo();
			room.defaultWaterLevel = room.Height - 1 - room.defaultWaterLevel;
			//room.floatWaterLevel = room.PixelHeight - room.floatWaterLevel;

			objs.Clear();
			reversedObjects[self] = null;
			reversedPlayer[self] = false;
		}

		public delegate float Orig_BodyChunk_submersion(BodyChunk b);
		// Chunk 'submerged' inverted (water on top)
		// reflected over in ctor hook
		public float Flipped_submersion(Orig_BodyChunk_submersion orig, BodyChunk self)
		{
			return 1f - orig(self);
		}

		private float Room_FloatWaterLevel(On.Room.orig_FloatWaterLevel orig, Room self, float horizontalPos)
		{
			return self.PixelHeight - orig(self, horizontalPos);
		}

		private ShortcutData Flipped_shortcutData(On.Room.orig_shortcutData_IntVector2 orig, Room self, RWCustom.IntVector2 pos)
        {
			return orig(self, new RWCustom.IntVector2(pos.x, self.Height - 1 - pos.y));
		}

        private Room.Tile Flipped_GetTile(On.Room.orig_GetTile_int_int orig, Room self, int x, int y)
        {
			return orig(self, x, self.Tiles.GetLength(1) - 1 - y);
        }

		// Patchup objects placed by player
		private void Room_AddObject(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
		{
			if (obj is CosmeticSprite cs)
			{
				var ph = self.PixelHeight;
				cs.pos.y = ph - cs.pos.y;
				cs.lastPos.y = ph - cs.lastPos.y;
				cs.vel.y *= -1f;

				if(cs is WaterDrip wd)
                {
					wd.lastLastPos = wd.pos;
					wd.lastLastLastPos = wd.pos;
				}
			}

			orig(self, obj);
		}

		public static AttachedField<Player, bool> reverseGravity = new AttachedField<Player, bool>();
        public static AttachedField<Player, bool> reversedPlayer = new AttachedField<Player, bool>();
        public static AttachedField<Player, int> forceStanding = new AttachedField<Player, int>();
		public static AttachedField<Player, List<PhysicalObject>> reversedObjects = new AttachedField<Player, List<PhysicalObject>>();
    }
}
