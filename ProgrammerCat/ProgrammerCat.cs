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

[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}

/*
 * This example interacts with SlugBase as little as possible.
 * 
 * The player select menu and sleep screen will display Survivor.
 * This makes the select screen ambiguous once a game is started, since the name is hidden.
 * Consider copying one of the slugcat select scenes and editing it.
 */


namespace ProgrammerCat
{
    public class ProgrammerCat : Partiality.Modloader.PartialityMod
    {
        public ProgrammerCat()
        {
            ModID = "ProgrammerCat";
            Version = "1.0";
            author = "Henpemaz";
        }

        public override void OnLoad()
        {
            PlayerManager.RegisterCharacter(new ProgrammerCatSlugcat());
        }
    }

    public class ProgrammerCatSlugcat : SlugBaseCharacter
    {
        public ProgrammerCatSlugcat() : base("ProgrammerCat", FormatVersion.V1, 0){}

        public override string DisplayName => "The Programmer";
        public override string Description =>
@"On its adorable programmer socks, this nimble and unprepared slugcat
has gotten pretty far from home and now has to face the real world!
Temperamental and on a weird diet, your journey will be a mess.";

        public override Color? SlugcatColor()
        {
            if (isBurnoutCycle) return new Color(1f, (0.69f + 0.96f) / 2f, 0.96f);
            return new Color(1f, 0.96f, 0.96f);
        }

        public override bool HasGuideOverseer => true;

        public override string StartRoom => "SI_B12";

        protected override void GetStats(SlugcatStats stats)
        {
            stats.bodyWeightFac *= 0.85f;
            stats.runspeedFac *= 0.9f;
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
            On.RainWorldGame.ctor += RainWorldGame_ctor_hk;
            On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitiateSaveState_hk;
        }

        List<Hook> myHooks = new List<Hook>();
        protected override void Enable() {
            Debug.Log("PC - Enable");
            foreach (var name in spritesOverwrite)
            {
                FAtlasElement fae;
                Futile.atlasManager._allElementsByName.TryGetValue(name, out fae);
                backupSprites.Add(name, fae);
                Futile.atlasManager._allElementsByName.Remove(name);
            }
            LoadAtlasStreamIntoManager(Futile.atlasManager, "programmerLegs.png", Assembly.GetExecutingAssembly().GetManifestResourceStream("ProgrammerCat.Resources.programmerLegs.png"), Assembly.GetExecutingAssembly().GetManifestResourceStream("ProgrammerCat.Resources.programmerLegs.txt"));
            LoadAtlasStreamIntoManager(Futile.atlasManager, "programmerBlush.png", Assembly.GetExecutingAssembly().GetManifestResourceStream("ProgrammerCat.Resources.programmerBlush.png"), Assembly.GetExecutingAssembly().GetManifestResourceStream("ProgrammerCat.Resources.programmerBlush.txt"));

            On.RainWorldGame.Win += RainWorldGame_Win_hk;

            On.Player.LungUpdate += Player_LungUpdate_hk;
            On.Player.AerobicIncrease += Player_AerobicIncrease_hk;
            On.Player.ObjectEaten += Player_ObjectEaten_hk;
            On.Player.FoodInRoom += Player_FoodInRoom_fx;
            On.Player.FoodInRoom_1 += Player_FoodInRoom_1_fx;

            On.Hazer.BitByPlayer += Hazer_BitByPlayer_hk;
            On.JellyFish.BitByPlayer += JellyFish_BitByPlayer;

            MethodInfo bugEatingFilterMehtod = typeof(ProgrammerCatSlugcat).GetMethod("BugEatingFilter");
            myHooks.Add(new Hook(typeof(Centipede).GetMethod("get_Edible"), bugEatingFilterMehtod));
            myHooks.Add(new Hook(typeof(SmallNeedleWorm).GetMethod("get_Edible"), bugEatingFilterMehtod));
            myHooks.Add(new Hook(typeof(Fly).GetMethod("get_Edible"), bugEatingFilterMehtod));
            myHooks.Add(new Hook(typeof(VultureGrub).GetMethod("get_Edible"), bugEatingFilterMehtod));

            On.CreatureState.ctor += CreatureState_ctor_hk;

            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites_hk;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer_hk;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
        }

