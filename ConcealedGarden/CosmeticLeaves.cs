using DevInterface;
using UnityEngine;

namespace ConcealedGarden
{
    internal class CosmeticLeaves : UpdatableAndDeletable, IDrawable
    {
        private PlacedObject placedObject;

        public CosmeticLeaves(PlacedObject placedObject, Room instance)
        {
            this.placedObject = placedObject;
            this.room = instance;



        }

        public override void Update(bool eu)
        {
            base.Update(eu);



            //if (room.ViewedByAnyCamera(this.pos, this.maxrad * 2))
            //{
            //    this.updatewindcycle();
            //    for (int i = 0; i < this.leaves.length; i++)
            //    {
            //        this.leaves[i].update();
            //    }
            //}
        }

        public class CosmeticLeaf
        {
            Vector3 sphericalCoords;

            CosmeticLeaves owner;
        }


        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White", true);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {

        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {

        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {

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
                            this.uselessButton.textColor = Color.red * Random.value + Color.green * Random.value + Color.blue * Random.value;
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