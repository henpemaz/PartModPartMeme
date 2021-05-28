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
    public class TremblingSeed : Rock
    {
        public TremblingSeed (AbstractPhysicalObject apo, World world) : base (apo, world)
        {

        }
        public override void ChangeMode(Mode newMode)
        {
#warning finish ChangeMode
            base.ChangeMode(newMode);
            switch (newMode)
            {
                case Mode.Thrown:
                    activationDelay = 16;
                    PopWindup = 0;
                    break;
                case Mode.StuckInWall:
                    
                    break;
            }
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);
            foreach (var sprite in sLeaser.sprites)
            {
                sprite.color = new Color(0f, 1f, 0f);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            lastPopWindup = PopWindup;
            lastActionCycle = ActionCycle;
            switch (this.mode)
            {
                case Mode.Thrown:
                    activationDelay--;
                    if (activationDelay > 0 || Cooldown > 0) break;
                    this.EvaluateSurroundings();
                    break;
                case Mode.StuckInWall:
                    ActionCycle--;
                    if (ActionCycle <= 0) this.ChangeMode(Mode.Free);
                    break;
            }
        }
        public void EvaluateSurroundings()
        {
            if (this.room?.abstractRoom == null) return;
            foreach (var crit in this.room.abstractRoom.creatures)
            {
                if (ManhattanDistance(this.abstractPhysicalObject.pos, crit.pos) <= 5) PopWindup++;
            }
            this.PopWindup += 10;
            if (UnityEngine.Random.value < (float)PopWindup / 100) this.Pop();
            
        }
        internal void Pop()
        {
            this.ChangeMode(Mode.StuckInWall);
            this.RemainingUses = Max(RemainingUses - 1, 0);
            this.room.AddObject(new DistortionZone(this.firstChunk.pos, 400));
        }


        internal AbstractTremblingSeed abstractSeed => (AbstractTremblingSeed)this.abstractPhysicalObject;
        internal int RemainingUses { get { return abstractSeed.RemainingUses; } set { abstractSeed.RemainingUses = value; } }
        internal int activationDelay;
        internal int Cooldown { get { return abstractSeed.Cooldown; } set { abstractSeed.Cooldown = value; } }
        internal int ActionCycle;
        internal int lastActionCycle;
        internal int PopWindup;
        internal int lastPopWindup;

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
#warning unfinished
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

                lifetime--;
                if (lifetime <= 0) { this.Destroy(); }
            }
            public override void Destroy()
            {
                base.Destroy();
                owner.gravity = initialG;
            }

            PhysicalObject owner => tiedToObject as PhysicalObject;
            internal float initialG;
            internal float initiakGk;
            internal float effectiveGk => Mathf.InverseLerp((float)maxLifetime, 0f, (float)lifetime) * initiakGk;
            internal float timeRemaining => (float)lifetime / (float)maxLifetime;
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
            public static void Apply()
            {

            }
        }
    }
}
