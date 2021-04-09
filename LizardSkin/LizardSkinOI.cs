

using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;
using OptionalUI;
using System.Collections.Generic;
using System.Linq;
using System;
using CompletelyOptional;
using RWCustom;
using Menu;

[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]


namespace LizardSkin
{
    internal class LizardSkinOI : OptionalUI.OptionInterface
    {
        //private static List<LizKinProfileData> lizKinProfiles;
        public static LizKinConfiguration configuration;

        private static bool loadingFromRefresh;
        private static int lastSelectedTab;
        private bool refreshOnNextFrame;

        private ProfileManager activeManager;
        private static LizKinCosmeticData cosmeticOnClipboard;
        private List<GroupPanel> panelsToAdd;

        public LizardSkinOI() : base(mod: LizardSkin.instance)
        {
            panelsToAdd = new List<GroupPanel>();
        }

        const string modDescription =
@"LizardSkinOI lets you create profiles of cosmetics to be applied on your slugcat. Use the tabs on the left to edit or create profiles.

When on a profile tab, you can select which characters that profile should apply to. If more than one profile applies to a slugcat, all cosmetics found will be applied. Advanced mode lets you specify difficulty, player-number or character-number so that you can get it working with custom slugcats too.

Inside a profile you can add Cosmetics by clicking on the box with a +. Cosmetics can be reordered, copied, pasted, duplicated and deleted. You can also control the base color and effect color for your slugcat to match any custom sprites or skins.

You can pick Cosmetics of several types, edit their settings and configure randomization. When you're done customizing, hit refresh on the preview panel to see what your sluggo looks like :3";


        public override void Initialize()
        {
            base.Initialize();
            Debug.Log("LizardSkinOI Initialize");

            LoadLizKinData();

            if (!OptionInterface.isOptionMenu)
            {
                this.Tabs = new OpTab[1] { new OptionalUI.OpTab("Dummy") };
                return;
            }

            this.Tabs = new OptionalUI.OpTab[2 + configuration.profiles.Count];

            // Title and Instructions tab
            Debug.Log("making instructions");
            this.Tabs[0] = new OptionalUI.OpTab("Instructions");
            CompletelyOptional.GeneratedOI.AddBasicProfile(Tabs[0], rwMod);
            Tabs[0].AddItems(new OpLabelLong(new Vector2(50f, 470f), new Vector2(500f, 0f), modDescription, alignment: FLabelAlignment.Center, autoWrap: true), 
                new NotAManagerTab(this),
                new ReloadHandler(this));

            // detect Concealed Garden

            // ????

            ////

            // Make profile tabs
            Debug.Log("making tabs");
            for (int i = 0; i < configuration.profiles.Count; i++)
            {
                Tabs[i + 1] = new OptionalUI.OpTab(configuration.profiles[i].profileName);
                ProfileManager manager = new ProfileManager(this, configuration.profiles[i], Tabs[i + 1]);
                Tabs[i + 1].AddItems(manager);
            }

            // Make Add Profile tab
            Debug.Log("making addprofile");
            Tabs[Tabs.Length-1] = new OptionalUI.OpTab("+");
            Tabs[Tabs.Length - 1].AddItems(new NewProfileHandler(this), new NotAManagerTab(this));

        }

        public override void Update(float dt)
        {
            base.Update(dt);
            if (refreshOnNextFrame)
            {
                Debug.Log("LizardSkinOI Refreshing");
                lastSelectedTab = ConfigMenu.tabCtrler.index;
                ConfigMenu.RefreshCurrentConfig();
                refreshOnNextFrame = false;
                loadingFromRefresh = true;
            }

            foreach (GroupPanel panel in panelsToAdd)
            {
                Debug.Log("adding panel to manager");
                activeManager.addPanel(panel);
            }
            panelsToAdd.Clear();


        }

        public override void Signal(UItrigger trigger, string signal)
        {
            base.Signal(trigger, signal);

            Debug.Log("LizardSkinOI got signal " + signal);

            if (signal == "btnProfileMvUp")
            {
                if (activeManager != null) activeManager.SignalSwitchOut();
                if (configuration.MoveProfileUp(activeManager.profileData))
                {
                    RequestRefresh();
                    ConfigMenu.tabCtrler.index--;
                    return;
                }
            }else
            if (signal == "btnProfileMvDown")
            {
                if (activeManager != null) activeManager.SignalSwitchOut();
                if (configuration.MoveProfileDown(activeManager.profileData))
                {
                    RequestRefresh();
                    ConfigMenu.tabCtrler.index++;
                    return;
                }
            }else
            if (signal == "btnProfileDuplicate")
            {
                if (activeManager != null) activeManager.SignalSwitchOut();
                if (configuration.DuplicateProfile(activeManager.profileData))
                {
                    RequestRefresh();
                    ConfigMenu.tabCtrler.index = Tabs.Length-1;
                    return;
                }
            }else
            if (signal == "btnProfileDelete")
            {
                if (activeManager != null) activeManager.SignalSwitchOut();
                if (configuration.DeleteProfile(activeManager.profileData))
                {
                    RequestRefresh();
                    if (ConfigMenu.tabCtrler.index == Tabs.Length - 2) ConfigMenu.tabCtrler.index--;
                    return;
                }
            }
            else
            {
                if (activeManager != null)
                {
                    activeManager.Signal(trigger, signal);
                }
            }
        }

