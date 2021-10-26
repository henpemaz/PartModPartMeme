using Partiality.Modloader;
using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using OptionalUI;
using UnityEngine;
using Menu;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: System.Runtime.CompilerServices.SuppressIldasmAttribute()]
namespace ConcealedGarden
{
    public partial class ConcealedGarden : PartialityMod
    {
        public ConcealedGarden()
        {
            this.ModID = "Concealed Garden";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static ConcealedGarden instance;
        public static ConcealedGardenOI instanceOI;
        public static ConcealedGardenProgression progression => ConcealedGardenProgression.progression;
        public static OptionalUI.OptionInterface LoadOI()
        {
            return new ConcealedGardenOI();
        }

        public class ConcealedGardenOI : OptionalUI.OptionInterface
        {
            public ConcealedGardenOI() : base(mod:instance)
            {
                instanceOI = this;
            }

            public override void Initialize()
            {
                base.Initialize();
                this.Tabs = new OpTab[1] { new OpTab() };
                CompletelyOptional.GeneratedOI.AddBasicProfile(Tabs[0], rwMod);

                if (!isOptionMenu) return;

                CGAchievementManager.MakeAchievementsOi(this, Tabs[0]);
                Tabs[0].AddItems(new OpLabel(40, 225, "Concealed Garden was brought to you by:", true),
                    new OpLabelLong(new Vector2(40, 10), new Vector2(530, 215),
@"Henpemaz - Lead Dev, most of the stuff in the region unless otherwise noted!
Thalber - Dev, made most of the underground rooms, overhauled the fist shelter & some more, bug-hunting, productive discussions and psychological support in my DMs :flushed:
DryCryCrystal - Colab, made a special tileset for LRU room, in exchange for the electricarc object.
Mehri'Kairothep - Early playtester, loved the region, helped me figure out what needed polishing.
Wrayk - Almost playtester, made a connection room (:
LB Gamer - Playtester, helpful and resourceful, also tried to help with a couple rooms.
Donschnulione - Playtester!
Sideways_Tumble - Playtester and room touchups!
ICWobbles & Sipik - Music mentorship every now and then.
Garrakx & Topicular - Makers of the awesome mods that help people make more mods!"
));
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            Debug.Log("CG Loading Start");

            //throw new Exception("modding");

            LizardSkin.LizardSkin.SetCGStoryProgression(0); // CG Progression Mode

            // Hooking code goose hre

            CGElectricArcs.Register();

            CGOrganicShelter.Register();

            CGLifeSimProjection.Register();

            CGSongSFX.Register();

            CGCosmeticLeaves.Register();

            CGGateCustomization.Register();

            // Almost sure I ended up not using these
            CGSlipperySlope.Register();

            CGBunkerShelterParts.Register();

            CGQuestionableLizardBits.Apply();

            CGSpawnCustomizations.Apply();

            CGNoLurkArea.Register();

            CGGravityGradient.Register();

            // *sad quack*
            TremblingSeed.SeedHooks.Apply();

            CGProgressionFilter.Register();

            CGLRUPickup.Register();

            CGCameraZoomEffect.Apply();

            CGCutscenes.Apply();

            CGMenuScenes.Apply();

            CGSkyLine.Register();

            CGCosmeticWater.Register();

            CGFourthLayerFix.Apply();

            // CG progression
            ConcealedGardenProgression.Apply();
            CGYellowThoughtsAdaptor.Apply();
            CGLizardBehaviorChange.Apply();

            CGCameraEffects.Apply();

            CGAchievementManager.Apply();

            CGAmbienceFix.Apply();

            CGSlugFilter.Register();

            CGShelterRain.Register();

            CGQuickAndDirtyFix.Register();

            CGDrySpot.Register();

            CGRootShelterGhostFix.Register();

            // Screaming into the void
            Debug.Log("CG Fully Loaded");
        }
    }
}
