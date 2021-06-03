using ManagedPlacedObjects;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RWCustom;

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
                    new PlacedObjectsManager.Vector2Field("01", new Vector2(-100, 30)),
                    new PlacedObjectsManager.ColorField("05", new Color(0.4f, 0.6f, 0.8f), PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider, "Color"),
                    };
                public Vector2 pos => owner.pos;
#pragma warning disable 0649 // We're reflecting over these fields, stop worrying about it stupid compiler

                [BackedByField("01")]
                public Vector2 end;
                [PlacedObjectsManager.IntegerField("02", 1, 100, 8, displayName: "Sparks", control: PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider)]
                public int numberOfSparks;
                [PlacedObjectsManager.FloatField("03", 0f, 20f, 4f, 0.1f, displayName: "Jumpyness")]
                public float jumpyness;
                [PlacedObjectsManager.FloatField("04", 0f, 1f, 0.005f, 0.001f, displayName: "Tightness")]
                public float tightness;
                [BackedByField("05")]
                public Color color;
                [PlacedObjectsManager.FloatField("06", -5f, 5f, 0.5f, 0.01f, displayName: "Pull")]
                public float gravitypull;
                [PlacedObjectsManager.FloatField("07", -0.5f, 0.5f, 0.005f, 0.001f, displayName: "Centerness")]
                public float centerness;
                [PlacedObjectsManager.FloatField("08", 0f, 1f, 0.05f, 0.001f, displayName: "ellasticity")]
                public float ellasticity;
                [PlacedObjectsManager.FloatField("09", 0f, 1f, 0.05f, 0.001f, displayName: "spread")]
                public float spread;
                [PlacedObjectsManager.FloatField("10", 0f, 10f, 0.5f, 0.01f, displayName: "minspace")]
                public float minspace;
                [PlacedObjectsManager.FloatField("11", 0f, 10f, 2f, 0.01f, displayName: "maxspace")]
                public float maxspace;
                [PlacedObjectsManager.IntegerField("12", 0, 120, 12, displayName: "natcooldown")]
                public int natcooldown;
                [PlacedObjectsManager.IntegerField("13", 0, 120, 30, displayName: "shockcooldown")]
                public int shockcooldown;
