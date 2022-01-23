using System;
using System.Collections.Generic;
using RWCustom;
using SlugBase;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	public class Kineticat : SlugBaseCharacter
	{
		public Kineticat() : base("zcpkineticat", FormatVersion.V1, 0, true) { }
		public override string DisplayName => "The Psychic";
		public override string Description => @"Not for the weak of mind.
In fact, only few can handle such power and not be overwhelmed by it.";

		public override bool HasGuideOverseer => false;
		public override string StartRoom => "SB_J04";
		public override void StartNewGame(Room room)
		{
			if (room.abstractRoom.name == StartRoom)
			{
				if (room.game.Players.Count > 0)
				{
					var p = room.game.Players[0];
					p.pos = new WorldCoordinate(room.abstractRoom.index, 165, 31, -1);
				}
				if (room.game.Players.Count > 1)
				{
					var p = room.game.Players[1];
					p.pos = new WorldCoordinate(room.abstractRoom.index, 172, 24, -1);
				}
				if (room.game.Players.Count > 2)
				{
					var p = room.game.Players[2];
					p.pos = new WorldCoordinate(room.abstractRoom.index, 165, 24, -1);
				}
				if (room.game.Players.Count > 3)
				{
					var p = room.game.Players[3];
					p.pos = new WorldCoordinate(room.abstractRoom.index, 159, 24, -1);
				}
			}
			if (room.game.IsStorySession)
			{
                //room.AddObject(new Messenger());
                if (room.game.Players.Count > 0)
                {
					room.game.cameras[0].followAbstractCreature = null;
					room.AddObject(new CameraMan(8));
				}

				room.game.rainWorld.progression.miscProgressionData.SaveDiscoveredShelter("SB_S01");

				// saveState.deathPersistentSaveData.karma = saveState.deathPersistentSaveData.karmaCap;

				var survivor = room.game.GetStorySession.saveState.deathPersistentSaveData.winState.GetTracker(WinState.EndgameID.Survivor, true) as WinState.IntegerTracker;
				survivor.SetProgress(survivor.max);
				survivor.lastShownProgress = survivor.progress;
			}
		}

		protected override void Disable()
		{
			On.Player.ctor -= Player_ctor;
			On.Player.Update -= Player_Update;
		}

		protected override void Enable()
		{
			On.Player.ctor += Player_ctor;
			On.Player.Update += Player_Update;

            On.Creature.TerrainImpact += Creature_TerrainImpact;
		}

        private void Creature_TerrainImpact(On.Creature.orig_TerrainImpact orig, Creature self, int chunk, IntVector2 direction, float speed, bool firstContact)
        {
			orig(self, chunk, direction, speed, firstContact);
			if (self.room == null || !firstContact || speed < self.impactTreshhold || speed < 2f) return;
            foreach (var p in self.room.game.Players )
            {
				if (p.realizedCreature is Player pp && isGrabbing[pp] && grabbedObject[pp].Target == self)
                {
					//if (self is Player) speed *= 0.5f;
					self.Violence(null, null, self.bodyChunks[chunk], null, Creature.DamageType.Blunt, speed * impactDamage, 0f);
					isGrabbing[pp] = false;
					grabbedObject[pp].Target = null;
					grabbingCooldown[pp] = 30;
				}
			}
        }

        protected override void GetStats(SlugcatStats stats)
        {
            base.GetStats(stats);
			stats.runspeedFac *= 0.8f;
			stats.poleClimbSpeedFac *= 0.8f;
		}

        private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
		{
			orig(self, abstractCreature, world);
			if (!IsMe(self)) return;

			isGrabbing[self] = false;
			grabbedObject[self] = new WeakReference(null);
			grabbedChunk[self] = 0;
			lastMousePos[self] = Vector2.zero;
			boltTime[self] = 0.1f;
			grabbedTimer[self] = 0;
			grabbingCooldown[self] = 0;
			halo[self] = new WeakReference(null);
		}

		public void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
		{
			orig(self, eu);
			if (!IsMe(self)) return;
			//Debug.Log("a");
			if (self.room?.game?.cameras is null) return; // into shortcut that frame.
			//Debug.Log("b");

			if (grabbingCooldown[self] > 0) grabbingCooldown[self]--;
			//Debug.Log("c");

			Vector2 a = Vector2.Lerp(lastMousePos[self], (Vector2)Input.mousePosition + self.room.game.cameras[0].pos, 0.3f);
			lastMousePos[self] = a;
			//Debug.Log("d");
			if (!self.dead && self.Consious && grabbingCooldown[self] <= 0 && Input.GetMouseButton(0))
			{
				if (isGrabbing[self] && grabbedObject[self].Target != null)
				{
					if (grabbedObject[self].Target<PhysicalObject>().room != self.room) isGrabbing[self] = false;
					else
					{
						boltTime[self] -= 0.033333335f;
						if (boltTime[self] <= 0f)
						{
							LightningBolt obj = new LightningBolt(halo[self].Target<KineticatHalo>().Center(0f) + Custom.DegToVec(UnityEngine.Random.value * 360f) * halo[self].Target<KineticatHalo>().Radius(2f, 0f), grabbedObject[self].Target<PhysicalObject>().bodyChunks[grabbedChunk[self]].pos + UnityEngine.Random.insideUnitCircle * 5f, UnityEngine.Random.Range(0.3f, 0.1f), Color.white);
							self.room.AddObject(obj);
							boltTime[self] = UnityEngine.Random.Range(minBoltTime, maxBoltTime);
							self.room.PlaySound(SoundID.SS_AI_Halo_Connection_Light_Up, self.mainBodyChunk, false, 3f, 1f, false);
						}
						grabbedTimer[self] = Mathf.MoveTowards(grabbedTimer[self], 1f, 0.033333335f);
						grabbedObject[self].Target<PhysicalObject>().bodyChunks[grabbedChunk[self]].vel = (a - grabbedObject[self].Target<PhysicalObject>().bodyChunks[grabbedChunk[self]].pos) / 2f;
						if (grabbedObject[self].Target<PhysicalObject>() is Creature c) c.Stun(c.stun + 2);
						self.Blink(30);
					}
				}
				else
				{
					isGrabbing[self] = false;
					grabbedObject[self].Target = null;
					if(self.stun < 2)
                    {
						float num = float.PositiveInfinity;
						List<PhysicalObject>[] physicalObjects = self.room.physicalObjects;
						for (int i = 0; i < physicalObjects.Length; i++)
						{
							foreach (PhysicalObject physicalObject in physicalObjects[i])
							{
								if (physicalObject.bodyChunks.Length != 0)
								{
									float num2 = Vector2.Distance(a, physicalObject.bodyChunks[0].pos);
									if (num2 < num)
									{
										num = num2;

										grabbedObject[self].Target = physicalObject;
									}
								}
							}
						}
						if (grabbedObject[self].Target != null)
						{
							if (grabbedObject[self].Target<PhysicalObject>().grabbedBy != null && grabbedObject[self].Target<PhysicalObject>().grabbedBy.Count > 0 && grabbedObject[self].Target<PhysicalObject>().grabbedBy[0] != null)
							{
								grabbedObject[self].Target = grabbedObject[self].Target<PhysicalObject>().grabbedBy[0].grabber;
								grabbedChunk[self] = 0;
							}
							if (num < grabRange)
							{
								grabbedChunk[self] = 0;
								for (int j = 1; j < grabbedObject[self].Target<PhysicalObject>().bodyChunks.Length; j++)
								{
									float num3 = Vector2.Distance(a, grabbedObject[self].Target<PhysicalObject>().bodyChunks[j].pos);
									if (num3 < num)
									{
										grabbedChunk[self] = j;
										num = num3;
									}
								}
								isGrabbing[self] = true;
								boltTime[self] = UnityEngine.Random.Range(minBoltTime, maxBoltTime);
								if (halo[self].Target == null)
								{
									var tmphalo = new KineticatHalo(self, 0);
									halo[self].Target = tmphalo;
									self.room.AddObject(tmphalo);
								}
                            }else{
								grabbedObject[self].Target = null;
							}
						}
					}
					
				}
				if (halo[self].Target<KineticatHalo>() != null && halo[self].Target<KineticatHalo>().room != self.room)
				{
					halo[self].Target<KineticatHalo>().RemoveFromRoom();
					self.room.AddObject(halo[self].Target<KineticatHalo>());
				}
			}
			else
			{
				grabbedTimer[self] = Mathf.MoveTowards(grabbedTimer[self], 0f, 0.033333335f);
				isGrabbing[self] = false;
			//Debug.Log("e");
				//if(grabbedObject[self] is null) Debug.Log("wtf");
				grabbedObject[self].Target = null;
				//Debug.Log("f");
			}
		}

		public override Color? SlugcatColor(int slugcatCharacter, Color baseColor)
		{
			Color col = new Color(0.6f, 0f, 0.6f);

			if (slugcatCharacter == -1)
				return col;
			else
				return Color.Lerp(baseColor, col, 0.75f);
		}

		public static AttachedField<Player, bool> isGrabbing = new AttachedField<Player, bool>();

		public static AttachedField<Player, WeakReference> grabbedObject = new AttachedField<Player, WeakReference>();

		public static AttachedField<Player, int> grabbedChunk = new AttachedField<Player, int>();
		public static AttachedField<Player, int> grabbingCooldown = new AttachedField<Player, int>();

		public static float grabRange = 40f;

		public static AttachedField<Player, Vector2> lastMousePos = new AttachedField<Player, Vector2>();

		private static AttachedField<Player, float> boltTime = new AttachedField<Player, float>();

		private static float maxBoltTime = 0.3f;

		private static float minBoltTime = 0.01f;

		public static AttachedField<Player, float> grabbedTimer = new AttachedField<Player, float>();

		public static AttachedField<Player, WeakReference> halo = new AttachedField<Player, WeakReference>();
		public static float impactDamage = 0.05f;
    }
}