using DevInterface;
using ManagedPlacedObjects;
using System;
using System.Collections.Generic;
using UnityEngine;
using RWCustom;

namespace ConcealedGarden
{
    internal class CosmeticLeaves : UpdatableAndDeletable, IDrawable
    {
        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("CosmeticLeaves",
                typeof(CosmeticLeaves), typeof(CosmeticLeavesObjectData), typeof(PlacedObjectsManager.ManagedRepresentation)));
        }

        public CosmeticLeavesObjectData data => (pObj.data as CosmeticLeavesObjectData);
        public PlacedObject pObj;
        private Color[] colors;
        private List<Branch> branches;
        private List<Leaf> leaves;

        public CosmeticLeaves(PlacedObject placedObject, Room instance)
        {
            this.pObj = placedObject;
            this.room = instance;

            this.branches = new List<Branch>();
            this.leaves = new List<Leaf>();

            new Branch(this, null, 0, data.handleA, data.handleB.magnitude);
        }

        public class Branch
        {
            private readonly CosmeticLeaves owner;
            private readonly Branch connectsTo;
            private readonly int connectsToIndex;
            private readonly Vector3 goal;
            private Vector3[] relpos;
            private float[] thicknesses;
            public Vector3[,] pos;

            public Branch(CosmeticLeaves owner, Branch connectsTo, int connectsToIndex, Vector3 goal, float thicknessAtBase)
            {
                owner.branches.Add(this);
                this.owner = owner;
                this.connectsTo = connectsTo;
                this.connectsToIndex = connectsToIndex;
                this.goal = goal;

                // Grow in a direction
                int nnodes = Mathf.CeilToInt(thicknessAtBase / 0.8f);
                Vector3 ppos = Vector3.zero;
                Vector3 dir = goal.normalized;
                float jump = goal.magnitude / nnodes;
                List<Vector3> poses = new List<Vector3>() { ppos };
                List<float> thicnesses = new List<float>() { thicknessAtBase };
                for (int i = 1; i < nnodes; i++)
                {
                    // last dir + random + goal
                    dir = (dir * 0.6f + UnityEngine.Random.insideUnitSphere).normalized * jump * (0.6f + 0.6f * UnityEngine.Random.value) + (goal - ppos) * ((nnodes - i) / (nnodes)) * (0.4f + 0.4f * UnityEngine.Random.value);
                    ppos += dir;
                    dir.Normalize();
                    thicknessAtBase = Mathf.Min(1f, thicknessAtBase - 0.8f);
                    poses.Add(ppos);
                    thicnesses.Add(thicknessAtBase);

                    if (thicknessAtBase > 2f && UnityEngine.Random.value < Mathf.Pow(0.2f, 1f / thicknessAtBase))
                    {
                        new Branch(owner, this, i, (dir * 0.6f + UnityEngine.Random.insideUnitSphere + (Vector3)UnityEngine.Random.insideUnitCircle) * jump * (nnodes - i - 1), thicknessAtBase);
                    }
                    if (thicknessAtBase < 5f && UnityEngine.Random.value < Mathf.Pow(0.6f, thicknessAtBase))
                    {
                        new Leaf(owner, this, i, (dir * 0.6f + UnityEngine.Random.insideUnitSphere + (Vector3)UnityEngine.Random.insideUnitCircle).normalized);
                    }
                }

                // Store initial positions
                this.relpos = poses.ToArray();
                this.thicknesses = thicnesses.ToArray();

                // Dynamic positions
                Vector3 from = this.attachedPoint;
                this.pos = new Vector3[2, nnodes];
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < nnodes; j++)
                    {
                        this.pos[i, j] = from + relpos[j];
                    }
                }
            }

            public void Update()
            {
                // Update dynamic poss
                // wind and such would go here
                Vector3 rootpost = attachedPoint;
                for (int i = 0; i < relpos.Length; i++)
                {
                    rootpost += relpos[i];
                    pos[1, i] = pos[0, i];
                    pos[0, i] = rootpost;
                }
            }

            Vector3 attachedPoint { get { return connectsTo?.pos[0, connectsToIndex] ?? owner.pObj.pos; } }
            Vector3 attachedPointLast { get { return connectsTo?.pos[1, connectsToIndex] ?? owner.pObj.pos; } }

            internal FSprite InitSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                TriangleMesh trimesh = TriangleMesh.MakeLongMesh(relpos.Length, true, true);
                trimesh.shader = this.owner.room.game.rainWorld.Shaders["CustomDepth"];
                return trimesh;
            }

            internal void ApplyPalette(FSprite fSprite, RoomPalette palette)
            {
                TriangleMesh trimesh = fSprite as TriangleMesh;
                for (int i = 0; i < relpos.Length - 1; i++)
                {
                    Vector3 a = pos[0, i];
                    Vector3 b = pos[0, i + 1];
                    Vector2 ab = b - a;
                    //Vector2 per = Custom.PerpendicularVector(ab);
                    float shine = ab.x / ab.magnitude;
                    Color[] pala = palette.texture.GetPixels(Mathf.Clamp(Mathf.FloorToInt(a.z), 0, 29), 0, 1, 3);
                    Color[] palb = palette.texture.GetPixels(Mathf.Clamp(Mathf.FloorToInt(b.z), 0, 29), 0, 1, 3);

                    Color upperA = Color.Lerp(pala[1], pala[0], shine);
                    Color lowerA = Color.Lerp(pala[1], pala[2], shine);
                    Color upperB = Color.Lerp(palb[1], palb[0], shine);
                    Color lowerB = Color.Lerp(palb[1], palb[2], shine);
                    upperA.a = a.z;
                    lowerA.a = a.z;
                    upperB.a = b.z;
                    lowerB.a = b.z;

                    trimesh.verticeColors[i * 4 + 0] = ab.y > 0 ? upperA : lowerA;
                    trimesh.verticeColors[i * 4 + 1] = ab.y > 0 ? lowerA : upperA;
                    trimesh.verticeColors[i * 4 + 2] = ab.y > 0 ? upperB : lowerB;
                    if (i == relpos.Length - 1) continue;
                    trimesh.verticeColors[i * 4 + 3] = ab.y > 0 ? lowerB : upperB;
                }
            }

            internal void DrawSprites(FSprite fSprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                TriangleMesh trimesh = fSprite as TriangleMesh;
                for (int i = 0; i < relpos.Length - 1; i++)
                {
                    Vector3 a = pos[0, i];
                    Vector3 b = pos[0, i + 1];
                    Vector2 ab = b - a;
                    Vector2 per = Custom.PerpendicularVector(ab);

                    trimesh.vertices[i * 4 + 0] = (Vector2)a + per * this.thicknesses[i] - camPos;
                    trimesh.vertices[i * 4 + 1] = (Vector2)a - per * this.thicknesses[i] - camPos;
                    trimesh.vertices[i * 4 + 2] = (Vector2)b + per * this.thicknesses[i+1] - camPos;
                    if (i == relpos.Length - 1) continue;
                    trimesh.vertices[i * 4 + 3] = (Vector2)b - per * this.thicknesses[i + 1] - camPos;
                }
            }
        }

        public class Leaf
        {
            private CosmeticLeaves owner;
            private Branch connectsTo;
            private int connectsToIndex;
            private Vector3 dir;

            public Leaf(CosmeticLeaves owner, Branch connectsTo, int connectsToIndex, Vector3 direction)
            {
                this.owner = owner;
                owner.leaves.Add(this);
                this.connectsTo = connectsTo;
                this.connectsToIndex = connectsToIndex;
                this.dir = direction;
            }

            Vector3 attachedPoint { get { return connectsTo?.pos[0, connectsToIndex] ?? owner.pObj.pos; } }
            internal FSprite InitSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                //TriangleMesh trimesh = TriangleMesh.MakeLongMesh(this.bones.GetLength(0), true, true);
                FSprite trimesh = new FSprite("Futile_White");
                trimesh.scale = 0.25f;
                trimesh.shader = rCam.game.rainWorld.Shaders["CustomDepth"];
                return trimesh;
            }

            internal void ApplyPalette(FSprite fSprite, RoomPalette palette)
            {
                Color light = Color.Lerp(owner.colors[2], owner.colors[0], attachedPoint.z);
                Color dark = Color.Lerp(owner.colors[3], owner.colors[1], attachedPoint.z);
                fSprite.color = light;
                //TriangleMesh trimesh = fSprite as TriangleMesh;
                //fSprite.alpha = Mathf.InverseLerp(0, 20, depth);
                //Vector2 prev = bones[0, 0];

                //for (int i = 0; i < bones.GetLength(0) - 1; i++)
                //{
                //    Vector2 next = bones[i + 1, 0];
                //    bool up = (next.y - prev.y) >= 0f;
                //    for (int j = 0; j < 4; j++)
                //    {
                //        trimesh.verticeColors[i * 4 + j] = up ? dark : light;
                //    }
                //    prev = next;
                //}
                //for (int j = 0; j < 3; j++)
                //{
                //    trimesh.verticeColors[(bones.GetLength(0) - 1) * 4 + j] = light;
                //}
            }

            internal void DrawSprites(FSprite fSprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                //TriangleMesh trimesh = fSprite as TriangleMesh;
                //Vector2 horiz = Vector2.Scale(scale, this.getHoriz());
                //Vector2 prev = Vector2.Scale(scale, Vector2.Lerp(bones[0, 1], bones[0, 0], timeStacker)) + rootpos - camPos;
                //for (int i = 0; i < bones.GetLength(0) - 1; i++)
                //{
                //    Vector2 cur = Vector2.Scale(scale, Vector2.Lerp(bones[i + 1, 1], bones[i + 1, 0], timeStacker)) + rootpos - camPos;
                //    trimesh.vertices[i * 4] = prev + horiz * this.width[i];
                //    trimesh.vertices[i * 4 + 1] = prev - horiz * this.width[i];
                //    trimesh.vertices[i * 4 + 2] = cur + horiz * this.width[i + 1];
                //    trimesh.vertices[i * 4 + 3] = cur - horiz * this.width[i + 1];
                //    prev = cur;
                //}
                //int last = bones.GetLength(0) - 1;
                //trimesh.vertices[last * 4] = prev + horiz * this.width[last];
                //trimesh.vertices[last * 4 + 1] = prev - horiz * this.width[last];
                //trimesh.vertices[last * 4 + 2] = Vector2.Scale(scale, Vector2.Lerp(bones[last, 1], bones[last, 0], timeStacker)) + rootpos - camPos;

                //trimesh.Refresh();

                fSprite.SetPosition((Vector2)attachedPoint - camPos);
                fSprite.alpha = 1f - (Mathf.Clamp(attachedPoint.z, 0f, 30f) / 30f);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            foreach (var item in branches)
            {
                item.Update();
            }
            //foreach (var item in leaves)
            //{
            //    item.Update();
            //}
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[branches.Count + leaves.Count];

            for (int i = 0; i < branches.Count; i++)
            {
                sLeaser.sprites[i] = branches[i].InitSprite(sLeaser, rCam);
            }
            for (int i = 0; i < leaves.Count; i++)
            {
                sLeaser.sprites[branches.Count + i] = leaves[i].InitSprite(sLeaser, rCam);
            }
            this.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            this.AddToContainer(sLeaser, rCam, null);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Water");
            }
            foreach (FSprite fsprite in sLeaser.sprites)
            {
                fsprite.RemoveFromContainer();
                newContatiner.AddChild(fsprite);
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if ((this.pObj.data as CosmeticLeavesObjectData).colorType == CosmeticLeavesObjectData.CosmeticLeavesColor.EffectColor1)
                this.colors = palette.texture.GetPixels(30, 4, 2, 2);
            if ((this.pObj.data as CosmeticLeavesObjectData).colorType == CosmeticLeavesObjectData.CosmeticLeavesColor.EffectColor2)
                this.colors = palette.texture.GetPixels(30, 2, 2, 2);

            for (int i = 0; i < branches.Count; i++)
            {
                branches[i].ApplyPalette(sLeaser.sprites[i], palette);
            }
            for (int i = 0; i < leaves.Count; i++)
            {
                leaves[i].ApplyPalette(sLeaser.sprites[branches.Count + i], palette);
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = 0; i < branches.Count; i++)
            {
                branches[i].DrawSprites(sLeaser.sprites[i], sLeaser, rCam, timeStacker, camPos);
            }
            for (int i = 0; i < leaves.Count; i++)
            {
                leaves[i].DrawSprites(sLeaser.sprites[branches.Count + i], sLeaser, rCam, timeStacker, camPos);
            }
        }


        public class CosmeticLeavesObjectData : PlacedObjectsManager.ManagedData
        {
#pragma warning disable 0649
            [BackedByField("ha")]
            public Vector2 handleA;
            [BackedByField("hb")]
            public Vector2 handleB;

            [PlacedObjectsManager.FloatField("dp", 0f, 30f, 2f, displayName: "Depth")]
            public float depth;

            public enum CosmeticLeavesColor
            {
                EffectColor1,
                EffectColor2,
            }

            [BackedByField("ct")]
            public CosmeticLeavesColor colorType;

#pragma warning restore 0649
            public CosmeticLeavesObjectData(PlacedObject owner) : base(owner, new PlacedObjectsManager.ManagedField[] {
                new PlacedObjectsManager.Vector2Field("ha", new Vector2(0, 100), PlacedObjectsManager.Vector2Field.VectorReprType.line),
                new PlacedObjectsManager.DrivenVector2Field("hb", "ha", new Vector2(-200, 0)),
                new PlacedObjectsManager.EnumField("ct", typeof(CosmeticLeavesColor), CosmeticLeavesColor.EffectColor1, displayName:"Color Type"),
            })
            { }
        }
    }
}