using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Squiddy
{
    public partial class SquiddyBase
    {
		// don't break up on player removal
		private void AbstractPhysicalObject_Destroy(On.AbstractPhysicalObject.orig_Destroy orig, AbstractPhysicalObject self)
		{
			if (self is AbstractCreature ac && cicada.TryGet(ac, out var _))
			{
				return;
			}
			orig(self);
		}

		// comming out of den trips the newroom which trips roommic to reset
		private void RoomCamera_MoveCamera_Room_int(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera self, Room newRoom, int camPos)
		{
			if (cicada.TryGet(self.followAbstractCreature, out _) && newRoom == self.room) return;
			orig(self, newRoom, camPos);
		}

		// cicada pather gets confused about entering shortcuts, let our code handle that instead, also patchup zerog
		private void Cicada_Act1(ILContext il)
		{
			var c = new ILCursor(il);
			ILLabel dorun = null;
			ILLabel dontrun = null;
			if (c.TryGotoNext(MoveType.AfterLabel,
				i => i.MatchLdloc(0),
				i => i.MatchLdfld<MovementConnection>("type"),
				i => i.MatchLdcI4(13),
				i => i.MatchBeq(out dorun),
				i => i.MatchLdloc(0),
				i => i.MatchLdfld<MovementConnection>("type"),
				i => i.MatchLdcI4(14),
				i => i.MatchBneUn(out dontrun)
				))
			{
				c.MoveAfterLabels();
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<Cicada, bool>>((self) => // squiddy don't
				{
					if (player.TryGet(self.abstractCreature, out var ap) && ap.realizedCreature is Player p)
					{
						return true;
					}
					return false;
				});
				c.Emit(OpCodes.Brtrue, dontrun); // dont run if squiddy
			}
			else Debug.LogException(new Exception("Couldn't IL-hook Cicada_Act1 from squiddy for enteringshortcut")); // deffendisve progrmanig

			// patchup zerog
			c.Index = 0;
			while (c.TryGotoNext(MoveType.After,
				i => (i.MatchMul() && i.Previous.MatchLdcR4(out _)) || i.MatchLdcR4(out _),
				i => i.MatchLdarg(0),
				i => i.MatchLdfld<Cicada>("flyingPower"),
				i => i.MatchMul(),
				i => i.MatchLdarg(0),
				i => i.MatchLdfld<Cicada>("stamina"),
				i => i.MatchMul(),
				i => i.MatchAdd(),
				i => i.MatchStfld<Vector2>("y")
				))
            {
                c.Index -= 2;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit<PhysicalObject>(OpCodes.Callvirt, "get_gravity");
                c.Emit(OpCodes.Mul);
                c.Emit(OpCodes.Ldc_R4, (float)(1d / 0.9d));
                c.Emit(OpCodes.Mul);
			}
		}

		private Vector2? ShortcutHandler_OnScreenPositionOfInShortCutCreature(On.ShortcutHandler.orig_OnScreenPositionOfInShortCutCreature orig, ShortcutHandler self, Room room, Creature crit)
		{
			var r = orig(self, room, crit);
			if (r == null && crit is Player p && p.inShortcut && IsMe(p))
			{
				foreach (var e in room.abstractRoom.entitiesInDens)
				{
					if (e == p.abstractCreature)
					{
						return room.MiddleOfTile(room.LocalCoordinateOfNode(p.abstractCreature.pos.abstractNode).Tile);
					}
				}
			}
			return r;
		}

		// Die! Squiddy
		private void Cicada_Die(On.Cicada.orig_Die orig, Cicada self)
		{
			orig(self);
			if (player.TryGet(self.abstractCreature, out var ap) && ap.realizedCreature is Player p)
			{
				//Debug.LogError(Environment.StackTrace);
				if (!p.dead) p.Die();
			}
		}

		private void Player_Stun(On.Player.orig_Stun orig, Player self, int st)
		{
			if (cicada.TryGet(self.abstractCreature, out var ac) && ac.realizedCreature is Cicada c)
			{
				c.Stun(st);
			}
			orig(self, st);
		}

		private void Player_Die(On.Player.orig_Die orig, Player self)
		{
			orig(self);
			if (cicada.TryGet(self.abstractCreature, out var ac) && ac.realizedCreature is Cicada c && !c.dead)
			{
				c.Die();
			}
		}

		private bool AbstractCreature_WantToStayInDenUntilEndOfCycle(On.AbstractCreature.orig_WantToStayInDenUntilEndOfCycle orig, AbstractCreature self)
		{
			if (player.TryGet(self, out var ap))
			{
				return self.state.dead;
			}
			return orig(self);
		}

		private void AbstractCreature_Abstractize(On.AbstractCreature.orig_Abstractize orig, AbstractCreature self, WorldCoordinate coord)
		{
			if (player.TryGet(self, out var ap))
			{
				return; // do NOT abstractize unless I tell you to
						// could happen at end of cycle, isenteringden + low rain timer
			}
			orig(self, coord);
		}

		// players goes in shortcuts for better behavior
		// needed for arena sessions, otherwise cant enter shelter
		private void ShortcutHandler_SuckInCreature(On.ShortcutHandler.orig_SuckInCreature orig, ShortcutHandler self, Creature creature, Room room, ShortcutData shortCut)
		{
			if (player.TryGet(creature.abstractCreature, out var ap) && ap.realizedCreature is Player p && shortCut.shortCutType != ShortcutData.Type.CreatureHole)
			{
				creature = p;
			}
			orig(self, creature, room, shortCut);
		}

		// move tentacles properly
		private void CicadaGraphics_Update(On.CicadaGraphics.orig_Update orig, CicadaGraphics self)
		{
			orig(self);
			if (player.TryGet(self.cicada.abstractCreature, out var ap) && ap.realizedCreature is Player p)
			{
				// move tentacles properly
				for (int m = 0; m < 2; m++)
				{
					for (int n = 0; n < 2; n++)
					{
						Limb limb = self.tentacles[m, n];
						if (limb.mode == Limb.Mode.HuntAbsolutePosition)
						{
							// catch up properly like in relative hunt pos
							limb.pos += limb.connection.vel;
						}
					}
				}

				// update mark
				var pg = p.graphicsModule as PlayerGraphics;
				pg.lastMarkAlpha = pg.markAlpha;
				if (!p.dead && p.room.game.session is StoryGameSession && (p.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark)
				{
					pg.markAlpha = Custom.LerpAndTick(pg.markAlpha, Mathf.Clamp(Mathf.InverseLerp(30f, 80f, (float)p.touchedNoInputCounter) - UnityEngine.Random.value * Mathf.InverseLerp(80f, 30f, (float)p.touchedNoInputCounter), 0f, 1f), 0.1f, 0.033333335f);
				}
				else
				{
					pg.markAlpha = 0f;
				}

				// glow
				if (pg.lightSource != null)
				{
					pg.lightSource.stayAlive = true;
					pg.lightSource.setPos = new Vector2?(p.mainBodyChunk.pos);
					if (pg.lightSource.slatedForDeletetion || p.room.Darkness(p.mainBodyChunk.pos) == 0f)
					{
						pg.lightSource = null;
					}
				}
				else if (p.room.Darkness(p.mainBodyChunk.pos) > 0f && p.glowing)
				{
					pg.lightSource = new LightSource(p.mainBodyChunk.pos, false, Color.Lerp(new Color(1f, 1f, 1f), PlayerGraphics.SlugcatColor(p.playerState.slugcatCharacter), 0.5f), self.cicada);
					pg.lightSource.requireUpKeep = true;
					pg.lightSource.setRad = new float?(300f);
					pg.lightSource.setAlpha = new float?(1f);
					p.room.AddObject(pg.lightSource);
				}

				// looker
				if (p.room != null)
                {
					pg.objectLooker.Update();
					if (self.creatureLooker.lookCreature == null && pg.objectLooker.looking)
					{
						Vector2 vector = Custom.DirVec(self.cicada.bodyChunks[1].pos, self.cicada.mainBodyChunk.pos);
						self.lookDir = Custom.DirVec(self.cicada.bodyChunks[0].pos, pg.objectLooker.mostInterestingLookPoint);
						Vector2 vector2 = Custom.RotateAroundOrigo(self.lookDir, Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), new Vector2(-vector.x, vector.y)));
						self.lookRotation = Custom.AimFromOneVectorToAnother(-vector2, vector2);
					}
				}
			}
		}

		public AttachedField<CicadaGraphics, int> firstExtra = new AttachedField<CicadaGraphics, int>();
		private void CicadaGraphics_InitiateSprites(On.CicadaGraphics.orig_InitiateSprites orig, CicadaGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			orig(self, sLeaser, rCam);
			if (player.TryGet(self.cicada.abstractCreature, out var _))
			{
				var firstextra = firstExtra[self] = sLeaser.sprites.Length;
				Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 2);

				sLeaser.sprites[firstextra] = new FSprite("Futile_White", true) { shader = rCam.game.rainWorld.Shaders["FlatLight"] };
				sLeaser.sprites[firstextra + 1] = new FSprite("pixel", true) { scale = 5f };

				CicadaGraphics_AddToContainer_Impl(self, sLeaser, rCam, null);
			}
		}

		private void CicadaGraphics_AddToContainer(On.CicadaGraphics.orig_AddToContainer orig, CicadaGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			orig(self, sLeaser, rCam, newContatiner);
			if (player.TryGet(self.cicada.abstractCreature, out var _))
			{
				if (sLeaser.sprites.Length <= firstExtra[self] || firstExtra[self] == 0) return;
				CicadaGraphics_AddToContainer_Impl(self, sLeaser, rCam, newContatiner);
			}
		}

		private void CicadaGraphics_AddToContainer_Impl(CicadaGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			newContatiner = rCam.ReturnFContainer("Foreground"); // the mark goes there
			int firstextra = firstExtra[self];
			for (int i = firstextra; i < firstextra + 2; i++)
			{
				newContatiner.AddChild(sLeaser.sprites[i]);
			}
		}

		// Stronger color of body in arena
		private void CicadaGraphics_ApplyPalette(On.CicadaGraphics.orig_ApplyPalette orig, CicadaGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			orig(self, sLeaser, rCam, palette);

			if (player.TryGet(self.cicada.abstractCreature, out var ap) && ap.realizedCreature is Player p)
			{
				if (self.cicada.room?.world?.game.IsArenaSession ?? false)
				{
					// use 1:1 color no blending no filter
					Color bodyColor = p.ShortCutColor();
					var bodyHSL = RXColor.HSLFromColor(bodyColor);
					if (bodyHSL.l < 0.5) // stupig nighat color palete
					{
						if (self.cicada.gender)
						{
							self.cicada.gender = false;
							orig(self, sLeaser, rCam, palette); // what could go wrong
							self.cicada.gender = true;
						}
					}

					sLeaser.sprites[self.HighlightSprite].color = Color.Lerp(bodyColor, new Color(1f, 1f, 1f), 0.7f);
					sLeaser.sprites[self.BodySprite].color = bodyColor;
					sLeaser.sprites[self.HeadSprite].color = bodyColor;

					for (int side = 0; side < 2; side++)
						for (int wing = 0; wing < 2; wing++)
						{
							sLeaser.sprites[self.TentacleSprite(side, wing)].color = bodyColor;
						}
				}
			}
		}

		private void CicadaGraphics_DrawSprites(On.CicadaGraphics.orig_DrawSprites orig, CicadaGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			if (player.TryGet(self.cicada.abstractCreature, out var ap) && ap.realizedCreature is Player p)
			{
				var pg = p.graphicsModule as PlayerGraphics;
				var firstextra = firstExtra[self];
				Vector2 vector9 = Vector2.Lerp(self.cicada.bodyChunks[0].lastPos, self.cicada.bodyChunks[0].pos, timeStacker) +  self.zRotation * 30f + 
					(Vector2.Lerp(self.cicada.bodyChunks[0].lastPos, self.cicada.bodyChunks[0].pos, timeStacker) - Vector2.Lerp(self.cicada.bodyChunks[1].lastPos, self.cicada.bodyChunks[1].pos, timeStacker)).normalized * -15f 
					+ new Vector2(0f, 70f);
				var pos = vector9 - camPos;
				var light = sLeaser.sprites[firstextra];
				var mark = sLeaser.sprites[firstextra + 1];
				var alpha = Mathf.Lerp(pg.lastMarkAlpha, pg.markAlpha, timeStacker);
				mark.x = pos.x;
				mark.y = pos.y;
				mark.alpha = alpha;
				light.x = pos.x;
				light.y = pos.y;
				light.alpha = 0.2f * alpha;
				light.scale = 1f + alpha;
			}
		}

		// arena colors
		private void Cicada_InitiateGraphicsModule(On.Cicada.orig_InitiateGraphicsModule orig, Cicada self)
		{
			if (self.graphicsModule == null && player.TryGet(self.abstractCreature, out var ap) && ap.realizedCreature is Player p)
			{
				Debug.Log("Squiddy: InitiateGraphicsModule!");
				//self.flying = false; // shh

				if (self.abstractCreature.world.game.IsArenaSession)
				{
					// hsl math was failing to lerp to survivor-white somehow, had a red tint. no hue weight compensation for low sat colors
					var col = RXColor.HSLFromColor(p.ShortCutColor());
					var del = Mathf.Abs(col.h * col.s - self.iVars.color.hue * self.iVars.color.saturation) * col.s;
					//Debug.Log("got del " + del + " for p " + p.playerState.playerNumber);
					// hunter was getting this ugly purple result from blending cyan and red, looks way better if you shift the other way around, through green
					// this solution is somewhat 'fragile' but I doubt we're getting that many more characters in arena
					if (del > 0.5f) // too divergent, blend towards
					{
						//col.h = Mathf.Lerp(col.h, 0.33f, 0.66f);
						col.h = self.iVars.color.hue; // huh just jumping to the opposite side looked great on hunter
													  //self.iVars.color.hue = (self.iVars.color.hue + 0.33f) / 2f;
					}
					self.iVars.color = HSLColor.Lerp(self.iVars.color, new HSLColor(col.h, col.s, col.l), 0.5f * col.s); // magical * sat so white doesnt blend smh
				}
			}
			orig(self);
			if (self.graphicsModule is CicadaGraphics cg && player.TryGet(self.abstractCreature, out  _) )
			{
				cg.cullRange = 0;
			}
		}

		// arena color mixing patchup
		private void CicadaGraphics_ApplyPalette(ILContext il)
		{
			var c = new ILCursor(il);
			if (c.TryGotoNext(MoveType.AfterLabel,
				i => i.MatchStloc(0) // I hope this one is stable
				))
			{
				c.MoveAfterLabels();
				c.Emit(OpCodes.Ldarg_0);
				c.EmitDelegate<Func<Color, CicadaGraphics, Color>>((cin, self) => // MORE tone of body
				{
					if (player.TryGet(self.cicada.abstractCreature, out var ap) && ap.realizedCreature is Player p)
					{
						if (self.cicada.room?.world?.game.IsArenaSession ?? false)
						{
							var col = RXColor.HSLFromColor(p.ShortCutColor());
							return HSLColor.Lerp(self.iVars.color, new HSLColor(col.h, col.s, col.l), 0.8f * col.s).rgb;
						}
					}
					return cin;
				});
			}
			else Debug.LogException(new Exception("Couldn't IL-hook CicadaGraphics_ApplyPalette from squiddy")); // deffendisve progrmanig
		}

		// use cada color for shortcuts unless in arena
		private Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
		{
			if (cicada.TryGet(self.abstractCreature, out var ac) && ac.realizedCreature is Cicada c && !self.abstractCreature.world.game.IsArenaSession)
			{
				return c.ShortCutColor();
			}
			return orig(self);
		}

		float densNeededAmount;
		bool densNeeded;
		// show me dens
		private void ShortcutGraphics_GenerateSprites(On.ShortcutGraphics.orig_GenerateSprites orig, ShortcutGraphics self)
		{
			orig(self);
			for (int l = 0; l < self.room.shortcuts.Length; l++)
			{
				if (self.entranceSprites[l, 0] == null && self.room.shortcuts[l].shortCutType == ShortcutData.Type.CreatureHole)
				{
					self.entranceSprites[l, 0] = new FSprite("ShortcutArrow", true);
					self.entranceSprites[l, 0].rotation = Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), -IntVector2.ToVector2(self.room.ShorcutEntranceHoleDirection(self.room.shortcuts[l].StartTile)));
					self.entranceSpriteLocations[l] = self.room.MiddleOfTile(self.room.shortcuts[l].StartTile) + IntVector2.ToVector2(self.room.ShorcutEntranceHoleDirection(self.room.shortcuts[l].StartTile)) * 15f;
					if (self.room.water && self.room.waterInFrontOfTerrain && self.room.PointSubmerged(self.entranceSpriteLocations[l] + new Vector2(0f, 5f)))
					{
						self.camera.ReturnFContainer("Items").AddChild(self.entranceSprites[l, 0]);
					}
					else
					{
						self.camera.ReturnFContainer("Shortcuts").AddChild(self.entranceSprites[l, 0]);
						self.camera.ReturnFContainer("Water").AddChild(self.entranceSprites[l, 1]);
					}
				}
			}
		}

		// draw dens when squiddy needs them
		private void ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
		{
			orig(self, timeStacker, camPos);
			for (int k = 0; k < self.entranceSprites.GetLength(0); k++)
			{
				if (self.entranceSprites[k, 0] != null && self.room.shortcuts[k].shortCutType == ShortcutData.Type.CreatureHole)
				{
					float t = 1 - densNeededAmount;
					self.entranceSprites[k, 0].color = Color.Lerp(self.entranceSprites[k, 0].color, self.camera.currentPalette.blackColor, t);
					self.entranceSprites[k, 1].color = Color.Lerp(self.entranceSprites[k, 0].color, self.camera.currentPalette.blackColor, t);
					self.entranceSprites[k, 1].alpha = Mathf.Lerp(self.entranceSprites[k, 1].alpha, 0, t);
				}
			}
		}

		// tick dens needed amount
		private void ShortcutGraphics_Update(On.ShortcutGraphics.orig_Update orig, ShortcutGraphics self)
		{
			orig(self);
			densNeededAmount = Custom.LerpAndTick(densNeededAmount, densNeeded ? 1f : 0f, 0.015f, 0.015f);
			densNeeded = false; // set every frame
		}

		// bugfix the gmae
		// 2 connected creatures leaving den = both added twice to the room.
		private void AbstractCreature_IsExitingDen(On.AbstractCreature.orig_IsExitingDen orig, AbstractCreature self)
		{
			//        if (Input.GetKey("l"))
			//        {
			//Debug.Log(Environment.StackTrace);
			//        }

			if (player.TryGet(self, out var ap) && ap.realizedCreature is Player p && self.realizedCreature != null) // if its us pulling things out of den, pull them in and prevent other creatures from being added to shortcuts
			{
				var room = self.Room;
				// from Abstractphisicalobjects but with changes so not to run isexitingden because that puts creatures in shortcuts
				// the alternative would be to intercept ALL calls to shortcuthandler.enteringfromabstractroom and that'd be a lot more code running
				foreach (var ent in self.GetAllConnectedObjects())
				{
					if (ent != self && ent.InDen)
					{
						ent.InDen = false;
						room.entitiesInDens.Remove(ent);
						room.AddEntity(ent);
					}
				}
			}
			orig(self);
		}

		// skip me for playerpushbacks please
		private void ShortcutHelper_Update(ILContext il)
		{
			var c = new ILCursor(il);
			ILLabel innerloop = null;
			ILLabel afterloop = null;
			int itercount = 0;
			MethodReference itemGetCall = null;
			int playerloc = 0;
			if (c.TryGotoNext(MoveType.Before,
				i => i.MatchLdarg(0),
				i => i.MatchLdfld<ShortcutHelper>("pushers"),
				i => i.MatchLdloc(out itercount),
				i => i.MatchCallOrCallvirt(out itemGetCall),
				i => i.MatchLdflda<ShortcutHelper.ShortcutPusher>("shortcutDir"),
				i => i.MatchLdfld<RWCustom.IntVector2>("y"),
				i => i.MatchLdcI4(0),
				i => i.MatchBle(out innerloop),

				i => i.MatchLdloc(out playerloc),
				i => i.MatchLdfld<Player>("animation"),
				i => i.MatchLdcI4(19),
				i => i.MatchBeq(out afterloop),
				i => i.MatchLdloc(out _),
				i => i.MatchLdfld<Player>("animation"),
				i => i.MatchLdcI4(3),
				i => i.MatchBeq(out _)
				))
			{
				c.MoveAfterLabels();
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldarg_0);
				c.Emit<ShortcutHelper>(OpCodes.Ldfld, "pushers");
				c.Emit(OpCodes.Ldloc, itercount);
				c.Emit(OpCodes.Callvirt, itemGetCall);
				c.Emit(OpCodes.Ldloc, playerloc);
				c.EmitDelegate<Func<ShortcutHelper, ShortcutHelper.ShortcutPusher, Player, bool>>((helper, pusher, self) => // bypass me, pushbacks
				{
					return IsMe(self) && helper.room.shortcutData(pusher.shortCutPos).shortCutType == ShortcutData.Type.CreatureHole;
				});
				c.Emit(OpCodes.Brtrue, afterloop);
			}
			else Debug.LogException(new Exception("Couldn't IL-hook ShortcutHelper_Update from squiddy")); // deffendisve progrmanig
		}

		// what do you think i am stupid
		private void SuperJumpInstruction_ctor(On.SuperJumpInstruction.orig_ctor orig, SuperJumpInstruction self, Room room, PlacedObject placedObject)
		{
			orig(self, room, placedObject);
			if (IsMe(room.game)) self.Destroy();
		}

		// remove squiddy from save.
		private void RegionState_AdaptRegionStateToWorld(On.RegionState.orig_AdaptRegionStateToWorld orig, RegionState self, int playerShelter, int activeGate)
		{
			for (int i = 0; i < self.world.NumberOfRooms; i++)
			{
				AbstractRoom abstractRoom = self.world.GetAbstractRoom(self.world.firstRoomIndex + i);
				//if (abstractRoom.index == activeGate) continue;
				for (int j = abstractRoom.entities.Count - 1; j >= 0; j--)
				{
					if (abstractRoom.entities[j] is AbstractCreature ac && player.TryGet(ac, out var ap) && ap.realizedCreature is Player p)
					{
						abstractRoom.RemoveEntity(ac);
						Debug.Log("Squiddy: removed from save");
					}
				}
				for (int j = abstractRoom.entitiesInDens.Count - 1; j >= 0; j--)
				{
					if (abstractRoom.entitiesInDens[j] is AbstractCreature ac && player.TryGet(ac, out var ap) && ap.realizedCreature is Player p)
					{
						abstractRoom.RemoveEntity(ac);
						Debug.Log("Squiddy: removed from save");
					}
				}
			}

			orig(self, playerShelter, activeGate);
		}
	}
}