using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using Partiality.Modloader;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace SavePreparator
{
    public class SavePreparator : PartialityMod
    {
        public SavePreparator()
        {
            this.ModID = "SavePreparator";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static SavePreparator instance;
        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            On.StoryGameSession.ctor += StoryGameSession_ctor;
        }

        private void StoryGameSession_ctor(On.StoryGameSession.orig_ctor orig, StoryGameSession self, int saveStateNumber, RainWorldGame game)
        {
            orig(self, saveStateNumber, game);

            var lt = game.rainWorld.progression.miscProgressionData.levelTokens;
            for (int i = 0; i < lt.Length; i++)
            {
                lt[i] = true;
            }
            var st = game.rainWorld.progression.miscProgressionData.sandboxTokens;
            for (int i = 0; i < st.Length; i++)
            {
                st[i] = true;
            }

            game.rainWorld.progression.miscProgressionData.watchedSleepScreens = 99;
            game.rainWorld.progression.miscProgressionData.watchedDeathScreens = 99;
            game.rainWorld.progression.miscProgressionData.watchedDeathScreensWithFlower = 99;
            game.rainWorld.progression.miscProgressionData.watchedMalnourishScreens = 99;
            game.rainWorld.progression.miscProgressionData.starvationTutorialCounter = -1;
            game.rainWorld.progression.miscProgressionData.warnedAboutKarmaLossOnExit = 99;

            game.rainWorld.progression.miscProgressionData.redHasVisitedPebbles = true;
            game.rainWorld.progression.miscProgressionData.redUnlocked = true;
            game.rainWorld.progression.miscProgressionData.redMeatEatTutorial = 99;
            game.rainWorld.progression.miscProgressionData.watchedSleepScreens = 99;
            game.rainWorld.progression.miscProgressionData.watchedSleepScreens = 99;
        }
    }
}