        private void Reloaded()
        {
            // If reload, jump to tab
            Debug.Log("LizardSkinOI Reloaded");
            if (loadingFromRefresh && lastSelectedTab < Tabs.Length - 1)
            {
                ConfigMenu.tabCtrler.index = lastSelectedTab;
            }
            loadingFromRefresh = false;
        }

        private void LoadLizKinData()
        {
            Debug.Log("LizardSkinOI LoadData");
            // read from disk


            // tbd

            if (configuration == null || configuration.profiles.Count == 0)
            {
                LoadEmptyLizKinData();
            }
        }

        private void LoadEmptyLizKinData()
        {
            configuration = new LizKinConfiguration();
            LizKinProfileData myProfile = new LizKinProfileData();
            myProfile.profileName = "My Profile";

            LizKinCosmeticData myCosmetic = new CosmeticAntennaeData() { profile = myProfile };
            LizKinCosmeticData myCosmetic1 = new CosmeticTailTuftData() { profile = myProfile };
            LizKinCosmeticData myCosmetic2 = new CosmeticTailTuftData() { profile = myProfile };

            myProfile.cosmetics.Add(myCosmetic);
            myProfile.cosmetics.Add(myCosmetic1);
            myProfile.cosmetics.Add(myCosmetic2);
            configuration.profiles.Add(myProfile);

            //LizKinProfileData myProfile2 = new LizKinProfileData();
            //myCosmetic = new CosmeticSpineSpikesData() { profile = myProfile2 };
            //myCosmetic1 = new CosmeticBumpHawkData() { profile = myProfile2 };
            //myCosmetic2 = new CosmeticWhiskersData() { profile = myProfile2 };

            //myProfile2.cosmetics.Add(myCosmetic);
            //myProfile2.cosmetics.Add(myCosmetic1);
            //myProfile2.cosmetics.Add(myCosmetic2);
            //configuration.profiles.Add(myProfile2);
        }

        private class NewProfileHandler : UIelement
        {
            private LizardSkinOI lizardSkinOI;

            public NewProfileHandler(LizardSkinOI lizardSkinOI) : base(new Vector2(0, 0), new Vector2(600, 600))
            {
                this.lizardSkinOI = lizardSkinOI;
            }

            public override void Show()
            {
                base.Show();
                lizardSkinOI.RequestNewProfile();
            }
        }

        private class NotAManagerTab : UIelement
        {
            private LizardSkinOI lizardSkinOI;
            public NotAManagerTab(LizardSkinOI lizardSkinOI) : base(new Vector2(0, 0), new Vector2(600, 600))
            {
                this.lizardSkinOI = lizardSkinOI;
            }
            public override void Show()
            {
                base.Show();
                lizardSkinOI.SwitchActiveManager(null);
            }
        }

        private class ReloadHandler : UIelement
        {
            private LizardSkinOI lizardSkinOI;
            public ReloadHandler(LizardSkinOI lizardSkinOI) : base(new Vector2(0, 0), new Vector2(600, 600))
            {
                this.lizardSkinOI = lizardSkinOI;
            }
            public override void Show()
            {
                base.Show();
                lizardSkinOI.Reloaded();
            }
        }

        

        private void SwitchActiveManager(ProfileManager profileTabManager)
        {
            Debug.Log("LizardSkinOI SwitchActiveManager");
            if (activeManager != null)
            {
                activeManager.SignalSwitchOut();
            }
                
            activeManager = profileTabManager;

        }

        private void RequestNewProfile()
        {
            Debug.Log("LizardSkinOI RequestNewProfile");
            // Ignore new profile if about to reset, moving around tabs and such
            if (!refreshOnNextFrame && configuration.AddDefaultProfile())
            {
                RequestRefresh();
                return;
            }
        }

        private void RequestRefresh()
        {
            Debug.Log("LizardSkinOI RequestRefresh");
            if (refreshOnNextFrame) return;
            if (activeManager != null) activeManager.SignalSwitchOut();
            this.refreshOnNextFrame = true;
        }


        private void RequestNewPanel(GroupPanel panel)
        {
            Debug.Log("LizardSkinOI RequestNewPanel");
            this.panelsToAdd.Add(panel);
        }

        internal class ProfileManager : UIelement
        {
            private LizardSkinOI lizardSkinOI;
            internal LizKinProfileData profileData;
            private OpTextBox nameBox;
            private OpResourceSelector appliesToModeSelector;
            private OpResourceSelector appliesToSelectorSelector;
            private OpCheckBox appliesTo0;
            private OpLabel appliesTo0Label;
            private OpCheckBox appliesTo1;
            private OpLabel appliesTo1Label;
            private OpCheckBox appliesTo2;
            private OpLabel appliesTo2Label;
            private OpCheckBox appliesTo3;
            private OpLabel appliesTo3Label;
            private OpTextBox appliesToInput;
            private OpTinyColorPicker effectColorPicker;
            private OpCheckBox overrideBaseCkb;
            private OpTinyColorPicker baseColorPicker;
            private EventfulFloatSlider previewRotationSlider;
            private MenuCosmeticsAdaptor cosmeticsPreview;
            private OpScrollBox cosmeticsBox;
            private List<GroupPanel> cosmPanels;
            private AddCosmeticPanelPanel addPanelPanel;

