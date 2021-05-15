using ManagedPlacedObjects;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ConcealedGarden
{
    internal class ElectricArcs
    {
        internal static void Register()
        {
            //throw new NotImplementedException();
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("ElectricArc", typeof(ElectricArc), typeof(ElectricArc.ElectricArcData), typeof(PlacedObjectsManager.ManagedRepresentation)));
        }

        public class ElectricArc : UpdatableAndDeletable
        {
            public class ElectricArcData : PlacedObjectsManager.ManagedData
            {
                public static PlacedObjectsManager.ManagedField[] otherFields = new PlacedObjectsManager.ManagedField[]{
                    new PlacedObjectsManager.Vector2Field("end", new Vector2(-100, 30)),
                    new PlacedObjectsManager.ColorField("color", new Color(0.4f, 0.6f, 0.8f), PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider, "Color"),
                    };
            public Vector2 pos => owner.pos;
                #pragma warning disable 0649 // We're reflecting over these fields, stop worrying about it stupid compiler

                [BackedByField("end")]
                public Vector2 end;
                [PlacedObjectsManager.IntegerField("num", 1, 10, 3, displayName:"Sparks")]
                public int numberOfSparks;
                [PlacedObjectsManager.FloatField("jmp", 0f, 1f, 0.5f, 0.01f, displayName: "Jumpyness")]
                public float jumpyness;
                [PlacedObjectsManager.FloatField("tht", 0f, 1f, 0.5f, 0.01f, displayName: "Tightness")]
                public float tightness;
                [BackedByField("color")]
                public Color color;
                #pragma warning restore 0649
                public ElectricArcData(PlacedObject owner) : base(owner, otherFields)
                {

                }
            }

            private readonly PlacedObject pObj;
            //private readonly Queue<Spark> sparks;

            private ElectricArcData data => pObj.data as ElectricArcData;
            public ElectricArc(Room room, PlacedObject pObj)
            {
                this.room = room;
                this.pObj = pObj;

                //this.sparks = new Queue<Spark>();

                room.AddObject(new Spark(pObj.pos, pObj.pos+data.end, 1, 0, 0, 0, room));
            
            }

            public class Spark : CosmeticSprite
            {
                private readonly Vector2 start;
                private readonly Vector2 stop;
                private readonly int nNodes;
                private readonly float jump;
                private readonly float minspace;
                private readonly float maxspace;
                private SparkNode[] nodes;

                public Spark(Vector2 start, Vector2 stop, int nNodes, float jump, float minspace, float maxspace, Room room)
                {

                    this.start = start;
                    this.stop = stop;
                    this.nNodes = nNodes;
                    this.jump = jump;
                    this.minspace = minspace;
                    this.maxspace = maxspace;
                    this.room = room;
                    this.nodes = new SparkNode[nNodes];
                    Debug.LogError("Created SPARK");
                }

                public override void Update(bool eu)
                {
                    base.Update(eu);

                    for (int i = 0; i < nodes.Length; i++)
                    {
                        Vector2 dir = UnityEngine.Random.insideUnitCircle;
                        nodes[i].pos += jump * dir * (1 / Mathf.Pow(dir.magnitude + 0.1f, 0.5f)) + new Vector2(0f, -room.gravity);
                    }
                }


                public struct SparkNode
                {
                    public Vector2 lastPos;
                    public Vector2 vel;
                    public Vector2 pos;

                    public SparkNode(Vector2 pos)
                    {
                        this.lastPos = pos;
                        this.vel = Vector2.zero;
                        this.pos = pos;
                    }
                }



                public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
                {
                    Debug.LogError("SPARK sprt init");
                    TriangleMesh triangleMesh = TriangleMesh.MakeLongMesh(2, false, true, "Futile_White");
                    triangleMesh.alpha = 0.5f;
                    sLeaser.sprites = new FSprite[1] { triangleMesh }; //nNodes + 
                    triangleMesh.UVvertices[0] = new Vector2(0f, 0f);
                    triangleMesh.UVvertices[1] = new Vector2(1f, 0f);
                    triangleMesh.UVvertices[2] = new Vector2(0f, 0.4f);
                    triangleMesh.UVvertices[3] = new Vector2(1f, 0.4f);
                    triangleMesh.UVvertices[4] = new Vector2(0f, 1f - 0.4f);
                    triangleMesh.UVvertices[5] = new Vector2(1f, 1f - 0.4f);
                    triangleMesh.UVvertices[6] = new Vector2(0f, 1f);
                    triangleMesh.UVvertices[7] = new Vector2(1f, 1f);

                    triangleMesh.verticeColors[0] = new Color(1f, 0f, 0f, 1f);
                    triangleMesh.verticeColors[1] = new Color(1f, 0f, 0f, 0f);
                    triangleMesh.verticeColors[2] = new Color(0f, 1f, 0f, 1f);
                    triangleMesh.verticeColors[3] = new Color(0f, 1f, 0f, 0f);
                    triangleMesh.verticeColors[4] = new Color(0f, 0f, 1f, 1f);
                    triangleMesh.verticeColors[5] = new Color(0f, 0f, 1f, 0f);
                    triangleMesh.verticeColors[6] = new Color(1f, 1f, 1f, 1f);
                    triangleMesh.verticeColors[7] = new Color(1f, 1f, 1f, 0f);


                    //sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["FlareBomb"];
                    //sLeaser.sprites[0].color = new Color(0.01f, 0.04f, 1f);

                    this.debuglabel = new FLabel("font", "shader");
                    rCam.ReturnFContainer("HUD").AddChild(debuglabel);

                    ApplyPalette(sLeaser, rCam, rCam.currentPalette);
                    AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));

                    shaderList = rCam.room.game.rainWorld.Shaders.ToList();
                }

                int currshader = 0;
                bool lastL = false;
                private List<KeyValuePair<string, FShader>> shaderList;
                private FLabel debuglabel;

                public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
                {
                    base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                    TriangleMesh trimesh = sLeaser.sprites[0] as TriangleMesh;
                    Vector2 perp = RWCustom.Custom.PerpendicularVector(stop - start).normalized;

                    Vector2 avr1 = 0.7f * start + 0.3f * stop;
                    Vector2 avr2 = 0.3f * start + 0.7f * stop;

                    trimesh.MoveVertice(0, start + perp * 20 - camPos);
                    trimesh.MoveVertice(1, start - perp * 20 - camPos);
                    trimesh.MoveVertice(2, avr1 + perp * 20 - camPos);
                    trimesh.MoveVertice(3, avr1 - perp * 20 - camPos);

                    trimesh.MoveVertice(4, avr2 + perp * 40 - camPos);
                    trimesh.MoveVertice(5, avr2 - perp * 40 - camPos);
                    trimesh.MoveVertice(6, stop + perp * 40 - camPos);
                    trimesh.MoveVertice(7, stop - perp * 40 - camPos);

                    
                    bool l = UnityEngine.Input.GetKey("l");
                    if (l && !lastL)
                    {
                        currshader = (currshader + 1) % shaderList.Count;
                    }
                    lastL = l;
                    
                    sLeaser.sprites[0].shader = shaderList[currshader].Value;
                    debuglabel.SetPosition(start + new Vector2(-50, 30) - camPos);
                    debuglabel.text = shaderList[currshader].Key;
                }

                public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
                {
                    
                }
            }
        }
    }
}