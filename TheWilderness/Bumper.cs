using RWCustom;
using System;
using System.Reflection;
using UnityEngine;

namespace TheWilderness
{
    public class Bumper : UpdatableAndDeletable
    {
        private IntRect rect;
        private FloatRect floatRect;

        internal static void Apply()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("Bumber", typeof(Bumper), typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation)));
        }

        public Bumper(PlacedObject pObj)
        {
            this.rect = (pObj.data as PlacedObject.GridRectObjectData).Rect;
            this.floatRect = new FloatRect((float)this.rect.left * 20f, (float)this.rect.bottom * 20f, (float)this.rect.right * 20f + 20f, (float)this.rect.top * 20f + 20f);
        }

		public override void Update(bool eu)
		{
			base.Update(eu);
			for (int i = 0; i < this.room.physicalObjects.Length; i++)
			{
				for (int j = 0; j < this.room.physicalObjects[i].Count; j++)
				{
					for (int k = 0; k < this.room.physicalObjects[i][j].bodyChunks.Length; k++)
					{
						var bc = this.room.physicalObjects[i][j].bodyChunks[k];
						if ((bc.ContactPoint.y != 0) || (bc.ContactPoint.x != 0))
						{
							//Vector2 a = this.room.physicalObjects[i][j].bodyChunks[k].ContactPoint.ToVector2().normalized;
							//Vector2 v = this.room.physicalObjects[i][j].bodyChunks[k].pos + a * (this.room.physicalObjects[i][j].bodyChunks[k].rad + 1f);
							//Vector2 v1 = this.room.physicalObjects[i][j].bodyChunks[k].pos + a * (this.room.physicalObjects[i][j].bodyChunks[k].rad + 10f);
							//Vector2 v2 = this.room.physicalObjects[i][j].bodyChunks[k].pos + a * (this.room.physicalObjects[i][j].bodyChunks[k].rad + 20f);
							//if (this.floatRect.Vector2Inside(v) || this.floatRect.Vector2Inside(v1) || this.floatRect.Vector2Inside(v2))
							//{
							//	//this.room.AddObject(new ZapCoil.ZapFlash(this.room.physicalObjects[i][j].bodyChunks[k].pos + a * this.room.physicalObjects[i][j].bodyChunks[k].rad, Mathf.InverseLerp(-0.05f, 15f, this.room.physicalObjects[i][j].bodyChunks[k].rad)));
							//	this.room.physicalObjects[i][j].bodyChunks[k].vel -= (a * 6f + Custom.RNV() * UnityEngine.Random.value) / this.room.physicalObjects[i][j].bodyChunks[k].mass;
							//	this.room.PlaySound(SoundID.Rock_Hit_Creature, this.room.physicalObjects[i][j].bodyChunks[k].pos, 1f, 1f);
							//	Debug.Log("Bonk!");
							//}
							var tempRect = floatRect;
							if (tempRect.Grow(bc.rad + 2f).Vector2Inside(bc.pos))
                            {
								Vector2 a = bc.ContactPoint.ToVector2().normalized;
								bc.vel -= (a * 6f + Custom.RNV() * UnityEngine.Random.value) / bc.mass;
                                this.room.PlaySound(SoundID.Rock_Hit_Creature, bc.pos, 1f, 1f);
                                Debug.Log("Bonk! contact point was " + bc.contactPoint.ToString());
                            }
                        }
					}
				}
			}
		}
	}
}
