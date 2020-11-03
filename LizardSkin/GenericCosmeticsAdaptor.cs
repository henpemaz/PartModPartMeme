using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSkin
{
    public abstract class GenericCosmeticsAdaptor : ICosmeticsAdaptor
    {
        //protected GraphicsModule _graphics;
        public GraphicsModule graphics { get ; protected set; }

        public float BodyAndTailLength { get => this.bodyLength + this.tailLength; }
        public Color effectColor { get; protected set; }
        public float bodyLength { get; protected set; }
        public float tailLength { get; protected set; }
        public RoomPalette palette { get; protected set; }
        public float showDominance { get; protected set; }
        public CosmeticsParams cosmeticsParams { get; protected set; }
        public List<GenericCosmeticsTemplate> cosmetics { get; protected set; }

        protected float depthRotation { get; set; }
        protected float lastDepthRotation { get; set; }
        public int firstSprite { get; protected set; }
        public int extraSprites { get; protected set; }

        public abstract int getFirstSpriteImpl();

        public GenericCosmeticsAdaptor(GraphicsModule graphicsModule)
        {
            this.graphics = graphicsModule;

            this.cosmeticsParams = new CosmeticsParams(0.5f, 0.5f);

            this.cosmetics = new List<GenericCosmeticsTemplate>();
            this.extraSprites = 0;
            this.firstSprite = this.getFirstSpriteImpl();
        }

        public virtual int AddCosmetic(int spriteIndex, GenericCosmeticsTemplate cosmetic)
        {
            this.cosmetics.Add(cosmetic);
            spriteIndex += cosmetic.numberOfSprites;
            this.extraSprites += cosmetic.numberOfSprites;
            return spriteIndex;
        }

        public virtual void Update()
        {
            this.showDominance = Mathf.Clamp(this.showDominance - 1f / Mathf.Lerp(60f, 120f, UnityEngine.Random.value), 0f, 1f);

            this.lastDepthRotation = this.depthRotation;
            float newRotation = 0;
            this.depthRotation = Mathf.Lerp(this.depthRotation, newRotation, 0.1f);

            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].Update();
            }
        }

        public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            // sLeaser.sprites = new FSprite[this.startOfExtraSprites + this.extraSprites];
            System.Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + this.extraSprites);
            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].InitiateSprites(sLeaser, rCam);
            }

            FContainer behind = new FContainer();
            FContainer behindHead = new FContainer();
            FContainer onTop = new FContainer();
            FContainer midground = rCam.ReturnFContainer("Midground");
            midground.AddChild(behind);
            behind.MoveBehindOtherNode(getBehindNode(sLeaser));
            midground.AddChild(behindHead);
            behind.MoveBehindOtherNode(getBehindHeadNode(sLeaser));
            midground.AddChild(onTop);
            behind.MoveBehindOtherNode(getOnTopNode(sLeaser));

            for (int j = 0; j < this.cosmetics.Count; j++)
            {
                if (this.cosmetics[j].spritesOverlap == GenericCosmeticsTemplate.SpritesOverlap.Behind)
                {
                    this.cosmetics[j].AddToContainer(sLeaser, rCam, behind);
                }
            }
            for (int m = 0; m < this.cosmetics.Count; m++)
            {
                if (this.cosmetics[m].spritesOverlap == GenericCosmeticsTemplate.SpritesOverlap.BehindHead)
                {
                    this.cosmetics[m].AddToContainer(sLeaser, rCam, behindHead);
                }
            }
            for (int m = 0; m < this.cosmetics.Count; m++)
            {
                if (this.cosmetics[m].spritesOverlap == GenericCosmeticsTemplate.SpritesOverlap.InFront)
                {
                    this.cosmetics[m].AddToContainer(sLeaser, rCam, onTop);
                }
            }

        }

        protected abstract FNode getOnTopNode(RoomCamera.SpriteLeaser sLeaser);

        protected abstract FNode getBehindHeadNode(RoomCamera.SpriteLeaser sLeaser);

        protected abstract FSprite getBehindNode(RoomCamera.SpriteLeaser sLeaser);

        public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int j = 0; j < this.cosmetics.Count; j++)
            {
                this.cosmetics[j].DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }
        }

        public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            this.palette = palette;
            for (int i = 0; i < this.cosmetics.Count; i++)
            {
                this.cosmetics[i].ApplyPalette(sLeaser, rCam, palette);
            }
        }

        public virtual void Reset()
        {

            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].Reset();
            }
        }

        public abstract float HeadRotation(float timeStacker);

        public abstract LizardGraphics.LizardSpineData SpinePosition(float spineFactor, float timeStacker);

        public abstract Color BodyColor(float y);

        public abstract Color HeadColor(float v);
    }
    public struct CosmeticsParams
    {
        internal float fatness;
        internal float tailFatness;

        public CosmeticsParams(float fatness, float tailFatness)
        {
            this.fatness = fatness;
            this.tailFatness = tailFatness;
        }
    }
}