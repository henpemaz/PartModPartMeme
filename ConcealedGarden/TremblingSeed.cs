using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static RWCustom.Custom;
using static System.Math;

namespace ConcealedGarden
{
    public static class EnumExt_SeedEnumThings
    {
        public static AbstractPhysicalObject.AbstractObjectType ShockSeed;
        
    }
    //stub as of now
    //throw behaviour:
    //windup -> charge -> pop -> active phase -> long cooldown
    public class TremblingSeed : Rock
    {
        public TremblingSeed (AbstractPhysicalObject apo, World world) : base (apo, world)
        {

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
                    windup = 16;
                    popCharge = 0;
                    break;
                case Mode.StuckInWall:
                    popCharge = 0;
                    ActionCycle = 40;
                    break;
                default:
                    if (oldmode == Mode.StuckInWall) { Cooldown = 1200; }
                    break;
            }
        }
        //just a green rock for now
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            foreach (var sprite in sLeaser.sprites)
            {
                sprite.color = new Color(0f, 1f, 0f);
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
            var outcome = base.HitSomething(result, eu);
            if (outcome) CancelEverything();
            return outcome;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            lastPopCharge = popCharge;
            lastActionCycle = ActionCycle;
            switch (this.mode)
            {
                case Mode.Thrown:
                    windup--;
                    if (windup > 0 || Cooldown > 0) break;
                    this.RunChargeupCheck();
                    break;
                case Mode.StuckInWall:
                    ActionCycle--;
                    if (ActionCycle <= 0) { this.ChangeMode(Mode.Free); CancelEverything(); }
                    break;
                default:
                    CancelEverything();
                    break;
            }
        }

        //increasing chance to pop every frame, faster if creatures are nearby
        //could also make it home in on creatures slightly?.. bad idea prolly
        public void RunChargeupCheck()
        {
            if (this.room?.abstractRoom == null) return;
            foreach (var crit in this.room.abstractRoom.creatures)
            {
                if (ManhattanDistance(this.abstractPhysicalObject.pos, crit.pos) <= 10) popCharge += 3;
            }
            this.popCharge += 10;
            if (UnityEngine.Random.value < (float)popCharge / 100) this.Pop();
        }
        internal void Pop()
        {
            this.room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_On, this.firstChunk);
            this.ChangeMode(Mode.StuckInWall);
            this.RemainingUses = Max(RemainingUses - 1, 0);
            this.room.AddObject(new DistortionZone(this.firstChunk.pos, 400));
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

        public class DistortionZone : UpdatableAndDeletable
        {
#warning nonfunctional, need implementing
            //not sure how to do the zerog thing yet. storing old G values in distortionzone = jank, uncontrollably sticky lowg and memory leaks?
            //maybe an attached UED that follows the target and watches over itself
            //derivative from lightsource for maximum cursedness
            public DistortionZone(Vector2 mypos, int duration = 0, float rad = 0f) { this.pos = mypos; lifetime = duration; radius = rad; }
            internal Vector2 pos;
            internal int lifetime;
            internal float radius;
            public override void Update(bool eu)
            {
                base.Update(eu);
                lifetime--;
                if (lifetime <= 0) this.Destroy();
            }

        }
        public class MysteriousLight : LightSource
        {
#warning unfinished, would likely cause issues
            //manipulating G is messy, might want to manually apply force instead
            public MysteriousLight (Vector2 initpos, bool envir, UpdatableAndDeletable bindTo, int lifetime, float gravityMultiplier) : base (initpos, envir, new Color(0.7f, 0.2f, 0.2f), bindTo)
            {
                this.maxLifetime = lifetime;
                this.initiakGk = gravityMultiplier;
                if (owner != null)
                {
                    initialG = owner.g;
                }
            }
            public override void Update(bool eu)
            {
                base.Update(eu);
                if (owner != null)
                {
                    owner.g = initialG * effectiveGk;
                }
                this.rad = Mathf.Lerp(15f, 35f, timeRemaining);
                lifetime--;
                if (lifetime <= 0) { this.Destroy(); this.room.PlaySound(SoundID.Broken_Anti_Gravity_Switch_Off, this.pos); }
            }
            public override void Destroy()
            {
                base.Destroy();
                owner.gravity = initialG;
            }

            PhysicalObject owner => tiedToObject as PhysicalObject;
            internal float initialG;
            internal float initiakGk;
            internal float effectiveGk => Mathf.Lerp(initiakGk, 1f, timeRemaining);
            internal float timeRemaining => Mathf.Clamp((float)lifetime / (float)maxLifetime, 0f, 1f);
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
            public int RemainingUses;
            public int Cooldown;
        }
        
        public static class SeedHooks
        {
#warning add hooks to spawn in, deser, etc
            public static void Apply()
            {

            }
        }
    }
}
