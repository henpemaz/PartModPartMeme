using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSkin
{
    public class PlayerGraphicsCosmeticsAdaptor : ICosmeticsAdaptor
    {
        private PlayerGraphics pGraphics;
        public GraphicsModule graphics { get => this.pGraphics; }

        private float _bodyLength;
        private float _tailLength;
        private RoomPalette _palette;

        public float BodyAndTailLength { get => this.bodyLength + this.tailLength; }

        public Color effectColor { get => _effectColor; set => _effectColor = value; }
        public float bodyLength { get => _bodyLength; set => _bodyLength = value; }
        public float tailLength { get => _tailLength; set => _tailLength = value; }
        public RoomPalette palette { get => _palette; set => _palette = value; }
        public float showDominance { get => _showDominance; set => _showDominance = value; }
        public CosmeticsParams cosmeticsParams { get => _cosmeticsParams; set => _cosmeticsParams = value; }
        public List<GenericCosmeticsTemplate> cosmetics { get => _cosmetics; set => _cosmetics = value; }

        private float _showDominance;
        private Color _effectColor;
        private float depthRotation;
        private float lastDepthRotation;
        private CosmeticsParams _cosmeticsParams;
        private List<GenericCosmeticsTemplate> _cosmetics;
        private int extraSprites;

        public static void ApplyHooksToGraphics()
        {
            On.PlayerGraphics.ctor += PlayerGraphics_ctor_hk;
            On.PlayerGraphics.Update += PlayerGraphics_Update_hk;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites_hk;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites_hk;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette_hk;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset_hk;
        }

        private static void PlayerGraphics_Reset_hk(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            staticAdaptor.Reset();
        }

        private static void PlayerGraphics_ApplyPalette_hk(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            staticAdaptor.ApplyPalette(sLeaser, rCam, palette);
        }

        private static void PlayerGraphics_DrawSprites_hk(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            staticAdaptor.DrawSprites(sLeaser, rCam, timeStacker, camPos);

        }

        private static void PlayerGraphics_InitiateSprites_hk(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            staticAdaptor.InitiateSprites(sLeaser, rCam);

        }

        public static PlayerGraphicsCosmeticsAdaptor staticAdaptor;

        public static void PlayerGraphics_ctor_hk(On.PlayerGraphics.orig_ctor orig, PlayerGraphics instance, PhysicalObject ow)
        {
            orig(instance, ow);
            staticAdaptor = new PlayerGraphicsCosmeticsAdaptor(instance);
        }
        private static void PlayerGraphics_Update_hk(On.PlayerGraphics.orig_Update orig, PlayerGraphics instance)
        {
            orig(instance);
            staticAdaptor.Update();
        }

        public PlayerGraphicsCosmeticsAdaptor(PlayerGraphics pGraphics)
        {
            this.pGraphics = pGraphics;

            this.bodyLength = this.pGraphics.player.bodyChunkConnections[0].distance;
            this.tailLength = 0f;
            for (int l = 0; l < this.pGraphics.tail.Length; l++)
            {
                this.tailLength += this.pGraphics.tail[l].connectionRad;
            }

            this.showDominance = Mathf.Clamp(this.showDominance - 1f / Mathf.Lerp(60f, 120f, UnityEngine.Random.value), 0f, 1f);

            this.depthRotation = 0;
            this.lastDepthRotation = this.depthRotation;


            this.effectColor = Custom.HSL2RGB(Custom.WrappedRandomVariation(0.49f, 0.04f, 0.6f), 1f, Custom.ClampedRandomVariation(0.5f, 0.15f, 0.1f));

            this.cosmeticsParams = new CosmeticsParams(0.5f, 0.5f);

            this.cosmetics = new List<GenericCosmeticsTemplate>();
            this.extraSprites = 0;

            // This is trouble!!!
            int spriteIndex = 12;
            spriteIndex = this.AddCosmetic(spriteIndex, new SlugcatTailTuft(this, spriteIndex));

            //for(int i = 0; i < (this.cosmetics[0] as SlugcatTailTuft).scalesPositions.Length; i++)
            //         {
            //	Debug.LogError((this.cosmetics[0] as SlugcatTailTuft).scalesPositions[i].ToString("F4"));
            //	//Debug.LogError("scales y = " + (this.cosmetics[0] as SlugcatTailTuft).scalesPositions[i].y);
            //	//Debug.LogError("scales x = " + (this.cosmetics[0] as SlugcatTailTuft).scalesPositions[i].x);
            //}
        }

        public int AddCosmetic(int spriteIndex, GenericCosmeticsTemplate cosmetic)
        {
            this.cosmetics.Add(cosmetic);
            spriteIndex += cosmetic.numberOfSprites;
            this.extraSprites += cosmetic.numberOfSprites;
            return spriteIndex;
        }

        public void Update()
        {

            //this.bodyLength = this.pGraphics.player.bodyChunks[0].rad;
            //this.bodyLength = this.pGraphics.head.rad;
            //this.bodyLength = Custom.Dist(this.pGraphics.head.pos, this.pGraphics.head.connection.pos);
            this.bodyLength = this.pGraphics.player.bodyChunkConnections[0].distance;
            //for (int n = 0; n < this.pGraphics.player.bodyChunkConnections.Length; n++)
            //            {
            //                this.bodyLength += this.pGraphics.player.bodyChunkConnections[n].distance;
            //            }

            this.tailLength = 0f;
            for (int l = 0; l < this.pGraphics.tail.Length; l++)
            {
                this.tailLength += this.pGraphics.tail[l].connectionRad;
            }

            this.showDominance = Mathf.Clamp(this.showDominance - 1f / Mathf.Lerp(60f, 120f, UnityEngine.Random.value), 0f, 1f);

            this.lastDepthRotation = this.depthRotation;
            float newRotation = 0;
            this.depthRotation = Mathf.Lerp(this.depthRotation, newRotation, 0.1f);

            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].Update();
            }
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
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

        protected FNode getOnTopNode(RoomCamera.SpriteLeaser sLeaser)
        {
            return sLeaser.sprites[6];
        }

        protected FNode getBehindHeadNode(RoomCamera.SpriteLeaser sLeaser)
        {
            return sLeaser.sprites[3];
        }

        protected FSprite getBehindNode(RoomCamera.SpriteLeaser sLeaser)
        {
            return sLeaser.sprites[0];
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            for (int j = 0; j < this.cosmetics.Count; j++)
            {
                this.cosmetics[j].DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            this.palette = palette;
            for (int i = 0; i < this.cosmetics.Count; i++)
            {
                this.cosmetics[i].ApplyPalette(sLeaser, rCam, palette);
            }
        }

        public void Reset()
        {

            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].Reset();
            }
        }

        public float HeadRotation(float timeStacker)
        {
            float num = Custom.AimFromOneVectorToAnother(Vector2.Lerp(this.pGraphics.drawPositions[0, 1], this.pGraphics.drawPositions[0, 0], timeStacker), Vector2.Lerp(this.pGraphics.head.lastPos, this.pGraphics.head.pos, timeStacker));
            return num;
        }

        public LizardGraphics.LizardSpineData SpinePosition(float spineFactor, float timeStacker)
        {
            // float num = this.pGraphics.player.bodyChunkConnections[0].distance + this.pGraphics.player.bodyChunkConnections[1].distance;
            Vector2 vector;
            float from;
            Vector2 direction;
            Vector2 vector2;
            float to;
            float t;
            if (spineFactor < this.bodyLength / this.BodyAndTailLength)
            {
                float inBodyFactor = Mathf.InverseLerp(0f, this.bodyLength / this.BodyAndTailLength, spineFactor);

                vector = Vector2.Lerp(this.pGraphics.drawPositions[0, 1], this.pGraphics.drawPositions[0, 0], timeStacker);
                from = this.pGraphics.player.bodyChunks[0].rad * this.cosmeticsParams.fatness;

                vector2 = Vector2.Lerp(this.pGraphics.drawPositions[1, 1], this.pGraphics.drawPositions[1, 0], timeStacker);
                to = this.pGraphics.player.bodyChunks[1].rad;
                direction = Custom.DirVec(vector, vector2);

                t = inBodyFactor;
            }
            else
            {
                float inTailFactor = Mathf.InverseLerp(this.bodyLength / this.BodyAndTailLength, 1f, spineFactor);
                int num6 = Mathf.FloorToInt(inTailFactor * (float)this.pGraphics.tail.Length - 1f);
                int num7 = Mathf.FloorToInt(inTailFactor * (float)this.pGraphics.tail.Length);
                if (num7 > this.pGraphics.tail.Length - 1)
                {
                    num7 = this.pGraphics.tail.Length - 1;
                }
                if (num6 < 0)
                {
                    vector = Vector2.Lerp(this.pGraphics.drawPositions[1, 1], this.pGraphics.drawPositions[1, 0], timeStacker);
                    from = this.pGraphics.player.bodyChunks[1].rad * this.cosmeticsParams.fatness;
                }
                else
                {
                    vector = Vector2.Lerp(this.pGraphics.tail[num6].lastPos, this.pGraphics.tail[num6].pos, timeStacker);
                    from = this.pGraphics.tail[num6].StretchedRad * this.cosmeticsParams.fatness * this.cosmeticsParams.tailFatness;
                }
                direction = Vector2.Lerp(this.pGraphics.tail[Mathf.Min(num7 + 1, this.pGraphics.tail.Length - 1)].lastPos, this.pGraphics.tail[Mathf.Min(num7 + 1, this.pGraphics.tail.Length - 1)].pos, timeStacker);
                vector2 = Vector2.Lerp(this.pGraphics.tail[num7].lastPos, this.pGraphics.tail[num7].pos, timeStacker);
                to = this.pGraphics.tail[num7].StretchedRad;
                t = Mathf.InverseLerp((float)(num6 + 1), (float)(num7 + 1), inTailFactor * (float)this.pGraphics.tail.Length);
            }
            Vector2 normalized = Vector2.Lerp(vector2 - vector, direction - vector2, t).normalized;
            if (normalized.x == 0f && normalized.y == 0f)
            {
                normalized = (this.pGraphics.tail[this.pGraphics.tail.Length - 1].pos - this.pGraphics.tail[this.pGraphics.tail.Length - 2].pos).normalized;
            }
            Vector2 vector3 = Custom.PerpendicularVector(normalized);
            float rad = Mathf.Lerp(from, to, t);
            float rot = Mathf.Lerp(this.lastDepthRotation, this.depthRotation, timeStacker);
            rot = Mathf.Pow(Mathf.Abs(rot), Mathf.Lerp(1.2f, 0.3f, Mathf.Pow(spineFactor, 0.5f))) * Mathf.Sign(rot);
            Vector2 outerPos = Vector2.Lerp(vector, vector2, t) + vector3 * rot * rad;
            return new LizardGraphics.LizardSpineData(spineFactor, Vector2.Lerp(vector, vector2, t), outerPos, normalized, vector3, rot, rad);
        }

        public Color BodyColor(float y)
        {
            return PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);
            //if (y < this.bodyLength / this.BodyAndTailLength || this.cosmeticsParams.tailColor == 0f)
            //            {
            //                return PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);
            //            }
            //            float value = Mathf.InverseLerp(this.bodyLength / this.BodyAndTailLength, 1f, y);
            //            float num = Mathf.Clamp(Mathf.InverseLerp(this.cosmeticsParams.tailColorationStart, 0.95f, value), 0f, 1f);
            //            num = Mathf.Pow(num, this.cosmeticsParams.tailColorationExponent) * this.cosmeticsParams.tailColor;
            //            return Color.Lerp(this.palette.blackColor, this.effectColor, num);
        }

        public Color HeadColor(float v)
        {
            return PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);
        }
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