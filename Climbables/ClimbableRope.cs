using UnityEngine;
using RWCustom;
using System.Collections.Generic;

namespace Climbables
{
    public class ClimbableRope : UpdatableAndDeletable, IClimbableVine, IDrawable
    {
        protected PlacedObject placedObject;
        protected Vector2 startPos;
        protected Vector2 endPos;
        protected float length;
        protected int nodeCount;
        protected int nsteps;
        protected float stepFactor;
        protected Vector2[,] nodes;
        protected Vector2[,] speeds;
        protected Rope[] ropes;
        protected float[,] lengths;
        protected float[,] twists;

        protected float conRad = 8f;
        private float mass = 0.3f;

        private List<Player> recentlySwingedOff;
        private List<Player> recentlyCrawledOff;
        private bool playerCrawlingOff;
        protected const float transmissionFactor = 0.95f;

        protected const float pullFactor = 0.67f;

        //protected const float stiffnessCoef = 0.0000f;
        //protected const float stiffnessDampCoef = 0.02f; // ok up to 0.05

        protected const float airFrictionA = 0.001f;
        protected const float airFrictionB = 0.0001f;

        const float externalTransfDisplace = 0.0f;
        const float externalTransfSpeed = 1.0f;

        public ClimbableRope(PlacedObject placedObject, Room instance)
        {
            this.placedObject = placedObject;
            this.room = instance;

            recentlySwingedOff = new List<Player>();
            recentlyCrawledOff = new List<Player>();


            this.startPos = placedObject.pos;
            this.endPos = this.startPos + (placedObject.data as PlacedObject.ResizableObjectData).handlePos;
            this.length = (placedObject.data as PlacedObject.ResizableObjectData).handlePos.magnitude;

            this.nodeCount = RWCustom.Custom.IntClamp((int)(length / this.conRad) + 1, 2, 200);
            this.conRad = length / (nodeCount - 1);

            this.nsteps = Mathf.CeilToInt(nodeCount / conRad);
            this.stepFactor = 1f / nsteps;

            this.nodes = new Vector2[nodeCount, 2];
            this.speeds = new Vector2[nodeCount, 2];
            this.ropes = new Rope[this.nodeCount - 1];
            this.lengths = new float[this.nodeCount - 1, 2];
            this.twists = new float[this.nodeCount - 1, 2];


            for (int i = 0; i < this.nodeCount; i++)
            {
                Vector2 speed = 0.1f * RWCustom.Custom.RNV(); // Speed
                this.speeds[i, 0] = speed;
                this.speeds[i, 1] = speed;

                Vector2 pos = Vector2.Lerp(startPos, endPos, (float)i / (float)(this.nodeCount - 1));
                this.nodes[i, 0] = pos; // Pos
                this.nodes[i, 1] = pos - speed; // Prev

            }
            this.speeds[0, 0] = Vector2.zero; // anchor
            this.speeds[0, 1] = Vector2.zero; // anchor

            for (int i = 0; i < this.ropes.Length; i++)
            {
                this.ropes[i] = new Rope(room, this.nodes[i, 0], this.nodes[i + 1, 0], 2f);

                this.lengths[i, 0] = this.ropes[i].totalLength;
                this.lengths[i, 1] = this.ropes[i].totalLength;

                this.twists[i, 0] = 0f;
                this.twists[i, 1] = 0f;
            }

            if (room.climbableVines == null)
            {
                room.climbableVines = new ClimbableVinesSystem();
                room.AddObject(room.climbableVines);
            }
            room.climbableVines.vines.Add(this);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            //if (ClimbablesMod.ropeWatch != null) ClimbablesMod.ropeWatch.Start();

            foreach (var player in recentlySwingedOff)
            {
                player.vineGrabDelay = 3;
            }
            recentlySwingedOff.Clear();


            this.playerCrawlingOff = false;
            foreach (var player in recentlyCrawledOff)
            {
                player.vineGrabDelay = 30;
            }
            recentlyCrawledOff.Clear();

            for (int n = 0; n < nsteps; n++)
            {
                // Down the chain, fixed spacing
                for (int i = 0; i < this.ropes.Length; i++)
                {
                    //Vector2 pullA = Custom.DirVec(ropes[i].A, ropes[i].AConnect);
                    Vector2 pullB = Custom.DirVec(ropes[i].B, ropes[i].BConnect);
                    Vector2 pullG = new Vector2(0f, -room.gravity);

                    //nodes[i + 1, 0] += stepFactor * pullB * (lengths[i, 0] - conRad);
                    nodes[i + 1, 0] += pullB * (lengths[i, 0] - conRad);

                    speeds[i + 1, 0] += stepFactor * pullG * pullFactor;
                    // speeds[i + 1, 0] = perpB * Vector2.Dot(speeds[i + 1, 0], perpB);
                }

                //re-rope
                for (int i = 0; i < this.ropes.Length; i++)
                {
                    this.ropes[i].Update(this.nodes[i, 0], this.nodes[i + 1, 0]);
                    this.lengths[i, 0] = this.ropes[i].totalLength;
                }

                ////// Straighten up
                //for (int i = 1; i < this.ropes.Length; i++)
                //{
                //    Vector2 dirprev = Custom.DirVec(this.ropes[i - 1].A, this.ropes[i - 1].AConnect);
                //    Vector2 dir = Custom.DirVec(this.ropes[i].A, this.ropes[i].AConnect);
                //    Vector2 perp = Custom.PerpendicularVector(dir);

                //    float twist = Custom.Angle(dir, dirprev);
                //    this.twists[i, 0] = twist;
                //    float deltaTwist = (twist - twists[i, 1]) / stepFactor;

                //    Vector2 reaction = stepFactor * perp * conRad * (twist * stiffnessCoef + deltaTwist * stiffnessDampCoef);
                //    speeds[i, 0] -= transmissionFactor * reaction / 2;
                //    speeds[i + 1, 0] += reaction / 2;
                //}

                // Up the chain, propagating "speed" (it's actually forces)
                for (int i = this.ropes.Length - 1; i >= 0; i--)
                {
                    Vector2 pullB = Custom.DirVec(ropes[i].B, ropes[i].BConnect);

                    Vector2 perpB = Custom.PerpendicularVector(pullB);
                    Vector2 relative = speeds[i + 1, 0] - speeds[i, 0];
                    Vector2 tangential = perpB * Vector2.Dot(relative, perpB) * transmissionFactor;
                    speeds[i, 0] += (relative - tangential);
                    //speeds[i + 1, 0] = tangential;
                    speeds[i + 1, 0] -= (relative - tangential);
                }

                speeds[0, 0] = Vector2.zero;

                for (int i = 0; i < this.ropes.Length; i++)
                {
                    this.nodes[i + 1, 1] = this.nodes[i + 1, 0];
                    this.speeds[i + 1, 1] = this.speeds[i + 1, 0];
                    this.speeds[i + 1, 0] *= 1 - stepFactor * (airFrictionA + this.speeds[i + 1, 0].magnitude * airFrictionB);
                    this.lengths[i, 1] = this.lengths[i, 0];
                    this.twists[i, 1] = this.twists[i, 0];

                    this.nodes[i + 1, 0] += stepFactor * this.speeds[i + 1, 0];

                    // Collision will go here

                    this.ropes[i].Update(this.nodes[i, 0], this.nodes[i + 1, 0]);
                    this.lengths[i, 0] = this.ropes[i].totalLength;
                }

            }

            //if (ClimbablesMod.ropeWatch != null) ClimbablesMod.ropeWatch.Stop();
        }