            public ProfileManager(LizardSkinOI lizardSkinOI, LizKinProfileData lizKinProfileData, OpTab opTab) : base(new Vector2(0, 0), new Vector2(600, 600))
            {
                this.lizardSkinOI = lizardSkinOI;
                this.profileData = lizKinProfileData;

                // Profile management row
                OpSimpleImageButton arrowDown;
                opTab.AddItems(new OpLabel(profileMngmtPos, new Vector2(40, 24), text: "Profile:", alignment: FLabelAlignment.Left),
                    this.nameBox = new OpTextBox(profileMngmtPos + new Vector2(50, 0), 100, "", defaultValue: profileData.profileName) { description = "Rename this profile" },
                    new OpSimpleImageButton(profileMngmtPos + new Vector2(160, 0), new Vector2(24, 24), "btnProfileMvUp", "LizKinArrow") { description = "Move this profile up" },
                    arrowDown = new OpSimpleImageButton(profileMngmtPos + new Vector2(190, 0), new Vector2(24, 24), "btnProfileMvDown", "LizKinArrow") { description = "Move this profile down" },
                    new OpSimpleImageButton(profileMngmtPos + new Vector2(220, 0), new Vector2(24, 24), "btnProfileDuplicate", "LizKinDuplicate") { description = "Duplicate this profile" },
                    new OpSimpleImageButton(profileMngmtPos + new Vector2(250, 0), new Vector2(24, 24), "btnProfileDelete", "LizKinDelete") { description = "Delete this profile" }
                    );
                arrowDown.sprite.scaleY *= -1;


                // Filters
                opTab.AddItems(new OpLabel(characterMngmtPos + new Vector2(0, 0), new Vector2(40, 24), text: "Applies to:", alignment: FLabelAlignment.Left),
                    appliesToModeSelector = new OpResourceSelector(characterMngmtPos + new Vector2(40, 30), 110, "", typeof(LizKinProfileData.ProfileAppliesToMode), profileData.appliesToMode.ToString()) { description = "How complicated should the selection filter be..." },
                    appliesToSelectorSelector = new OpResourceSelector(characterMngmtPos + new Vector2(80, 0), 100, "", typeof(LizKinProfileData.ProfileAppliesToSelector), profileData.appliesToSelector.ToString()) { description = "Filter by Difficulty (story-mode), Character (arena) or Player-number..." },

                    appliesTo0 = new OpCheckBox(characterMngmtPos + new Vector2(0, -30), ""),
                    appliesTo0Label = new OpLabel(characterMngmtPos + new Vector2(40, -30), new Vector2(40, 24), text: "Survivor", alignment: FLabelAlignment.Left),
                    appliesTo1 = new OpCheckBox(characterMngmtPos + new Vector2(100, -30), ""),
                    appliesTo1Label = new OpLabel(characterMngmtPos + new Vector2(140, -30), new Vector2(40, 24), text: "Monk", alignment: FLabelAlignment.Left),
                    appliesTo2 = new OpCheckBox(characterMngmtPos + new Vector2(0, -60), ""),
                    appliesTo2Label = new OpLabel(characterMngmtPos + new Vector2(40, -60), new Vector2(40, 24), text: "Hunter", alignment: FLabelAlignment.Left),
                    appliesTo3 = new OpCheckBox(characterMngmtPos + new Vector2(100, -60), ""),
                    appliesTo3Label = new OpLabel(characterMngmtPos + new Vector2(140, -60), new Vector2(40, 24), text: "Nightcat", alignment: FLabelAlignment.Left),

                    appliesToInput = new OpTextBox(characterMngmtPos + new Vector2(10, -30), 160, "", defaultValue: "-1") { allowSpace = true, description = "Which indexes to apply to, comma-separated, everything being zero-indexed, or -1 for all" }
                    );

                FiltersConformToConfig();

                // Colors
                opTab.AddItems(new OpLabel(colorMngmtPos + new Vector2(80, 0), new Vector2(40, 24), text: "Effect Color:", alignment: FLabelAlignment.Right),
                new OpLabel(colorMngmtPos + new Vector2(80, -30), new Vector2(40, 24), text: "Override Base Color:", alignment: FLabelAlignment.Right),
                this.overrideBaseCkb = new OpCheckBox(colorMngmtPos + new Vector2(130, -30), "", profileData.overrideBaseColor) { description = "Use a different Base Color than the slugcat's default color" });
                
                // see iHaveChildren
                this.effectColorPicker = new OpTinyColorPicker(colorMngmtPos + new Vector2(130, 0), "btnProfileEffectColor", OpColorPicker.ColorToHex(profileData.effectColor)) { description = "Pick the Effect Color for the highlights" };
                effectColorPicker.AddSelfAndChildrenToTab(opTab);
                this.baseColorPicker = new OpTinyColorPicker(colorMngmtPos + new Vector2(160, -30), "btnProfileBaseColor", OpColorPicker.ColorToHex(profileData.baseColorOverride)) { description = "Pick the Base Color for the cosmetics" };
                baseColorPicker.AddSelfAndChildrenToTab(opTab);

                effectColorPicker.OnChanged += ColorPicker_OnChanged;
                effectColorPicker.OnFrozenUpdate += ColorPicker_OnFrozenUpdate;
                baseColorPicker.OnChanged += ColorPicker_OnChanged;
                baseColorPicker.OnFrozenUpdate += ColorPicker_OnFrozenUpdate;


                // Preview pannel 
                EventfulImageButton refreshBtn;
                opTab.AddItems(
                    new OpRect(previewPanelPos + new Vector2(0, -420), new Vector2(220, 420)),
                    cosmeticsPreview = new MenuCosmeticsAdaptor(previewPanelPos + new Vector2(110, -100), profileData),
                    previewRotationSlider = new EventfulFloatSlider(previewPanelPos + new Vector2(30, -45), "", new Vector2(-1, 1), 160),
                    refreshBtn = new EventfulImageButton(previewPanelPos + new Vector2(5, -29), new Vector2(24,24), "btnProfileRefresh", "LizKinReload")
                    );

                previewRotationSlider.OnChanged += PreviewRotationSlider_OnChanged;
                previewRotationSlider.OnFrozenUpdate += FreezingButtons_OnFrozenUpdate;

                refreshBtn.OnSignal += RefreshBtn_OnSignal;
                refreshBtn.OnFrozenUpdate += FreezingButtons_OnFrozenUpdate;

                // Cosmetics Panenl
                Debug.Log("Cosmetic panel start");
                cosmPanels = new List<GroupPanel>();
                Debug.Log("Cosmetic box make");
                cosmeticsBox = new OpScrollBox(cosmeticsPanelPos, new Vector2(370, 540), 0, hasSlideBar: false);
                Debug.Log("Cosmetic box add");
                opTab.AddItems(cosmeticsBox);

                Debug.Log("cosmeticsBox.contentSize is " + cosmeticsBox.GetContentSize());

                Debug.Log("add pannel make");

                addPanelPanel = new AddCosmeticPanelPanel(new Vector2(5, 0), 360f);
                Debug.Log("add pannel add");

                addPanelPanel.AddSelfAndChildrenToScroll(cosmeticsBox);

                addPanelPanel.OnAdd += AddPanelPanel_OnAdd;
                addPanelPanel.OnPaste += AddPanelPanel_OnPaste;

                Debug.Log("make pannels");

                MakeCosmEditPannels();

                OrganizePannels();

                Debug.Log("ProfileTabManager done");

            }

