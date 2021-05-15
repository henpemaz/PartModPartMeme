﻿using DevInterface;
using ManagedPlacedObjects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConcealedGarden
{
    internal class CosmeticLeaves : UpdatableAndDeletable, IDrawable
    {
        public CosmeticLeavesObjectData data => (placedObject.data as CosmeticLeavesObjectData);

        private const float spacingh = 10f;
        private const float spacingv = 10f;
        private const float varh = 5f;
        private const float varv = 5f;
        public PlacedObject placedObject;
        private CosmeticLeaf[] leaves;
        private float maxrad;
        private float depth = 0f;
        private float thickness = 0.2f;
        private Color[] colors;

        public CosmeticLeaves(PlacedObject placedObject, Room instance)
        {
            this.placedObject = placedObject;
            this.room = instance;

            maxrad = 10;
            depth = data.depth;

            Debug.LogError("owner depth is " + depth);

            List<CosmeticLeaf> leavesList = new List<CosmeticLeaf>();

            int rows = this.IdealRows(0f);
            float rowstep = 2f / (rows+1);
            for (float iv = -1 + rowstep; iv < (1- rowstep/2); iv += rowstep)
            {
                int cols = this.IdealCols(iv);
                float colstep = 2f / (cols + 1);
                for (float iu = -1 + colstep; iu < (1-colstep/2); iu += colstep)
                {
                    float du = iu * Mathf.PI / 2f;
                    float dv = iv * Mathf.PI / 2f;
                    Vector2 vari = Vector2.Scale(UnityEngine.Random.insideUnitCircle, Varmag(iu,iv));
                    Vector2 offset = vari + new Vector2(Mathf.Sin(du)* Mathf.Abs(Mathf.Cos(dv))*data.handleA.magnitude, Mathf.Sin(dv) * data.handleB.magnitude);
                    Vector2 rootpos = placedObject.pos + offset;
                    CosmeticLeaf leaf = new CosmeticLeaf(this, rootpos, iu, iv);
                    leavesList.Add(leaf);
                }
            }

            this.leaves = leavesList.ToArray();
        }

        private Vector2 Varmag(float iu, float iv)
        {
            return new Vector2(varh, varv) * Mathf.Abs(Mathf.Cos(iu * Mathf.PI / 2f)) * Mathf.Abs(Mathf.Cos(iv * Mathf.PI / 2f));
        }

        private int IdealRows(float v)
        {
            int nr = Mathf.CeilToInt(this.data.handleB.magnitude / spacingh);
            if (v == 0)
            {
                return nr;
            }
            return Mathf.CeilToInt(Mathf.Abs(Mathf.Cos(v * Mathf.PI / 2f)) * nr);
        }

        private int IdealCols(float u)
        {
            int nc = Mathf.CeilToInt(this.data.handleA.magnitude / spacingv);
            if (u == 0)
            {
                return nc;
            }
            return Mathf.CeilToInt(Mathf.Abs(Mathf.Cos(u * Mathf.PI / 2f)) * nc);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);



            if (room.ViewedByAnyCamera(this.placedObject.pos, this.maxrad * 2))
            {
                //this.updatewindcycle();
                for (int i = 0; i < this.leaves.Length; i++)
                {
                    this.leaves[i].Update();
                }
            }
        }

        public class CosmeticLeaf
        {
            Vector2[,] bones;
            CosmeticLeaves owner;
            private Vector2 rootpos;
            private float depth;
            private float u;
            private float v;
            private Vector2 uv;
            private Vector2 scale;
            private static Vector2[] basebones = new Vector2[]
                {
                    new Vector2(0f,0f), new Vector2(0f,0.3f),new Vector2(0f,-0.2f),new Vector2(0f,-0.7f),
                };
            private static Vector2[] bonewheightss = new Vector2[]
                {
                    new Vector2(0f,0f), new Vector2(0.4f,0.3f),new Vector2(0.8f,0.6f),new Vector2(1f,1f),
                };
            private float[] width;
            private static float[] basewidth = new float[]
                {
                    0.05f, 0.15f, 0.13f, 0f,
                };

            public CosmeticLeaf(CosmeticLeaves cosmeticLeaves, Vector2 rootpos, float iu, float iv)
            {
                this.owner = cosmeticLeaves;
                this.rootpos = rootpos;
                this.depth = owner.depth + Mathf.Sqrt((1 - Math.Abs(Mathf.Cos(iu))) * (1 - Math.Abs(Mathf.Cos(iv)))) * owner.thickness;
                Debug.LogError("leaf depth is " + depth);
                this.u = iu;
                this.v = iv;
                this.uv = new Vector2(iu, iv);
                this.scale = new Vector2(15f, 15f);
                this.bones = new Vector2[basebones.Length,2];
                for (int i = 0; i < bones.GetLength(0); i++)
                {
                    bones[i, 0] = basebones[i] + Vector2.Scale(bonewheightss[i], uv);
                    bones[i, 1] = bones[i, 0];
                }
                this.width = new float[basewidth.Length];
                for (int i = 0; i < width.Length; i++)
                {
                    width[i] = basewidth[i] * Mathf.Lerp(0.1f, 1f, Mathf.Cos(Mathf.Abs(u)));
                }
            }
            internal FSprite InitSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                TriangleMesh trimesh = TriangleMesh.MakeLongMesh(this.bones.GetLength(0), true, true);
                trimesh.shader = rCam.game.rainWorld.Shaders["CustomDepth"];
                return trimesh;
            }

            internal void Update()
            {
                
            }


            internal void ApplyPalette(FSprite fSprite, RoomPalette palette)
            {
                Color light = Color.Lerp(owner.colors[2], owner.colors[0], depth);
                Color dark = Color.Lerp(owner.colors[3], owner.colors[1], depth);
                TriangleMesh trimesh = fSprite as TriangleMesh;
                fSprite.alpha = depth;
                Vector2 prev = bones[0,0];

                for (int i = 0; i < bones.GetLength(0) - 1; i++)
                {
                    Vector2 next = bones[i+1,0];
                    bool up = (next.y - prev.y) >= 0f;
                    for (int j = 0; j < 4; j++)
                    {
                        trimesh.verticeColors[i * 4 + j] = up ? dark : light;
                    }
                    prev = next;
                }
                for (int j = 0; j < 3; j++)
                {
                    trimesh.verticeColors[(bones.GetLength(0) - 1) * 4 + j] = light;
                }
            }

            internal void DrawSprites(FSprite fSprite, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                TriangleMesh trimesh = fSprite as TriangleMesh;
                Vector2 horiz = Vector2.Scale(scale, this.getHoriz());
                Vector2 prev = Vector2.Scale(scale, Vector2.Lerp(bones[0, 1], bones[0, 0], timeStacker)) + rootpos - camPos;
                for (int i = 0; i < bones.GetLength(0) - 1; i++)
                {
                    Vector2 cur = Vector2.Scale(scale, Vector2.Lerp(bones[i+1, 1], bones[i+1, 0], timeStacker)) + rootpos - camPos;
                    trimesh.vertices[i * 4] = prev + horiz*this.width[i];
                    trimesh.vertices[i * 4+1] = prev - horiz * this.width[i];
                    trimesh.vertices[i * 4+2] = cur + horiz * this.width[i+1];
                    trimesh.vertices[i * 4+3] = cur - horiz * this.width[i+1];
                    prev = cur;
                }
                int last = bones.GetLength(0) - 1;
                trimesh.vertices[last * 4] = prev + horiz * this.width[last];
                trimesh.vertices[last * 4 + 1] = prev - horiz * this.width[last];
                trimesh.vertices[last * 4 + 2] = Vector2.Scale(scale, Vector2.Lerp(bones[last, 1], bones[last, 0], timeStacker)) + rootpos - camPos;

                trimesh.Refresh();
            }

            private Vector2 getHoriz()
            {
                return new Vector2(1f, 0f);
            }
        }


        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[leaves.Length];
            for (int i = 0; i < leaves.Length; i++)
            {
                sLeaser.sprites[i] = leaves[i].InitSprite(sLeaser, rCam);
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
            if ((this.placedObject.data as CosmeticLeavesObjectData).colorType == CosmeticLeavesObjectData.CosmeticLeavesColor.EffectColor1)
                this.colors = palette.texture.GetPixels(30, 4, 2, 2);
            if ((this.placedObject.data as CosmeticLeavesObjectData).colorType == CosmeticLeavesObjectData.CosmeticLeavesColor.EffectColor2)
                this.colors = palette.texture.GetPixels(30, 2, 2, 2);

            for (int i = 0; i < leaves.Length; i++)
            {
                leaves[i].ApplyPalette(sLeaser.sprites[i], palette);
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int i = 0; i < leaves.Length; i++)
            {
                leaves[i].DrawSprites(sLeaser.sprites[i], sLeaser, rCam, timeStacker, camPos);
            }
        }

        
        public class CosmeticLeavesObjectData : PlacedObjectsManager.ManagedData
        {
#pragma warning disable 0649
            [BackedByField("ha")]
            public Vector2 handleA;
            [BackedByField("hb")]
            public Vector2 handleB;

            [PlacedObjectsManager.FloatField("dp", 0f, 30f, 2f, displayName:"Depth")]
            public float depth;

            public enum CosmeticLeavesColor
            {
                EffectColor1,
                EffectColor2,
                Custom,
                NONE
            }

            [BackedByField("ct")]
            public CosmeticLeavesColor colorType;
            [BackedByField("cc")]
            public Color customColor;
#pragma warning restore 0649
            public CosmeticLeavesObjectData(PlacedObject owner) : base(owner, new PlacedObjectsManager.ManagedField[] {
                new PlacedObjectsManager.Vector2Field("ha", new Vector2(0, 100), PlacedObjectsManager.Vector2Field.VectorReprType.line),
                //new PlacedObjectsManager.Vector2Field("hb", , PlacedObjectsManager.Vector2Field.VectorReprType.none),
                new PlacedObjectsManager.DrivenVector2Field("hb", "ha", new Vector2(-200, 0)),
                new PlacedObjectsManager.EnumField("ct", typeof(CosmeticLeavesColor), CosmeticLeavesColor.EffectColor1, displayName:"Color Type"),
                new PlacedObjectsManager.ColorField("cc", new Color(0.8f, 0.1f, 0.6f), displayName:"CustomColor")
            })
            {

            }
        }
    }
}