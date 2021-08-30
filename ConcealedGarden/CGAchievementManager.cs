using System;
using System.Reflection;

namespace ConcealedGarden
{
    internal static class FuckingHellIJustWantedToMakeSomeExtensionsManWhyDoYouHaveToMakeThingsSoComplicated
    {
        internal static CGAchievementManager GetCGAchievementManager(this RainWorld rw)
        {
            foreach (MainLoopProcess mlp in rw.processManager.sideProcesses)
            {
                if (mlp is CGAchievementManager cgam) return cgam;
            }
            return null;
        }
    }

    internal class CGAchievementManager : MainLoopProcess
    {
        internal static void Apply()
        {
            On.ProcessManager.ctor += ProcessManager_ctor;
            On.RainWorld.AchievementAlreadyDisplayed += RainWorld_AchievementAlreadyDisplayed;
            On.RainWorld.PingAchievement += RainWorld_PingAchievement;
            On.GhostWorldPresence.PassageAchievementID += GhostWorldPresence_PassageAchievementID;

            On.RainWorld.LoadResources += RainWorld_LoadResources;
        }

        private static void RainWorld_LoadResources(On.RainWorld.orig_LoadResources orig, RainWorld self)
        {
            orig(self);

            CustomAtlasLoader.LoadCustomAtlas("Achievement_1_popup", Assembly.GetExecutingAssembly().GetManifestResourceStream("ConcealedGarden.Resources.Achievement_1_popup.png"));
            CustomAtlasLoader.LoadCustomAtlas("Achievement_2_popup", Assembly.GetExecutingAssembly().GetManifestResourceStream("ConcealedGarden.Resources.Achievement_2_popup.png"));
        }

        public static class EnumExt_CGAchievementManager
        {
#pragma warning disable 0649
            public static ProcessManager.ProcessID CGAchievementManager;
            public static RainWorld.AchievementID CGGhostCG;
            public static RainWorld.AchievementID CGTransfurred;
#pragma warning restore 0649
        }

        // Hooks n stuff
        private static void ProcessManager_ctor(On.ProcessManager.orig_ctor orig, ProcessManager self, RainWorld rainWorld)
        {
            orig(self, rainWorld);
            self.sideProcesses.Add(new CGAchievementManager(self));
        }

        private static RainWorld.AchievementID GhostWorldPresence_PassageAchievementID(On.GhostWorldPresence.orig_PassageAchievementID orig, GhostWorldPresence.GhostID ghostID)
        {
            RainWorld.AchievementID id = orig(ghostID);
            if (id == RainWorld.AchievementID.None && ghostID.ToString() == "CG") id = EnumExt_CGAchievementManager.CGGhostCG;
            return id;
        }

        private static void RainWorld_PingAchievement(On.RainWorld.orig_PingAchievement orig, RainWorld self, RainWorld.AchievementID ID)
        {
            CGAchievementManager cgam = self.GetCGAchievementManager();
            if (cgam != null && IsManagedAchievement(ID))
            {
                orig(self, RainWorld.AchievementID.None);
                cgam.PingAchievement(ID);
            }
            else orig(self, ID);
        }

        private static bool RainWorld_AchievementAlreadyDisplayed(On.RainWorld.orig_AchievementAlreadyDisplayed orig, RainWorld self, RainWorld.AchievementID ID)
        {
            CGAchievementManager cgam = self.GetCGAchievementManager();
            if(cgam != null && IsManagedAchievement(ID))
            {
                orig(self, RainWorld.AchievementID.None);
                return cgam.AchievementAlreadyDisplayed(ID);
            }
            return orig(self, ID);
        }

        private bool currentlyDisplaying;

        // Actual code
        private RainWorld.AchievementID currentlyDisplayingAchievement;
        private FSprite sprite;
        private float inAnimationTimer;

        public float goUpTimer => 0.8f;
        public float stayUpTimer => 5.2f;
        public float goDownTimer => 6f;

        public CGAchievementManager(ProcessManager manager) : base(manager, EnumExt_CGAchievementManager.CGAchievementManager)
        {
            this.sprite = new FSprite("Futile_White", true);
            Futile.stage.AddChild(this.sprite);
            this.sprite.isVisible = false;
        }

        private static bool IsManagedAchievement(RainWorld.AchievementID ID)
        {
            if (ID == RainWorld.AchievementID.None) return false;
            if (ID == EnumExt_CGAchievementManager.CGGhostCG) return true;
            if (ID == EnumExt_CGAchievementManager.CGTransfurred) return true;
            return false;
        }

        private static string GetAchievementPopup(RainWorld.AchievementID ID)
        {
            if (ID == EnumExt_CGAchievementManager.CGTransfurred) return "Achievement_1_popup";
            if (ID == EnumExt_CGAchievementManager.CGGhostCG) return "Achievement_2_popup";
            return null;
        }

        private bool AchievementAlreadyDisplayed(RainWorld.AchievementID ID)
        {
            if (ID == this.currentlyDisplayingAchievement) return true;
            if (ID == EnumExt_CGAchievementManager.CGGhostCG) return ConcealedGarden.progression.achievementEcho;
            if (ID == EnumExt_CGAchievementManager.CGTransfurred) return ConcealedGarden.progression.achievementTransfurred;
            return false;
        }

        private void PingAchievement(RainWorld.AchievementID ID)
        {
            if (ID == this.currentlyDisplayingAchievement) return;
            if (ID == EnumExt_CGAchievementManager.CGGhostCG) ConcealedGarden.progression.achievementEcho = true;
            else if (ID == EnumExt_CGAchievementManager.CGTransfurred) ConcealedGarden.progression.achievementTransfurred = true;

            UnityEngine.Debug.Log("CG: Pinged achievement " + ID.ToString());
            this.currentlyDisplaying = true;
            this.inAnimationTimer = 0;
            this.currentlyDisplayingAchievement = ID;
            this.sprite.SetElementByName(GetAchievementPopup(ID));
            this.sprite.x = this.manager.rainWorld.options.ScreenSize.x - 120f;
            this.sprite.y = -48f;
            this.sprite.isVisible = true;
        }

        public override void RawUpdate(float dt)
        {
            base.RawUpdate(dt);
            if (currentlyDisplaying)
            {
                this.inAnimationTimer += dt;
                this.sprite.MoveToFront();
                if (inAnimationTimer < this.goUpTimer)
                {
                    this.sprite.y = -47f + 94f * UnityEngine.Mathf.InverseLerp(0, goUpTimer, inAnimationTimer);
                }
                else if (inAnimationTimer < this.stayUpTimer)
                {
                    this.sprite.y = 47f;
                }
                else if (inAnimationTimer < this.goDownTimer)
                {
                    this.sprite.y = 47f - 94f * UnityEngine.Mathf.InverseLerp(stayUpTimer, goDownTimer, inAnimationTimer);
                }
                else
                {
                    this.sprite.isVisible = false;
                    this.currentlyDisplaying = false;
                    UnityEngine.Debug.Log("CG: Done displaying achievement " + currentlyDisplayingAchievement.ToString());
                    this.currentlyDisplayingAchievement = RainWorld.AchievementID.None;
                }
            }
        }
    }
}