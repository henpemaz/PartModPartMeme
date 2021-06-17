using SlugBase;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using MonoMod.RuntimeDetour;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ProgrammerCat
{
    public class ProgrammerCat : Partiality.Modloader.PartialityMod
    {
        public ProgrammerCat()
        {
            ModID = "ProgrammerCat";
            Version = "1.1";
            author = "Henpemaz";
        }

        public override void OnLoad()
        {
            PlayerManager.RegisterCharacter(new ProgrammerCatSlugcat());
        }
    }

    public class ProgrammerCatSlugcat : SlugBaseCharacter
    {
        public ProgrammerCatSlugcat() : base("ProgrammerCat", FormatVersion.V1, 0) { }

        public override string DisplayName => "The Programmer";
        public override string Description =>
@"On its adorable programmer socks, this nimble and unprepared slugcat
has gotten pretty far from home and now has to face the real world!
Temperamental and on a weird diet, your journey will be a mess.";

        public bool IsPlayerProgrammer(Player player)
        {
            return player.playerState.slugcatCharacter == SlugcatIndex;
        }
        public static bool programmerUpdateLock = false;

        public override Color? SlugcatColor()
        {
            if (isBurnoutCycle) return new Color(1f, (0.69f + 0.96f) / 2f, 0.96f);
            return new Color(1f, 0.96f, 0.96f);
        }

        public override bool HasGuideOverseer => true;

        public override string StartRoom => "SI_B12";

        protected override void GetStats(SlugcatStats stats)
        {
            // Slugbase already "checks" if its the character, but... ?
            stats.bodyWeightFac *= 0.85f;
            stats.runspeedFac *= 0.9f;
            //stats.runspeedFac *= 4.0f;
            stats.poleClimbSpeedFac *= 1.1f;
            stats.corridorClimbSpeedFac *= 1.4f;
            stats.loudnessFac *= 0.85f;
            stats.generalVisibilityBonus = -0.1f;
            stats.visualStealthInSneakMode = 0.6f;
        }

        public override void GetFoodMeter(out int maxFood, out int foodToSleep)
        {
            if (!isBurnoutCycle)
            {
                maxFood = 7;
                foodToSleep = 4;
            }
            else
            {
                maxFood = 7;
                foodToSleep = 7;
            }
        }

        static bool isBurnoutCycle = false;
        static int burnoutMessagesThisSession = 0;

        public override bool CanEatMeat(Player player, Creature crit)
        {
            if (!IsPlayerProgrammer(player)) return base.CanEatMeat(player, crit);

            if(!isBurnoutCycle)
            {
                return crit.dead && (crit.Template.type == CreatureTemplate.Type.Centipede
                    || crit.Template.type == CreatureTemplate.Type.Centiwing
                    || crit.Template.type == CreatureTemplate.Type.RedCentipede
                    || crit.Template.type == CreatureTemplate.Type.CicadaA
                    || crit.Template.type == CreatureTemplate.Type.CicadaB
                    || crit.Template.type == CreatureTemplate.Type.EggBug
                    || crit.Template.type == CreatureTemplate.Type.BigSpider
                    || crit.Template.type == CreatureTemplate.Type.SpitterSpider
                    || crit.Template.type == CreatureTemplate.Type.Snail
                    || crit.Template.type == CreatureTemplate.Type.BigNeedleWorm
                    || crit.Template.type == CreatureTemplate.Type.BrotherLongLegs
                    || crit.Template.type == CreatureTemplate.Type.DaddyLongLegs
                    || crit.Template.type == CreatureTemplate.Type.DropBug
                    || crit.Template.type == CreatureTemplate.Type.GarbageWorm
                    || crit.Template.type == CreatureTemplate.Type.TubeWorm);
            }

            return crit.dead && (crit.Template.IsVulture
                    || crit.Template.IsLizard
                    || crit.Template.type == CreatureTemplate.Type.BigEel
                    || crit.Template.type == CreatureTemplate.Type.Deer
                    || crit.Template.type == CreatureTemplate.Type.JetFish
                    || crit.Template.type == CreatureTemplate.Type.LanternMouse
                    || crit.Template.type == CreatureTemplate.Type.MirosBird
                    || crit.Template.type == CreatureTemplate.Type.Scavenger
                    || crit.Template.type == CreatureTemplate.Type.Slugcat
                    || crit.Template.type == CreatureTemplate.Type.TempleGuard);

        }

        protected bool IsThisABurnoutCycle(SaveState saveState)
        {
            // Some funny waveform with cycles between 6 to 9 ingame cycles
            // tuned by hand I couldnt bother doing the maths
            int oldseed = UnityEngine.Random.seed;
            UnityEngine.Random.seed = saveState.seed + 133769;
            float x = saveState.cycleNumber;
            float startingOffset = -3.14f / 2f;
            float scalefac = -3.14f / 2f + 3.14f * UnityEngine.Random.value;
            float spreadfac = 0.4f + 0.3f * UnityEngine.Random.value;
            float wavea = 0.7f + 0.2f * UnityEngine.Random.value;
            float waveb = 0.2f + 0.1f * UnityEngine.Random.value;

            float res = Mathf.Sin(startingOffset + wavea * x + spreadfac * Mathf.Sin(waveb * x + scalefac));
            x--;
            float pre = Mathf.Sin(startingOffset + wavea * x + spreadfac * Mathf.Sin(waveb * x + scalefac));
            x--;
            float prepre = Mathf.Sin(startingOffset + wavea * x + spreadfac * Mathf.Sin(waveb * x + scalefac));

            //x = saveState.cycleNumber;
            //for (int i = 0; i < 30; i++)
            //{
            //    Debug.Log(Mathf.Sin(startingOffset + wavea * x + spreadfac * Mathf.Sin(waveb * x + scalefac)));
            //    x++;
            //}

            UnityEngine.Random.seed = oldseed;
            return (res > 0.85f && pre < 0.85f && prepre < 0.85f);
        }

        protected override void Prepare()
        {
            Debug.Log("PC - Prepare");
            //Debug.Log("PC - Jolly playerCharacters: " + String.Join(", ", new List<int>(JollyCoop.JollyMod.config.playerCharacters)
            // .ConvertAll(i => i.ToString())
            // .ToArray()));
            On.RainWorldGame.ctor += RainWorldGame_ctor_hk;
            On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitiateSaveState_hk;
        }

        bool atlasesLoaded = false;
        List<Hook> myHooks = new List<Hook>();
        protected override void Enable() {
            Debug.Log("PC - Enable");
            if (!atlasesLoaded)
            {
                CustomAtlasLoader.LoadCustomAtlas("programmerLegs.png", Assembly.GetExecutingAssembly().GetManifestResourceStream("ProgrammerCat.Resources.programmerLegs.png"), Assembly.GetExecutingAssembly().GetManifestResourceStream("ProgrammerCat.Resources.programmerLegs.txt"));
                CustomAtlasLoader.LoadCustomAtlas("programmerBlush.png", Assembly.GetExecutingAssembly().GetManifestResourceStream("ProgrammerCat.Resources.programmerBlush.png"), Assembly.GetExecutingAssembly().GetManifestResourceStream("ProgrammerCat.Resources.programmerBlush.txt"));
                atlasesLoaded = true;
            }

            On.RainWorldGame.Win += RainWorldGame_Win_hk;

            On.Player.Update += Player_Update_hk;
            On.Player.LungUpdate += Player_LungUpdate_hk;
            On.Player.AerobicIncrease += Player_AerobicIncrease_hk;
            On.Player.ObjectEaten += Player_ObjectEaten_hk;
            On.Player.FoodInRoom += Player_FoodInRoom_fx;
            On.Player.FoodInRoom_1 += Player_FoodInRoom_1_fx;

            MethodInfo allergicFilterMehtod = typeof(ProgrammerCatSlugcat).GetMethod("AllergiesFilter");
            myHooks.Add(new Hook(typeof(Hazer).GetMethod("BitByPlayer"), allergicFilterMehtod));
            myHooks.Add(new Hook(typeof(JellyFish).GetMethod("BitByPlayer"), allergicFilterMehtod));

            MethodInfo bugEatingFilterMehtod = typeof(ProgrammerCatSlugcat).GetMethod("BugEatingFilter");
            myHooks.Add(new Hook(typeof(Centipede).GetMethod("get_Edible"), bugEatingFilterMehtod));
            myHooks.Add(new Hook(typeof(SmallNeedleWorm).GetMethod("get_Edible"), bugEatingFilterMehtod));
            myHooks.Add(new Hook(typeof(Fly).GetMethod("get_Edible"), bugEatingFilterMehtod));
            myHooks.Add(new Hook(typeof(VultureGrub).GetMethod("get_Edible"), bugEatingFilterMehtod));

            On.PlayerSessionRecord.AddEat += PlayerSessionRecord_AddEat_hk;

            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites_hk;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer_hk;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        protected override void Disable()
        {
            Debug.Log("PC - Disable");
            //Futile.atlasManager.UnloadAtlas("programmerLegs.png");
            //Futile.atlasManager.UnloadAtlas("programmerBlush.png");

            On.RainWorldGame.ctor -= RainWorldGame_ctor_hk;
            On.PlayerProgression.GetOrInitiateSaveState -= PlayerProgression_GetOrInitiateSaveState_hk;

            On.RainWorldGame.Win -= RainWorldGame_Win_hk;

            On.Player.Update -= Player_Update_hk;
            On.Player.LungUpdate -= Player_LungUpdate_hk;
            On.Player.AerobicIncrease -= Player_AerobicIncrease_hk;
            On.Player.ObjectEaten -= Player_ObjectEaten_hk;
            On.Player.FoodInRoom -= Player_FoodInRoom_fx;
            On.Player.FoodInRoom_1 -= Player_FoodInRoom_1_fx;

            foreach (Hook hook in myHooks)
            {
                hook.Undo();
                hook.Free();
            }
            myHooks.Clear();

            On.PlayerSessionRecord.AddEat -= PlayerSessionRecord_AddEat_hk;

            On.PlayerGraphics.InitiateSprites -= PlayerGraphics_InitiateSprites_hk;
            On.PlayerGraphics.AddToContainer -= PlayerGraphics_AddToContainer_hk;
            On.PlayerGraphics.ApplyPalette -= PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DrawSprites -= PlayerGraphics_DrawSprites;
        }


        // On game initialization, if starting room hard-set position to a valid spot
        // also spawn tutorial thinghies
        private void RainWorldGame_ctor_hk(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            isBurnoutCycle = false; // will be set to tru in loadsavestate
            orig(self, manager);

            for (int i = 0; i < self.Players.Count; i++)
            {
                if (self.world.GetAbstractRoom(self.Players[i].pos) != null)
                {
                    Room theRoom = self.Players[i].Room.realizedRoom;
                    if (self.world.GetAbstractRoom(self.Players[i].pos).name == StartRoom)
                    {
                        self.Players[i].pos.Tile = new RWCustom.IntVector2(24, 85);
                        theRoom.AddObject(new ProgrammerStart(theRoom));
                    }

                    if (isBurnoutCycle && burnoutMessagesThisSession < 2)
                    {
                        theRoom.AddObject(new ProgrammerBurnoutTutorial(theRoom));
                    }
                }
            }

            Debug.Log("PC - Game ctor done");
            //Debug.Log("PC - Jolly playerCharacters: " + String.Join(", ", new List<int>(JollyCoop.JollyMod.config.playerCharacters)
            //             .ConvertAll(i => i.ToString())
            // .ToArray()));
        }


        private SaveState PlayerProgression_GetOrInitiateSaveState_hk(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, int saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
        {
            try
            {
                return orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
            }
            finally
            {
                isBurnoutCycle = IsThisABurnoutCycle(self.currentSaveState);
                if (self.currentSaveState.cycleNumber <= 1) burnoutMessagesThisSession = 0;
            }
        }

        private void RainWorldGame_Win_hk(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            if (!malnourished && isBurnoutCycle) burnoutMessagesThisSession++;
            orig(self, malnourished);
        }


        class ProgrammerStart : UpdatableAndDeletable
        {
            private Player player => (room.game.Players.Count <= 0) ? null : (room.game.Players[0].realizedCreature as Player);

            public ProgrammerStart(Room room)
            {
                this.room = room;
            }

            public override void Update(bool eu)
            {
                if (player != null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        player.bodyChunks[i].HardSetPosition(room.MiddleOfTile(24, 85 + i));
                        player.bodyChunks[i].vel = Vector2.zero;
                    }
                }
                if (room.game.manager.blackDelay <= 0f && room.game.manager.fadeToBlack < 0.9f)
                {
                    this.Destroy();
                }

                base.Update(eu);
            }
        }

        internal class ProgrammerBurnoutTutorial : UpdatableAndDeletable
        {

            public ProgrammerBurnoutTutorial(Room room)
            {
                this.room = room;
                if (room.game.session.Players[0].pos.room != room.abstractRoom.index)
                {
                    this.Destroy();
                }
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (this.room.game.session.Players[0].realizedCreature != null && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.textPrompt != null && this.room.game.cameras[0].hud.textPrompt.messages.Count < 1)
                {
                    switch (this.message)
                    {
                        case 0:
                            this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("The Programmer is going through a cycle of burnout"), 120, 160, false, true);
                            this.message++;
                            break;
                        case 1:
                            this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("Bugs are off the menu, but you're open to new tastes"), 0, 160, true, false);
                            this.message++;
                            break;
                        case 2:
                            this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate("Get out there and go find something tasty to eat!"), 0, 120, true, false);
                            this.message++;
                            break;
                        default:
                            this.Destroy();
                            break;
                    }
                }
            }
            public int message;
        }

        private void Player_Update_hk(On.Player.orig_Update orig, Player self, bool eu)
        {
            programmerUpdateLock = IsPlayerProgrammer(self);
            orig(self, eu);
            programmerUpdateLock = false;
        }


        private int Player_FoodInRoom_1_fx(On.Player.orig_FoodInRoom_1 orig, Player self, Room checkRoom, bool eatAndDestroy)
        {
            if(IsPlayerProgrammer(self)) return self.FoodInStomach;
            return orig(self, checkRoom, eatAndDestroy);
        }

        private int Player_FoodInRoom_fx(On.Player.orig_FoodInRoom orig, Player self, bool eatAndDestroy)
        {
            if (IsPlayerProgrammer(self)) return self.FoodInStomach;
            return orig(self, eatAndDestroy);
        }

        public delegate bool IPlayerEdible_Edible(IPlayerEdible self);
        public static bool BugEatingFilter(IPlayerEdible_Edible orig, IPlayerEdible self)
        {
            if (programmerUpdateLock)
                if (isBurnoutCycle) return false;

            return orig(self);
        }

        public delegate void IPlayerEdible_BitByPlayer(IPlayerEdible self, Creature.Grasp grasp, bool eu);
        public static void AllergiesFilter(IPlayerEdible_BitByPlayer orig, IPlayerEdible self, Creature.Grasp grasp, bool eu)
        {
            if (programmerUpdateLock)
            {
                Player player = grasp.grabber as Player;
                player.AerobicIncrease(2f + 7f * Mathf.Lerp(0.9f, 0.3f, player.aerobicLevel));
                if (!isBurnoutCycle)
                {
                    Debug.Log("PC - allergic to seafood");
                    player.exhausted = true;
                    bool shouldStun = (player.aerobicLevel > UnityEngine.Random.value);
                    if (shouldStun)
                    {
                        int stunamount = UnityEngine.Random.Range(10, 69);
                        player.Stun(stunamount);
                        player.standing = false;
                        player.room.AddObject(new CreatureSpasmer(player, true, Mathf.FloorToInt(0.69f * player.stun)));
                        if (UnityEngine.Random.value < 0.6f) player.LoseAllGrasps();
                    }
                }
            }
            orig(self, grasp, eu);
        }

        // Nerf food value of some things the programmer can eat when they eat it
        // Bugs out if someone else takes a bite first, or  if the same food is eaten across several cycles
        // Can't help but change the full meatLeft, since its all intergers...
        private void PlayerSessionRecord_AddEat_hk(On.PlayerSessionRecord.orig_AddEat orig, PlayerSessionRecord self, PhysicalObject eatenObject)
        {
            bool isNew = true;
            if (programmerUpdateLock)
            {
                for (int i = self.eats.Count - 1; i >= 0; i--)
                {
                    if (self.eats[i].ID == eatenObject.abstractPhysicalObject.ID)
                    {
                        isNew = false;
                    }
                }
            }

            orig(self, eatenObject);

            if (programmerUpdateLock)
            {
                if (isNew & eatenObject is Creature)
                {
                    CreatureState state = (eatenObject as Creature).State;
                    AbstractCreature creature = (eatenObject as Creature).abstractCreature;

                    if (state.meatLeft == creature.creatureTemplate.meatPoints && (
                       creature.creatureTemplate.type == CreatureTemplate.Type.CicadaA
                    || creature.creatureTemplate.type == CreatureTemplate.Type.CicadaB
                    || creature.creatureTemplate.type == CreatureTemplate.Type.BigSpider
                    || creature.creatureTemplate.type == CreatureTemplate.Type.SpitterSpider
                    || creature.creatureTemplate.type == CreatureTemplate.Type.BigNeedleWorm
                    || creature.creatureTemplate.type == CreatureTemplate.Type.DropBug
                    ))
                    {
                        state.meatLeft = Mathf.FloorToInt(state.meatLeft * 0.67f);
                    }
                    else if (state.meatLeft == creature.creatureTemplate.meatPoints && (
                              creature.creatureTemplate.IsVulture
                           || creature.creatureTemplate.IsLizard
                           || creature.creatureTemplate.type == CreatureTemplate.Type.JetFish
                           || creature.creatureTemplate.type == CreatureTemplate.Type.Scavenger
                           ))
                    {
                        state.meatLeft = Mathf.FloorToInt(state.meatLeft * 0.75f);
                    }
                }
            }
        }

        private void Player_ObjectEaten_hk(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            if (IsPlayerProgrammer(self))
            {
                // completelly replaces original
                if (self.graphicsModule != null)
                {
                    (self.graphicsModule as PlayerGraphics).LookAtNothing();
                }

                if (!isBurnoutCycle)
                {
                    // But slugcat is allergic
                    if (edible is Hazer || edible is JellyFish)
                    {
                        Debug.Log("PC - death by allergy");
                        self.Die();
                    }
                    else if (edible is DangleFruit
                     || edible is EggBugEgg
                     || edible is JellyFish
                     || edible is SlimeMold
                     || edible is SwollenWaterNut)
                    {
                        for (int i = 0; i < edible.FoodPoints; i++)
                        {
                            self.AddQuarterFood();
                        }
                    }
                    else
                    {
                        self.AddFood(edible.FoodPoints);
                    }
                }
                else // Burnout mode
                {
                    // allergies ok I suppose
                    if (edible is DangleFruit
                      //|| edible is EggBugEgg // naaah egg op stays nerfed
                      || edible is JellyFish
                      || edible is SlimeMold
                      || edible is SwollenWaterNut)
                    {
                        self.AddFood(edible.FoodPoints);
                    }
                    else
                    {
                        for (int i = 0; i < edible.FoodPoints; i++)
                        {
                            self.AddQuarterFood();
                        }

                    }
                }
                if (self.spearOnBack != null)
                {
                    self.spearOnBack.interactionLocked = true;
                }
            }
            else
            {
                orig(self, edible);
            }
        }

        // TODO make compatible with Fancy
        private int LegSprite(PlayerGraphics pg)
        {
            return 4;
        }
        private int EyeSprite(PlayerGraphics pg)
        {
            return 9;
        }

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (!IsPlayerProgrammer(self.player)) return;

            int ls = LegSprite(self);
            sLeaser.sprites[ls].element = Futile.atlasManager.GetElementWithName("programmer" + sLeaser.sprites[ls].element.name);

            int firstExtra = PlayerFirstExtraSprite.Get(self);
            int es = EyeSprite(self);

            sLeaser.sprites[firstExtra].alpha = RWCustom.Custom.LerpMap(self.player.aerobicLevel, 0.3f, 0.9f, 0f, 1f, 2);
            sLeaser.sprites[firstExtra].SetPosition(sLeaser.sprites[es].GetPosition());
            sLeaser.sprites[firstExtra].rotation = sLeaser.sprites[es].rotation;
            sLeaser.sprites[firstExtra].scaleX = sLeaser.sprites[es].scaleX;
            sLeaser.sprites[firstExtra].scaleY = sLeaser.sprites[es].scaleY;
            string elname = sLeaser.sprites[es].element.name;
            if (elname.EndsWith("d"))
            {
                sLeaser.sprites[firstExtra].element = Futile.atlasManager.GetElementWithName("programmerBlush0");
            }
            else
            {
                sLeaser.sprites[firstExtra].element = Futile.atlasManager.GetElementWithName("programmerBlush" + elname[elname.Length-1]);
            }
        }

        private void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (!IsPlayerProgrammer(self.player)) return;
            int firstExtra = PlayerFirstExtraSprite.Get(self);
            sLeaser.sprites[firstExtra].color = new Color(0.96f, 0.69f * 0.69f, 0.69f * 0.69f);
        }

        private void PlayerGraphics_AddToContainer_hk(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (!IsPlayerProgrammer(self.player)) return;
            if (initiateSpritesToContainerLock) return;

            PlayerGraphics_AddToContainer_impl(self, sLeaser, rCam, newContatiner);
        }

        private void PlayerGraphics_AddToContainer_impl(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                newContatiner = rCam.ReturnFContainer("Midground");
            }
            int firstExtra = PlayerFirstExtraSprite.Get(self);
            for (int i = firstExtra; i < firstExtra + 1; i++) // dunno maybe one day well have more lol
            {
                newContatiner.AddChild(sLeaser.sprites[i]);
            }
            sLeaser.sprites[firstExtra].MoveBehindOtherNode(sLeaser.sprites[EyeSprite(self)]); // blush goes behind eyes
        }

        AttachedField<PlayerGraphics, int> PlayerFirstExtraSprite = new AttachedField<PlayerGraphics, int>();
        static bool initiateSpritesToContainerLock = false;
        private void PlayerGraphics_InitiateSprites_hk(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            initiateSpritesToContainerLock = true;
            orig(self, sLeaser, rCam);
            initiateSpritesToContainerLock = false;

            if (!IsPlayerProgrammer(self.player)) return;
            int firstExtra = sLeaser.sprites.Length;
            this.PlayerFirstExtraSprite.Set(self, firstExtra);
            System.Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);

            sLeaser.sprites[firstExtra] = new FSprite("programmerBlush0", true);
            sLeaser.sprites[firstExtra].color = new Color(0.96f, 0.69f, 0.69f);

            PlayerGraphics_AddToContainer_impl(self, sLeaser, rCam, null);
        }

        private void Player_AerobicIncrease_hk(On.Player.orig_AerobicIncrease orig, Player self, float f)
        {
            if (IsPlayerProgrammer(self) && !self.slugcatStats.malnourished)
            {
                f *= RWCustom.Custom.LerpMap(self.aerobicLevel, 0.2f, 0.8f, 1.234f, 0.69f);
            }
            orig(self, f);
            //Debug.Log("PC - aero inc now " + self.aerobicLevel);
        }

        AttachedField<Player, bool> playerFocedExhausted = new AttachedField<Player, bool>();
        private void Player_LungUpdate_hk(On.Player.orig_LungUpdate orig, Player self)
        {
            // This is the earliest hook point after the vanilla exhaust calculation
            // an alternative would be to manipulate "malnourished" on player.update but it would require some significant fixes
            //if (UnityEngine.Random.value < 0.05) Debug.Log("PC - pre update aero is " + self.aerobicLevel);

            if (IsPlayerProgrammer(self) && !self.slugcatStats.malnourished) // Behavior on normal cycles similar to starvation, but more tame
            {
                if (self.aerobicLevel > 0.99f) // == 1f) but there was a decay cycle in main code
                {
                    playerFocedExhausted.Set(self, true);
                }
                else if (self.aerobicLevel < 0.7f)
                {
                    playerFocedExhausted.Set(self, false);
                }

                // Idiom A - tryget, apply default if missing
                bool exhausted;
                if (!playerFocedExhausted.TryGet(self, out exhausted)) exhausted = false;
                self.exhausted = exhausted;
                // Idiom B - get with default/guard
                //self.exhausted = playerFocedExhausted.Get(self, false); ;

                if (self.exhausted)
                {
                    self.slowMovementStun = Math.Max(self.slowMovementStun, (int)RWCustom.Custom.LerpMap(self.aerobicLevel, 0.9f, 0.6f, 6f, 0f));
                    // lowered chances of stuff
                    if (self.aerobicLevel > 0.9f && UnityEngine.Random.value < 0.002f)
                    {
                        //Debug.Log("PC - Stun");
                        self.Stun(7);
                    }
                    if (self.aerobicLevel > 0.9f && UnityEngine.Random.value < 0.0069f)
                    {
                        //Debug.Log("PC - Fall");
                        self.standing = false;
                    }
                    if (!self.lungsExhausted || self.animation == Player.AnimationIndex.SurfaceSwim)
                    {
                        self.swimCycle += 0.05f;
                    }
                }
                else
                {
                    // slightly adjusted slowdown
                    self.slowMovementStun = Math.Max(self.slowMovementStun, (int)RWCustom.Custom.LerpMap(self.aerobicLevel, 1f, 0.7f, 2f, 0f, 2f));
                    
                    if(self.aerobicLevel > 0.6f)
                    {
                        // decay slightly faster
                        if (!self.lungsExhausted)
                        {
                            self.aerobicLevel = Mathf.Max(1f - self.airInLungs,
                                self.aerobicLevel - 1f / (((self.input[0].x != 0 || self.input[0].y != 0) ? 600f : 200f) * (1f + (exhausted? 3f : 1f) * Mathf.InverseLerp(0.6f, 1f, self.aerobicLevel))));
                        }
                    }
                }
            }

            orig(self);
        }

        static Dictionary<string, string> iAmDoingMyBestToShipAModWithoutStolenGameAssets = new Dictionary<string, string>
        {
            { @"Scenes\Outro_3_Face\5 - CloudsB.png", @"Scenes\Outro 3 - Face\5 - CloudsB.png" },
            { @"Scenes\Outro_3_Face\4 - CloudsA.png", @"Scenes\Outro 3 - Face\4 - CloudsA.png" },
            { @"Scenes\Outro_3_Face\3 - BloomLights.png", @"Scenes\Outro 3 - Face\3 - BloomLights.png" },
            { @"Scenes\Outro_3_Face\2 - FaceCloseUp.png", @"Scenes\Outro 3 - Face\2 - FaceCloseUp.png" },
            { @"Scenes\Outro_3_Face\1 - FaceBloom.png", @"Scenes\Outro 3 - Face\1 - FaceBloom.png" },
            { @"Scenes\SleepScreen\Sleep - 5.png", @"Scenes\Sleep Screen - White\Sleep - 5.png" },
        };
        static char[] embedRepl = new char[] { ' ', '\u00A0', '.', ',', ';', '|', '~', '@', '#', '%', '^', '&', '*', '+', '-', '/', '\\', '<', '>', '?', '[', ']', '(', ')', '{', '}', '"', '\'', ':', '!' };
        public override Stream GetResource(params string[] path)
        {
            string pathstr = String.Join(Path.DirectorySeparatorChar.ToString(), path);
            //Debug.Log("PC - Requested resource: " + pathstr);

            if (iAmDoingMyBestToShipAModWithoutStolenGameAssets.ContainsKey(pathstr))
            {
                //Debug.Log("PC - Serving from game assets");
                return new MemoryStream(new WWW(string.Concat(new object[]
                {
                        "file:///",
                        RWCustom.Custom.RootFolderDirectory(),
                        "Assets",
                        Path.DirectorySeparatorChar,
                        "Futile",
                        Path.DirectorySeparatorChar,
                        "Resources",
                        Path.DirectorySeparatorChar,
                        iAmDoingMyBestToShipAModWithoutStolenGameAssets[pathstr]
                })).bytes);
            }

            string[] patchedPath = new string[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                patchedPath[i] = path[i].Replace(" ", "_");
                if(i < path.Length - 1)
                {
                    patchedPath[i] = path[i];
                    foreach (char rep in embedRepl)
                    {
                        patchedPath[i] = patchedPath[i].Replace(rep, '_');
                    }
                }
                else
                {
                    // ????????????
                    // any other crazy replacements to do ?
                }
            }
            string patchedpathstr = String.Join(".", patchedPath);
            Stream tryGet = Assembly.GetExecutingAssembly().GetManifestResourceStream("ProgrammerCat.Resources." + patchedpathstr);
            if (tryGet != null) return tryGet;
            return base.GetResource(path);
        }
    }
}
