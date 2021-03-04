using DevInterface;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConcealedGarden
{
    internal class CosmeticLeaves : UpdatableAndDeletable, IDrawable
    {
        public CosmeticLeavesObjectData data => (placedObject.data as CosmeticLeavesObjectData);
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


            List<CosmeticLeaf> leavesList = new List<CosmeticLeaf>();

            int rows = this.idealRows(0f);
            float rowstep = 2f / (rows+1);
            for (float iv = -1 + rowstep; iv < (1- rowstep/2); iv += rowstep)
            {
                int cols = this.idealCols(iv);
                float colstep = 2f / (cols + 1);
                for (float iu = -1 + colstep; iu < (1-colstep/2); iu += colstep)
                {
                    Vector2 offset = new Vector2(Mathf.Sin(iu * Mathf.PI / 2f)*data.handles[0].magnitude, Mathf.Sin(iv * Mathf.PI / 2f) * data.handles[1].magnitude);
                    Vector2 rootpos = placedObject.pos + offset;
                    CosmeticLeaf leaf = new CosmeticLeaf(this, rootpos, iu, iv);
                    leavesList.Add(leaf);
                }

            }

            this.leaves = leavesList.ToArray();



        }

        private int idealRows(float v)
        {
            return 4;
        }

        private int idealCols(float v)
        {
            return 6;
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
                this.u = iu;
                this.v = iv;
                this.uv = new Vector2(iu, iv);
                this.scale = new Vector2(30f, 30f);
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
                return TriangleMesh.MakeLongMesh(this.bones.GetLength(0), true, true);
            }

            internal void Update()
            {
                
            }

            static bool logthisonce = true;

            internal void ApplyPalette(FSprite fSprite, RoomPalette palette)
            {
                Color light = Color.Lerp(owner.colors[2], owner.colors[0], depth);
                Color dark = Color.Lerp(owner.colors[3], owner.colors[1], depth);
                TriangleMesh trimesh = fSprite as TriangleMesh;
                Vector2 prev = bones[0,0];
                //Debug.LogError("bones is " + bones.GetLength(0));
                //Debug.LogError("vertices is " + trimesh.verticeColors.Length);

                for (int i = 0; i < bones.GetLength(0) - 1; i++)
                {
                    Vector2 next = bones[i+1,0];
                    if (logthisonce) Debug.LogError("y is " +next.y);
                    bool up = (next.y - prev.y) >= 0f;
                    if (logthisonce) Debug.LogError("up is " + up);

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
                logthisonce = false;

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
            if ((this.placedObject.data as CosmeticLeavesObjectData).color == CosmeticLeavesObjectData.CosmeticLeavesColor.EffectColor1)
                this.colors = palette.texture.GetPixels(30, 4, 2, 2);
            if ((this.placedObject.data as CosmeticLeavesObjectData).color == CosmeticLeavesObjectData.CosmeticLeavesColor.EffectColor2)
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

        public class CosmeticLeavesObjectRepresentation : PlacedObjectRepresentation
        {
            public CosmeticLeavesControlPanel controlPanel;
            public Handle handleU;
            public Handle handleV;
            public CosmeticLeaves obj;
            public FSprite circleSprite;
            public FSprite lineSprite;

            public CosmeticLeavesObjectRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
            {
                this.controlPanel = new CosmeticLeavesControlPanel(owner, "CosmeticLeaves_Panel", this, new Vector2(0f, 100f));
                this.subNodes.Add(this.controlPanel);
                this.controlPanel.pos = (pObj.data as CosmeticLeavesObjectData).panelPos;
                handleU = new Handle(owner, "U_Handle", this, new Vector2(0f, 100f));
                handleV = new Handle(owner, "V_Handle", this, new Vector2(100f, 0f));
                this.subNodes.Add(handleU);
                this.subNodes.Add(handleV);

                this.handleU.pos = (pObj.data as CosmeticLeavesObjectData).handles[0];
                this.handleV.pos = (pObj.data as CosmeticLeavesObjectData).handles[1];

                this.lineSprite = new FSprite("pixel", true);
                this.lineSprite.anchorY = 0f;
                this.fSprites.Add(lineSprite);
                owner.placedObjectsContainer.AddChild(this.lineSprite);

                this.circleSprite = new FSprite("Futile_White", true);
                this.circleSprite.shader = owner.room.game.rainWorld.Shaders["VectorCircle"];
                this.circleSprite.alpha = 0.01f;
                this.fSprites.Add(circleSprite);
                owner.placedObjectsContainer.AddChild(this.circleSprite);

                for (int i = 0; i < owner.room.updateList.Count; i++)
                {
                    if (owner.room.updateList[i] is CosmeticLeaves && (owner.room.updateList[i] as CosmeticLeaves).placedObject == pObj)
                    {
                        this.obj = (owner.room.updateList[i] as CosmeticLeaves);
                        break;
                    }
                }
                if (this.obj == null)
                {
                    this.obj = new CosmeticLeaves(pObj, owner.room);
                    owner.room.AddObject(this.obj);
                }
            }

            public override void Refresh()
            {
                base.Refresh();
                lineSprite.SetPosition(absPos);
                lineSprite.scaleY = controlPanel.pos.magnitude;
                lineSprite.rotation = RWCustom.Custom.AimFromOneVectorToAnother(this.absPos, controlPanel.absPos);

                circleSprite.SetPosition(absPos);
                circleSprite.scaleY = handleU.pos.magnitude / 8f;
                circleSprite.scaleX = handleV.pos.magnitude / 8f;
                circleSprite.rotation = RWCustom.Custom.AimFromOneVectorToAnother(this.absPos, handleU.absPos);
                handleV.pos = RWCustom.Custom.PerpendicularVector(handleU.pos) * handleV.pos.magnitude;

                (this.pObj.data as CosmeticLeavesObjectData).panelPos = this.controlPanel.pos;
                (this.pObj.data as CosmeticLeavesObjectData).handles[0] = handleU.pos;
                (this.pObj.data as CosmeticLeavesObjectData).handles[1] = handleV.pos;

                //this.obj.meshDirty = true;
            }

            public class CosmeticLeavesControlPanel : Panel, IDevUISignals
            {
                private Button colorButton;
                private Button uselessButton;

                public CosmeticLeavesControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 85f), "Cosmetic Leaves")
                {

                    this.subNodes.Add(new CosmeticLeavesSlider(owner, "Depth_Slider", this, new Vector2(5f, 65f), "Depth: "));
                    this.subNodes.Add(new CosmeticLeavesSlider(owner, "Useless_Slider", this, new Vector2(5f, 45f), "Useless: "));

                    this.colorButton = new Button(owner, "Color_Button", this, new Vector2(5f, 25f), 240f, "Color");
                    this.subNodes.Add(this.colorButton);
                    this.uselessButton = new Button(owner, "Useless_Button", this, new Vector2(5f, 5f), 240f, "Useless");
                    this.subNodes.Add(this.uselessButton);
                }

                public override void Refresh()
                {
                    base.Refresh();
                    this.colorButton.Text = ((this.parentNode as CosmeticLeavesObjectRepresentation).pObj.data as CosmeticLeavesObjectData).color.ToString();
                    
                }

                public void Signal(DevUISignalType type, DevUINode sender, string message)
                {
                    string idstring = sender.IDstring;
                    if (idstring != null)
                    {
                        if (idstring == "Color_Button")
                        {
                            CosmeticLeavesObjectData data = ((this.parentNode as CosmeticLeavesObjectRepresentation).pObj.data as CosmeticLeavesObjectData);
                            data.color++;
                            if (data.color == CosmeticLeavesObjectData.CosmeticLeavesColor.NONE) data.color = CosmeticLeavesObjectData.CosmeticLeavesColor.EffectColor1;
                        }
                        if (idstring == "Useless_Button")
                        {
                            this.uselessButton.textColor = Color.red * UnityEngine.Random.value + Color.green * UnityEngine.Random.value + Color.blue * UnityEngine.Random.value;
                        }
                        this.Refresh();
                    }
                }

                public class CosmeticLeavesSlider : Slider
                {
                    public CosmeticLeavesSlider(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title, false, 110f)
                    {
                    }

                    // Token: 0x0600092A RID: 2346 RVA: 0x0005D624 File Offset: 0x0005B824
                    public override void Refresh()
                    {
                        base.Refresh();
                        float num = 0f;
                        string idstring = this.IDstring;
                        if (idstring != null)
                        {
                            if (idstring == "Depth_Slider")
                            {
                                num = ((this.parentNode.parentNode as CosmeticLeavesObjectRepresentation).pObj.data as CosmeticLeavesObjectData).depth;
                                base.NumberText = ((int)(num * 30f)).ToString();
                            }
                            if (idstring == "Useless_Slider")
                            {

                            }
                        }
                        base.RefreshNubPos(num);
                    }

                    public override void NubDragged(float nubPos)
                    {
                        string idstring = this.IDstring;
                        if (idstring != null)
                        {
                            if (idstring == "Depth_Slider")
                            {
                                ((this.parentNode.parentNode as CosmeticLeavesObjectRepresentation).pObj.data as CosmeticLeavesObjectData).depth = nubPos;
                            }
                            if (idstring == "Useless_Slider")
                            {
                                base.NumberText = nubPos.ToString();
                            }
                            this.parentNode.parentNode.Refresh();
                            this.Refresh();
                        }
                    }
                }
            }
        }

        public class CosmeticLeavesObjectData : PlacedObject.Data
        {
            public Vector2[] handles;
            public float depth;
            public CosmeticLeavesColor color;
            public Vector2 panelPos;


            public CosmeticLeavesObjectData(PlacedObject owner) : base(owner)
            {
                this.handles = new Vector2[2];
                this.handles[0] = new Vector2(0f, 40f);
                this.handles[1] = new Vector2(40f, 0f);

                this.depth = 0f;
                this.color = CosmeticLeavesColor.EffectColor1;
            }

            public override void FromString(string s)
            {
                string[] array = System.Text.RegularExpressions.Regex.Split(s, "~");
                handles[0].x = float.Parse(array[0]);
                handles[0].y = float.Parse(array[1]);
                handles[1].x = float.Parse(array[2]);
                handles[1].y = float.Parse(array[3]);
                panelPos.x = float.Parse(array[4]);
                panelPos.y = float.Parse(array[5]);
                depth = float.Parse(array[6]);
                color = (CosmeticLeavesColor)System.Enum.Parse(typeof(CosmeticLeavesColor), array[7]);
            }

            // Token: 0x06000924 RID: 2340 RVA: 0x0005D178 File Offset: 0x0005B378
            public override string ToString()
            {
                return string.Concat(new object[]
                {
                handles[0].x,"~",
                handles[0].y,"~",
                handles[1].x,"~",
                handles[1].y,"~",
                panelPos.x,"~",
                panelPos.y,"~",
                depth,"~",
                color
                });
            }

            public enum CosmeticLeavesColor
            {
                EffectColor1,
                EffectColor2,
                NONE
            }
        }
    }
}