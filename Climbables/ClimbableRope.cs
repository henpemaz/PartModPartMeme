using UnityEngine;
using RWCustom;

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

        protected float conRad = 5f;
        private float mass = 0.2f;
        protected const float transmissionFactor = 0.95f;

        protected const float stiffnessCoef = 0.0000f;
        protected const float stiffnessDampCoef = 0.02f; // ok up to 0.05

        protected const float airFrictionA = 0.001f;
        protected const float airFrictionB = 0.0001f;

        const float externalTransfDisplace = 1.0f;
        const float externalTransfSpeed = 1.0f;

        public ClimbableRope(PlacedObject placedObject, Room instance)
        {
            this.placedObject = placedObject;
            this.room = instance;


            this.startPos = placedObject.pos;
            this.endPos = this.startPos + (placedObject.data as PlacedObject.ResizableObjectData).handlePos;
            this.length = (placedObject.data as PlacedObject.ResizableObjectData).handlePos.magnitude;

            this.nodeCount = RWCustom.Custom.IntClamp((int)(length / this.conRad) + 1, 2, 200);
            this.conRad = length / (nodeCount - 1);

            this.nsteps = Mathf.CeilToInt(nodeCount / 5f);
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

                    speeds[i + 1, 0] += stepFactor * pullG;
                    // speeds[i + 1, 0] = perpB * Vector2.Dot(speeds[i + 1, 0], perpB);
                }

                //re-rope
                for (int i = 0; i < this.ropes.Length; i++)
                {
                    this.ropes[i].Update(this.nodes[i, 0], this.nodes[i + 1, 0]);
                    this.lengths[i, 0] = this.ropes[i].totalLength;
                }

                // Straighten up
                for (int i = 1; i < this.ropes.Length; i++)
                {
                    Vector2 dirprev = Custom.DirVec(this.ropes[i - 1].A, this.ropes[i - 1].AConnect);
                    Vector2 dir = Custom.DirVec(this.ropes[i].A, this.ropes[i].AConnect);
                    Vector2 perp = Custom.PerpendicularVector(dir);

                    float twist = Custom.Angle(dir, dirprev);
                    this.twists[i, 0] = twist;
                    float deltaTwist = (twist - twists[i, 1]) / stepFactor;

                    Vector2 reaction = stepFactor * perp * conRad * (twist * stiffnessCoef + deltaTwist * stiffnessDampCoef);
                    speeds[i, 0] -= transmissionFactor * reaction / 2;
                    speeds[i + 1, 0] += reaction / 2;
                }

                // Up the chain, propagating "speed" (it's actually forces)
                for (int i = this.ropes.Length - 1; i >= 0; i--)
                {
                    Vector2 pullB = Custom.DirVec(ropes[i].B, ropes[i].BConnect);

                    Vector2 perpB = Custom.PerpendicularVector(pullB);

                    Vector2 tangential = perpB * Vector2.Dot(speeds[i + 1, 0], perpB) * transmissionFactor;
                    speeds[i, 0] += (speeds[i + 1, 0] - tangential)  ;
                    //speeds[i + 1, 0] = tangential;
                    speeds[i + 1, 0] -= (speeds[i + 1, 0] - tangential) ;
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
            // pass
        }

        bool IClimbableVine.CurrentlyClimbable()
        {
            return true;
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
                this.speeds[index, 0] += movement * externalTransfSpeed;
                this.nodes[index, 0] += movement * externalTransfDisplace;
            }
        }

        float IClimbableVine.Rad(int index)
        {
            return 2f;
        }

        int IClimbableVine.TotalPositions()
        {
            return this.nodeCount;
        }
    }
}