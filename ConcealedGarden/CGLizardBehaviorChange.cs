using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal static class CGLizardBehaviorChange
    {
        internal static void Apply()
        {
            // Lizards nicer and easier to tame with cg-tf flag
            On.LizardAI.LikeOfPlayer += LizardAI_LikeOfPlayer;
            On.LizardAI.ctor += LizardAI_ctor;
        }

        private static void LizardAI_ctor(On.LizardAI.orig_ctor orig, LizardAI self, AbstractCreature creature, World world)
        {
            orig(self, creature, world);
            if (ConcealedGarden.progression?.transfurred ?? false && self.friendTracker != null)
            {
                self.friendTracker.tamingDifficlty = Mathf.Lerp(self.friendTracker.tamingDifficlty, 0.8f, 0.3f);
            }
        }

        private static float LizardAI_LikeOfPlayer(On.LizardAI.orig_LikeOfPlayer orig, LizardAI self, Tracker.CreatureRepresentation player)
        {
            float val = orig(self, player);
            if (ConcealedGarden.progression?.transfurred ?? false && val < 0.6f)
            {
                val = Mathf.Clamp(Mathf.Lerp(val, 0.6f, 0.25f + self.lizard.abstractCreature.personality.sympathy / 2f), -1f, 1f);
                val = Mathf.Clamp(Mathf.Lerp(val, val - 0.4f, self.lizard.abstractCreature.personality.dominance), -1f, 1f);
            }

            return val;
        }
    }
}