        protected override void Disable()
        {
            Debug.Log("PC - Disable");
            Futile.atlasManager.UnloadAtlas("programmerLegs.png");
            foreach (var name in spritesOverwrite)
            {
                FAtlasElement fae;
                backupSprites.TryGetValue(name, out fae);
                Futile.atlasManager._allElementsByName.Add(name, fae);
            }
            backupSprites.Clear();
            Futile.atlasManager.UnloadAtlas("programmerBlush.png");

            On.RainWorldGame.ctor -= RainWorldGame_ctor_hk;
            On.PlayerProgression.GetOrInitiateSaveState -= PlayerProgression_GetOrInitiateSaveState_hk;

            On.RainWorldGame.Win -= RainWorldGame_Win_hk;

            On.Player.LungUpdate -= Player_LungUpdate_hk;
            On.Player.AerobicIncrease -= Player_AerobicIncrease_hk;
            On.Player.ObjectEaten -= Player_ObjectEaten_hk;
            On.Player.FoodInRoom -= Player_FoodInRoom_fx;
            On.Player.FoodInRoom_1 -= Player_FoodInRoom_1_fx;

            On.Hazer.BitByPlayer -= Hazer_BitByPlayer_hk;
            On.JellyFish.BitByPlayer -= JellyFish_BitByPlayer;

            foreach (Hook hook in myHooks)
            {
                hook.Undo();
                hook.Free();
            }
            myHooks.Clear();

            On.CreatureState.ctor -= CreatureState_ctor_hk;

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
                    if (self.world.GetAbstractRoom(self.Players[i].pos).name == "SI_B12")
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

        private int Player_FoodInRoom_1_fx(On.Player.orig_FoodInRoom_1 orig, Player self, Room checkRoom, bool eatAndDestroy)
        {
            return self.FoodInStomach;
        }

        private int Player_FoodInRoom_fx(On.Player.orig_FoodInRoom orig, Player self, bool eatAndDestroy)
        {
            return self.FoodInStomach;
        }

        public delegate bool IPlayerEdible_Edible(IPlayerEdible self);
        public static bool BugEatingFilter(IPlayerEdible_Edible orig, IPlayerEdible self)
        {
            if (isBurnoutCycle) return false;
            //return false;
            return orig(self);
        }

        private void JellyFish_BitByPlayer(On.JellyFish.orig_BitByPlayer orig, JellyFish self, Creature.Grasp grasp, bool eu)
        {
            Player player = grasp.grabber as Player;
            ProcAllergies(player);

            orig(self, grasp, eu);
        }

        private void Hazer_BitByPlayer_hk(On.Hazer.orig_BitByPlayer orig, Hazer self, Creature.Grasp grasp, bool eu)
        {
            Player player = grasp.grabber as Player;
            ProcAllergies(player);

            orig(self, grasp, eu);
        }

        private void ProcAllergies(Player player)
        {
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


        // Nerf meat value of some easy creatures
        private void CreatureState_ctor_hk(On.CreatureState.orig_ctor orig, CreatureState self, AbstractCreature creature)
        {
            orig(self, creature);
            if (self.meatLeft == creature.creatureTemplate.meatPoints && (
                       creature.creatureTemplate.type == CreatureTemplate.Type.CicadaA
                    || creature.creatureTemplate.type == CreatureTemplate.Type.CicadaB
                    || creature.creatureTemplate.type == CreatureTemplate.Type.BigSpider
                    || creature.creatureTemplate.type == CreatureTemplate.Type.SpitterSpider
                    || creature.creatureTemplate.type == CreatureTemplate.Type.BigNeedleWorm
                    || creature.creatureTemplate.type == CreatureTemplate.Type.DropBug
                    ))
            {
                self.meatLeft = Mathf.FloorToInt(self.meatLeft * 0.67f);
            }
            else if (self.meatLeft == creature.creatureTemplate.meatPoints && (
                      creature.creatureTemplate.IsVulture
                   || creature.creatureTemplate.IsLizard
                   || creature.creatureTemplate.type == CreatureTemplate.Type.JetFish
                   || creature.creatureTemplate.type == CreatureTemplate.Type.Scavenger
                   ))
            {
                self.meatLeft = Mathf.FloorToInt(self.meatLeft * 0.75f);
            }
        }


        private void Player_ObjectEaten_hk(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
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
                else if(edible is DangleFruit
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
                // Hazer ok I suppose
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

        private void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            int firstExtra = PlayerFirstExtraSprite.Get(self);
            sLeaser.sprites[firstExtra].alpha = RWCustom.Custom.LerpMap(self.player.aerobicLevel, 0.3f, 0.9f, 0f, 1f, 2);
            sLeaser.sprites[firstExtra].SetPosition(sLeaser.sprites[9].GetPosition());
            sLeaser.sprites[firstExtra].rotation = sLeaser.sprites[9].rotation;
            sLeaser.sprites[firstExtra].scaleX = sLeaser.sprites[9].scaleX;
            sLeaser.sprites[firstExtra].scaleY = sLeaser.sprites[9].scaleY;
            string elname = sLeaser.sprites[9].element.name;
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
            int firstExtra = PlayerFirstExtraSprite.Get(self);
            sLeaser.sprites[firstExtra].color = new Color(0.96f, 0.69f * 0.69f, 0.69f * 0.69f);
        }

        private void PlayerGraphics_AddToContainer_hk(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
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
            sLeaser.sprites[firstExtra].MoveBehindOtherNode(sLeaser.sprites[9]); // blush goes behind eyes
        }

        AttachedField<PlayerGraphics, int> PlayerFirstExtraSprite = new AttachedField<PlayerGraphics, int>();
        static bool initiateSpritesToContainerLock = false;
        private void PlayerGraphics_InitiateSprites_hk(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            initiateSpritesToContainerLock = true;
            orig(self, sLeaser, rCam);
            initiateSpritesToContainerLock = false;

            int firstExtra = sLeaser.sprites.Length;
            this.PlayerFirstExtraSprite.Set(self, firstExtra);
            System.Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);

            sLeaser.sprites[firstExtra] = new FSprite("programmerBlush0", true);
            sLeaser.sprites[firstExtra].color = new Color(0.96f, 0.69f, 0.69f);

            PlayerGraphics_AddToContainer_impl(self, sLeaser, rCam, null);
        }

        private void Player_AerobicIncrease_hk(On.Player.orig_AerobicIncrease orig, Player self, float f)
        {
            if (!self.slugcatStats.malnourished)
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

            if (!self.slugcatStats.malnourished) // Behavior on normal cycles similar to starvation, but more tame
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
            //Debug.Log("PC - Failed to get embedded file for ProgrammerCat.Resources." + patchedpathstr);

            return base.GetResource(path);
        }


        string[] spritesOverwrite = new string[] { "LegsA0", "LegsA1", "LegsA2", "LegsA3", "LegsA4", "LegsA5", "LegsA6", "LegsAAir0", "LegsAAir1", "LegsAClimbing0", "LegsAClimbing1", "LegsAClimbing2", "LegsAClimbing3", "LegsAClimbing4", "LegsAClimbing5", "LegsAClimbing6", "LegsACrawling0", "LegsACrawling1", "LegsACrawling2", "LegsACrawling3", "LegsACrawling4", "LegsACrawling5", "LegsAOnPole0", "LegsAOnPole1", "LegsAOnPole2", "LegsAOnPole3", "LegsAOnPole4", "LegsAOnPole5", "LegsAOnPole6", "LegsAPole", "LegsAVerticalPole", "LegsAWall" };
        private Dictionary<string, FAtlasElement> backupSprites = new Dictionary<string, FAtlasElement>();

        void LoadAtlasStreamIntoManager(FAtlasManager atlasManager, string atlasName, System.IO.Stream textureStream, System.IO.Stream jsonStream)
        {
            try
            {
                // load texture
                Texture2D texture2D = new Texture2D(0, 0, TextureFormat.ARGB32, false);
                byte[] bytes = new byte[textureStream.Length];
                textureStream.Read(bytes, 0, (int)textureStream.Length);
                texture2D.LoadImage(bytes);
                // from rainWorld.png.meta unity magic
                texture2D.anisoLevel = 1;
                texture2D.filterMode = 0;

                // make fake singleimage atlas
                FAtlas fatlas = new FAtlas(atlasName, texture2D, FAtlasManager._nextAtlasIndex++);
                fatlas._elements.Clear();
                fatlas._elementsByName.Clear();
                fatlas._isSingleImage = false;

                // actually load the atlas
                StreamReader sr = new StreamReader(jsonStream, Encoding.UTF8);
                Dictionary<string, object> dictionary = sr.ReadToEnd().dictionaryFromJson();

                //ctrl c
                //ctrl v

                Dictionary<string, object> dictionary2 = (Dictionary<string, object>)dictionary["frames"];
                float resourceScaleInverse = Futile.resourceScaleInverse;
                int num = 0;
                foreach (KeyValuePair<string, object> keyValuePair in dictionary2)
                {
                    FAtlasElement fatlasElement = new FAtlasElement();
                    fatlasElement.indexInAtlas = num++;
                    string text = keyValuePair.Key;
                    if (Futile.shouldRemoveAtlasElementFileExtensions)
                    {
                        int num2 = text.LastIndexOf(".");
                        if (num2 >= 0)
                        {
                            text = text.Substring(0, num2);
                        }
                    }
                    fatlasElement.name = text;
                    IDictionary dictionary3 = (IDictionary)keyValuePair.Value;
                    fatlasElement.isTrimmed = (bool)dictionary3["trimmed"];
                    if ((bool)dictionary3["rotated"])
                    {
                        throw new NotSupportedException("Futile no longer supports TexturePacker's \"rotated\" flag. Please disable it when creating the " + fatlas._dataPath + " atlas.");
                    }
                    IDictionary dictionary4 = (IDictionary)dictionary3["frame"];
                    float num3 = float.Parse(dictionary4["x"].ToString());
                    float num4 = float.Parse(dictionary4["y"].ToString());
                    float num5 = float.Parse(dictionary4["w"].ToString());
                    float num6 = float.Parse(dictionary4["h"].ToString());
                    Rect uvRect = new Rect(num3 / fatlas._textureSize.x, (fatlas._textureSize.y - num4 - num6) / fatlas._textureSize.y, num5 / fatlas._textureSize.x, num6 / fatlas._textureSize.y);
                    fatlasElement.uvRect = uvRect;
                    fatlasElement.uvTopLeft.Set(uvRect.xMin, uvRect.yMax);
                    fatlasElement.uvTopRight.Set(uvRect.xMax, uvRect.yMax);
                    fatlasElement.uvBottomRight.Set(uvRect.xMax, uvRect.yMin);
                    fatlasElement.uvBottomLeft.Set(uvRect.xMin, uvRect.yMin);
                    IDictionary dictionary5 = (IDictionary)dictionary3["sourceSize"];
                    fatlasElement.sourcePixelSize.x = float.Parse(dictionary5["w"].ToString());
                    fatlasElement.sourcePixelSize.y = float.Parse(dictionary5["h"].ToString());
                    fatlasElement.sourceSize.x = fatlasElement.sourcePixelSize.x * resourceScaleInverse;
                    fatlasElement.sourceSize.y = fatlasElement.sourcePixelSize.y * resourceScaleInverse;
                    IDictionary dictionary6 = (IDictionary)dictionary3["spriteSourceSize"];
                    float left = float.Parse(dictionary6["x"].ToString()) * resourceScaleInverse;
                    float top = float.Parse(dictionary6["y"].ToString()) * resourceScaleInverse;
                    float width = float.Parse(dictionary6["w"].ToString()) * resourceScaleInverse;
                    float height = float.Parse(dictionary6["h"].ToString()) * resourceScaleInverse;
                    fatlasElement.sourceRect = new Rect(left, top, width, height);
                    fatlas._elements.Add(fatlasElement);
                    fatlas._elementsByName.Add(fatlasElement.name, fatlasElement);
                }
                //pray
                atlasManager.AddAtlas(fatlas);

            }
            finally
            {
                textureStream.Close();
                jsonStream.Close();
            }

            
        }


    }
}