            private void AddPanelPanel_OnPaste()
            {
                DuplicateCosmetic(LizardSkinOI.cosmeticOnClipboard);
            }

            internal void SetClipboard(LizKinCosmeticData data)
            {
                LizardSkinOI.cosmeticOnClipboard = LizKinCosmeticData.Clone(data); // release profile ref, detach from original
            }

            internal void DuplicateCosmetic(LizKinCosmeticData data)
            {
                profileData.cosmetics.Add(LizKinCosmeticData.Clone(data));
                profileData.cosmetics[profileData.cosmetics.Count - 1].profile = profileData;
                GroupPanel panel = profileData.cosmetics[profileData.cosmetics.Count - 1].MakeEditPanel(this);
                lizardSkinOI.RequestNewPanel(panel);
            }

            internal void DeleteCosmetic(LizKinCosmeticData data)
            {
                profileData.cosmetics.Remove(data);
                // currently cannot remove elements from CM
                lizardSkinOI.RequestRefresh();
            }

            internal void MakeCosmEditPannels()
            {
                Debug.Log("MakeCosmEditPannels");
                foreach (LizKinCosmeticData cosmeticData in profileData.cosmetics)
                {
                    Debug.Log("Makin new panel");

                    GroupPanel panel = cosmeticData.MakeEditPanel(this);
                    //CosmeticEditPanel editPanel = new CosmeticEditPanel(new Vector2(5, 0), new Vector2(360, 100));
                    cosmPanels.Add(panel);
                    panel.AddSelfAndChildrenToScroll(cosmeticsBox);
                }
                Debug.Log("MakeCosmEditPannels done");
            }

            internal void OrganizePannels()
            {
                Debug.Log("OrganizePannels");
                float cosmEditPanelMarginVert = 6;
                float totalheight = cosmEditPanelMarginVert + addPanelPanel.size.y;
                foreach (GroupPanel panel in cosmPanels)
                {
                    totalheight += panel.size.y + cosmEditPanelMarginVert;
                }

                // these two loops could happen at once.
                Debug.Log("calculating heights");
                totalheight = Mathf.Max(totalheight, cosmeticsBox.GetContentSize());
                Debug.Log("setting height");

                if (totalheight != cosmeticsBox.GetContentSize()) cosmeticsBox.SetContentSize(totalheight);


                float topleftpos = cosmEditPanelMarginVert/2;
                foreach (GroupPanel panel in cosmPanels)
                {
                    
                    panel.topLeft = new Vector2(3, totalheight - topleftpos);
                    Debug.Log("Moved panel to " + (totalheight - topleftpos));
                    topleftpos += panel.size.y + cosmEditPanelMarginVert;
                }

                Debug.Log("Moved add panel to " + (totalheight - topleftpos));
                addPanelPanel.topLeft = new Vector2(3, totalheight - topleftpos);

                Debug.Log("OrganizePannels done");
            }


