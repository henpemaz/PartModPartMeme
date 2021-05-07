using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ConcealedGarden
{
    public static class OrganicShelter
    {
        public class OrganicLock : UpdatableAndDeletable, IDrawable, ShelterBehaviors.IReactToShelterClosing
        {
            private readonly PlacedObject pObj;
            private readonly RainCycle rainCycle;
            private float closedFac;
            private float closeSpeed;
            ManagedPlacedObjects.PlacedObjectsManager.ManagedData data;

            private float openUpTicks = 350f;
            private float initialWait = 80f;
            private Vector2 dir;
            private Vector2 perp;
            private float rad;
            private float excitement;
            private DisembodyedPart[] parts;
            Vector2[] target;
            List<Creature> creaturesInRoom;


            public OrganicLock(Room room, PlacedObject pObj)
            {
                this.room = room;
                this.pObj = pObj;
                this.data = pObj.data as ManagedPlacedObjects.PlacedObjectsManager.ManagedData;

                this.rainCycle = room.world.rainCycle;
                if (this.Broken)
                {
                    
                }
                
                if (this.rainCycle == null)
                {
                    this.closedFac = 1f;
                    this.closeSpeed = 1f;
                }
                else
                {
                    this.closedFac = ((!room.game.setupValues.cycleStartUp) ? 1f : Mathf.InverseLerp(this.initialWait + this.openUpTicks, this.initialWait, (float)this.rainCycle.timer));
                    this.closeSpeed = -1f;
                }
                this.dir = data.GetValue<Vector2>("h");
                this.rad = dir.magnitude;
                this.perp = RWCustom.Custom.PerpendicularVector(this.dir);

                target = new Vector2[2];
                this.parts = new DisembodyedPart[2] { new DisembodyedPart(0.94f, pObj.pos), new DisembodyedPart(0.94f, pObj.pos) };
                creaturesInRoom = new List<Creature>();

                for (int i = 0; i < target.Length; i++)
                {
                    this.target[i] = pObj.pos + (i == 0 ? 1f : -1f) * perp * rad * (1 - closedFac);
                    DisembodyedPart part = parts[i];
                    part.Reset(target[i]);
                }
            }

            public override void Update(bool eu)
            {
                base.Update(eu);

                creaturesInRoom.Clear();
                foreach (var upd in room.updateList)
                {
                    if (upd is Creature) creaturesInRoom.Add(upd as Creature);
                }

                if(closedFac < 0)
                {
                    closedFac = 0;
                    closeSpeed = 0;
                }
                if (closedFac > 1)
                {
                    closedFac = 1;
                    closeSpeed = 0;
                }

                for (int i = 0; i < target.Length; i++)
                {
                    this.target[i] = pObj.pos + (i == 0 ? 1f : -1f) * perp * rad * (1 - closedFac);
                }
                excitement = Mathf.Max(closedFac, excitement - 0.005f);
                this.closedFac += closeSpeed;
                for (int i = 0; i < parts.Length; i++)
                {
                    DisembodyedPart part = parts[i];
                    part.Update();
                    part.ConnectToPoint(target[i], rad * 0.2f * (1f - closedFac) * (0.5f + 0.5f * excitement), false, 0.002f, Vector2.zero, 0f, 0f);
                    if (UnityEngine.Random.value < excitement) part.vel += RWCustom.Custom.RNV() * (0.5f + 0.5f * excitement) * 5f;
                    foreach (var crit in creaturesInRoom)
                    {
                        crit.PushOutOf(part.pos, rad * 0.8f, -1);
                        foreach (var chunk in crit.bodyChunks)
                        {
                            part.PushFromPoint(chunk.pos, chunk.rad + rad, 0.2f);
                        }
                    }
                }

            }

            public bool Broken
            {
                get
                {
                    return this.room.world.brokenShelters[this.room.abstractRoom.shelterIndex];
                }
            }

            // Token: 0x06002745 RID: 10053 RVA: 0x002965D0 File Offset: 0x002947D0
            public void Close()
            {
                if (this.Broken)
                {
                    return;
                }
                this.closeSpeed = 0.003125f;
                excitement = Mathf.Lerp(excitement, 1f, 0.5f);
            }

            public void OnShelterClose()
            {
                Close();
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                if (newContatiner == null) newContatiner = rCam.ReturnFContainer("Midground");
                foreach (var sprt in sLeaser.sprites)
                {
                    newContatiner.AddChild(sprt);
                }
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                Color color = new Color(data.GetValue<float>("r"), data.GetValue<float>("g"), data.GetValue<float>("b"));
                sLeaser.sprites[0].color = color;
                sLeaser.sprites[1].color = color;
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                sLeaser.sprites[0].x = Mathf.Lerp(this.parts[0].lastPos.x, this.parts[0].pos.x, timeStacker) - rCam.pos.x;
                sLeaser.sprites[0].y = Mathf.Lerp(this.parts[0].lastPos.y, this.parts[0].pos.y, timeStacker) - rCam.pos.y;
                sLeaser.sprites[1].x = Mathf.Lerp(this.parts[1].lastPos.x, this.parts[1].pos.x, timeStacker) - rCam.pos.x;
                sLeaser.sprites[1].y = Mathf.Lerp(this.parts[1].lastPos.y, this.parts[1].pos.y, timeStacker) - rCam.pos.y;
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[2];
                sLeaser.sprites[0] = new FSprite("Futile_White", true);
                sLeaser.sprites[0].scale = this.rad / 8f;
                sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["JaggedCircle"];
                sLeaser.sprites[0].alpha = 0.6f;
                sLeaser.sprites[1] = new FSprite("Futile_White", true);
                sLeaser.sprites[1].scale = this.rad / 8f;
                sLeaser.sprites[1].shader = rCam.room.game.rainWorld.Shaders["JaggedCircle"];
                sLeaser.sprites[1].alpha = 0.6f;

                this.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
                this.AddToContainer(sLeaser, rCam, null);
            }
        }



		public class DisembodyedPart
		{
			public DisembodyedPart(float aFric, Vector2 startpos)
			{
                this.airFriction = aFric;
                this.Reset(startpos);
            }

			public virtual void Update()
			{
                this.lastPos = this.pos;
                this.pos += this.vel;
                this.vel *= this.airFriction;
            }

			public virtual void Reset(Vector2 resetPoint)
			{
				this.pos = resetPoint + RWCustom.Custom.DegToVec(UnityEngine.Random.value * 360f);
				this.lastPos = this.pos;
				this.vel = new Vector2(0f, 0f);
			}

			public void ConnectToPoint(Vector2 pnt, float connectionRad, bool push, float elasticMovement, Vector2 hostVel, float adaptVel, float exaggerateVel)
			{
				if (elasticMovement > 0f)
				{
					this.vel += RWCustom.Custom.DirVec(this.pos, pnt) * Vector2.Distance(this.pos, pnt) * elasticMovement;
				}
				this.vel += hostVel * exaggerateVel;
				if (push || !RWCustom.Custom.DistLess(this.pos, pnt, connectionRad))
				{
					float num = Vector2.Distance(this.pos, pnt);
					Vector2 a = RWCustom.Custom.DirVec(this.pos, pnt);
					this.pos -= (connectionRad - num) * a * 1f;
					this.vel -= (connectionRad - num) * a * 1f;
				}
				this.vel -= hostVel;
				this.vel *= 1f - adaptVel;
				this.vel += hostVel;
			}

			// Token: 0x06000F39 RID: 3897 RVA: 0x000AFFE0 File Offset: 0x000AE1E0
			public void PushFromPoint(Vector2 pnt, float pushRad, float elasticity)
			{
				if (RWCustom.Custom.DistLess(this.pos, pnt, pushRad))
				{
					float num = Vector2.Distance(this.pos, pnt);
					Vector2 a = RWCustom.Custom.DirVec(this.pos, pnt);
					this.pos -= (pushRad - num) * a * elasticity;
					this.vel -= (pushRad - num) * a * elasticity;
				}
			}
			public Vector2 lastPos;
			public Vector2 pos;
			public Vector2 vel;
			protected float airFriction;
		}
	}
}
