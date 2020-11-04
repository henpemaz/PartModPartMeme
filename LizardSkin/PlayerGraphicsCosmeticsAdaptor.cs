﻿using RWCustom;
using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace LizardSkin
{
    public class PlayerGraphicsCosmeticsAdaptor : GenericCosmeticsAdaptor
    {
        protected PlayerGraphics pGraphics { get => this.graphics as PlayerGraphics; }

        public static void ApplyHooksToPlayerGraphics()
        {
            On.PlayerGraphics.ctor += PlayerGraphics_ctor_hk;
            On.PlayerGraphics.Update += PlayerGraphics_Update_hk;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites_hk;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites_hk;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette_hk;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset_hk;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer_hk;
        }

        public static void ApplyHooksToJollyPlayerGraphicsHK()
        {
            Type jollypg = Type.GetType("JollyCoop.PlayerGraphicsHK, JollyCoop");

            new Hook(jollypg.GetMethod("PlayerGraphics_ApplyPalette", BindingFlags.NonPublic | BindingFlags.Static), typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("PlayerGraphics_ApplyPalette_jolly_fix", BindingFlags.NonPublic | BindingFlags.Static));
            new Hook(jollypg.GetMethod("SwichtLayersVanilla", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static), typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("Jolly_SwichtLayersVanilla_fix", BindingFlags.NonPublic | BindingFlags.Static));
        }


        public static PlayerGraphicsCosmeticsAdaptor[] playerAdaptors = new PlayerGraphicsCosmeticsAdaptor[4];
        public static List<PlayerGraphicsCosmeticsAdaptor> ghostAdaptors = new List<PlayerGraphicsCosmeticsAdaptor>();
        protected static PlayerGraphicsCosmeticsAdaptor getAdaptor(PlayerGraphics instance)
        {
            PlayerState playerState = (instance.owner as Player).playerState;
            if (!playerState.isGhost)
            {
                //Debug.LogError("Retreiving LS Adaptor for player " + playerState.playerNumber);
                return playerAdaptors[playerState.playerNumber];
            }
            PlayerGraphicsCosmeticsAdaptor toReturn = null;
            for(int i = ghostAdaptors.Count; i >= 0; i--)
            {
                if (ghostAdaptors[i].graphics.owner.slatedForDeletetion)
                {
                    ghostAdaptors.RemoveAt(i);
                }
                else if (toReturn is null && ghostAdaptors[i].pGraphics == instance)
                {
                    toReturn = ghostAdaptors[i];
                }
            }
            return toReturn;
        }

        protected static void addAdaptor(PlayerGraphicsCosmeticsAdaptor adaptor)
        {
            PlayerState playerState = (adaptor.graphics.owner as Player).playerState;
            if (!playerState.isGhost)
            {
                //Debug.LogError("Adding LS Adaptor for player " + playerState.playerNumber);
                playerAdaptors[playerState.playerNumber] = adaptor;
            }
            else
            {
                ghostAdaptors.Add(adaptor);
            }
        }


        protected static void PlayerGraphics_ctor_hk(On.PlayerGraphics.orig_ctor orig, PlayerGraphics instance, PhysicalObject ow)
        {
            orig(instance, ow);
            addAdaptor(new PlayerGraphicsCosmeticsAdaptor(instance));
        }
        protected static void PlayerGraphics_Reset_hk(On.PlayerGraphics.orig_Reset orig, PlayerGraphics instance)
        {
            orig(instance);
            getAdaptor(instance).Reset();
        }

        protected static void PlayerGraphics_ApplyPalette_hk(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(instance, sLeaser, rCam, palette);
            getAdaptor(instance).ApplyPalette(sLeaser, rCam, palette);
        }

        public delegate void jolly_ApplyPalette_hook(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette);
        protected static void PlayerGraphics_ApplyPalette_jolly_fix(jolly_ApplyPalette_hook orig_hook, On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            // Who hooks the hookers ???
            orig_hook(orig, instance, sLeaser, rCam, palette);
            getAdaptor(instance).ApplyPalette(sLeaser, rCam, palette);
        }

        public delegate void SwichtLayersVanilla(PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, bool newOverlap);
        protected static void Jolly_SwichtLayersVanilla_fix(SwichtLayersVanilla orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, bool newOverlap)
        {
            orig(instance, sLeaser, rCam, newOverlap);
            FContainer fcontainer = rCam.ReturnFContainer(newOverlap ? "Background" : "Midground");
            getAdaptor(instance).AddToContainer(sLeaser, rCam, fcontainer);
        }

        protected static void PlayerGraphics_DrawSprites_hk(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(instance, sLeaser, rCam, timeStacker, camPos);
            getAdaptor(instance).DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        protected static void PlayerGraphics_InitiateSprites_hk(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(instance, sLeaser, rCam);
            getAdaptor(instance).InitiateSprites(sLeaser, rCam);
        }

        protected static void PlayerGraphics_AddToContainer_hk(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(instance, sLeaser, rCam, newContatiner);
            getAdaptor(instance).AddToContainer(sLeaser, rCam, newContatiner);
        }

        protected static void PlayerGraphics_Update_hk(On.PlayerGraphics.orig_Update orig, PlayerGraphics instance)
        {
            orig(instance);
            getAdaptor(instance).Update();
        }

        Type jolly_ref;
        public PlayerGraphicsCosmeticsAdaptor(PlayerGraphics pGraphics) : base(pGraphics)
        {
            jolly_ref = Type.GetType("JollyCoop.PlayerGraphicsHK, JollyCoop");

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

            this.cosmetics = new List<GenericCosmeticTemplate>();
            this.extraSprites = 0;

            this.AddCosmetic(new SlugcatTailTuft(this));
            this.AddCosmetic(new SlugcatTailTuft(this));
            this.AddCosmetic(new SlugcatTailTuft(this));

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

        protected override FNode getBehindNode(RoomCamera.SpriteLeaser sLeaser)
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

        public Color color_from_jolly(Color color)
        {
            if (pGraphics.player.room != null && !pGraphics.player.room.game.IsStorySession)
            {
                return color;
            }
            if (!JollyCoop.JollyMod.config.enableColors[pGraphics.player.playerState.playerNumber] && !(typeof(JollyCoop.JollyMod.CoopConfig).GetField("randomColors", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(JollyCoop.JollyMod.config) as bool[])[pGraphics.player.playerState.playerNumber])
            {
                return color;
            }
            color = JollyCoop.JollyMod.config.playerBodyColors[pGraphics.player.playerState.playerNumber];
            if (pGraphics.malnourished > 0f)
            {
                float num = (!pGraphics.player.Malnourished) ? Mathf.Max(0f, pGraphics.malnourished - 0.005f) : pGraphics.malnourished;
                color = Color.Lerp(color, Color.gray, 0.4f * num);
            }
            return color;
        }

        public override Color BodyColor(float y)
        {
            Color color = PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);
            if (jolly_ref != null)
            {
                color = color_from_jolly(color);
            }
            return color;
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
            //return PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);
            return BodyColor(0f);
        }

        public override int getFirstSpriteImpl()
        {
            return 12;
        }
    }
}