            private void AddPanelPanel_OnAdd()
            {
                Debug.Log("AddPanelPanel_OnAdd");

                profileData.AddEmptyCosmetic();
                GroupPanel panel = profileData.cosmetics[profileData.cosmetics.Count - 1].MakeEditPanel(this);
                lizardSkinOI.RequestNewPanel(panel);

                Debug.Log("AddPanelPanel_OnAdd done");
            }

            // Called from OI
            internal void addPanel(GroupPanel panel)
            {
                cosmPanels.Add(panel);
                panel.AddSelfAndChildrenToScroll(cosmeticsBox);
                OrganizePannels();
            }

            internal class AddCosmeticPanelPanel : GroupPanel
            {
                private EventfulButton addbutton;
                private EventfulImageButton pastebutton;

                public AddCosmeticPanelPanel(Vector2 pos, float size) : base(pos, new Vector2(size, 60f))
                {
                    children.Add(addbutton = new EventfulButton(new Vector2(size / 2 - 30, -42), new Vector2(24, 24), "", "+") { description = "Add a new Cosmetic to this profile" });
                    children.Add(pastebutton = new EventfulImageButton(new Vector2(size / 2 + 6, -42), new Vector2(24, 24), "", "LizKinClipboard") { description = "Paste a Cosmetic from your clipboard" });
                }

                public event OnButtonSignalHandler OnAdd { add { addbutton.OnSignal += value; } remove { addbutton.OnSignal -= value; } }
                public event OnButtonSignalHandler OnPaste { add { pastebutton.OnSignal += value; } remove { pastebutton.OnSignal -= value; } }
            }

            private void RefreshBtn_OnSignal()
            {
                cosmeticsPreview.Reset();
            }

            private void FreezingButtons_OnFrozenUpdate(float dt)
            {
                cosmeticsPreview.Update(dt);
            }

            private void PreviewRotationSlider_OnChanged()
            {
                cosmeticsPreview.SetRotation(previewRotationSlider.valueFloat);
            }

            private void ColorPicker_OnChanged()
            {
                profileData.effectColor = effectColorPicker.valuecolor;
                profileData.overrideBaseColor = overrideBaseCkb.valueBool;
                profileData.baseColorOverride = baseColorPicker.valuecolor;
            }

            private void ColorPicker_OnFrozenUpdate(float dt)
            {
                cosmeticsPreview.Update(dt);
            }

            public override void Update(float dt)
            {
                base.Update(dt);

                if (profileData.appliesToMode != (LizKinProfileData.ProfileAppliesToMode)Enum.Parse(typeof(LizKinProfileData.ProfileAppliesToMode), appliesToModeSelector.value)
                    || profileData.appliesToSelector != (LizKinProfileData.ProfileAppliesToSelector)Enum.Parse(typeof(LizKinProfileData.ProfileAppliesToSelector), appliesToSelectorSelector.value))
                {
                    // config change
                    FiltersGrabConfig();
                    // might need layout change
                    FiltersConformToConfig();
                }

                //profileData.effectColor = effectColorPicker.valuecolor;
                //profileData.overrideBaseColor = overrideBaseCkb.valueBool;
                //profileData.baseColorOverride = baseColorPicker.valuecolor;
            }


            public override void Show()
            {
                base.Show();
                lizardSkinOI.SwitchActiveManager(this);

                // Because OI unhid my hidden elements smh
                FiltersConformToConfig();
            }

            internal void SignalSwitchOut()
            {
                Debug.Log("ProfileManager SignalSwitchOut");
                bool needsRefresh = false;
                // Save data
                if (nameBox.value != profileData.profileName)
                {
                    profileData.profileName = nameBox.value;
                    needsRefresh = true;
                }

                FiltersGrabConfig();

                if (needsRefresh)
                {
                    lizardSkinOI.RequestRefresh();
                }
            }

            internal void Signal(UItrigger trigger, string signal)
            {
                //throw new NotImplementedException();
            }

            #region FILTERSTUFF

            private void FiltersGrabConfig()
            {
                //Debug.Log("FiltersGrabConfig");
                //Debug.Log("profileData.appliesToList was " + String.Join(", ", profileData.appliesToList.Select(n => n.ToString()).ToArray()));
                LizKinProfileData.ProfileAppliesToMode previousMode = profileData.appliesToMode;
                LizKinProfileData.ProfileAppliesToMode newMode = (LizKinProfileData.ProfileAppliesToMode)Enum.Parse(typeof(LizKinProfileData.ProfileAppliesToMode), appliesToModeSelector.value);
                profileData.appliesToMode = newMode;
                profileData.appliesToSelector = (LizKinProfileData.ProfileAppliesToSelector)Enum.Parse(typeof(LizKinProfileData.ProfileAppliesToSelector), appliesToSelectorSelector.value);
                
                switch (previousMode)
                {
                    case LizKinProfileData.ProfileAppliesToMode.Basic:
                    case LizKinProfileData.ProfileAppliesToMode.Advanced:
                        profileData.appliesToList = new List<int>();
                        if (appliesTo0.valueBool && appliesTo1.valueBool && appliesTo2.valueBool && appliesTo3.valueBool)
                        {
                            //Debug.Log("all checks set");
                            profileData.appliesToList.Add(-1);
                        }
                        else
                        {
                            if (appliesTo0.valueBool) profileData.appliesToList.Add(0);
                            if (appliesTo1.valueBool) profileData.appliesToList.Add(1);
                            if (appliesTo2.valueBool) profileData.appliesToList.Add(2);
                            if (appliesTo3.valueBool) profileData.appliesToList.Add(3);
                        }
                        break;
                    case LizKinProfileData.ProfileAppliesToMode.VeryAdvanced:
                        profileData.appliesToList = new List<int>();
                        string raw = appliesToInput.value;
                        string[] rawsplit = raw.Split(',');
                        foreach (string rawsingle in rawsplit)
                        {
                            int myint;
                            if (int.TryParse(rawsingle.Trim(), out myint)) profileData.appliesToList.Add(myint);
                        }

                        break;
                }
                //Debug.Log("profileData.appliesToList now is  " + String.Join(", ", profileData.appliesToList.Select(n => n.ToString()).ToArray()));
            }


