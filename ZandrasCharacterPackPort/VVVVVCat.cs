using System;
using System.Collections.Generic;
using System.Linq;
using MonoMod.RuntimeDetour;
using SlugBase;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	internal class VVVVVCat : SlugBaseCharacter
	{
		public VVVVVCat() : base("zcpVVVVVcat", FormatVersion.V1, 0, true) {
			chunkDetour = new Hook(typeof(BodyChunk).GetProperty("submersion").GetGetMethod(), typeof(VVVVVCat).GetMethod("Flipped_submersion"), this);
			chunkDetour.Undo();
		}
		public override string DisplayName => "The VVVVV";
		public override string Description => @"An unstable prototype, created for reaching places no other slugcat ever reached.";

		// this was a lot more complicated than it should have been.
		protected override void Disable()
		{
			On.Player.ctor -= Player_ctor;
			On.Player.Update -= Player_Update;
            On.Player.GraphicsModuleUpdated -= Player_GraphicsModuleUpdated;
			On.PlayerGraphics.Update -= PlayerGraphics_Update;
			On.PlayerGraphics.InitiateSprites -= PlayerGraphics_InitiateSprites;
			On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
            On.PlayerGraphics.Reset -= PlayerGraphics_Reset;
		}

        protected override void Enable()
		{
			On.Player.ctor += Player_ctor;
			On.Player.Update += Player_Update;
			On.Player.GraphicsModuleUpdated += Player_GraphicsModuleUpdated;
			On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
			On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;
		}

        private void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
			if (IsMe(self.player))
			{
				previousDraw.Unset(self);
			}
			orig(self, sLeaser, rCam);
		}

        private void Player_GraphicsModuleUpdated(On.Player.orig_GraphicsModuleUpdated orig, Player self, bool actuallyViewed, bool eu)
		{
			if (!IsMe(self))
			{
				orig(self, actuallyViewed, eu);
				return;
			}
			// switched behavior
			if (reverseGravity[self] && self.room != null && !alreadyReversedPlayer[self])
			{
				Room room = self.room;
				ReversePlayer(self, room);
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

		private void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
		{
			if (!IsMe(self.player))
			{
				orig(self);
				return;
			}
			// switched behavior
			if (reverseGravity[self.player] && self.owner.room != null && !alreadyReversedPlayer[self.player])
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

		private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
			if (!IsMe(self.player))
			{
				orig(self);
				return;
			}
			// switched behavior
			if (reverseGravity[self.player] && self.owner.room != null && !alreadyReversedPlayer[self.player])
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

        public static AttachedField<PlayerGraphics, Vector2> previousDraw = new AttachedField<PlayerGraphics, Vector2>();
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
				// seems uneccessary
                //float pheight = self.owner.room.PixelHeight;
                //foreach (var c in self.owner.bodyChunks)
                //{
                //	c.pos = new Vector2(c.pos.x, pheight - c.pos.y);
                //	c.lastPos = new Vector2(c.lastPos.x, pheight - c.lastPos.y);
                //	c.lastLastPos = new Vector2(c.lastLastPos.x, pheight - c.lastLastPos.y);
                //	c.contactPoint.y *= -1;
                //}
                orig(self, sLeaser, rCam, timeStacker, camPos);
                //foreach (var c in self.owner.bodyChunks)
                //{
                //	c.pos = new Vector2(c.pos.x, pheight - c.pos.y);
                //	c.lastPos = new Vector2(c.lastPos.x, pheight - c.lastPos.y);
                //	c.lastLastPos = new Vector2(c.lastLastPos.x, pheight - c.lastLastPos.y);
                //	c.contactPoint.y *= -1;
                //}
                if (reverseGravity[self.player])
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

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
			orig(self, abstractCreature, world);
			if (!IsMe(self)) return;

			reverseGravity[self] = false;
			alreadyReversedPlayer[self] = false;
			forceStanding[self] = 0;
		}

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
				DeversePlayer(self, room);
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

			// die if too far oob
			if (self.room != null && self.bodyChunks[0].pos.y > self.room.PixelHeight + self.bodyChunks[0].restrictInRoomRange - 1f)
			{
				self.Die();
				self.Destroy();
			}
		}

        private void ReversePlayer(Player self, Room room)
        {
			if (!reverseGravity[self] || alreadyReversedPlayer[self]) return;
			List<PhysicalObject> objs;
			reversedObjects[self] = objs = new List<PhysicalObject>();
			float pheight = room.PixelHeight;
			On.Room.GetTile_3 += Flipped_GetTile;
			On.Room.shortcutData += Flipped_shortcutData;
            On.Room.FloatWaterLevel += Room_FloatWaterLevel;
			chunkDetour.Apply();
			//MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify() /// maaaaan
			foreach (var c in self.bodyChunks)
			{
				c.pos = new Vector2(c.pos.x, pheight - c.pos.y);
				c.lastPos = new Vector2(c.lastPos.x, pheight - c.lastPos.y);
				c.lastLastPos = new Vector2(c.lastLastPos.x, pheight - c.lastLastPos.y);
				c.contactPoint.y *= -1;
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
						if(c.setPos != null) c.setPos = new Vector2(c.setPos.Value.x, pheight - c.setPos.Value.y);
					}
					objs.Add(g.grabbed);
				}
            }
            if (self.enteringShortCut != null) self.enteringShortCut = new RWCustom.IntVector2(self.enteringShortCut.Value.x, room.Height - 1 - self.enteringShortCut.Value.y);
			alreadyReversedPlayer[self] = true;
		}

		public delegate float Orig_BodyChunk_submersion(BodyChunk b);
		// reflected over in ctor hook
		public float Flipped_submersion(Orig_BodyChunk_submersion orig, BodyChunk self)
        {
			return 1f - orig(self);
        }


		private float Room_FloatWaterLevel(On.Room.orig_FloatWaterLevel orig, Room self, float horizontalPos)
        {
			return self.PixelHeight - orig(self, horizontalPos);
        }

        private void DeversePlayer(Player self, Room room)
		{
			if (!reverseGravity[self] || !alreadyReversedPlayer[self]) return;
			List<PhysicalObject> objs = reversedObjects[self];
			float pheight = room.PixelHeight;
			foreach (var c in self.bodyChunks)
			{
				c.pos = new Vector2(c.pos.x, pheight - c.pos.y);
				c.lastPos = new Vector2(c.lastPos.x, pheight - c.lastPos.y);
				c.lastLastPos = new Vector2(c.lastLastPos.x, pheight - c.lastLastPos.y);
				c.contactPoint.y *= -1;
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
						if (c.setPos != null) c.setPos = new Vector2(c.setPos.Value.x, pheight - c.setPos.Value.y);
					}
					objs.Remove(g.grabbed);
                }
            }
			foreach(var o in objs)
            {
				foreach (var c in o.bodyChunks)
				{
					c.pos = new Vector2(c.pos.x, pheight - c.pos.y);
					c.lastPos = new Vector2(c.lastPos.x, pheight - c.lastPos.y);
					c.lastLastPos = new Vector2(c.lastLastPos.x, pheight - c.lastLastPos.y);
					c.contactPoint.y *= -1;
					if (c.setPos != null) c.setPos = new Vector2(c.setPos.Value.x, pheight - c.setPos.Value.y);
				}
			}
            if (self.enteringShortCut != null) self.enteringShortCut = new RWCustom.IntVector2(self.enteringShortCut.Value.x, room.Height - 1 - self.enteringShortCut.Value.y);
			On.Room.GetTile_3 -= Flipped_GetTile;
			On.Room.shortcutData -= Flipped_shortcutData;
			On.Room.FloatWaterLevel -= Room_FloatWaterLevel;
			chunkDetour.Undo();
			objs.Clear();
			reversedObjects[self] = null;
			alreadyReversedPlayer[self] = false;
		}


		private ShortcutData Flipped_shortcutData(On.Room.orig_shortcutData orig, Room self, RWCustom.IntVector2 pos)
        {
			return orig(self, new RWCustom.IntVector2(pos.x, self.Height - 1 - pos.y));
		}

        private Room.Tile Flipped_GetTile(On.Room.orig_GetTile_3 orig, Room self, int x, int y)
        {
			return orig(self, x, self.Tiles.GetLength(1) - 1 - y);
        }

        public static AttachedField<Player, bool> reverseGravity = new AttachedField<Player, bool>();
        public static AttachedField<Player, bool> alreadyReversedPlayer = new AttachedField<Player, bool>();
        public static AttachedField<Player, int> forceStanding = new AttachedField<Player, int>();
		public static AttachedField<Player, List<PhysicalObject>> reversedObjects = new AttachedField<Player, List<PhysicalObject>>();
        private Hook chunkDetour;
    }
}
