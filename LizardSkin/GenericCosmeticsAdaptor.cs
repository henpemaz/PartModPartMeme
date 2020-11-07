using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSkin
{
    public abstract class GenericCosmeticsAdaptor : BodyPart, ICosmeticsAdaptor
    {
        protected static void LogMethodName()
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            System.Diagnostics.StackFrame stackFrame = stackTrace.GetFrame(1);
            System.Reflection.MethodBase methodBase = stackFrame.GetMethod();
            // Debug.Log("LizardSkin: " + methodBase.Name);
        }

        public GraphicsModule graphics { get ; protected set; }

        public float BodyAndTailLength { get => this.bodyLength + this.tailLength; }
        public Color effectColor { get; protected set; }
        public float bodyLength { get; protected set; }
        public float tailLength { get; protected set; }
        public RoomPalette palette { get; protected set; }
        public CosmeticsParams cosmeticsParams { get; protected set; }
        public List<GenericCosmeticTemplate> cosmetics { get; protected set; }

        public float depthRotation { get; set; }
        public float headDepthRotation { get; set; }
        public float lastHeadDepthRotation { get; set; }
        public float lastDepthRotation { get; set; }

        public float showDominance { get; protected set; }

        public int firstSprite { get; protected set; }
        public int extraSprites { get; protected set; }

        public BodyPart head { get => this.getHeadImpl(); }
        public BodyPart baseOfTail { get => this.getBaseOfTailImpl(); }

        public BodyChunk mainBodyChunk { get => this.graphics.owner.firstChunk; }

        PhysicalObject ICosmeticsAdaptor.owner { get => this.graphics.owner; }
        public abstract BodyPart getHeadImpl();
        public abstract BodyPart getBaseOfTailImpl();

        public GenericCosmeticsAdaptor(GraphicsModule graphicsModule) : base(graphicsModule)
        {
            LogMethodName();
            this.graphics = graphicsModule;

            this.cosmeticsParams = new CosmeticsParams(0.5f, 0.5f, 0.5f);

            this.cosmetics = new List<GenericCosmeticTemplate>();
        }

        public virtual void AddCosmetic(GenericCosmeticTemplate cosmetic)
        {
            LogMethodName();
            this.cosmetics.Add(cosmetic);
            cosmetic.startSprite = this.extraSprites;
            this.extraSprites += cosmetic.numberOfSprites;
        }

        protected abstract void updateRotation();

        public override void Update()
        {
            updateRotation();
            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].Update();
            }
        }

        public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            LogMethodName();
            this.firstSprite = sLeaser.sprites.Length;

            // Debug.Log("Before: " + sLeaser.sprites.Length);
            System.Array.Resize(ref sLeaser.sprites, this.firstSprite + this.extraSprites);
            // Debug.Log("After: " + sLeaser.sprites.Length);
            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].InitiateSprites(sLeaser, rCam);
            }
            this.AddToContainer(sLeaser, rCam, null);
        }

        public virtual void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            LogMethodName();
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Midground");
            }
            FContainer behind = new FContainer();
            FContainer behindHead = new FContainer();
            FContainer onTop = new FContainer();
            newContatiner.AddChild(behind);
            behind.MoveBehindOtherNode(getBehindNode(sLeaser));
            newContatiner.AddChild(behindHead);
            behindHead.MoveBehindOtherNode(getBehindHeadNode(sLeaser));
            newContatiner.AddChild(onTop);
            onTop.MoveInFrontOfOtherNode(getOnTopNode(sLeaser));

            for (int j = 0; j < this.cosmetics.Count; j++)
            {
                if (this.cosmetics[j].spritesOverlap == GenericCosmeticTemplate.SpritesOverlap.Behind)
                {
                    this.cosmetics[j].AddToContainer(sLeaser, rCam, behind);
                }
            }
            for (int m = 0; m < this.cosmetics.Count; m++)
            {
                if (this.cosmetics[m].spritesOverlap == GenericCosmeticTemplate.SpritesOverlap.BehindHead)
                {
                    this.cosmetics[m].AddToContainer(sLeaser, rCam, behindHead);
                }
            }
            for (int m = 0; m < this.cosmetics.Count; m++)
            {
                if (this.cosmetics[m].spritesOverlap == GenericCosmeticTemplate.SpritesOverlap.InFront)
                {
                    this.cosmetics[m].AddToContainer(sLeaser, rCam, onTop);
                }
            }
        }

        protected abstract FNode getOnTopNode(RoomCamera.SpriteLeaser sLeaser);

        protected abstract FNode getBehindHeadNode(RoomCamera.SpriteLeaser sLeaser);

        protected abstract FNode getBehindNode(RoomCamera.SpriteLeaser sLeaser);

        public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int j = 0; j < this.cosmetics.Count; j++)
            {
                this.cosmetics[j].DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }
        }

        public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            LogMethodName();
            this.palette = palette;
            for (int i = 0; i < this.cosmetics.Count; i++)
            {
                this.cosmetics[i].ApplyPalette(sLeaser, rCam, palette);
            }
        }

        public virtual void Reset()
        {
            LogMethodName();
            base.Reset(graphics.owner.firstChunk.pos);

            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].Reset();
            }
        }

        public abstract float HeadRotation(float timeStacker);

        public abstract LizardGraphics.LizardSpineData SpinePosition(float spineFactor, float timeStacker);

        public abstract Color BodyColor(float y);

        public abstract Color HeadColor(float timeStacker);
    }
    public struct CosmeticsParams
    {
        internal float fatness;
        internal float tailFatness;
        internal float headSize;

        public CosmeticsParams(float fatness, float tailFatness, float headSize)
        {
            this.fatness = fatness;
            this.tailFatness = tailFatness;
            this.headSize = headSize;
        }
    }
}