        void IDrawable.InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMeshAtlased(this.nodeCount, false, true);

            (this as IDrawable).AddToContainer(sLeaser, rCam, null);
        }

        void IDrawable.AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.sprites[0].RemoveFromContainer();
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Midground");
            }
            newContatiner.AddChild(sLeaser.sprites[0]);
        }

        void IDrawable.ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[0].color = palette.blackColor;
        }

        void IDrawable.DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            // Joars code :slugmystery:
            Vector2 vector = Vector2.Lerp(this.nodes[0, 1], this.nodes[0, 0], timeStacker);
            vector += RWCustom.Custom.DirVec(Vector2.Lerp(this.nodes[1, 1], this.nodes[1, 0], timeStacker), vector) * 1f;
            float d = 2f;
            for (int i = 0; i < this.nodeCount; i++)
            {
                float num = (float)i / (float)(this.nodeCount - 1);
                Vector2 vector2 = Vector2.Lerp(this.nodes[i, 1], this.nodes[i, 0], timeStacker);
                Vector2 normalized = (vector - vector2).normalized;
                Vector2 a = RWCustom.Custom.PerpendicularVector(normalized);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4, vector - a * d - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 1, vector + a * d - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - a * d - camPos);
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + a * d - camPos);
                vector = vector2;
            }
        }

        void IClimbableVine.BeingClimbedOn(Creature crit)
        {
            this.playerCrawlingOff = false;
            if (crit is Player)
            {
                Player p = (crit as Player);
                int ropeindex = Mathf.FloorToInt(Mathf.Lerp(0, this.ropes.Length - 1, p.vinePos.floatPos));
                Vector2 speee = speeds[ropeindex, 0];
                Vector2 updir = (ropes[ropeindex].A - ropes[ropeindex].AConnect).normalized;
                Vector2 perp = Custom.PerpendicularVector(updir);
                Vector2 dir = new Vector2(p.input[0].x, p.input[0].y);

                // Stop the player from pushing against terrain, fricking hell
                Vector2 contactPoint = new Vector2(p.bodyChunks[0].ContactPoint.x, p.bodyChunks[0].ContactPoint.y);
                if(contactPoint.magnitude > 0 && Vector2.Dot(contactPoint.normalized, updir) > 0f)
                {
                    if (Mathf.Abs(contactPoint.x) > 0) // Trim to vertical component
                    {
                        p.vineClimbCursor = Vector2.up * Vector2.Dot(Vector2.up, p.vineClimbCursor);
                    }
                    if (Mathf.Abs(contactPoint.y) > 0)// Trim to horiz component
                    {
                        p.vineClimbCursor = Vector2.right * Vector2.Dot(Vector2.right, p.vineClimbCursor);
                    }
                }

                // Crawl near the top if on narrow terrain
                if (p.input[0].y == 1 && ropeindex < 5 && (this.room.aimap.getAItile(p.bodyChunks[0].pos).narrowSpace || this.room.aimap.getAItile(p.bodyChunks[1].pos).narrowSpace))
                {
                    this.playerCrawlingOff = true;
                    this.recentlyCrawledOff.Add(p);
                }
                else if (dir.magnitude > 0) // Swing and Swingjump
                {
                    if (p.input[0].jmp && !p.input[1].jmp)
                    {
                        //Debug.Log("dir is " + dir.x + "n" + dir.y);
                        //Debug.Log("floatpos is " + p.vinePos.floatPos);
                        //p.canJump = 1; // jump too strong
                        //p.wantToJump = 1; // jump too strong
                        p.jumpBoost = 6;

                        if (p.input[0].y != -1)
                            p.standing = true;
                        if(p.input[0].y == 1 && ropeindex < 5) // Can jump up when near the top
                        {
                            p.canJump = 1;
                            p.wantToJump = 1;
                            p.jumpBoost = 4;
                        }
                        else if (Vector2.Dot(dir, speee.normalized) > 0.67f)
                        {
                            // Needed better direction of boost;
                            Vector2 directionOfBoost = (speee.normalized + updir.normalized + dir.normalized + Vector2.up).normalized;
                            float boostSpeed = Mathf.Clamp01(Vector2.Dot(dir.normalized, speee.normalized)) * Custom.LerpMap(ropeindex, 2f, 80f, 1.5f, 6f, 1.2f) * Mathf.Pow(p.vinePos.floatPos, 0.5f);
                            p.bodyChunks[0].vel += directionOfBoost * boostSpeed;
                            if (p.input[0].x != 0)
                            {
                                this.recentlySwingedOff.Add(p);
                            }
                        }
                    }
                    else
                    {
                        // Catch up to speed
                        Vector2 speedInDirectionOfSwing = Vector2.Dot(p.bodyChunks[0].vel, speee.normalized) * speee.normalized;
                        if (Vector2.Dot(p.bodyChunks[0].vel, speedInDirectionOfSwing) > 0 && speedInDirectionOfSwing.magnitude < speee.magnitude)
                        {
                            p.bodyChunks[0].vel += speedInDirectionOfSwing.normalized * (speee.magnitude - speedInDirectionOfSwing.magnitude);
                        }
                        // Extra swing motion
                        if (Vector2.Dot(dir, speee.normalized) > 0.67f)
                        {
                            p.bodyChunks[0].vel += Mathf.Pow(p.vinePos.floatPos, 0.5f) * Vector2.Dot(dir.normalized, perp.normalized) * perp * Custom.LerpMap(ropeindex, 2f, 80f, 0.8f, 1.2f, 1.2f);
                        }
                    }
                }
            }
        }

        bool IClimbableVine.CurrentlyClimbable()
        {
            return !this.playerCrawlingOff;
        }

        float IClimbableVine.Mass(int index)
        {
            return mass;
        }

        Vector2 IClimbableVine.Pos(int index)
        {
            return this.nodes[index, 0];
        }

        void IClimbableVine.Push(int index, Vector2 movement)
        {
            if (index > 0 && index < nodeCount)
            {
                Vector2 lineDirection;
                if (index == 0 || ropes.Length == 1) lineDirection = ropes[0].A - ropes[0].B;
                else if (index == nodeCount - 1) lineDirection = ropes[index - 1].A - ropes[index - 1].B;
                else
                {
                    lineDirection = ropes[index - 1].A - ropes[index - 1].B + ropes[index].A - ropes[index].B;
                }
                lineDirection = lineDirection.normalized;
                //Vector2 perpDirection = Custom.PerpendicularVector(lineDirection.normalized);
                movement = movement * 0.5f + 0.5f * (movement - lineDirection * Vector2.Dot(lineDirection, movement));

                this.speeds[index, 0] += movement * externalTransfSpeed;
                this.nodes[index, 0] += movement * externalTransfDisplace;
                if (index == nodeCount - 1) this.speeds[index, 0] = Vector2.Lerp(this.speeds[index, 0], this.speeds[index -1, 0], 0.67f);
            }
        }

        float IClimbableVine.Rad(int index)
        {
            return 3f;
        }

        int IClimbableVine.TotalPositions()
        {
            return this.nodeCount;
        }
    }
}