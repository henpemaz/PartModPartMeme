using RWCustom;
using System;
using UnityEngine;

namespace TheWilderness
{
    internal class SlugBall : PhysicalObject
    {
        private Vector2 pos;
        private Player player;

        internal static void Apply()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("SlugBall", typeof(SlugBall), null, null));
        }

        private static void CustomcheckAgainstSlopesVertically(BodyChunk self)
        {

			Vector2 from = self.lastPos;
			Vector2 to = self.pos + self.vel.normalized * self.slopeRad;
			Vector2 towards = to - from;
			float dist = towards.magnitude;
			towards = towards.normalized;
			IntVector2 tilePosition;
			IntVector2 b;
			Room.SlopeDirection slopeDirection;
			while (dist > 0f)
            {
				tilePosition = self.owner.room.GetTilePosition(from);
				b = new IntVector2(0, 0);
				slopeDirection = self.owner.room.IdentifySlope(from);
				if (self.owner.room.GetTile(from).Terrain != Room.Tile.TerrainType.Slope)
				{
					if (self.owner.room.IdentifySlope(tilePosition.x - 1, tilePosition.y) != Room.SlopeDirection.Broken && from.x - self.slopeRad <= self.owner.room.MiddleOfTile(from).x - 10f)
					{
						slopeDirection = self.owner.room.IdentifySlope(tilePosition.x - 1, tilePosition.y);
						b.x = -1;
					}
					else if (self.owner.room.IdentifySlope(tilePosition.x + 1, tilePosition.y) != Room.SlopeDirection.Broken && from.x + self.slopeRad >= self.owner.room.MiddleOfTile(from).x + 10f)
					{
						slopeDirection = self.owner.room.IdentifySlope(tilePosition.x + 1, tilePosition.y);
						b.x = 1;
					}
					else if (from.y - self.slopeRad < self.owner.room.MiddleOfTile(from).y - 10f)
					{
						if (self.owner.room.IdentifySlope(tilePosition.x, tilePosition.y - 1) != Room.SlopeDirection.Broken)
						{
							slopeDirection = self.owner.room.IdentifySlope(tilePosition.x, tilePosition.y - 1);
							b.y = -1;
						}
					}
					else if (from.y + self.slopeRad > self.owner.room.MiddleOfTile(from).y + 10f && self.owner.room.IdentifySlope(tilePosition.x, tilePosition.y + 1) != Room.SlopeDirection.Broken)
					{
						slopeDirection = self.owner.room.IdentifySlope(tilePosition.x, tilePosition.y + 1);
						b.y = 1;
					}
				}

				if (slopeDirection != Room.SlopeDirection.Broken)
				{
					Debug.Log("bounce bounce");
					Vector2 vector = self.owner.room.MiddleOfTile(self.owner.room.GetTilePosition(self.pos) + b);
					int num = 0;
					float num2;
					int num3;
					Vector2 dir;
					switch (slopeDirection)
					{
						case Room.SlopeDirection.UpLeft:
							num = -1;
							num2 = self.pos.x - (vector.x - 10f) + (vector.y - 10f);
							num3 = -1;
							dir = new Vector2(-1, 1);
							break;
						case Room.SlopeDirection.UpRight:
							num = 1;
							num2 = 20f - (self.pos.x - (vector.x - 10f)) + (vector.y - 10f);
							num3 = -1;
							dir = new Vector2(1, 1);
							break;
						case Room.SlopeDirection.DownLeft:
							num2 = 20f - (self.pos.x - (vector.x - 10f)) + (vector.y - 10f);
							num3 = 1;
							dir = new Vector2(-1, -1);
							break;
						default:
							num2 = self.pos.x - (vector.x - 10f) + (vector.y - 10f);
							num3 = 1;
							dir = new Vector2(1, -1);
							break;
					}
					dir = dir.normalized;
					float into = Vector2.Dot(self.vel, dir);
					if (num3 == -1 && self.pos.y <= num2 + self.slopeRad + self.slopeRad)
					{
						self.pos.y = num2 + self.slopeRad + self.slopeRad;
						self.contactPoint.y = -1;
						self.contactPoint = new IntVector2((int)-Mathf.Sign(dir.x), (int)-Mathf.Sign(dir.y));

						if (into < 0f)
						{
							self.vel = self.vel - dir * into * 2f;
							Debug.Log("bounce bounce A");
						}
						else
						{
							//self.vel.x = self.vel.x * (1f - self.owner.surfaceFriction);
							//self.vel.x = self.vel.x + Mathf.Abs(self.vel.y) * Mathf.Clamp(0.5f - self.owner.surfaceFriction, 0f, 0.5f) * (float)num * 0.2f;
							//self.vel.y = 0f;
							Debug.Log("NOT bounce bounce A");
						}

						self.onSlope = num;
						self.slopeRad = self.TerrainRad - 1f;
						Debug.Log("collided with " + slopeDirection.ToString());
						Debug.Log("contactPoint = " + self.contactPoint);
						break;
					}
					else if (num3 == 1 && self.pos.y >= num2 - self.slopeRad - self.slopeRad)
					{
						self.pos.y = num2 - self.slopeRad - self.slopeRad;
						self.contactPoint.y = 1;
						self.contactPoint = new IntVector2((int)-Mathf.Sign(dir.x), (int)-Mathf.Sign(dir.y));
						if (into < 0f)
						{
							self.vel = self.vel - dir * into * 2f;
							Debug.Log("bounce bounce B");
						}
						else
						{
							//self.vel.y = 0f;
							//self.vel.x = self.vel.x * (1f - self.owner.surfaceFriction);
							Debug.Log("NOT bounce bounce B");
						}
						self.slopeRad = self.TerrainRad - 1f;
						Debug.Log("collided with " + slopeDirection.ToString());
						Debug.Log("contactPoint = " + self.contactPoint);
						break;
					}
				}
				from += towards * 10f;
				dist -= 10f;
				if(dist > 10) Debug.Log("check check");
			}
		}

        public SlugBall(PlacedObject pObj, Room room) : base(new AbstractPhysicalObject(room.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, room.GetWorldCoordinate(pObj.pos), room.game.GetNewID()))
        {
            this.pos = pObj.pos;

            base.bodyChunks = new BodyChunk[1];
            base.bodyChunks[0] = new BodyChunk(this, 0, pos, 8f, 0.8f);
            this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
            base.airFriction = 0.97f;
            base.gravity = 1.0f;
            this.bounce = 0.92f;
            this.surfaceFriction = 0.5f;
            this.collisionLayer = 2;
            base.waterFriction = 1f;
            base.buoyancy = 0.4f;

            CollideWithObjects = false;
        }

        public override void Update(bool eu)
        {
			var ck = firstChunk;
			ck.collideWithSlopes = false;
			ck.collideWithTerrain = false;
            base.Update(eu);
			CustomcheckAgainstSlopesVertically(ck);
			var contactPoint = ck.contactPoint;
			ck.CheckVerticalCollision();
			ck.CheckHorizontalCollision();
			ck.contactPoint += contactPoint;
			ck.contactPoint.x = (int)Mathf.Sign(ck.contactPoint.x);
			ck.contactPoint.y = (int)Mathf.Sign(ck.contactPoint.y);

			if (this.player == null)
            {
                if(room.game.Players[0].realizedCreature is Player p && (p.firstChunk.pos - pos).magnitude < 20f){
                    this.player = p;
                    player.gravity = 0f;
                    player.CollideWithObjects = false;
                    player.CollideWithTerrain = false;
                    player.CollideWithSlopes = false;
                    firstChunk.vel = Vector2.up * 50f;
                }
            }
            else
            {
                player.gravity = 0f;
                player.CollideWithObjects = false;
                player.CollideWithTerrain = false;
                player.CollideWithSlopes = false;
				player.bodyChunks[0].pos = firstChunk.pos;
				player.bodyChunks[0].vel = firstChunk.vel;
				player.bodyChunks[1].pos = firstChunk.lastPos;
				player.bodyChunks[1].vel = firstChunk.vel;
			}
        }

    }
}