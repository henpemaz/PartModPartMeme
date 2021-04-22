using RWCustom;
using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace LizardSkin
{
    public class PlayerGraphicsCosmeticsAdaptor : GraphicsModuleCosmeticsAdaptor
    {
        // Hooks in here
        #region Hooks
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

        public static void ApplyHooksToColorfootPlayerGraphicsPatch()
        {
            Type colorfootpg = Type.GetType("Colorfoot.PlayerGraphicsPatch, Colorfoot");
            new Hook(colorfootpg.GetMethod("ApplyPalette", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static), typeof(PlayerGraphicsCosmeticsAdaptor).GetMethod("Colorfoot_ApplyPalette_fix", BindingFlags.NonPublic | BindingFlags.Static));
        }

        public static void InitDebugLabels(PlayerGraphics pg, PhysicalObject ow)
        {
            pg.DEBUGLABELS = new DebugLabel[6];
            pg.DEBUGLABELS[0] = new DebugLabel(ow, new Vector2(0f, 50f));
            pg.DEBUGLABELS[1] = new DebugLabel(ow, new Vector2(0f, 40f));
            pg.DEBUGLABELS[2] = new DebugLabel(ow, new Vector2(0f, 30f));
            pg.DEBUGLABELS[3] = new DebugLabel(ow, new Vector2(0f, 20f));
            pg.DEBUGLABELS[4] = new DebugLabel(ow, new Vector2(0f, 10f));
            pg.DEBUGLABELS[5] = new DebugLabel(ow, new Vector2(0f, 0f));
        }

        protected static void PlayerGraphics_ctor_hk(On.PlayerGraphics.orig_ctor orig, PlayerGraphics instance, PhysicalObject ow)
        {
            orig(instance, ow);
            //InitDebugLabels(instance, ow);

            if (LizardSkin.fpg_ref != null && LizardSkin.fpg_ref.IsInstanceOfType(instance))
            {
                // skip default constructor if Fancy
                // Debug.Log("LizardSkin: Skipping default adaptor, Fancy detected");
                return;
            }

            PlayerGraphicsCosmeticsAdaptor adaptor = new PlayerGraphicsCosmeticsAdaptor(instance);
            System.Array.Resize(ref instance.bodyParts, instance.bodyParts.Length + 1);
            instance.bodyParts[instance.bodyParts.Length - 1] = adaptor;
            AddAdaptor(adaptor);
        }

        protected static void PlayerGraphics_Reset_hk(On.PlayerGraphics.orig_Reset orig, PlayerGraphics instance)
        {
            orig(instance);
            GetAdaptor(instance).Reset();
        }

        protected static void PlayerGraphics_ApplyPalette_hk(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(instance, sLeaser, rCam, palette);
            GetAdaptor(instance).ApplyPalette(sLeaser, rCam, palette);
        }

        public delegate void jolly_ApplyPalette_hook(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette);
        protected static void PlayerGraphics_ApplyPalette_jolly_fix(jolly_ApplyPalette_hook orig_hook, On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            // Who hooks the hookers ???
            orig_hook(orig, instance, sLeaser, rCam, palette);
            // Your code here
        }

        public delegate void SwichtLayersVanilla(PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, bool newOverlap);
        protected static void Jolly_SwichtLayersVanilla_fix(SwichtLayersVanilla orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, bool newOverlap)
        {
            orig(instance, sLeaser, rCam, newOverlap);
            FContainer fcontainer = rCam.ReturnFContainer(newOverlap ? "Background" : "Midground");
            GetAdaptor(instance).AddToContainer(sLeaser, rCam, fcontainer);
        }

        public delegate void Colorfoot_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette);
        protected static void Colorfoot_ApplyPalette_fix(Colorfoot_ApplyPalette orig_hook, On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig_hook(orig, instance, sLeaser, rCam, palette);
            if (Colorfoot.LegMod.config.setting == 2)
            {
                GetAdaptor(instance).ApplyPalette(sLeaser, rCam, palette);
            }
        }

        protected static void PlayerGraphics_DrawSprites_hk(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(instance, sLeaser, rCam, timeStacker, camPos);
            GetAdaptor(instance).DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        protected static bool orig_InitiateSprites_lock; // initialize calls palette lock
        protected static void PlayerGraphics_InitiateSprites_hk(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig_InitiateSprites_lock = true;
            orig(instance, sLeaser, rCam);
            orig_InitiateSprites_lock = false;

            GetAdaptor(instance).InitiateSprites(sLeaser, rCam);
        }

        protected static void PlayerGraphics_AddToContainer_hk(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(instance, sLeaser, rCam, newContatiner);
            if (orig_InitiateSprites_lock)
            {
                // Debug.Log("LizardSkin: Avoiding orig_InitiateSprites_lock");
            }
            else
            {
                GetAdaptor(instance).AddToContainer(sLeaser, rCam, newContatiner);
            }
        }

        protected static void PlayerGraphics_Update_hk(On.PlayerGraphics.orig_Update orig, PlayerGraphics instance)
        {
            orig(instance);
            GetAdaptor(instance).Update();
        }

        #endregion Hooks

        // Static Adaptor lists and methods
        #region AdaptorManagement
        //public static PlayerGraphicsCosmeticsAdaptor[] playerAdaptors = new PlayerGraphicsCosmeticsAdaptor[4];
        //public static List<PlayerGraphicsCosmeticsAdaptor> ghostAdaptors = new List<PlayerGraphicsCosmeticsAdaptor>();
        public static WeakReference[] playerAdaptors = new WeakReference[4];
        public static List<WeakReference> ghostAdaptors = new List<WeakReference>();
        protected static PlayerGraphicsCosmeticsAdaptor GetAdaptor(PlayerGraphics instance)
        {
            PlayerState playerState = (instance.owner as Player).playerState;
            if (!playerState.isGhost)
            {
                // Debug.LogError("LizardSkin: Retreiving LS Adaptor for player " + playerState.playerNumber);
                return playerAdaptors[playerState.playerNumber].Target as PlayerGraphicsCosmeticsAdaptor;
            }
            PlayerGraphicsCosmeticsAdaptor toReturn = null;
            for (int i = ghostAdaptors.Count - 1; i >= 0; i--)
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

        protected static void AddAdaptor(PlayerGraphicsCosmeticsAdaptor adaptor)
        {
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
        #endregion AdaptorManagement

        protected PlayerGraphics pGraphics { get => this.graphics as PlayerGraphics; }
        protected Player player { get => this.graphics.owner as Player; }

        // Implementing properties
        public override Vector2 headPos => this.pGraphics.head.pos;
        public override Vector2 headLastPos => this.pGraphics.head.lastPos;
        public override Vector2 baseOfTailPos => this.pGraphics.tail[0].pos;
        public override Vector2 baseOfTailLastPos => this.pGraphics.tail[0].lastPos;
        public override Vector2 mainBodyChunkPos => this.player.mainBodyChunk.pos;
        public override Vector2 mainBodyChunkLastPos => this.player.mainBodyChunk.lastPos;
        public override Vector2 mainBodyChunkVel => this.player.mainBodyChunk.vel;

        public override BodyChunk mainBodyChunckSecret => this.player.mainBodyChunk;

        protected override FNode getOnTopNode(RoomCamera.SpriteLeaser sLeaser) => sLeaser.sprites[6];
        protected override FNode getBehindHeadNode(RoomCamera.SpriteLeaser sLeaser) => sLeaser.sprites[3];
        protected override FNode getBehindNode(RoomCamera.SpriteLeaser sLeaser) => sLeaser.sprites[0];

        public PlayerGraphicsCosmeticsAdaptor(PlayerGraphics pGraphics) : base(pGraphics)
        {
            

            this.bodyLength = this.pGraphics.player.bodyChunkConnections[0].distance;
            this.tailLength = 0f;
            for (int l = 0; l < this.pGraphics.tail.Length; l++)
            {
                this.tailLength += this.pGraphics.tail[l].connectionRad;
            }


            this.showDominance = 0;
            this.depthRotation = 0;
            this.lastDepthRotation = this.depthRotation;

            List<LizKinCosmeticData> cosmeticDefs = LizardSkinOI.configuration.GetCosmeticsForSlugcat((int)player.slugcatStats.name, player.playerState.slugcatCharacter, player.playerState.playerNumber);


            foreach (LizKinCosmeticData cosmeticData in cosmeticDefs)
            {
                this.AddCosmetic(GenericCosmeticTemplate.MakeCosmetic(this, cosmeticData));
            }

        }

        public override void Update()
        {
            this.bodyLength = this.pGraphics.player.bodyChunkConnections[0].distance;
            this.tailLength = 0f;
            for (int l = 0; l < this.pGraphics.tail.Length; l++)
            {
                this.tailLength += this.pGraphics.tail[l].connectionRad;
            }

            UpdateRotation();
            base.Update();
        }

        protected void UpdateRotation()
        {
            /*
            Completely re-work the rotation system
            Add z-depth info to cosmetic so they render in-front or behind slugcat based on rotation

            OR don't worry about overlap
            but change the logic to sprites that behave like they're on top vs sprites that behave like they're behind (rot *= -1)
             
            note: currently no vanilla cosmetics use SpritesOverlap.Behind, everything is BehindHead or InFront
             
            TODO
            better body depth rotation 
            using face direction is influenced wayyy to much by look direction


            tail rotation
            increase tail rotation towards +- 1 depending on its perpendicularity to updir, correctly handle 180 turns on long tails
             */




            //if (this.pGraphics.player.input[0].jmp)
            //{
            //    this.showDominance += 0.05f;
            //}
            if (this.pGraphics.player.input[0].thrw)
            {
                this.showDominance += 0.2f;
            }
            if (showDominance > 0)
            {
                this.showDominance = Mathf.Clamp(this.showDominance - 1f / Mathf.Lerp(60f, 120f, UnityEngine.Random.value), 0f, 1f);
            }
            this.lastDepthRotation = this.depthRotation;
            this.lastHeadDepthRotation = this.headDepthRotation;
            //this.lastHeadRotation = this.headRotation;


            float newDepth;
            float newHeadDepth;
            //float newHeadRotation;

            // From playergraphics.draw
            Vector2 neck = this.pGraphics.drawPositions[0, 0];
            Vector2 hips = this.pGraphics.drawPositions[1, 0];
            Vector2 head = this.pGraphics.head.pos;
            float breathfac = 0.5f + 0.5f * Mathf.Sin(this.pGraphics.breath * 3.1415927f * 2f);
            //if (this.player.aerobicLevel > 0.5f)
            //{
            //    neck += Custom.DirVec(hips, neck) * Mathf.Lerp(-1f, 1f, breathfac) * Mathf.InverseLerp(0.5f, 1f, this.player.aerobicLevel) * 0.5f;
            //    head -= Custom.DirVec(hips, neck) * Mathf.Lerp(-1f, 1f, breathfac) * Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, this.player.aerobicLevel), 1.5f) * 0.75f;
            //}
            //float tilt = Custom.AimFromOneVectorToAnother(Vector2.Lerp(hips, neck, 0.5f), head);

            if (this.player.aerobicLevel > 0.5f)
            {
                neck += Custom.DirVec(hips, neck) * Mathf.Lerp(-1f, 1f, breathfac) * Mathf.InverseLerp(0.5f, 1f, this.player.aerobicLevel) * 0.5f;
                head -= Custom.DirVec(hips, neck) * Mathf.Lerp(-1f, 1f, breathfac) * Mathf.Pow(Mathf.InverseLerp(0.5f, 1f, this.player.aerobicLevel), 1.5f) * 0.75f;
            }
            float skew = Mathf.InverseLerp(0.3f, 0.5f, Mathf.Abs(Custom.DirVec(hips, neck).y));
            //sLeaser.sprites[0].x = neck.x - camPos.x;
            //sLeaser.sprites[0].y = neck.y - camPos.y - this.player.sleepCurlUp * 4f + Mathf.Lerp(0.5f, 1f, this.player.aerobicLevel) * breathfac * (1f - skew);
            //sLeaser.sprites[0].rotation = Custom.AimFromOneVectorToAnother(hips, neck);
            //sLeaser.sprites[0].scaleX = 1f + Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(-0.05f, -0.15f, this.malnourished), 0.05f, breathfac) * skew, 0.15f, this.player.sleepCurlUp);
            //sLeaser.sprites[1].x = (hips.x * 2f + neck.x) / 3f - camPos.x;
            //sLeaser.sprites[1].y = (hips.y * 2f + neck.y) / 3f - camPos.y - this.player.sleepCurlUp * 3f;
            //sLeaser.sprites[1].rotation = Custom.AimFromOneVectorToAnother(neck, Vector2.Lerp(this.tail[0].lastPos, this.tail[0].pos, timeStacker));
            //sLeaser.sprites[1].scaleY = 1f + this.player.sleepCurlUp * 0.2f;
            //sLeaser.sprites[1].scaleX = 1f + this.player.sleepCurlUp * 0.2f + 0.05f * breathfac - 0.05f * this.malnourished;
            Vector2 previoustailpos = (hips * 3f + neck) / 4f;
            float d = 1f - 0.2f * this.pGraphics.malnourished;
            float d2 = 6f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 tailpos = this.pGraphics.tail[i].pos;
                Vector2 taildir = (tailpos - previoustailpos).normalized;
                Vector2 perptaildir = Custom.PerpendicularVector(taildir);
                float d3 = Vector2.Distance(tailpos, previoustailpos) / 5f;
                if (i == 0)
                {
                    d3 = 0f;
                }
                //(sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4, previoustailpos - perptaildir * d2 * d + taildir * d3 - camPos);
                //(sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 1, previoustailpos + perptaildir * d2 * d + taildir * d3 - camPos);
                if (i < 3)
                {
                    //(sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 2, tailpos - perptaildir * this.tail[i].StretchedRad * d - taildir * d3 - camPos);
                    //(sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 3, tailpos + perptaildir * this.tail[i].StretchedRad * d - taildir * d3 - camPos);
                }
                else
                {
                    //(sLeaser.sprites[2] as TriangleMesh).MoveVertice(i * 4 + 2, tailpos - camPos);
                }
                d2 = this.pGraphics.tail[i].StretchedRad;
                previoustailpos = tailpos;
            }
            float tilt = Custom.AimFromOneVectorToAnother(Vector2.Lerp(hips, neck, 0.5f), head);
            int tiltIndex = Mathf.RoundToInt(Mathf.Abs(tilt / 360f * 34f));
            if (this.player.sleepCurlUp > 0f)
            {
                tiltIndex = 7;
                tiltIndex = Custom.IntClamp((int)Mathf.Lerp((float)tiltIndex, 4f, this.player.sleepCurlUp), 0, 8);
            }
            Vector2 lookdirx3 = this.pGraphics.lookDirection * 3f * (1f - this.player.sleepCurlUp);
            if (this.player.sleepCurlUp > 0f)
            {
                //sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((this.pGraphics.blink <= 0) ? "A" : "B") + Custom.IntClamp((int)Mathf.Lerp((float)tiltIndex, 1f, this.player.sleepCurlUp), 0, 8));
                //sLeaser.sprites[9].scaleX = Mathf.Sign(neck.x - hips.x);
                //sLeaser.sprites[9].rotation = tilt * (1f - this.player.sleepCurlUp);

                newHeadDepth = Mathf.Clamp(Mathf.Lerp(Mathf.Abs(tilt / 360f * 34f), 1f, this.player.sleepCurlUp) / 8f, 0f, 1f);
                newHeadDepth *= Mathf.Sign(newHeadDepth) * Mathf.Sign(neck.x - hips.x);
                tilt = Mathf.Lerp(tilt, 45f * Mathf.Sign(neck.x - hips.x), this.player.sleepCurlUp);

                newDepth = Mathf.Clamp(Mathf.Lerp(Mathf.Abs(tilt / 360f * 34f), 1f, this.player.sleepCurlUp) / 8f, 0f, 1f);
                newDepth *= Mathf.Sign(newDepth) * Mathf.Sign(neck.x - hips.x);

                head.y += 1f * this.player.sleepCurlUp;
                head.x += Mathf.Sign(neck.x - hips.x) * 2f * this.player.sleepCurlUp;
                lookdirx3.y -= 2f * this.player.sleepCurlUp;
                lookdirx3.x -= 4f * Mathf.Sign(neck.x - hips.x) * this.player.sleepCurlUp;
            }
            else if (base.owner.owner.room != null && base.owner.owner.room.gravity == 0f)
            {
                tiltIndex = 0;
                newHeadDepth = 0;
                newDepth = 0;
                //sLeaser.sprites[9].rotation = tilt;
                //if (this.player.Consious)
                //{
                //    sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((this.blink <= 0) ? "A" : "B") + "0");
                //}
                //else
                //{
                //    sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((!this.player.dead) ? "Stunned" : "Dead"));
                //}
            }
            else if (this.player.Consious)
            {
                if ((this.player.bodyMode == Player.BodyModeIndex.Stand && this.player.input[0].x != 0) || this.player.bodyMode == Player.BodyModeIndex.Crawl)
                {
                    newDepth = 1.0f;
                    newDepth *= Mathf.Sign(newDepth) * Mathf.Sign(neck.x - hips.x);
                    if (this.player.bodyMode == Player.BodyModeIndex.Crawl)
                    {
                        newHeadDepth = 0.88f;
                        newHeadDepth *= Mathf.Sign(newHeadDepth) * Mathf.Sign(neck.x - hips.x);
                        //tiltIndex = 7;
                        //sLeaser.sprites[9].scaleX = Mathf.Sign(neck.x - hips.x);
                    }
                    else
                    {
                        newHeadDepth = 0.66f;
                        newHeadDepth *= Mathf.Sign(newHeadDepth) * ((tilt >= 0f) ? 1f : -1f);
                        //tiltIndex = 6;
                        //sLeaser.sprites[9].scaleX = ((tilt >= 0f) ? 1f : -1f);
                    }
                    lookdirx3.x = 0f;
                    //sLeaser.sprites[9].y += 1f;
                    //sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((this.blink <= 0) ? "A" : "B") + "4");
                }
                else
                {
                    Vector2 animationtilt = head - hips;
                    animationtilt.x *= 1f - lookdirx3.magnitude / 6f; // /3f;
                    animationtilt = animationtilt.normalized;
                    newHeadDepth = Mathf.Clamp(Custom.VecToDeg(animationtilt)/180f, -1, 1);


                    //sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((this.blink <= 0) ? "A" : "B") + Mathf.RoundToInt(Mathf.Abs(Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), animationtilt) / 22.5f)));
                    if (Mathf.Abs(lookdirx3.x) < 0.1f)
                    {
                        newHeadDepth *= Mathf.Sign(newHeadDepth) * ((tilt >= 0f) ? 1f : -1f);
                        //sLeaser.sprites[9].scaleX = ((tilt >= 0f) ? 1f : -1f);
                    }
                    else
                    {
                        newHeadDepth *= Mathf.Sign(newHeadDepth) * Mathf.Sign(lookdirx3.x);
                        //sLeaser.sprites[9].scaleX = Mathf.Sign(lookdirx3.x);
                    }
                    newDepth = newHeadDepth;
                }
                //sLeaser.sprites[9].rotation = 0f;
            }
            else
            {
                newHeadDepth = 0;
                newDepth = 0;
                lookdirx3 *= 0f;
                tiltIndex = 0;
                //sLeaser.sprites[9].element = Futile.atlasManager.GetElementWithName("Face" + ((!this.player.dead) ? "Stunned" : "Dead"));
                //sLeaser.sprites[9].rotation = tilt;
            }


            this.depthRotation = Mathf.Lerp(this.depthRotation, Mathf.Clamp(newDepth, -1f, 1f), 0.2f);
            this.headDepthRotation = Mathf.Lerp(this.headDepthRotation, Mathf.Clamp(newHeadDepth, -1f, 1f), 0.2f);

            if (this.pGraphics.DEBUGLABELS != null)
            {
                this.pGraphics.DEBUGLABELS[0].label.text = "depthRotation: " + depthRotation;
                this.pGraphics.DEBUGLABELS[1].label.text = "headDepthRotation: " + depthRotation;
                SpineData spinehead = SpinePosition(0f, true, 1f);
                SpineData spinetail = SpinePosition(1f, true, 1f);
                this.pGraphics.DEBUGLABELS[2].label.text = "spineDepthAtHead: " + spinehead.depthRotation;
                this.pGraphics.DEBUGLABELS[3].label.text = "spineDepthAtTail: " + spinetail.depthRotation;
                this.pGraphics.DEBUGLABELS[4].label.text = "spineAngleAtHead: " + Custom.VecToDeg(spinehead.dir);
                this.pGraphics.DEBUGLABELS[5].label.text = "spineAngleAtTail: " + Custom.VecToDeg(spinetail.dir);
            }
        }

        public override SpineData SpinePosition(float spineFactor, bool inFront, float timeStacker)
        {
            // float num = this.pGraphics.player.bodyChunkConnections[0].distance + this.pGraphics.player.bodyChunkConnections[1].distance;
            Vector2 topPos;
            float fromRadius;
            Vector2 direction;
            Vector2 bottomPos;
            float toRadius;
            float t;
            if (spineFactor < this.bodyLength / this.BodyAndTailLength)
            {
                float inBodyFactor = Mathf.InverseLerp(0f, this.bodyLength / this.BodyAndTailLength, spineFactor);

                topPos = Vector2.Lerp(this.pGraphics.drawPositions[0, 1], this.pGraphics.drawPositions[0, 0], timeStacker);
                fromRadius = this.pGraphics.player.bodyChunks[0].rad * 0.9f;

                bottomPos = Vector2.Lerp(this.pGraphics.drawPositions[1, 1], this.pGraphics.drawPositions[1, 0], timeStacker);
                toRadius = this.pGraphics.player.bodyChunks[1].rad * 0.95f;
                direction = Custom.DirVec(topPos, bottomPos);

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
                    topPos = Vector2.Lerp(this.pGraphics.drawPositions[1, 1], this.pGraphics.drawPositions[1, 0], timeStacker);
                    fromRadius = this.pGraphics.player.bodyChunks[1].rad;
                }
                else
                {
                    topPos = Vector2.Lerp(this.pGraphics.tail[num6].lastPos, this.pGraphics.tail[num6].pos, timeStacker);
                    fromRadius = this.pGraphics.tail[num6].StretchedRad;
                }
                Vector2 nextPos = Vector2.Lerp(this.pGraphics.tail[Mathf.Min(num7 + 1, this.pGraphics.tail.Length - 1)].lastPos, this.pGraphics.tail[Mathf.Min(num7 + 1, this.pGraphics.tail.Length - 1)].pos, timeStacker);
                bottomPos = Vector2.Lerp(this.pGraphics.tail[num7].lastPos, this.pGraphics.tail[num7].pos, timeStacker);
                toRadius = this.pGraphics.tail[num7].StretchedRad;
                t = Mathf.InverseLerp((float)(num6 + 1), (float)(num7 + 1), inTailFactor * (float)this.pGraphics.tail.Length);
                direction = Vector2.Lerp(bottomPos - topPos, nextPos - bottomPos, t).normalized;
                if (direction.x == 0f && direction.y == 0f)
                {
                    direction = (this.pGraphics.tail[this.pGraphics.tail.Length - 1].pos - this.pGraphics.tail[this.pGraphics.tail.Length - 2].pos).normalized;
                }
            }

            Vector2 perp = Custom.PerpendicularVector(direction);
            float rad = Mathf.Lerp(fromRadius, toRadius, t);
            float rot = Mathf.Lerp(this.lastDepthRotation, this.depthRotation, timeStacker);
            if (!inFront)
            {
                rot = -rot;
                // perp = -perp;
            }
            rot = Mathf.Pow(Mathf.Abs(rot), Mathf.Lerp(1.2f, 0.3f, Mathf.Pow(spineFactor, 0.5f))) * Mathf.Sign(rot);
            Vector2 pos = Vector2.Lerp(topPos, bottomPos, t);
            Vector2 outerPos = pos + perp * rot * rad;
            return new SpineData(spineFactor, pos, outerPos, direction, perp, rot, rad);
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

        public override Color BodyColorFallback(float y)
        {

            if (y < this.bodyLength / this.BodyAndTailLength || LizardSkin.custail_ref == null)
            {
                return BaseBodyColor();
            }

            float tailFactor = Mathf.InverseLerp(this.bodyLength / this.BodyAndTailLength, 1f, y);
            return CustomTailColor(tailFactor);
        }

        public virtual Color BaseBodyColor()
        {
            Color color = PlayerGraphics.SlugcatColor((pGraphics.player.State as PlayerState).slugcatCharacter);

            if (LizardSkin.jolly_ref != null)
            {
                color = color_from_jolly(color);
            }

            // Vanilla and Jolly
            if (pGraphics.malnourished > 0f)
            {
                float num = (!pGraphics.player.Malnourished) ? Mathf.Max(0f, pGraphics.malnourished - 0.005f) : pGraphics.malnourished;
                color = Color.Lerp(color, Color.gray, 0.4f * num);
            }

            if (LizardSkin.colorfoot_ref != null)
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
    }
}