            private void FiltersConformToConfig()
            {
                //Debug.Log("FiltersConformToConfig");
                //Debug.Log("profileData.appliesToList was " + String.Join(", ", profileData.appliesToList.Select(n => n.ToString()).ToArray()));
                LizKinProfileData.ProfileAppliesToMode currentMode = profileData.appliesToMode;
                appliesTo0.valueBool = profileData.appliesToList.Contains(-1) || profileData.appliesToList.Contains(0);
                appliesTo1.valueBool = profileData.appliesToList.Contains(-1) || profileData.appliesToList.Contains(1);
                appliesTo2.valueBool = profileData.appliesToList.Contains(-1) || profileData.appliesToList.Contains(2);
                appliesTo3.valueBool = profileData.appliesToList.Contains(-1) || profileData.appliesToList.Contains(3);
                appliesToInput.value = String.Join(", ", profileData.appliesToList.Select(n => n.ToString()).ToArray());
                switch (currentMode)
                {
                    case LizKinProfileData.ProfileAppliesToMode.Basic:
                    case LizKinProfileData.ProfileAppliesToMode.Advanced:
                        appliesTo0.Show();
                        appliesTo0Label.Show();
                        appliesTo1.Show();
                        appliesTo1Label.Show();
                        appliesTo2.Show();
                        appliesTo2Label.Show();
                        appliesTo3.Show();
                        appliesTo3Label.Show();
                        appliesToInput.Hide();
                        break;
                    case LizKinProfileData.ProfileAppliesToMode.VeryAdvanced:
                        appliesTo0.Hide();
                        appliesTo0Label.Hide();
                        appliesTo1.Hide();
                        appliesTo1Label.Hide();
                        appliesTo2.Hide();
                        appliesTo2Label.Hide();
                        appliesTo3.Hide();
                        appliesTo3Label.Hide();
                        appliesToInput.Show();
                        break;
                }
                switch (currentMode)
                {
                    case LizKinProfileData.ProfileAppliesToMode.Basic:
                        profileData.appliesToSelector = LizKinProfileData.ProfileAppliesToSelector.Character;
                        appliesToSelectorSelector.value = LizKinProfileData.ProfileAppliesToSelector.Character.ToString();
                        appliesToSelectorSelector.Hide();
                        appliesTo0Label.text = "Survivor";
                        appliesTo1Label.text = "Monk";
                        appliesTo2Label.text = "Hunter";
                        appliesTo3Label.text = "Nightcat";
                        appliesTo3Label.description = "... or whatever custom slugcat is character #3 in your game";
                        break;
                    case LizKinProfileData.ProfileAppliesToMode.Advanced:
                        switch (profileData.appliesToSelector)
                        {
                            case LizKinProfileData.ProfileAppliesToSelector.Character:
                            case LizKinProfileData.ProfileAppliesToSelector.Difficulty:
                                appliesTo0Label.text = "0";
                                appliesTo1Label.text = "1";
                                appliesTo2Label.text = "2";
                                appliesTo3Label.text = "3";
                                break;
                            case LizKinProfileData.ProfileAppliesToSelector.Player:
                                appliesTo0Label.text = "1";
                                appliesTo1Label.text = "2";
                                appliesTo2Label.text = "3";
                                appliesTo3Label.text = "4";
                                break;
                        }
                        //break;
                        // why wont you let control fall through, stupid ass language I know you translate to assembly in the end just don't fucking jump and let it flow
                        goto case LizKinProfileData.ProfileAppliesToMode.VeryAdvanced;
                    case LizKinProfileData.ProfileAppliesToMode.VeryAdvanced:
                        appliesToSelectorSelector.Show();
                        appliesTo3Label.description = "";
                        break;
                }
                //Debug.Log("profileData.appliesToList now is  " + String.Join(", ", profileData.appliesToList.Select(n => n.ToString()).ToArray()));
            }

            #endregion FILTERSTUFF

            Vector2 profileMngmtPos => new Vector2(15, 570);
            Vector2 characterMngmtPos => new Vector2(380, 540);
            Vector2 colorMngmtPos => new Vector2(380, 450);
            Vector2 previewPanelPos => new Vector2(380, 420);
            Vector2 cosmeticsPanelPos => new Vector2(0, 0);


        }


