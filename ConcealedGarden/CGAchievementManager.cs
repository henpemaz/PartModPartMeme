using Menu;
using OptionalUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

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

            CustomAtlasLoader.LoadCustomAtlas("CGAchievement_1", Assembly.GetExecutingAssembly().GetManifestResourceStream("ConcealedGarden.Resources.Achievement_1.png"));
            CustomAtlasLoader.LoadCustomAtlas("CGAchievement_1_popup", Assembly.GetExecutingAssembly().GetManifestResourceStream("ConcealedGarden.Resources.Achievement_1_popup.png"));
            CustomAtlasLoader.LoadCustomAtlas("CGAchievement_2", Assembly.GetExecutingAssembly().GetManifestResourceStream("ConcealedGarden.Resources.Achievement_2.png"));
            CustomAtlasLoader.LoadCustomAtlas("CGAchievement_2_popup", Assembly.GetExecutingAssembly().GetManifestResourceStream("ConcealedGarden.Resources.Achievement_2_popup.png"));
        }

        public static void MakeAchievementsOi(OptionInterface Oi, OpTab opTab)
        {
            opTab.AddItems(new OpLabel(40, 468, "Achievements", true));
            OpScrollBox scrollBox = new OpScrollBox(new Vector2(40f, 260f), new Vector2(520, 200), 300f);
            opTab.AddItems(scrollBox);
            
            List<AchievementEntry> achievements = new List<AchievementEntry>();
            float placement = 300f;
            if (ConcealedGarden.progression.achievementTransfurred)
            {
                placement -= 76f;
                achievements.Add(new AchievementEntry(new Vector2(0f, placement), "CGAchievement_1", "Plastified", "You grow closer to the wilderness around you"));
            }
            if (ConcealedGarden.progression.achievementEcho)
            {
                placement -= 76f;
                achievements.Add(new AchievementEntry(new Vector2(0f, placement), "CGAchievement_2", "A Green Crown over Shades of Blue", "You encountered the echo of A Green Crown over Shades of Blue"));
            }
            if(achievements.Count < 2)
            {
                placement -= 76f;
                achievements.Add(new MissingAchievementsEntry(new Vector2(0f, placement), 2 - achievements.Count));
            }

            scrollBox.AddItems(achievements.ToArray());
        }

        private class AchievementEntry : UIelement
        {
            protected FSprite spr;
            protected FSprite bg;
            protected FLabel titlelbl;
            protected FLabel descrlbl;

            public AchievementEntry(Vector2 pos, string spriteName, string title, string description) : base(pos, Vector2.zero)
            {
                this.spr = new FSprite(spriteName, true)
                {
                    anchorX = 0f,
                    anchorY = 0f,
                    x = 6f,
                    y = 6f,
                };
                this.bg = new FSprite("Futile_White", true)
                {
                    color = new Color(0.08f, 0.08f, 0.25f),
                    alpha = 0.75f,
                    width = 417f,
                    height = 64f,
                    anchorX = 0f,
                    anchorY = 0f,
                    x = 76f,
                    y = 6f,
                };
                this.myContainer.AddChild(this.spr);
                this.myContainer.AddChild(this.bg);
                this.titlelbl = new FLabel("font", title);
                this.titlelbl.SetPosition(titleOffset);
                this.titlelbl.alignment = FLabelAlignment.Left;
                this.descrlbl = new FLabel("font", description);
                this.descrlbl.SetPosition(descOffset);
                this.descrlbl.alpha = 0.6f;
                this.descrlbl.alignment = FLabelAlignment.Left;
                this.myContainer.AddChild(this.titlelbl);
                this.myContainer.AddChild(this.descrlbl);
                OnChange();
            }

            public Vector2 titleOffset => new Vector2(86f, 58f);

            public Vector2 descOffset => new Vector2(86f, 44f);

            public override void Hide()
            {
                base.Hide();
                this.spr.isVisible = false;
                this.bg.isVisible = false;
                this.titlelbl.isVisible = false;
                this.descrlbl.isVisible = false;
            }
            public override void Show()
            {
                base.Show();
                this.spr.isVisible = true;
                this.bg.isVisible = true;
                this.titlelbl.isVisible = true;
                this.descrlbl.isVisible = true;
            }
            public override void Unload()
            {
                base.Unload();
                this.spr.RemoveFromContainer();
                this.bg.RemoveFromContainer();
                this.titlelbl.RemoveFromContainer();
                this.descrlbl.RemoveFromContainer();
            }
        }

        private class MissingAchievementsEntry : AchievementEntry
        {
            private FLabel plusText;

            public MissingAchievementsEntry(Vector2 pos, int count) : base(pos, "Futile_White", count.ToString() + " hidden achievement" + (count > 1 ? "s" : string.Empty) + " remaining", "Details for each achievement will be revealed once unlocked")
            {
                this.spr.color = new Color(0.1f, 0.1f, 0.3f);
                this.spr.alpha = 0.6f;
                this.spr.scale = 4;
                this.plusText = new FLabel("DisplayFont", "+" + count);
                this.plusText.SetPosition(new Vector2(36, 40));
                this.myContainer.AddChild(this.plusText);
            }

            public override void Hide()
            {
                base.Hide();
                this.plusText.isVisible = false;
            }
            public override void Show()
            {
                base.Show();
                this.plusText.isVisible = true;
            }

            public override void Unload()
            {
                base.Unload();
                this.plusText.RemoveFromContainer();
            }
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
            if (ID == EnumExt_CGAchievementManager.CGTransfurred) return "CGAchievement_1_popup";
            if (ID == EnumExt_CGAchievementManager.CGGhostCG) return "CGAchievement_2_popup";
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