using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ManagedPlacedObjects;

using static ManagedPlacedObjects.PlacedObjectsManager;
using static ConcealedGarden.Utils.CGUtils;
using static RWCustom.Custom;
using ConcealedGarden.Utils;
using static UnityEngine.Mathf;

namespace ConcealedGarden
{
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
            var R = new System.Random(apo.ID.RandomSeed);
            tailPos = firstChunk.pos;
            osp = new GOscParams(UnityEngine.Random.Range(2f, 3f), 
                UnityEngine.Random.Range(0.01f, 0.03f), 
                UnityEngine.Random.Range(-0.5f, 0.5f),
                (UnityEngine.Random.value > 0.5f) ? new Func<float, float>(Sin) : new Func<float, float>(Cos));
        }
        public override void ChangeMode(Mode newMode)
        {
        //#warning finish ChangeMode
        //more or less like that?
        //should watch out for escaping indeces
            var oldmode = mode;
            base.ChangeMode(newMode);
            switch (newMode)
            {
                case Mode.Thrown:
                    windup = 4;
                    popCharge = 0;
                    break;
                case Mode.StuckInWall:
                    //stuckPos = firstChunk.pos;
                    popCharge = 0;
                    ActionCycle = 200;
                    break;
                default:
                    if (oldmode == Mode.StuckInWall) { Cooldown = AbstractTremblingSeed.nominalCooldown; }
                    break;
            }
        }
        //kills windups
        public override void HitWall()
        {
            base.HitWall();
            CancelEverything();
        }
        //kills windups
        public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
        {
            //copied from rock
            if (result.obj == null)
            {
                return false;
            }
            if (thrownBy is Scavenger && (thrownBy as Scavenger).AI != null)
            {
                (thrownBy as Scavenger).AI.HitAnObjectWithWeapon(this, result.obj);
            }
            vibrate = 20;
            ChangeMode(Mode.Free);
            if (result.obj is Creature)
            {
                (result.obj as Creature).Violence(firstChunk, new Vector2?(firstChunk.vel * firstChunk.mass), result.chunk, result.onAppendagePos, Creature.DamageType.Blunt, 0.1f, 150f);
            }
            else if (result.chunk != null)
            {
                result.chunk.vel += firstChunk.vel * firstChunk.mass / result.chunk.mass;
            }
            else if (result.onAppendagePos != null)
            {
                (result.obj as IHaveAppendages).ApplyForceOnAppendage(result.onAppendagePos, firstChunk.vel * firstChunk.mass);
            }
            firstChunk.vel = firstChunk.vel * -0.28f + RNV() * Lerp(0.05f, 0.15f, UnityEngine.Random.value) * firstChunk.vel.magnitude;
            room.PlaySound(SoundID.Rock_Hit_Creature, firstChunk, false, 1.2f, 0.8f);
            if (result.chunk != null)
            {
                room.AddObject(new ExplosionSpikes(room, result.chunk.pos + DirVec(result.chunk.pos, result.collisionPoint) * result.chunk.rad, 5, 2f, 4f, 4.5f, 30f, new Color(1f, 1f, 1f, 0.5f)));
            }
            SetRandomSpin();
            CancelEverything();
            return true;
        }
        public override void Thrown(Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            base.Thrown(thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            
        }
        //public override void Grabbed(Creature.Grasp grasp)
        //{
        //    base.Grabbed(grasp);
        //    stalk?.SeedTakenAway();
        //    stalk = null;
        //}
        //public override void HitByWeapon(Weapon weapon)
        //{
        //    base.HitByWeapon(weapon);
        //    stalk?.SeedTakenAway(true);
        //    stalk = null;
        //}
        //public override void HitByExplosion(float hitFac, Explosion explosion, int hitChunk)
        //{
        //    base.HitByExplosion(hitFac, explosion, hitChunk);
        //    stalk?.SeedTakenAway(true);
        //    stalk = null;
        //}
        public override void Update(bool eu)
        {
            lt++;
            if (firstChunk.ContactPoint.y != 0) rotationSpeed = 0f;
            base.Update(eu);
            CollideWithTerrain = true;
            lastShellOffset = shellOffset;
            shellOffset = osp.GetRes(lt);
            lastPopCharge = popCharge;
            lastActionCycle = ActionCycle;
            lastCoreColor = coreColor;
            coreColor.g = Lerp(0.3f, 0.8f, 1f - (float)Cooldown / (float)AbstractTremblingSeed.nominalCooldown);
            coreColor.r = Lerp(0.2f, 0.6f, (float)RemainingUses / (float)AbstractTremblingSeed.maxUses);
            coreColor.b = 0.15f;
            coreColor.a = 1f;
            coreColor.ClampToNormal();
            switch (mode)
            {
                case Mode.Thrown:
                    windup--;
                    if (windup > 0 || Cooldown > 0 || RemainingUses == 0) break;
                    RunTheCounters();
                    break;
                case Mode.StuckInWall:
                    airFriction = 0.85f;
                    gravity = -0.03f;
                    ActionCycle--;
                    if (ActionCycle <= 0) { ChangeMode(Mode.Free); CancelEverything(); }
                    break;
                default:
                    airFriction = 1f;
                    gravity = 0.7f;
                    CancelEverything();
                    break;
            }
            //if (stalk != null) gravity = 0;
        }
        //increasing chance to pop every frame, faster if creatures are nearby
        //could also make it home in on creatures slightly?.. bad idea prolly
        public void RunTheCounters()
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
            this.room.AddObject(new SeedDistortion(this, this.ActionCycle, 120f));
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

        //internal SeedStalk stalk;
        #region idrawable things
        float lt;
        GOscParams osp;
        
        float lastShellOffset;
        float shellOffset;
        float shellOffsetBase = 3f;

        Color coreColor;
        Color lastCoreColor;

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
            var MESH = new TriangleMesh("Futile_White", meshT, true, false) { color = Color.white };
            sLeaser.sprites[tail] = MESH;
            AddToContainer(sLeaser, rCam, null);
        }
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            var cOrigin = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
            var cRot = LerpAngle(VecToDeg(lastRotation), VecToDeg(rotation), timeStacker);
            //core
            sLeaser.sprites[core].SetPosition(cOrigin);
            sLeaser.sprites[core].color = Color.Lerp(lastCoreColor, coreColor, timeStacker);
            //shells
            var shellDir = PerpendicularVector(rotation).normalized;
            var sOf = shellDir * Lerp(lastShellOffset + shellOffsetBase, shellOffset + shellOffsetBase, timeStacker);
            sLeaser.sprites[shell(true)].SetPosition(cOrigin + sOf);
            sLeaser.sprites[shell(true)].rotation = VecToDeg(shellDir);
            sLeaser.sprites[shell(false)].SetPosition(cOrigin - sOf);
            sLeaser.sprites[shell(false)].rotation = VecToDeg(shellDir * -1f);
            //tail
            var tailmesh = sLeaser.sprites[tail] as TriangleMesh;
            tailmesh.MoveVertice(0, cOrigin + shellDir * 2f);
            tailmesh.MoveVertice(1, cOrigin + shellDir * -2f);
            tailmesh.MoveVertice(2, Vector2.Lerp(tailPos, firstChunk.lastPos, timeStacker) - camPos);
            tailmesh.verticeColors[0].a = 1f;
            tailmesh.verticeColors[1].a = 1f;
            tailmesh.verticeColors[2].a = 0.8f;
            if (slatedForDeletetion || room != rCam.room) sLeaser.CleanSpritesAndRemove();
        }
        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaser, rCam, newContatiner);
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[tail].color = palette.blackColor;
        }
        #endregion

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
                //Debug.Log($"Seed distortion created: {ownerID}, d:{duration}, rad:{rad}"); 
            }

            readonly Color lightcBase = new Color(0.2f, 0.8f, 0.2f);
            readonly Color lightcDev = new Color(0.08f, 0.08f, 0.08f);
            LightSource mLs;
            Smoke.FireSmoke mySmoke;
            //this smoke is janky, replace with something else?
            TremblingSeed oSeed;
            float gSlice;
            internal Vector2 pos => oSeed.firstChunk.pos;
            //internal int? ownerID;
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
                mLs.setRad = ClampedFloatDeviation(radius * 0.9f, 5f);
                mLs.setPos = pos;
                mLs.stayAlive = true;
                mLs.color = lightcBase.Deviation(lightcDev);
                mLs.color.ClampToNormal();
                foreach (var layer in room.physicalObjects)
                {
                    for (int i = layer.Count - 1; i > -1; i--)
                    {
                        var obj = layer[i];
                        if (obj is TremblingSeed || obj.gravity == 0f) continue;
                        foreach (var chunk in obj.bodyChunks) {
                            if (DistLess(chunk.pos, pos, radius)) chunk.vel.y += obj.gravity * Max(0, gSlice * Dist(chunk.pos, pos) / radius);
                        } 
                        if (DistLess(obj.firstChunk.pos, pos, radius) && !DistLess(obj.firstChunk.lastLastPos, pos, radius) && obj is Weapon w && w.mode == Mode.Thrown)
                        {
                            w.WeaponDeflect(w.firstChunk.pos + w.firstChunk.vel * 0.1f, (w.firstChunk.vel * -1).normalized * 3f, 2f);
                        }
                    }
                }
                distortionLifetime--;
                if (distortionLifetime < 0) this.Destroy();
                for (int i = 0; i < UnityEngine.Random.Range(4, 8); i++)
                {
                    var off = RNV();
                    var nsmPos = pos + off * radius;
                    if (room.GetTile(nsmPos).Solid) continue;

                    //var smokeBit = mySmoke.AddParticle(nsmPos, PerpendicularVector(off) * 2f, 15f);
                    //if (smokeBit != null)
                    //{
                    //    smokeBit.pos = nsmPos;
                    //    room.AddObject(smokeBit);
                    //}
                    //mySmoke.EmitSmoke(nsmPos, PerpendicularVector(off).normalized, Color.cyan, 2);
                    var nsp = new Smoke.FireSmoke.FireSmokeParticle();
                    nsp.Reset(mySmoke, nsmPos, PerpendicularVector(off), 40f);
                    nsp.moveDir = VecToDeg(PerpendicularVector(off));
                    nsp.effectColor = Color.cyan;
                    nsp.colorFadeTime = 20;
                    room.AddObject(nsp);
                }
            }
            //internal void AttemptReachOut(PhysicalObject po)
            //{
            //    //Debug.Log($"Seed distortion trying to touch {po}...");
            //    if (po.room != this.room || po is TremblingSeed || (po.firstChunk.pos - pos).magnitude < 100f) return;
            //    var hash = po.GetHashCode();
            //    if (affectedObjects.Contains(hash)) return;
            //    this.room.AddObject(new MysteriousLight(
            //        po.firstChunk.pos,
            //        false,
            //        po,
            //        initialLifetime: UnityEngine.Random.Range(this.distortionLifetime / 2, distortionLifetime * 2),
            //        gravityMultiplier: Mathf.Lerp(0.3f, 0.9f, UnityEngine.Random.value)
            //        ));
            //    affectedObjects.Add(hash);
            //    //Debug.Log($"Seed [{this.ownerID}] reaching out to {po.GetType()}, {po.abstractPhysicalObject.ID}");
            //}
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
        public class AbstractTremblingSeed : AbstractPhysicalObject
        {
            public AbstractTremblingSeed(World world, PhysicalObject po, WorldCoordinate wc, EntityID eid, int usesLeft = maxUses) : base(world, EnumExt_SeedEnumThings.ShockSeed, po, wc, eid)
            {
                RemainingUses = usesLeft;
            }
            public override void Update(int time)
            {
                base.Update(time);
                if (realizedObject != null) Cooldown = Max(0, Cooldown - time);
            }
            public override void Realize()
            {
                realizedObject = new TremblingSeed(this, world);
                base.Realize();
            }
            public int RemainingUses = maxUses;
            public const int maxUses = 100;
            public int Cooldown;
            public const int nominalCooldown = 160;
        }
        //public class SeedStalk : UpdatableAndDeletable, IDrawable
        //{
        //    public SeedStalk(PlacedObject owner) { Owner = owner; fixpos = Owner.pos; }
        //    PlacedObject Owner;
        //    SeedConsData scd => Owner?.data as SeedConsData;
        //    Vector2 fixpos;
        //    bool amConsumed => (room?.game.session as StoryGameSession)?.saveState.ItemConsumed(room.world, false, room.abstractRoom.index, room.roomSettings.placedObjects.IndexOf(Owner)) ?? false;
        //    public override void Update(bool eu)
        //    {
        //        base.Update(eu);
        //        if (!amConsumed && abSeed == null) { 
        //            abSeed = new AbstractTremblingSeed(room.world, null, room.GetWorldCoordinate(fixpos), room.game.GetNewID());
        //            room.abstractRoom.AddEntity(abSeed);
        //            abSeed.RealizeInRoom();
        //            abSeed.realizedObject.firstChunk.HardSetPosition(fixpos);
        //            (abSeed.realizedObject as TremblingSeed).stalk = this;
        //        }
        //        if (rSeed != null)
        //        {

        //        }
        //    }
        //    public void SeedTakenAway(bool violent = false) 
        //            { (room.game.session as StoryGameSession)?.saveState.
        //            ReportConsumedItem(room.world, false, 
        //            room.abstractRoom.index, 
        //            room.roomSettings.placedObjects.IndexOf(Owner), 
        //            UnityEngine.Random.Range(scd.minC, scd.maxC)); 
        //    }
        //    AbstractTremblingSeed abSeed;
        //    TremblingSeed rSeed => abSeed.realizedObject as TremblingSeed;
        //    public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        //    {
        //        throw new NotImplementedException();
        //    }
        //    public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        //    {
        //        throw new NotImplementedException();
        //    }
        //    public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        //    {
        //        throw new NotImplementedException();
        //    }
        //    public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        public class SeedConsData : ManagedData
        {
            [IntegerField("minC", 1, 40, 3, ManagedFieldWithPanel.ControlType.slider, "min cooldown")]
            public int minC;
            [IntegerField("maxC", 1, 40, 3, ManagedFieldWithPanel.ControlType.slider, "max cooldown")]
            public int maxC;
            [Vector2Field("basePoint", 30f, 30f)]
            public Vector2 stalkBase;
            public SeedConsData(PlacedObject owner) : base(owner, null) { }
        }
        public static class SeedHooks
        {
        //add hooks to spawn in, deser, etc
            public static void Apply()
            {
                //for later: chaange hooks from On to manual
                //RegistermanagedObject<SeedStalk, SeedConsData, ManagedRepresentation>("CGSeed");
                On.SaveState.AbstractPhysicalObjectFromString += seed_APOFS;
                On.Room.Loaded += Room_Loaded;
                On.Player.ctor += TempSpawnIn;
            }

            private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
            {
                orig(self);
                if (self.game == null || !self.abstractRoom.firstTimeRealized) return;
                for (int i = 0; i < self.roomSettings.placedObjects.Count; i++) 
                    if (self.roomSettings.placedObjects[i].data is SeedConsData sd)
                    {
                    
                }
            }

            public static void TempSpawnIn(On.Player.orig_ctor orig, Player instance, AbstractCreature absc, World world)
            {
                orig(instance, absc, world);
                if (absc.Room.shelter) return;
                var seed = new AbstractTremblingSeed(world, null, instance.abstractCreature.pos, instance.room.game.GetNewID());
                instance.room.abstractRoom.entities.Add(seed);
                //seed.Realize();
                seed.RealizeInRoom();
            }
            public static void Apply()
            {
                On.SaveState.AbstractPhysicalObjectFromString += seed_APOFS;
                //On.Player.ctor += TempSpawnIn;
            }

            private static AbstractPhysicalObject seed_APOFS(On.SaveState.orig_AbstractPhysicalObjectFromString orig, World world, string objString)
            {
                var res = orig(world, objString);
                try
                {
                    var objAtts = System.Text.RegularExpressions.Regex.Split(objString, "<oA>");
                    var aotype = ParseEnum<AbstractPhysicalObject.AbstractObjectType>(objAtts[1]);
                    var EID = EntityID.FromString(objAtts[0]);
                    var wctext = objAtts[2].Split('.');
                    var wc = new WorldCoordinate(
                        int.Parse(wctext[0]), 
                        int.Parse(wctext[1]), 
                        int.Parse(wctext[2]), 
                        int.Parse(wctext[3]));
                    if (aotype == EnumExt_SeedEnumThings.ShockSeed)
                    {
                        return new AbstractTremblingSeed(world, null, wc, EID);
                    }
                }
                catch { }
                return res;
            }
        }

        //todo:
        //make and import sprites
        //make stalk (or make it appear on branches?)
        //add windup and action cycle telegraph
        //
        //idea stash:
        //downslam on hang expire?
    }
}
