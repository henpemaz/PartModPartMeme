using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSkin
{
    public class PlayerGraphicsCosmeticsAdaptor : GenericCosmeticsAdaptor
    {
        private PlayerGraphics pGraphics { get => this.graphics as PlayerGraphics; }

        public static void ApplyHooksToPlayerGraphics()
        {
            On.PlayerGraphics.ctor += PlayerGraphics_ctor_hk;
            On.PlayerGraphics.Update += PlayerGraphics_Update_hk;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites_hk;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites_hk;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette_hk;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset_hk;
        }

        // Quick and dirty
        public static PlayerGraphicsCosmeticsAdaptor staticAdaptor;
        private static void PlayerGraphics_ctor_hk(On.PlayerGraphics.orig_ctor orig, PlayerGraphics instance, PhysicalObject ow)
        {
            orig(instance, ow);
            staticAdaptor = new PlayerGraphicsCosmeticsAdaptor(instance);
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

        private static void PlayerGraphics_Update_hk(On.PlayerGraphics.orig_Update orig, PlayerGraphics instance)
        {
            orig(instance);
            staticAdaptor.Update();
        }

        public PlayerGraphicsCosmeticsAdaptor(PlayerGraphics pGraphics) : base(pGraphics)
        {
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
            int spriteIndex = firstSprite;
            spriteIndex = this.AddCosmetic(spriteIndex, new SlugcatTailTuft(this, spriteIndex));

            //for(int i = 0; i < (this.cosmetics[0] as SlugcatTailTuft).scalesPositions.Length; i++)
            //         {
            //	Debug.LogError((this.cosmetics[0] as SlugcatTailTuft).scalesPositions[i].ToString("F4"));
            //	//Debug.LogError("scales y = " + (this.cosmetics[0] as SlugcatTailTuft).scalesPositions[i].y);
            //	//Debug.LogError("scales x = " + (this.cosmetics[0] as SlugcatTailTuft).scalesPositions[i].x);
            //}
        }

        public override void Update()
        {
            this.bodyLength = this.pGraphics.player.bodyChunkConnections[0].distance;
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

        protected override FNode getOnTopNode(RoomCamera.SpriteLeaser sLeaser)
        {
            return sLeaser.sprites[6];
        }

        protected override FNode getBehindHeadNode(RoomCamera.SpriteLeaser sLeaser)
        {
            return sLeaser.sprites[3];
        }

        protected override FSprite getBehindNode(RoomCamera.SpriteLeaser sLeaser)
        {
            return sLeaser.sprites[0];
        }

        public override float HeadRotation(float timeStacker)
        {
            float num = Custom.AimFromOneVectorToAnother(Vector2.Lerp(this.pGraphics.drawPositions[0, 1], this.pGraphics.drawPositions[0, 0], timeStacker), Vector2.Lerp(this.pGraphics.head.lastPos, this.pGraphics.head.pos, timeStacker));
            return num;
        }

        public override LizardGraphics.LizardSpineData SpinePosition(float spineFactor, float timeStacker)
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

        public override Color BodyColor(float y)
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

        public override Color HeadColor(float v)
        {
            return PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);
        }

        public override int getFirstSpriteImpl()
        {
            return 12;
        }
    }
}