

using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;
using OptionalUI;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;

[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]


namespace LizardSkin
{
    internal class LizardSkinOI : OptionalUI.OptionInterface
    {
        private List<LizKinProfileData> lizKinProfiles;

        public LizardSkinOI() : base(mod: LizardSkin.instance) { }

        const string modDescription =
@"LizardSkin lets you create profiles of cosmetics to be applied on your slugcat.
Use the tabs on the left to edit or create profiles.

When on a profile tab, you can select which characters that profile should apply to.
If more than one profile applies to a slugcat, all cosmetics found will be applied.
Advanced mode let you specify difficulty, player-number or character-number so that
you can get it working with custom slugcats too.

Inside a profile you can add Cosmetics by clicking on the box with a +.
Cosmetics can be reordered, copied, pasted, duplicated and deleted.
You can also control the base color and effect color for your slugcat to match
any custom sprites or skins.

You can pick Cosmetics of several types, edit their settings and configure randomization.
When you're done customizing, hit refresh on the preview panel to see
what your sluggo looks like.";



        public override void Initialize()
        {
            base.Initialize();

            LoadLizKinData();

            this.Tabs = new OptionalUI.OpTab[2 + lizKinProfiles.Count];
            this.Tabs[0] = new OptionalUI.OpTab("Instructions");

            CompletelyOptional.GeneratedOI.AddBasicProfile(Tabs[0], rwMod);

            Tabs[0].AddItems(new OpLabelLong(new Vector2(50f, 250f), new Vector2(500f, 230f), modDescription, alignment: FLabelAlignment.Center, autoWrap: false));

            // detect Concealed Garden



            ////


            for (int i = 0; i < lizKinProfiles.Count; i++)
            {
                Tabs[i+1] = new OptionalUI.OpTab(lizKinProfiles[i].profileName);

                // Make profile tabs
                Tabs[i + 1].AddItems(new ProfileTabManager(this, lizKinProfiles[i], Tabs[i+1]));


                ////

            }

            Tabs[Tabs.Length-1] = new OptionalUI.OpTab("+");

            // Make Add Profile tab

            //OpContainer myContainer = new MenuCosmeticsAdaptor(new Vector2(475, 340));
            //this.Tabs[0].AddItems(myContainer);
        }

        private void LoadLizKinData()
        {
            this.lizKinProfiles = new List<LizKinProfileData>();

            // read from disk


            // tbd

            if(lizKinProfiles == null || lizKinProfiles.Count == 0)
            {
                LoadEmptyLizKinData();
            }
        }

        private void LoadEmptyLizKinData()
        {
            this.lizKinProfiles = new List<LizKinProfileData>();

            lizKinProfiles.Add(GetDefaultProfile());
        }

        private LizKinProfileData GetDefaultProfile()
        {
            LizKinProfileData myProfile = new LizKinProfileData();
            myProfile.profileName = "My Profile";

            CosmeticAntennaeData myCosmetic = new CosmeticAntennaeData() { profile = myProfile };
            CosmeticTailTuftData myCosmetic1 = new CosmeticTailTuftData() { profile = myProfile };
            CosmeticTailTuftData myCosmetic2 = new CosmeticTailTuftData() { profile = myProfile };

            myProfile.cosmetics.Add(myCosmetic);
            myProfile.cosmetics.Add(myCosmetic1);
            myProfile.cosmetics.Add(myCosmetic2);
            return myProfile;
        }

        public override void Update(float dt)
        {
            base.Update(dt);
        }

    }

    internal class ProfileTabManager : UIelement
    {
        private LizardSkinOI lizardSkinOI;
        private LizKinProfileData lizKinProfileData;
        private OpTab opTab;

        public ProfileTabManager(LizardSkinOI lizardSkinOI, LizKinProfileData lizKinProfileData, OpTab opTab):base(new Vector2(0,0), new Vector2(600,600))
        {
            this.lizardSkinOI = lizardSkinOI;
            this.lizKinProfileData = lizKinProfileData;
            this.opTab = opTab;
        }
    }
}