#pragma warning restore 0649
                public ElectricArcData(PlacedObject owner) : base(owner, otherFields) { }
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
            private int cooldown;

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (spark == null || spark.slatedForDeletetion || spark.broken)
                {
                    spark = null;
                    cooldown--;
                    if (cooldown <= 0)
                    {
                        room.AddObject(spark = new Spark(room, pObj.pos, pObj.pos + data.end, this, data.numberOfSparks, data));
                        cooldown = -1;
                    }
                }
            }

            public class Spark : CosmeticSprite
            {
                public Vector2 start;
                public Vector2 stop;
                private readonly ElectricArc owner;
                private readonly int nNodes;
                private readonly ElectricArcData data;
                private readonly float spacing;
                private SparkNode[] nodes;
                public bool broken = false;
                private float intensity;
                private StaticSoundLoop soundLoop;
                private StaticSoundLoop disruptedLoop;

                public Spark(Room room, Vector2 start, Vector2 stop, ElectricArc owner, int nNodes, ElectricArcData data)
                {

                    this.start = start;
                    this.stop = stop;
                    this.owner = owner;
                    this.nNodes = nNodes;
                    this.data = data;
                    this.spacing = (start - stop).magnitude / (float)nNodes;
                    this.nodes = new SparkNode[nNodes];

                    intensity = 1f;

                    for (int i = 0; i < nNodes; i++)
                    {
                        nodes[i] = new SparkNode(Vector2.Lerp(start, stop, Mathf.InverseLerp(-1, nNodes, i)));
                    }

                    this.soundLoop = new StaticSoundLoop(SoundID.Zapper_LOOP, Vector2.Lerp(start, stop, 0.5f), room, 0f, 1f);
                    this.disruptedLoop = new StaticSoundLoop(SoundID.Zapper_Disrupted_LOOP, Vector2.Lerp(start, stop, 0.5f), room, 0f, 1f);

                    Debug.Log("Created SPARK");
                }

                public override void Update(bool eu)
                {
                    base.Update(eu);

                    this.soundLoop.Update();
                    this.disruptedLoop.Update();
                    soundLoop.volume = Mathf.Clamp01(intensity * (broken ? 0.5f : 1f));
                    disruptedLoop.volume = Mathf.Clamp01(intensity * (broken ? 0.8f : 0f));

                    Vector2 direction = (stop - start).normalized;
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        Vector2 jump = UnityEngine.Random.insideUnitCircle * data.jumpyness;
                        nodes[i].vel += jump *(broken ? 2f : 1f);// * dir * (1 / Mathf.Pow(dir.magnitude + 0.1f, 0.5f)) 
                        nodes[i].vel += new Vector2(0f, room.gravity) * data.gravitypull;
                        Vector2 correctPosition = Vector2.Lerp(start, stop, Mathf.InverseLerp(-1, nNodes, i));
                        Vector2 pull = correctPosition - nodes[i].pos;
                        nodes[i].vel += pull * data.tightness;
                        pull -= Vector2.Dot(pull, direction) * direction;
                        nodes[i].vel += pull * data.centerness;

                        for (int j = -1; j < 2; j+=2)
                        {
                            if (i == 0 && j == -1) pull = start - nodes[i].pos;
                            else if(i == nodes.Length - 1 && j == 1) pull = stop - nodes[i].pos;
                            else pull = nodes[i + j].pos - nodes[i].pos;
                            nodes[i].vel += pull * data.ellasticity / 2f;
                            float mag = pull.magnitude;
                            nodes[i].vel += data.spread / 2f * pull.normalized * (mag - spacing*data.minspace);
                            if (mag > data.maxspace * spacing) this.Break();
                        }

                    }
                    Vector2 previous = start;
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        nodes[i].Update();
                        foreach (var physgroup in room.physicalObjects)
                        {
                            foreach (var phys in physgroup)
                            {
                                if((phys.firstChunk.pos - nodes[i].pos).magnitude < data.maxspace + phys.collisionRange) // in range for testing
                                {
                                    for (int k = 0; k < phys.bodyChunks.Length; k++)
                                    {
                                        BodyChunk chunk = phys.bodyChunks[k];
                                        Vector2 closest = Custom.ClosestPointOnLineSegment(previous, nodes[i].pos, chunk.pos);
                                        if((closest - chunk.pos).magnitude < chunk.rad + 2 || Custom.IsPointBetweenPoints(chunk.pos, chunk.lastPos, closest)) // NOT PERFECT would need some more serious checks considering lastpos but its goodenuff
                                        {
                                            this.Shock(phys, k, closest);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (broken)
                    {
                        this.intensity *= 0.85f;
                        if (intensity < 0.05f) this.Destroy();
                    }
                }

                private void Shock(PhysicalObject phys, int chunkindex, Vector2 contact)
                {
                    Creature crit = phys as Creature;
                    if (broken && crit != null)  // shocked during decay
                    {
                        crit.room.AddObject(new CreatureSpasmer(crit, true, Mathf.FloorToInt(20 * intensity)));
                        crit.Stun(Mathf.FloorToInt(20 * intensity));
                    }
                    else
                    {
                        this.intensity = Mathf.Lerp(2.0f, phys.TotalMass, 0.5f);
                        this.broken = true;
                        owner.cooldown = data.shockcooldown;
                        if (crit != null)
                        {
                            crit.Die();
                            crit.room.AddObject(new CreatureSpasmer(crit, true, Mathf.FloorToInt(40 * intensity)));
                        }
                        this.room.AddObject(new ZapCoil.ZapFlash(contact, Mathf.InverseLerp(-0.05f, 15f, phys.bodyChunks[chunkindex].rad)));
                        phys.bodyChunks[chunkindex].vel += ((phys.bodyChunks[chunkindex].pos - contact).normalized * 6f + Custom.RNV() * UnityEngine.Random.value) / phys.bodyChunks[chunkindex].mass;
                        this.room.PlaySound(SoundID.Zapper_Zap, phys.bodyChunks[chunkindex].pos, 1f, 1f);
                    }
                }

                private void Break()
                {
                    if (broken) return;
                    this.broken = true;
                    this.intensity = 1.5f;
                    owner.cooldown = data.natcooldown;
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
                    TriangleMesh outermesh = TriangleMesh.MakeLongMesh(nNodes + 1, false, false, "Futile_White");
                    TriangleMesh innermesh = TriangleMesh.MakeLongMesh(nNodes + 1, false, false, "Futile_White");
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
                        float factor = Mathf.Lerp(edge, 1 - edge, Mathf.InverseLerp(0, nodes.Length, i - 0.3f));
                        float factor2 = Mathf.Lerp(edge, 1 - edge, Mathf.InverseLerp(0, nodes.Length, i + 0.3f));
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
                    AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));

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
                    float width = 0.5f;

                    for (int i = 0; i <= nodes.Length; i++)
                    {
                        Vector2 next = i == nodes.Length ? stop : Vector2.Lerp(nodes[i].pos, nodes[i].lastPos, timeStacker);
                        Vector2 perp = RWCustom.Custom.PerpendicularVector(next - prev).normalized;
                        Vector2 nextPerp = i < nodes.Length - 1 ? RWCustom.Custom.PerpendicularVector(Vector2.Lerp(nodes[i + 1].pos, nodes[i + 1].lastPos, timeStacker) - next).normalized
                            : i == nodes.Length - 1 ? RWCustom.Custom.PerpendicularVector(stop - next).normalized : perp;
                        perp = Vector2.Lerp(lastPerp, perp, 0.3f).normalized;
                        nextPerp = Vector2.Lerp(perp, nextPerp, 0.5f).normalized;
                        float nextWidth;
                        if (i != nodes.Length) nextWidth = Mathf.Lerp(1f + Mathf.Abs(Vector2.Dot(perp, nodes[i].vel)), width, 0.5f);
                        else nextWidth  = 0.5f;

                        Vector2 avr1 = i == 0 ? prev : Vector2.Lerp(prev, next, 0.2f);
                        Vector2 avr2 = i == nodes.Length ? next : Vector2.Lerp(prev, next, 0.8f);

                        innermesh.MoveVertice(4 * i + 0, avr1 + perp * width - camPos);
                        innermesh.MoveVertice(4 * i + 1, avr1 - perp * width - camPos);
                        innermesh.MoveVertice(4 * i + 2, avr2 + nextPerp * nextWidth - camPos);
                        innermesh.MoveVertice(4 * i + 3, avr2 - nextPerp * nextWidth - camPos);

                        avr1 = i == 0 ? prev : Vector2.Lerp(prev, next, 0.3f);
                        avr2 = i == nodes.Length ? next : Vector2.Lerp(prev, next, 0.7f);

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

                    innermesh.alpha = intensity * 0.7f;
                    outermesh.alpha = intensity * 0.45f;

                    base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                }

                public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
                {

                }
            }
        }
    }
}