        internal interface IHaveChildren
        {
            void AddSelfAndChildrenToTab(OpTab tab);
            void AddSelfAndChildrenToScroll(OpScrollBox scroll);
            //void RemoveSelfAndChildrenFromTab(OpTab tab);
            //void RemoveSelfAndChildrenFromScroll(OpScrollBox scroll);

        }

        internal delegate void OnChangeHendler();

        internal class GroupPanel : OpRect, IHaveChildren
        {
            protected List<UIelement> children;
            private List<Vector2> originalPositions;

            public GroupPanel(Vector2 pos, Vector2 size) : base(pos, size)
            {
                children = new List<UIelement>();
                originalPositions = new List<Vector2>();
            }

            public void AddSelfAndChildrenToTab(OpTab tab)
            {
                foreach (UIelement child in children)
                {
                    originalPositions.Add(child.GetPos());
                    child.pos += topLeft;
                }
                tab.AddItems(this);
                foreach (UIelement child in children)
                {
                    if (child is IHaveChildren) (child as IHaveChildren).AddSelfAndChildrenToTab(tab);
                    else tab.AddItems(child);
                }
                // if (children.Count > 0) tab.AddItems(children.ToArray());
            }
            public void AddSelfAndChildrenToScroll(OpScrollBox scroll)
            {
                // Debug.LogError("call to AddSelfAndChildrenToScroll");
                foreach (UIelement child in children)
                {
                    originalPositions.Add(child.GetPos());
                    child.pos += topLeft; // calls on change anyways
                }
                scroll.AddItems(this);
                foreach (UIelement child in children)
                {
                    if (child is IHaveChildren) (child as IHaveChildren).AddSelfAndChildrenToScroll(scroll);
                    else scroll.AddItems(child);
                }
                //if (children.Count > 0) scroll.AddItems(children.ToArray());
            }
            // CM no likey removeing stuff :(
            //public void RemoveSelfAndChildrenFromTab(OpTab tab)
            //{
            //    tab.RemoveItems(this);
            //    this.Unload();
            //    // tab.RemoveItems(children.ToArray());
            //    foreach (UIelement child in children)
            //    {
            //        if (child is IHaveChildren) (child as IHaveChildren).RemoveSelfAndChildrenFromTab(tab);
            //        else tab.RemoveItems(child);
            //        child.Unload();
            //    }
            //    children.Clear();
            //}
            //public void RemoveSelfAndChildrenFromScroll(OpScrollBox scroll)
            //{
            //    OpScrollBox.RemoveItemsFromScrollBox(this);
            //    this.Unload();
            //    //OpScrollBox.RemoveItemsFromScrollBox(children.ToArray());
            //    foreach (UIelement child in children)
            //    {
            //        if (child is IHaveChildren) (child as IHaveChildren).RemoveSelfAndChildrenFromScroll(scroll);
            //        else OpScrollBox.RemoveItemsFromScrollBox(child);
            //        child.Unload();
            //    }
            //    children.Clear();
            //}

            public override void OnChange()
            {
                base.OnChange();
                //if (children == null) return; // called from ctor before initialized ????
                for (int i = 0; i < children.Count; i++)
                {
                    children[i].pos = topLeft + originalPositions[i];
                    Debug.Log("Moving child element to " + (topLeft + originalPositions[i]));
                }
            }

            public Vector2 topLeft { get { return GetPos() + new Vector2(0, size.y); } set { pos = value - new Vector2(0, size.y); OnChange(); } } // Setting this should call onchange and move children
        }

        public delegate void OnFrozenUpdateHandler(float dt);
        internal class OpTinyColorPicker : EventfulButton, IHaveChildren
        {
            private OpColorPicker colorPicker;
            private bool currentlyPicking;
            const float mouseTimeout = 0.5f;
            float mouseOutCounter;

            public OpTinyColorPicker(Vector2 pos, string signal, string defaultHex) : base(pos, new Vector2(24, 24), signal)
            {
                this.colorPicker = new OpColorPicker(pos + new Vector2(-60, 24), "", defaultHex);

                this.currentlyPicking = false;

                this.colorFill = colorPicker.valueColor;
                this.rect.fillAlpha = 1f;
            }

            public void AddSelfAndChildrenToTab(OpTab tab)
            {
                tab.AddItems(this,
                    colorPicker);
                this.colorPicker.Hide();
            }

            public void AddSelfAndChildrenToScroll(OpScrollBox scroll)
            {
                scroll.AddItems(this,
                    colorPicker);
                this.colorPicker.Hide();
            }

            public void RemoveSelfAndChildrenFromTab(OpTab tab)
            {
                tab.RemoveItems(this,
                    colorPicker);
                this.Unload();
                colorPicker.Unload();
            }

            public void RemoveSelfAndChildrenFromScroll(OpScrollBox scroll)
            {
                OpScrollBox.RemoveItemsFromScrollBox(this,
                    colorPicker);
                this.Unload();
                colorPicker.Unload();
            }

            public override void Signal()
            {
                // base.Signal();
                if (!currentlyPicking)
                {
                    colorPicker.Show();
                    currentlyPicking = true;
                }
                else
                {
                    currentlyPicking = false;
                    colorFill = colorPicker.valueColor;
                    OnChanged?.Invoke();
                    colorPicker.Hide();
                    base.Signal();
                }
            }

