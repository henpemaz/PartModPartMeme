using RWCustom;
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
        protected Player player { get => this.graphics.owner as Player; }

        public static void ApplyHooksToPlayerGraphics()
        {
            LogMethodName();
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
            LogMethodName();
            Type jollypg = Type.GetType("JollyCoop.PlayerGraphicsHK, JollyCoop");
            new Hook(jollypg.GetMethod("PlayerGraphics_ApplyPalette", BindingFlags.NonPublic | BindingFlags.Static), typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("PlayerGraphics_ApplyPalette_jolly_fix", BindingFlags.NonPublic | BindingFlags.Static));
            new Hook(jollypg.GetMethod("SwichtLayersVanilla", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static), typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("Jolly_SwichtLayersVanilla_fix", BindingFlags.NonPublic | BindingFlags.Static));
        }

        public static void ApplyHooksToColorfootPlayerGraphicsPatch()
        {
            LogMethodName();
            Type colorfootpg = Type.GetType("Colorfoot.PlayerGraphicsPatch, Colorfoot");
            new Hook(colorfootpg.GetMethod("ApplyPalette", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static), typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("Colorfoot_ApplyPalette_fix", BindingFlags.NonPublic | BindingFlags.Static));
        }

        //public static PlayerGraphicsCosmeticsAdaptor[] playerAdaptors = new PlayerGraphicsCosmeticsAdaptor[4];
        //public static List<PlayerGraphicsCosmeticsAdaptor> ghostAdaptors = new List<PlayerGraphicsCosmeticsAdaptor>();
        public static WeakReference[] playerAdaptors = new WeakReference[4];
        public static List<WeakReference> ghostAdaptors = new List<WeakReference>();
        protected static PlayerGraphicsCosmeticsAdaptor getAdaptor(PlayerGraphics instance)
        {
            PlayerState playerState = (instance.owner as Player).playerState;
            if (!playerState.isGhost)
            {
                // Debug.LogError("LizardSkin: Retreiving LS Adaptor for player " + playerState.playerNumber);
                return playerAdaptors[playerState.playerNumber].Target as PlayerGraphicsCosmeticsAdaptor;
            }
            PlayerGraphicsCosmeticsAdaptor toReturn = null;
            for(int i = ghostAdaptors.Count-1; i >= 0; i--)
            {
                if (!ghostAdaptors[i].IsAlive || (ghostAdaptors[i].Target as PlayerGraphicsCosmeticsAdaptor).graphics.owner.slatedForDeletetion)
                {
                    ghostAdaptors.RemoveAt(i);
                }
                else if (toReturn is null && (ghostAdaptors[i].Target as PlayerGraphicsCosmeticsAdaptor).pGraphics == instance)
                {
                    toReturn = ghostAdaptors[i].Target as PlayerGraphicsCosmeticsAdaptor;
                }
            }
            return toReturn;
        }

        protected static void addAdaptor(PlayerGraphicsCosmeticsAdaptor adaptor)
        {
            LogMethodName();
            PlayerState playerState = (adaptor.graphics.owner as Player).playerState;
            if (!playerState.isGhost)
            {
                // Debug.LogError("LizardSkin: Adding LS Adaptor for player " + playerState.playerNumber);
                playerAdaptors[playerState.playerNumber] = new WeakReference(adaptor);
            }
            else
            {
                ghostAdaptors.Add(new WeakReference(adaptor));
            }
        }


        protected static void PlayerGraphics_ctor_hk(On.PlayerGraphics.orig_ctor orig, PlayerGraphics instance, PhysicalObject ow)
        {
            LogMethodName();
            orig(instance, ow);
            Type fpg = Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats");
            if (fpg != null && fpg.IsInstanceOfType(instance))
            {
                // skip default constructor if Fancy
                // Debug.Log("LizardSkin: Skipping default adaptor, Fancy detected");
                return;
            }

            PlayerGraphicsCosmeticsAdaptor adaptor = new PlayerGraphicsCosmeticsAdaptor(instance);
            System.Array.Resize(ref instance.bodyParts, instance.bodyParts.Length + 1);
            instance.bodyParts[instance.bodyParts.Length - 1] = adaptor;
            addAdaptor(adaptor);
        }
        protected static void PlayerGraphics_Reset_hk(On.PlayerGraphics.orig_Reset orig, PlayerGraphics instance)
        {
            LogMethodName();
            orig(instance);
            getAdaptor(instance).Reset();
        }

        protected static void PlayerGraphics_ApplyPalette_hk(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            LogMethodName();
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

        public delegate void Colorfoot_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette);
        protected static void Colorfoot_ApplyPalette_fix(Colorfoot_ApplyPalette orig_hook, On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig_hook(orig, instance, sLeaser, rCam, palette);
            if (Colorfoot.LegMod.config.setting == 2)
            {
                getAdaptor(instance).ApplyPalette(sLeaser, rCam, palette);
            }
        }

        protected static void PlayerGraphics_DrawSprites_hk(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(instance, sLeaser, rCam, timeStacker, camPos);
            getAdaptor(instance).DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        protected static void PlayerGraphics_InitiateSprites_hk(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            LogMethodName();
            orig_InitiateSprites_lock = true;
            orig(instance, sLeaser, rCam);
            orig_InitiateSprites_lock = false;

            getAdaptor(instance).InitiateSprites(sLeaser, rCam);
        }

        protected static void PlayerGraphics_AddToContainer_hk(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            LogMethodName();
            orig(instance, sLeaser, rCam, newContatiner);
            if (orig_InitiateSprites_lock)
            {
                // Debug.Log("LizardSkin: Avoiding orig_InitiateSprites_lock");
            }
            else
            {
                getAdaptor(instance).AddToContainer(sLeaser, rCam, newContatiner);
            }
        }

        protected static void PlayerGraphics_Update_hk(On.PlayerGraphics.orig_Update orig, PlayerGraphics instance)
        {
            orig(instance);
            getAdaptor(instance).Update();
        }

        Type jolly_ref;
        Type custail_ref;
        Type colorfoot_ref;
        protected static bool orig_InitiateSprites_lock; // initialize calls palette

        public PlayerGraphicsCosmeticsAdaptor(PlayerGraphics pGraphics) : base(pGraphics)
        {
            LogMethodName();
            jolly_ref = Type.GetType("JollyCoop.PlayerGraphicsHK, JollyCoop");
            custail_ref = Type.GetType("CustomTail.CustomTail, CustomTail");
            colorfoot_ref = Type.GetType("Colorfoot.LegMod, Colorfoot");

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

            //this.AddCosmetic(new GenericTailTuft(this));
            //this.AddCosmetic(new GenericSpineSpikes(this));

            //this.AddCosmetic(new GenericAxolotlGills(this));
            //this.AddCosmetic(new GenericTailFin(this));

            //this.AddCosmetic(new GenericWingScales(this));
            //this.AddCosmetic(new GenericTailGeckoScales(this));
            //this.AddCosmetic(new GenericJumpRings(this));

            //this.AddCosmetic(new GenericBumpHawk(this));

            //this.AddCosmetic(new GenericLongShoulderScales(this));
            //this.AddCosmetic(new GenericShortBodyScales(this));

            //this.AddCosmetic(new GenericLongHeadScales(this));

            //this.AddCosmetic(new GenericWhiskers(this));
            //this.AddCosmetic(new GenericAntennae(this));

            this.AddCosmetic(new GenericTailTuft(this));
            this.AddCosmetic(new GenericAxolotlGills(this));
            this.AddCosmetic(new GenericJumpRings(this));

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



            base.Update();
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


        static int debug_counter = 0;
        protected override void updateRotation()
        {
            this.showDominance = Mathf.Clamp(this.showDominance - 1f / Mathf.Lerp(60f, 120f, UnityEngine.Random.value), 0f, 1f);

            this.lastDepthRotation = this.depthRotation;
            this.lastHeadDepthRotation = this.headDepthRotation;

            float newRotation;

            Vector2 upDir = Custom.DirVec(this.pGraphics.drawPositions[1, 0], this.pGraphics.drawPositions[0, 0]);
            float upRot = Custom.VecToDeg(upDir);
            Vector2 lookDir = this.pGraphics.lookDirection * 3f * (1f - this.player.sleepCurlUp);
            if (this.player.sleepCurlUp > 0f)
            {
                lookDir.y -= 2f * this.player.sleepCurlUp;
                lookDir.x -= 4f * Mathf.Sign(this.pGraphics.drawPositions[0, 0].x - this.pGraphics.drawPositions[1, 0].x) * this.player.sleepCurlUp;
            }
            else if (this.player.room.gravity != 0 && this.player.Consious)
            {
                if (this.player.bodyMode == Player.BodyModeIndex.Stand && this.player.input[0].x != 0)
                { lookDir.x += 4f * Mathf.Sign(this.player.input[0].x); lookDir.y++; }
                else if (this.player.bodyMode == Player.BodyModeIndex.Crawl)
                { lookDir.x += 4f * Mathf.Sign(this.pGraphics.drawPositions[0, 0].x - this.pGraphics.drawPositions[1, 0].x); lookDir.y++; }
            }
            else { lookDir *= 0f; }
            float lookRot = lookDir.magnitude > float.Epsilon ? (Custom.VecToDeg(lookDir) -
                (this.player.Consious && this.player.bodyMode == Player.BodyModeIndex.Crawl ? 0f : upRot)) : 0f;
            if (Mathf.Abs(lookRot) < 90f)
            { newRotation = Custom.LerpMap(lookRot, 0f, Mathf.Sign(lookRot) * 90f, 0f, Mathf.Sign(lookRot) * 60f, 0.5f); }
            else
            { newRotation = Custom.LerpMap(lookRot, Mathf.Sign(lookRot) * 180f, Mathf.Sign(lookRot) * 90f, Mathf.Sign(lookRot) * 60f, 0f, 0.5f); }
            // Tail rotation
            //float totTailRot = newRotation, lastTailRot = -upRot;
            //for (int t = 0; t < 4; t++)
            //{
            //    float tailRot = -Custom.AimFromOneVectorToAnother(t == 0 ? this.pGraphics.drawPositions[1, 0]
            //        : this.pGraphics.tail[t - 1].pos, this.pGraphics.tail[t].pos);
            //    tailRot -= lastTailRot; lastTailRot += tailRot;
            //    totTailRot += tailRot;
            //    //dbg[t] = tailRot;
            //    zRot[1 + t, 0] = totTailRot < 0f ? Mathf.Clamp(totTailRot, -90f, 0f) : Mathf.Clamp(totTailRot, 0f, 90f);
            //}

            this.depthRotation = Mathf.Lerp(this.depthRotation, Mathf.Clamp(newRotation / 60f, -1f, 1f), 0.1f);
            this.headDepthRotation = depthRotation;

            debug_counter++;
            if(debug_counter % 100 == 0)
            {
                Debug.Log("LizardSkin: depthRotation is " + depthRotation);
            }
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

        public Color color_from_colorfoot(Color color)
        {
            if (Colorfoot.LegMod.config.setting != 0)
            {
                return Colorfoot.PlayerGraphicsPatch.bodyColors[pGraphics.player.playerState.slugcatCharacter];
            }
            return color;
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
            
            return color;
        }

        public override Color BodyColor(float y)
        {

            if (y < this.bodyLength / this.BodyAndTailLength || this.custail_ref == null)
            {
                return BaseBodyColor();
            }

            float tailFactor = Mathf.InverseLerp(this.bodyLength / this.BodyAndTailLength, 1f, y);
            return CustomTailColor(tailFactor);
        }

        public virtual Color BaseBodyColor()
        {
            Color color = PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);

            if (jolly_ref != null)
            {
                color = color_from_jolly(color);
            }

            // Vanilla and Jolly
            if (pGraphics.malnourished > 0f)
            {
                float num = (!pGraphics.player.Malnourished) ? Mathf.Max(0f, pGraphics.malnourished - 0.005f) : pGraphics.malnourished;
                color = Color.Lerp(color, Color.gray, 0.4f * num);
            }

            if (colorfoot_ref != null)
            {
                color = color_from_colorfoot(color);
            }

            return color;
        }

        public virtual Color CustomTailColor(float tailFactor)
        {
            CustomTail.TailConfig tailConfig = CustomTail.CustomTail.GetTailConfig(this.pGraphics.player.playerState.slugcatCharacter);

            Color color = BaseBodyColor();
            Color color2 = tailConfig.baseTint;
            Color color3 = tailConfig.tipTint;
            if (color2 == Color.black)
            {
                color2 = color;
            }
            if (color3 == Color.black)
            {
                color3 = color;
            }
            return Color.Lerp(color2, color3, tailFactor);
        }

        public override Color HeadColor(float timeStacker)
        {
            //return PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);
            return BaseBodyColor();
        }

        public override BodyPart getHeadImpl()
        {
            return this.pGraphics.head;
        }

        public override BodyPart getBaseOfTailImpl()
        {
            return this.pGraphics.tail[0];
        }
    }
}