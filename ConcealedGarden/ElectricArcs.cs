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
            }

            Spark spark;
            public override void Update(bool eu)
            {
                base.Update(eu);
                if (spark == null || spark.slatedForDeletetion || spark.broken)
                {
                    room.AddObject(spark = new Spark(pObj.pos, pObj.pos + data.end, data.numberOfSparks, Mathf.Lerp(0.5f, 20f, data.jumpyness), Mathf.Lerp(0.001f, 0.1f, Mathf.Pow(data.tightness, 2)), Mathf.Lerp(0.001f, 0.1f, Mathf.Pow(data.tightness, 2)), 6f, 16f, room));
                }
            }

            public class Spark : CosmeticSprite
            {
                private readonly Vector2 start;
                private readonly Vector2 stop;
                private readonly int nNodes;
                private float jump;
                private float tightness;
                private readonly float ellasticPull;
                private readonly float minspace;
                private readonly float maxspace;
                private SparkNode[] nodes;
                public bool broken = false;
                private float intensity;


                public Spark(Vector2 start, Vector2 stop, int nNodes, float jump, float tightness, float ellasticPull, float minspace, float maxspace, Room room)
                {
                    
                    this.start = start;
                    this.stop = stop;
                    this.nNodes = nNodes;
                    this.jump = jump;
                    this.tightness = tightness;
                    this.ellasticPull = ellasticPull;
                    this.minspace = (start - stop).magnitude / nNodes * (2f - tightness);//minspace;
                    this.maxspace = this.minspace * (2f - tightness); // maxspace;
                    this.room = room;
                    this.nodes = new SparkNode[nNodes];

                    intensity = 1;

                    for (int i = 0; i < nNodes; i++)
                    {
                        nodes[i] = new SparkNode(Vector2.Lerp(start, stop, Mathf.InverseLerp(-1, nNodes, i)));
                    }
                    Debug.LogError("Created SPARK");
                }

                public override void Update(bool eu)
                {
                    base.Update(eu);

                    if (!broken)
                    {
                        for (int i = 0; i < nodes.Length; i++)
                        {
                            Vector2 dir = UnityEngine.Random.insideUnitCircle;
                            nodes[i].vel += jump * dir * (1 / Mathf.Pow(dir.magnitude + 0.1f, 0.5f)) + new Vector2(0f, room.gravity);

                            if (i > 0)
                            {
                                Vector2 pull = nodes[i - 1].pos - nodes[i].pos;
                                nodes[i].vel += pull * ellasticPull / 2f;
                                float mag = pull.magnitude;
                                if (mag > minspace)
                                    nodes[i].vel += tightness / 2f * pull.normalized * (mag - minspace);
                                if (mag > maxspace) broken = true;
                            }
                            if (i == 0)
                            {
                                Vector2 pull = start - nodes[i].pos;
                                nodes[i].vel += pull * ellasticPull;
                                float mag = pull.magnitude;
                                if (mag > minspace)
                                    nodes[i].vel += tightness * pull.normalized * (mag - minspace);
                                if (mag > maxspace) broken = true;
                            }
                            if (i < nodes.Length - 1)
                            {
                                Vector2 pull = nodes[i + 1].pos - nodes[i].pos;
                                nodes[i].vel += pull * ellasticPull / 2f;
                                float mag = pull.magnitude;
                                if (mag > minspace)
                                    nodes[i].vel += tightness / 2f * pull.normalized * (mag - minspace);
                                if (mag > maxspace) broken = true;
                            }
                            if (i == nodes.Length - 1)
                            {
                                Vector2 pull = stop - nodes[i].pos;
                                nodes[i].vel += pull * ellasticPull;
                                float mag = pull.magnitude;
                                if (mag > minspace)
                                    nodes[i].vel += tightness * pull.normalized * (mag - minspace);
                                if (mag > maxspace) broken = true;
                            }

                        }
                        for (int i = 0; i < nodes.Length; i++)
                        {
                            nodes[i].Update();
                        }
                    }
                    if(broken)
                    {
                        this.jump *= 1.15f;
                        this.tightness *= 0.9f;
                        this.intensity *= 0.8f;
                        if (intensity < 0.05f) this.Destroy();
                    }
                    
                }

                public class SparkNode
                {
                    public Vector2 lastPos;
                    public Vector2 vel;
                    public Vector2 pos;
                    public float fric = 0.5f;

                    public SparkNode(Vector2 pos)
                    {
                        this.lastPos = pos;
                        this.vel = Vector2.zero;
                        this.pos = pos;
                    }

                    public void Update()
                    {
                        this.lastPos = pos;
                        this.pos += vel;
                        this.vel *= fric;
                    }
                }

                public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
                {
                    Debug.LogError("SPARK sprt init");
                    TriangleMesh outermesh = TriangleMesh.MakeLongMesh(nNodes+1, false, false, "Futile_White");
                    TriangleMesh innermesh = TriangleMesh.MakeLongMesh(nNodes+1, false, false, "Futile_White");
                    sLeaser.sprites = new FSprite[2] { outermesh, innermesh }; //nNodes + 
                    //outermesh.UVvertices[0] = new Vector2(0f, 0f);
                    //outermesh.UVvertices[1] = new Vector2(1f, 0f);
                    //outermesh.UVvertices[2] = new Vector2(0f, 0.4f);
                    //outermesh.UVvertices[3] = new Vector2(1f, 0.4f);
                    //outermesh.UVvertices[4] = new Vector2(0f, 1f - 0.4f);
                    //outermesh.UVvertices[5] = new Vector2(1f, 1f - 0.4f);
                    //outermesh.UVvertices[6] = new Vector2(0f, 1f);
                    //outermesh.UVvertices[7] = new Vector2(1f, 1f);


                    float edge = 0.4f;

                    outermesh.UVvertices[0] = new Vector2(0f, 0f);
                    outermesh.UVvertices[1] = new Vector2(1f, 0f);
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        float factor = Mathf.Lerp(edge, 1 - edge, Mathf.InverseLerp(0, nodes.Length, i-0.3f));
                        float factor2 = Mathf.Lerp(edge, 1 - edge, Mathf.InverseLerp(0, nodes.Length, i+0.3f));
                        outermesh.UVvertices[i * 4 + 2] = new Vector2(0f, factor);
                        outermesh.UVvertices[i * 4 + 3] = new Vector2(1f, factor);
                        outermesh.UVvertices[i * 4 + 4] = new Vector2(0f, factor2);
                        outermesh.UVvertices[i * 4 + 5] = new Vector2(1f, factor2);
                    }
                    outermesh.UVvertices[nodes.Length * 4 + 2] = new Vector2(0f, 1f);
                    outermesh.UVvertices[nodes.Length * 4 + 3] = new Vector2(1f, 1f);

                    innermesh.shader = rCam.room.game.rainWorld.Shaders["OverseerZip"];
                    innermesh.color = new Color(0.56f, 0.66f, 0.98f);
                    innermesh.alpha = intensity;


                    outermesh.shader = rCam.room.game.rainWorld.Shaders["FlareBomb"];
                    outermesh.color = new Color(0.01f, 0.04f, 1f);
                    outermesh.alpha = 0.8f;

                    //this.debuglabel = new FLabel("font", "shader");
                    //rCam.ReturnFContainer("HUD").AddChild(debuglabel);

                    ApplyPalette(sLeaser, rCam, rCam.currentPalette);
                    AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Water"));

                    //shaderList = rCam.room.game.rainWorld.Shaders.ToList();
                }

                public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
                {
                    //TriangleMesh trimesh = sLeaser.sprites[0] as TriangleMesh;

                    TriangleMesh outermesh = sLeaser.sprites[0] as TriangleMesh;
                    TriangleMesh innermesh = sLeaser.sprites[1] as TriangleMesh;
                    Vector2 prev = start;
                    Vector2 lastPerp = nNodes > 0 ? RWCustom.Custom.PerpendicularVector(Vector2.Lerp(nodes[0].pos, nodes[0].lastPos, timeStacker) - prev).normalized : RWCustom.Custom.PerpendicularVector(stop - prev).normalized;
                    lastPerp = Vector2.Lerp(lastPerp, RWCustom.Custom.PerpendicularVector(stop - start).normalized, 0.6f);
                    float width = 1f;

                    for (int i = 0; i <= nodes.Length; i++)
                    {
                        Vector2 next = i == nodes.Length ? stop : Vector2.Lerp(nodes[i].pos, nodes[i].lastPos, timeStacker);
                        Vector2 perp = RWCustom.Custom.PerpendicularVector(next - prev).normalized;
                        Vector2 nextPerp = i < nodes.Length-1 ? RWCustom.Custom.PerpendicularVector(Vector2.Lerp(nodes[i+1].pos, nodes[i+1].lastPos, timeStacker)-next).normalized
                            : i == nodes.Length - 1 ? RWCustom.Custom.PerpendicularVector(stop - next).normalized : perp;
                        perp = Vector2.Lerp(lastPerp, perp, 0.3f).normalized;
                        nextPerp = Vector2.Lerp(perp, nextPerp, 0.5f).normalized;
                        float nextWidth = i == nodes.Length ? 1f : 1f + Mathf.Abs(Vector2.Dot(perp, nodes[i].vel));
                        nextWidth = Mathf.Lerp(nextWidth, width, 0.5f);

                        Vector2 avr1 = Vector2.Lerp(prev, next, 0.2f);
                        Vector2 avr2 = Vector2.Lerp(prev, next, 0.8f);

                        innermesh.MoveVertice(4 * i + 0, avr1 + perp * width - camPos);
                        innermesh.MoveVertice(4 * i + 1, avr1 - perp * width - camPos);
                        innermesh.MoveVertice(4 * i + 2, avr2 + nextPerp * nextWidth - camPos);
                        innermesh.MoveVertice(4 * i + 3, avr2 - nextPerp * nextWidth - camPos);

                        avr1 = Vector2.Lerp(prev, next, 0.3f);
                        avr2 = Vector2.Lerp(prev, next, 0.7f);

                        if (i == 0) avr1 = prev + (prev - next).normalized * 40f * intensity;
                        if (i == nodes.Length) avr2 = next + (next - prev).normalized * 40f * intensity;
                        outermesh.MoveVertice(4 * i + 0, avr1 + perp * 20 * Mathf.Lerp(width * intensity, 1f, 0.7f) - camPos);
                        outermesh.MoveVertice(4 * i + 1, avr1 - perp * 20 * Mathf.Lerp(width * intensity, 1f, 0.7f) - camPos);
                        outermesh.MoveVertice(4 * i + 2, avr2 + nextPerp * 20 * Mathf.Lerp(nextWidth * intensity, 1f, 0.7f) - camPos);
                        outermesh.MoveVertice(4 * i + 3, avr2 - nextPerp * 20 * Mathf.Lerp(nextWidth * intensity, 1f, 0.7f) - camPos);

                        prev = next;
                        lastPerp = nextPerp;
                        width = nextWidth;
                    }

                    innermesh.alpha = intensity*0.6f;
                    outermesh.alpha = intensity*0.4f;

                    base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                }

                public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
                {
                    
                }
            }
        }
    }
}