            public event OnChangeHendler OnChanged;
            //public override void OnChange()
            //{
            //    base.OnChange();

            //}

            public Color valuecolor => colorPicker.valueColor;

            //public event OnFrozenUpdateHandler OnFrozenUpdate;

            public override void Update(float dt)
            {
                // we do a little tricking
                //if (currentlyPicking && !this.MouseOver) this.held = false;
                base.Update(dt);
                if (currentlyPicking && !this.MouseOver)
                {
                    colorPicker.Update(dt);
                    //base.OnFrozenUpdate?.Invoke(dt);
                    OnChanged?.Invoke();
                    this.held = true;
                }

                if (currentlyPicking && !this.MouseOver && !colorPicker.MouseOver)
                {
                    mouseOutCounter += dt;
                }
                else
                {
                    mouseOutCounter = 0f;
                }
                if (mouseOutCounter >= mouseTimeout)
                {
                    Signal();
                }
            }

            public override void GrafUpdate(float dt)
            {
                base.GrafUpdate(dt);
                if (currentlyPicking)
                {
                    this.colorFill = colorPicker.valueColor;
                }
                rect.fillAlpha = 1f;
            }

            public override void Show()
            {
                base.Show();
                colorPicker.Hide();
            }
        }

        internal class EventfulFloatSlider : OpSlider
        {
            private int grrrlength;
            public bool showLabel;
            private Vector2 range;
            private MenuLabel labelRef;

            public EventfulFloatSlider(Vector2 pos, string key, Vector2 range, int length, bool showLabel = false, bool vertical = false, float defaultValue = 0) : base(pos, key, new IntVector2(0, length - 1), vertical: vertical,
                defaultValue: Mathf.RoundToInt(Custom.LerpMap(defaultValue, range.x, range.y, 0, length - 1)))
            {
                this.grrrlength = length;
                this.showLabel = showLabel;
                this.range = range;
            }

            protected override void Initialize()
            {
                base.Initialize();
                this.labelRef = (Menu.MenuLabel)typeof(OpSlider).GetField("label", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(this);
            }

            new public float valueFloat => Custom.LerpMap(valueInt, 0, grrrlength - 1, range.x, range.y);

            public override void Show()
            {
                base.Show();
                if (labelRef != null) labelRef.label.isVisible = showLabel;
            }

            public event OnFrozenUpdateHandler OnFrozenUpdate;

            public override void Update(float dt)
            {
                bool wasHeld = held;
                base.Update(dt);

                if(wasHeld && held) OnFrozenUpdate?.Invoke(dt);
            }

            public event OnChangeHendler OnChanged;

            public override void OnChange()
            {
                base.OnChange();
                OnChanged?.Invoke();
                //Debug.Log("floatval is " + valueFloat);
            }
        }

        internal delegate void OnButtonSignalHandler();

        internal class EventfulButton : OpSimpleButton
        {
            public EventfulButton(Vector2 pos, Vector2 size, string signal, string text = "") : base(pos, size, signal, text)
            {
            }
            
            public event OnButtonSignalHandler OnSignal;
            public override void Signal()
            {
                base.Signal();
                OnSignal?.Invoke();
            }

            public event OnFrozenUpdateHandler OnFrozenUpdate;
            public override void Update(float dt)
            {
                bool wasHeld = held;
                base.Update(dt);

                if (wasHeld && held) OnFrozenUpdate?.Invoke(dt);
            }

        }

        internal class EventfulImageButton : OpSimpleImageButton
        {
            public EventfulImageButton(Vector2 pos, Vector2 size, string signal, string fAtlasElement) : base(pos, size, signal, fAtlasElement)
            {
            }

            public EventfulImageButton(Vector2 pos, Vector2 size, string signal, Texture2D image) : base(pos, size, signal, image)
            {
            }

            public event OnButtonSignalHandler OnSignal;
            public override void Signal()
            {
                base.Signal();
                OnSignal?.Invoke();
            }


            public event OnFrozenUpdateHandler OnFrozenUpdate;
            public override void Update(float dt)
            {
                bool wasHeld = held;
                base.Update(dt);

                if (wasHeld && held) OnFrozenUpdate?.Invoke(dt);
            }

        }

        internal class EventfulComboBox : OpComboBox
        {
            public EventfulComboBox(Vector2 pos, float width, string key, List<ListItem> list, string defaultName = "") : base(pos, width, key, list, defaultName)
            {
            }

            public EventfulComboBox(Vector2 pos, float width, string key, string[] array, string defaultName = "") : base(pos, width, key, array, defaultName)
            {
            }

            public event OnChangeHendler OnChangeEvent;

            public override void OnChange()
            {
                base.OnChange();
                OnChangeEvent?.Invoke();
            }
        }

        internal class EventfulTextBox : OpTextBox
        {
            public EventfulTextBox(Vector2 pos, float sizeX, string key, string defaultValue = "TEXT", Accept accept = Accept.StringASCII) : base(pos, sizeX, key, defaultValue, accept)
            {
            }

            public event OnChangeHendler OnChangeEvent;
            public override void OnChange()
            {
                base.OnChange();
                OnChangeEvent?.Invoke();
            }
        }
    }
}
