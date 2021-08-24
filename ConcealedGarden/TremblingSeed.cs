using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using static ConcealedGarden.Utils.CGUtils;
using static RWCustom.Custom;
using ConcealedGarden.Utils;
using static UnityEngine.Mathf;

namespace ConcealedGarden
{
    //todo: redo aoe, add visual area indication, add cooldown color telegraph, make and import sprites
    //make midair stopping smoother, make 
    //idea stash: downslam on hang expire
    public static class EnumExt_SeedEnumThings
    {
        public static AbstractPhysicalObject.AbstractObjectType ShockSeed;
    }
    public class TremblingSeed : Weapon
    {
        public TremblingSeed (AbstractPhysicalObject apo, World world) : base (apo, world)
        {
            bodyChunks = new BodyChunk[1];
            bodyChunks[0] = new BodyChunk(this, 0, default, 7f, 0.3f);
            bodyChunkConnections = new BodyChunkConnection[0];
            airFriction = 1f;
            gravity = 0.7f;
            collisionLayer = 2;
            buoyancy = 1.03f;
            tailPos = firstChunk.pos;
            osp = new GOscParams(UnityEngine.Random.Range(5f, 7f), 
                UnityEngine.Random.Range(0.05f, 0.15f), 
                UnityEngine.Random.Range(-0.5f, 0.5f),
                (UnityEngine.Random.value > 0.5f) ? new Func<float, float>(Sin) : new Func<float, float>(Cos));
        }
        public override void ChangeMode(Mode newMode)
        {
        //#warning finish ChangeMode
        //more or less like that?
        //should watch out for escaping indeces
            var oldmode = this.mode;
            base.ChangeMode(newMode);
            switch (newMode)
            {
                case Mode.Thrown:
                    windup = 7;
                    popCharge = 0;
                    break;
                case Mode.StuckInWall:
                    stuckPos = firstChunk.pos;
                    popCharge = 0;
                    ActionCycle = 200;
                    break;
                default:
                    if (oldmode == Mode.StuckInWall) { Cooldown = 1200; }
                    break;
            }
        }
        #region idrawable things
        float lt;
        GOscParams osp;
        float lastShellOffset;
        float shellOffset;
        //just a green rock for now
        const int core = 2;
        int shell(bool first) => (first)? 0 : 1;
        const int tail = 3;

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[4];
            sLeaser.sprites[core] = new FSprite("Circle20") { scale = 0.5f, color = new Color(0f, 1f, 0f) };
            sLeaser.sprites[shell(true)] = new FSprite("pixel") { scale = 5, color = new Color(1f, 0f, 0f) };
            sLeaser.sprites[shell(false)] = new FSprite("pixel") { scale = 5, color = new Color(1f, 0f, 0f) };
            var meshT = new TriangleMesh.Triangle[]
            {
                new TriangleMesh.Triangle(0, 1, 2)
            };
            var MESH = new TriangleMesh("Futile_White", meshT, false, false) { color = Color.white };
            sLeaser.sprites[tail] = MESH;
            AddToContainer(sLeaser, rCam, null);
        }
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            var cOrigin = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
            var cRot = LerpAngle(VecToDeg(lastRotation), VecToDeg(rotation), timeStacker);
            sLeaser.sprites[core].SetPosition(cOrigin);
            var shellDir = PerpendicularVector(rotation).normalized;
            var sOf = shellDir * Lerp(lastShellOffset, shellOffset, timeStacker);
            sLeaser.sprites[shell(true)].SetPosition(cOrigin + sOf);
            sLeaser.sprites[shell(true)].rotation = VecToDeg(shellDir);
            sLeaser.sprites[shell(false)].SetPosition(cOrigin - sOf);
            sLeaser.sprites[shell(false)].rotation = VecToDeg(shellDir * -1f);
            var tailmesh = sLeaser.sprites[tail] as TriangleMesh;
            tailmesh.MoveVertice(0, cOrigin + shellDir * 3f);
            tailmesh.MoveVertice(1, cOrigin + shellDir * -3f);
            tailmesh.MoveVertice(2, Vector2.Lerp(tailPos, firstChunk.lastPos, timeStacker) - camPos);
            if (slatedForDeletetion) sLeaser.CleanSpritesAndRemove();
        }
        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaser, rCam, newContatiner);
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {

            //base.ApplyPalette(sLeaser, rCam, palette);
        }
        #endregion
        //kills windups
        public override void HitWall()
        {
            base.HitWall();
            CancelEverything();
        }
        //kills windups
        public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
        {
            var outcome = base.HitSomething(result, eu);
            if (outcome) CancelEverything();
            return outcome;
        }
        //override 

        public override void Update(bool eu)
        {
            lt++;
            if (firstChunk.ContactPoint.y != 0) rotationSpeed = 0f;
            base.Update(eu);
            lastShellOffset = shellOffset;
            shellOffset = osp.GetRes(lt);
            lastPopCharge = popCharge;
            lastActionCycle = ActionCycle;
            switch (this.mode)
            {
                case Mode.Thrown:
                    windup--;
                    if (windup > 0 || Cooldown > 0 || RemainingUses == 0) break;
                    this.RunChargeupCheck();
                    break;
                case Mode.StuckInWall:
                    airFriction = 5f;
                    gravity = -0.02f;
                    ActionCycle--;
                    if (ActionCycle <= 0) { this.ChangeMode(Mode.Free); CancelEverything(); }
                    break;
                default:
                    airFriction = 1f;
                    gravity = 0.7f;
                    CancelEverything();
                    break;
            }
        }

        private Vector2 stuckPos;
        //increasing chance to pop every frame, faster if creatures are nearby
        //could also make it home in on creatures slightly?.. bad idea prolly
        public void RunChargeupCheck()
        {
            if (this.room?.abstractRoom == null) return;
            foreach (var crit in this.room.abstractRoom.creatures)
            {
                if (ManhattanDistance(this.abstractPhysicalObject.pos, crit.pos) <= 10) popCharge += 3;
            }
            this.popCharge += 20;
            if (UnityEngine.Random.value < (float)popCharge / 100) this.Pop();
        }
        internal void Pop()
        {
            Debug.Log("seed POP!");
            this.room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_On, this.firstChunk);
            this.ChangeMode(Mode.StuckInWall);
            this.RemainingUses = Max(RemainingUses - 1, 0);
            this.room.AddObject(new SeedDistortion(this.firstChunk.pos, this.ActionCycle, 120f));
        }
        internal void CancelEverything()
        {
            windup = 0;
            ActionCycle = 0;
            lastActionCycle = 0;
            popCharge = 0;
            lastPopCharge = 0;
        }

        internal AbstractTremblingSeed abstractSeed => (AbstractTremblingSeed)this.abstractPhysicalObject;
        //total uses are limited
        //NOTE: add a way to recharge
        //or maybe just make it recharge fully by not serializing remaining uses lol
        internal int RemainingUses { get { return abstractSeed.RemainingUses; } set { abstractSeed.RemainingUses = value; } }
        //long term use cooldown, abstract stored
        internal int Cooldown { get { return abstractSeed.Cooldown; } set { abstractSeed.Cooldown = value; } }
        //post throw windup, can only attemot to pop when it's done
        internal int windup;
        //counts how long the seed should remain in active phase
        internal int ActionCycle;
        internal int lastActionCycle;
        //used during flight, after windup is over, to determine how likely the seed is to pop
        internal int popCharge;
        internal int lastPopCharge;

        public class SeedDistortion : UpdatableAndDeletable//, IDrawable
        {
            //magical light it is
            //should be not too cursed now
            public SeedDistortion(TremblingSeed owner, int duration = 0, float rad = 0f, float gmod = 0.9f)
            {
                //pos = mypos;
                oSeed = owner;
                distortionLifetime = duration;
                radius = rad;
                gSlice = gmod;
                affectedObjects = new List<int>();
                mLs = new LightSource(pos, false, new Color(0.2f, 0.8f, 0.2f).Deviation(new Color(0.05f, 0.2f, 0.05f)), this);
                mLs.HardSetPos(pos);
                mLs.requireUpKeep = true;
                Debug.Log($"Seed distortion created: {ownerID}, d:{duration}, rad:{rad}"); }

            Color forDevs = new Color(0.01f, 0.01f, 0.01f);
            LightSource mLs;
            Smoke.FireSmoke mySmoke;
            //List<Smoke.FireSmoke> mSmokes;
            TremblingSeed oSeed;
            float gSlice;
            internal Vector2 pos => oSeed.firstChunk.pos;
            internal int? ownerID;
            internal int distortionLifetime;
            internal float radius;
            //could do with attachedfields but eh
            internal List<int> affectedObjects;
            public override void Update(bool eu)
            {
                base.Update(eu);
                if (mySmoke == null)
                {
                    mySmoke = new Smoke.FireSmoke(room);
                    room.AddObject(mLs);
                    room.AddObject(mySmoke);
                }
                mLs.setAlpha = ClampedFloatDeviation(0.8f, 0.07f);
                mLs.setRad = ClampedFloatDeviation(radius * 0.9f, 10f);
                mLs.setPos = pos;
                mLs.stayAlive = true;
                mLs.color = mLs.color.Deviation(forDevs);
                mLs.color.ClampToNormal();
                foreach (var layer in this.room.physicalObjects)
                {
                    for (int i = layer.Count - 1; i > -1; i--)
                    {
                        var obj = layer[i];
                        if (obj is TremblingSeed || obj.gravity == 0f) continue;
                        foreach (var chunk in obj.bodyChunks) {
                            if (DistLess(chunk.pos, pos, radius)) chunk.vel.y += obj.gravity * Max(0, gSlice * Dist(chunk.pos, pos) / radius);
                        } 
                        if (obj is Weapon w && w.mode == Mode.Thrown)
                        {
                            if (UnityEngine.Random.value < 0.14f) w.WeaponDeflect(w.firstChunk.pos + w.firstChunk.vel * 0.1f, (w.firstChunk.vel * -1).normalized * 3f, 5f);
                        }
                        //for (int i )
                    }
                }
                distortionLifetime--;
                if (distortionLifetime < 0) this.Destroy();
                for (int i = 0; i < UnityEngine.Random.Range(3, 7); i++)
                {
                    var off = RNV();
                    var nsmPos = pos + off * radius;
                    if (room.GetTile(nsmPos).Solid) continue;
                    mySmoke.EmitSmoke(nsmPos, PerpendicularVector(off) * UnityEngine.Random.Range(2f, 5f), new Color(0.3f, 0.1f, 0.1f).Deviation(new Color(0.1f, 0.05f, 0.05f)), 10);
                }
            }
            internal void AttemptReachOut(PhysicalObject po)
            {
                //Debug.Log($"Seed distortion trying to touch {po}...");
                if (po.room != this.room || po is TremblingSeed || (po.firstChunk.pos - pos).magnitude < 100f) return;
                var hash = po.GetHashCode();
                if (affectedObjects.Contains(hash)) return;
                this.room.AddObject(new MysteriousLight(
                    po.firstChunk.pos,
                    false,
                    po,
                    initialLifetime: UnityEngine.Random.Range(this.distortionLifetime / 2, distortionLifetime * 2),
                    gravityMultiplier: Mathf.Lerp(0.3f, 0.9f, UnityEngine.Random.value)
                    ));
                affectedObjects.Add(hash);
                Debug.Log($"Seed [{this.ownerID}] reaching out to {po.GetType()}, {po.abstractPhysicalObject.ID}");
            }
            public override void Destroy()
            {
                mySmoke.Destroy();
                base.Destroy();
                this.room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_Off, this.pos);
                Debug.Log("Seed distortion lifetime over. Bye!");
            }

            #region IDrawable things

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                throw new NotImplementedException();
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                throw new NotImplementedException();
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                throw new NotImplementedException();
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                throw new NotImplementedException();
            }
            #endregion
        }
        public class MysteriousLight : LightSource
        {
#warning unfinished, would likely cause issues
            //not modifying g anymore
            //tho math might be sketchy
            public MysteriousLight (Vector2 initpos, bool envir, UpdatableAndDeletable bindTo, int initialLifetime = 80, float gravityMultiplier = 1f) : base (initpos, envir, new Color(0.6f, 0.8f, 0.8f).Deviation(new Color(0.3f, 0.2f, 0.2f)), bindTo)
            {
                this.requireUpKeep = false;
                this.maxLifetime = initialLifetime;
                this.lifetime = initialLifetime;
                this.initialGReduction = gravityMultiplier;
                Debug.Log($"Seed light created {bindTo.GetType()}, {owner?.abstractPhysicalObject.ID}");
                
            }
            public override void Update(bool eu)
            {
                base.Update(eu);
                try
                {
                    if (owner != null)
                    {
                        stayAlive = true;
                        setPos = owner.firstChunk.pos;

                        setRad = Lerp(30f, 220f, timeRemaining);
                        setAlpha = Lerp(0f, 1f, timeRemaining);
                        foreach (var chunk in owner.bodyChunks)
                        {
                            chunk.vel.y += owner.gravity * effectiveGReduction;
                        }
                    }
                    else Destroy();
                    lifetime--;
                    if (lifetime < 0 || this.tiedToObject?.room != this.room) { this.Destroy(); Debug.Log("Seedlight is dead. Bye!"); }
                }
                catch (NullReferenceException)
                {
                    Debug.LogWarning("nullref in mysteriousLight.Update!");
                    this.Destroy();
                }
            }

            PhysicalObject owner => tiedToObject as PhysicalObject;
            internal float initialGReduction;
            internal float effectiveGReduction => Lerp(0f, initialGReduction, timeRemaining);
            internal float timeRemaining => Clamp(lifetime / (float)maxLifetime, 0f, 1f);
            internal int maxLifetime;
            internal int lifetime;
        }
        public class AbstractTremblingSeed : AbstractPhysicalObject
        {
            public AbstractTremblingSeed(World world, PhysicalObject po, WorldCoordinate wc, EntityID eid, int usesLeft) : base(world, EnumExt_SeedEnumThings.ShockSeed, po, wc, eid)
            {
                RemainingUses = usesLeft;
            }
            public override void Update(int time)
            {
                base.Update(time);
                Cooldown = Max(0, Cooldown - time);
            }
            public override void Realize()
            {

                this.realizedObject = new TremblingSeed(this, world);
                base.Realize();
            }
            public int RemainingUses;
            public int Cooldown;
        }
        
        public static class SeedHooks
        {
#warning add hooks to spawn in, deser, etc
            public static void TempSpawnIn(On.Player.orig_ctor orig, Player instance, AbstractCreature absc, World world)
            {
                orig(instance, absc, world);
                var seed = new AbstractTremblingSeed(world, null, instance.abstractCreature.pos, instance.room.game.GetNewID(), 4);
                instance.room.abstractRoom.entities.Add(seed);
                //seed.Realize();
                seed.RealizeInRoom();
            }
            public static void Apply()
            {
                On.Player.ctor += TempSpawnIn;
            }
        }
    }
}
