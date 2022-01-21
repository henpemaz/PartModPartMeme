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
		public override string Description => @"Not for the weak of mind. In fact, only few can handle such power and not be overwhelmed by it.";

		protected override void Disable()
		{
			On.Player.ctor -= Player_ctor;
			On.Player.Update -= Player_Update;
		}

		protected override void Enable()
		{
			On.Player.ctor += Player_ctor;
			On.Player.Update += Player_Update;
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
			halo[self] = new WeakReference(null);
		}

		public void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
		{
			orig(self, eu);
			if (!IsMe(self)) return;
			if (self.room is null) return; // into shortcut that frame.

			Vector2 a = Vector2.Lerp(lastMousePos[self], (Vector2)Input.mousePosition + self.room.game.cameras[0].pos, 0.3f);
			lastMousePos[self] = a;
			if (!self.dead && Input.GetMouseButton(0))
			{
				if (isGrabbing[self])
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
						self.Blink(30);
					}
				}
				else
				{
					grabbedObject[self].Target = null;
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
									grabbedObject[self].Target = physicalObject;
									num = num2;
								}
							}
						}
					}
					if(grabbedObject[self].Target != null)
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
						}
					}
				}
				if (halo[self].Target<KineticatHalo>().room != self.room)
				{
					halo[self].Target<KineticatHalo>().RemoveFromRoom();
					self.room.AddObject(halo[self].Target<KineticatHalo>());
				}
			}
			else
			{
				grabbedTimer[self] = Mathf.MoveTowards(grabbedTimer[self], 0f, 0.033333335f);
				isGrabbing[self] = false;
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

		public static float grabRange = 40f;

		public static AttachedField<Player, Vector2> lastMousePos = new AttachedField<Player, Vector2>();

		private static AttachedField<Player, float> boltTime = new AttachedField<Player, float>();

		private static float maxBoltTime = 0.3f;

		private static float minBoltTime = 0.01f;

		public static AttachedField<Player, float> grabbedTimer = new AttachedField<Player, float>();

		public static AttachedField<Player, WeakReference> halo = new AttachedField<Player, WeakReference>();
	}
}