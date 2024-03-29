﻿

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
using System.IO;

namespace LizardSkin
{
    internal class LizardSkinOI : OptionalUI.OptionInterface
    {
        //private static List<LizKinProfileData> lizKinProfiles;
        private LizKinConfiguration configBeingEdited;
        public static LizKinConfiguration configBeingUsed;

        private static bool loadingFromRefresh;
        private static int lastSelectedTab;
        private bool refreshOnNextFrame;

        private ProfileManager activeManager;
        private static LizKinCosmeticData cosmeticOnClipboard;
        private List<LizKinCosmeticData.CosmeticPanel> panelsToRemove;
        private readonly List<LizKinCosmeticData.CosmeticPanel> panelsToAdd;

        public LizardSkinOI() : base(mod: LizardSkin.instance)
        {
            LizardSkin.instanceOI = this;
            panelsToAdd = new List<LizKinCosmeticData.CosmeticPanel>();
            panelsToRemove = new List<LizKinCosmeticData.CosmeticPanel>();
        }

        readonly string modDescription =
@"LizardSkinOI lets you create profiles of cosmetics to be applied on your slugcat. Use the tabs on the left to edit or create profiles.

When on a profile tab, you can select which characters that profile should apply to. If more than one profile applies to a slugcat, all cosmetics found will be applied. Advanced mode lets you specify difficulty, player-number or character-number so that you can get it working with custom slugcats too.

Inside a profile you can add Cosmetics by clicking on the box with a +. Cosmetics can be reordered, copied, pasted, duplicated and deleted. You can also control the base color and effect color for your slugcat to match any custom sprites or skins. For the color override to take effect you must tick the checkbox next to the color picker.

You can pick Cosmetics of several types, edit their settings and configure randomization. When you're done customizing, hit refresh on the preview panel to see what your sluggo looks like :3";

        private OpLabel cglabel1;
        private OpCheckBox cgbox1;
        private OpLabelLong cglabel2;
        private int? jumpToTab;
        private bool isTheStupidRefreshOnChange;

        public override void Initialize()
        {
            base.Initialize();
            Debug.Log("LizardSkinOI Initialize");

            if (ConfigMenu.instance == null || !(bool)typeof(ConfigMenu).GetField("refresh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(ConfigMenu.instance))
            {
                // This needs to run
                // 1; when the config is loaded at launch
                // 2; when opening the configmenu
                // 3; when coming back from a config reset, but not from a refresh
                Debug.Log("LizardSkinOI Init load data");
                LoadLizKinData();
                configBeingEdited = LizKinConfiguration.Clone(configBeingUsed);
            }
            else
            {
                Debug.Log("LizardSkinOI Init skip loading");
                isTheStupidRefreshOnChange = true;
            }
            if (!OptionInterface.isOptionMenu)
            {
                // our settings are too complex and we serialize them on a dedicated file.
                this.Tabs = new OpTab[1] { new OptionalUI.OpTab("Dummy") };

                // this is the only setting that is simple enough for OI to store :forbiddenmonk:
                this.Tabs[0].AddItems(new OpCheckBox(Vector2.zero, "LizardSkinOICGProgressionLocked", true));

                return;
            }

            this.Tabs = new OptionalUI.OpTab[2 + configBeingEdited.profiles.Count];

            // Title and Instructions tab
            Debug.Log("making instructions");
            this.Tabs[0] = new OptionalUI.OpTab("Instructions");
            CompletelyOptional.GeneratedOI.AddBasicProfile(Tabs[0], rwMod);
            Tabs[0].AddItems(new OpLabelLong(new Vector2(50f, 470f), new Vector2(500f, 0f), modDescription, alignment: FLabelAlignment.Center, autoWrap: true),
                new NotAManagerTab(this),
                new ReloadHandler(this));

            Tabs[0].AddItems(cglabel1 = new OpLabel(175f, 100f, "Concealed Garden progression mode:", false),
                cgbox1 = new OpCheckBox(new Vector2(395f, 100f), "LizardSkinOICGProgressionLocked", true)
                {
                    description = "Activate this mod by progressing through Concealed Garden.",
                    greyedOut = !LizardSkin.CGEverBeaten || !LizardSkin.CGIntegration,
                },
                cglabel2 = new OpLabelLong(new Vector2(50f, 70f), new Vector2(500f, 0f),
                 "Activate this mod by progressing through Concealed Garden.\n" +
                 "You can control this option after having done so at least once."
                 , alignment: FLabelAlignment.Center, autoWrap: true));

            if (LizardSkin.CGIntegration && !LizardSkin.CGEverBeaten)
            {
                this.Tabs = new OptionalUI.OpTab[] { Tabs[0] };
                return; // No configs if unbeaten
            }
            // Make profile tabs
            Debug.Log("making tabs");
            for (int i = 0; i < configBeingEdited.profiles.Count; i++)
            {
                Tabs[i + 1] = new OptionalUI.OpTab(configBeingEdited.profiles[i].profileName);
                ProfileManager manager = new ProfileManager(this, configBeingEdited.profiles[i], Tabs[i + 1]);
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

            // I'd love to hide these from the ctor but CM won't let me :/
            // I still need to create the checkbox so the config is stored and not discarted even if the setting isn't relevant atm because the other mod isn't running.
            if (!LizardSkin.CGIntegration && ConfigMenu.tabCtrler.index == 0)
            {
                cglabel1.Hide();
                cgbox1.Hide();
                cglabel2.Hide();
            }
            else if (LizardSkin.CGEverBeaten && ConfigMenu.tabCtrler.index == 0)
            {
                cglabel2.Hide();
            }

            foreach (LizKinCosmeticData.CosmeticPanel panel in panelsToAdd)
            {
                Debug.Log("LizardSkinOI adding panel to manager");
                activeManager.AddPanel(panel);
            }
            panelsToAdd.Clear();

            foreach (LizKinCosmeticData.CosmeticPanel panel in panelsToRemove)
            {
                Debug.Log("LizardSkinOI removing panel");
                activeManager.RemovePanel(panel);
            }
            panelsToRemove.Clear();

            if (refreshOnNextFrame)
            {
                Debug.Log("LizardSkinOI Refreshing");
                lastSelectedTab = ConfigMenu.tabCtrler.index;
                ConfigMenu.RefreshCurrentConfig();
                refreshOnNextFrame = false;
                loadingFromRefresh = true;
            }
            if (jumpToTab.HasValue)
            {
                ConfigMenu.tabCtrler.index = jumpToTab.Value;
                jumpToTab = null;
            }
        }

        public override void Signal(UItrigger trigger, string signal)
        {
            base.Signal(trigger, signal);

            Debug.Log("LizardSkinOI got signal " + signal);

            if (signal == "btnProfileMvUp")
            {
                if (activeManager != null) activeManager.SignalSwitchOut();
                if (configBeingEdited.MoveProfileUp(activeManager.profileData))
                {
                    RequestRefresh();
                    DataChanged();
                    ConfigMenu.tabCtrler.index--;
                    return;
                }
            }else
            if (signal == "btnProfileMvDown")
            {
                if (activeManager != null) activeManager.SignalSwitchOut();
                if (configBeingEdited.MoveProfileDown(activeManager.profileData))
                {
                    RequestRefresh();
                    DataChanged();
                    ConfigMenu.tabCtrler.index++;
                    return;
                }
            }else
            if (signal == "btnProfileDuplicate")
            {
                if (activeManager != null) activeManager.SignalSwitchOut();
                if (configBeingEdited.DuplicateProfile(activeManager.profileData))
                {
                    RequestRefresh();
                    DataChanged();
                    ConfigMenu.tabCtrler.index = Tabs.Length-1;
                    return;
                }
            }else
            if (signal == "btnProfileDelete")
            {
                if (activeManager != null) activeManager.SignalSwitchOut();
                if (configBeingEdited.DeleteProfile(activeManager.profileData))
                {
                    RequestRefresh();
                    DataChanged();
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
                //ConfigMenu.tabCtrler.index = lastSelectedTab;
                jumpToTab = new int?(lastSelectedTab);
            }
            loadingFromRefresh = false;
        }

        // When our data changes so we can singal CM there's stuff to be saved
        internal void DataChanged()
        {
            OptionScript.configChanged = true;
        }


        // CM callback
        public override void ConfigOnChange()
        {
            Debug.Log("LizardSkinOI ConfigOnChange");
            // base.ConfigOnChange(); HAHAHAHAHA no. This resets all cosmetic UIElements and loses my data wtf
            LizardSkin.CGSkipProgression = !bool.Parse(config["LizardSkinOICGProgressionLocked"]);
            if (!isTheStupidRefreshOnChange)// CM never clears ConfigMenu.refresh :( (!(bool)typeof(ConfigMenu).GetField("refresh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(ConfigMenu.instance) && OptionScript.configChanged)
            {
                Debug.Log("LizardSkinOI Conf save data");
                configBeingUsed = LizKinConfiguration.Clone(configBeingEdited);
                SaveLizKinData();
            }
            else
            {
                Debug.Log("LizardSkinOI Conf skip save data");
                isTheStupidRefreshOnChange = false; // Bad refresh once, then it should be the real deal
            }
        }

        private static string GetPath()
        { 
            return string.Concat(OptionMod.directory.FullName, LizardSkin.instanceOI.rwMod.ModID, Path.DirectorySeparatorChar, "lizardskin.json"); 
        }
        internal static void LoadLizKinData()
        {
            Debug.Log("LizardSkinOI LoadLizKinData");
            // read from disk
            string path = GetPath();
            if (File.Exists(path))
            {
                string raw = File.ReadAllText(path);
                configBeingUsed = LizKinConfiguration.MakeFromJson(Json.Deserialize(raw) as Dictionary<string, object>);
            }

            if (configBeingUsed == null || configBeingUsed.profiles.Count == 0)
            {
                configBeingUsed = MakeEmptyLizKinData();
            }
        }

        private static LizKinConfiguration MakeEmptyLizKinData()
        {
            LizKinConfiguration conf = new LizKinConfiguration();
            LizKinProfileData myProfile = new LizKinProfileData();
            myProfile.profileName = "My Profile";
            myProfile.effectColor = new Color(33f / 255f, 245f / 255f, 235f / 255f);

            LizKinCosmeticData myCosmetic = new CosmeticAntennaeData() { profile = myProfile };
            LizKinCosmeticData myCosmetic1 = new CosmeticTailTuftData() { profile = myProfile };

            myProfile.cosmetics.Add(myCosmetic);
            myProfile.cosmetics.Add(myCosmetic1);
            conf.profiles.Add(myProfile);
            return conf;
        }

        internal static void ConfigureForCG()
        {
            Debug.Log("LizardSkin ConfigureForCG");
            if (configBeingUsed == null) return; // Bad call
            bool whiteFound = false, yellowFound = false, redFound = false;
            foreach (var profile in configBeingUsed.profiles)
            {
                if (profile.profileName == "CG - Survivor") whiteFound = true;
                else if (profile.profileName == "CG - Monk") yellowFound = true;
                else if (profile.profileName == "CG - Hunter") redFound = true;
                else
                {
                    profile.appliesToList = new List<int>() { }; // Disable others
                }
            }
            if (!whiteFound)
            {
                LizKinProfileData myProfile = new LizKinProfileData();
                myProfile.profileName = "CG - Survivor";
                myProfile.appliesToList = new List<int>() { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
                myProfile.effectColor = new Color(33f / 255f, 245f / 255f, 235f / 255f);

                LizKinCosmeticData myCosmetic = new CosmeticAntennaeData() { profile = myProfile };
                LizKinCosmeticData myCosmetic1 = new CosmeticTailTuftData() { profile = myProfile };

                myProfile.cosmetics.Add(myCosmetic);
                myProfile.cosmetics.Add(myCosmetic1);
                configBeingUsed.profiles.Add(myProfile);
            }
            if (!yellowFound)
            {
                LizKinProfileData myProfile = new LizKinProfileData();
                myProfile.profileName = "CG - Monk";
                myProfile.appliesToList = new List<int>() { 1 };
                myProfile.effectColor = new Color(250f / 255f, 16f / 255f, 235f / 255f);

                LizKinCosmeticData myCosmetic = new CosmeticAntennaeData() { profile = myProfile };
                LizKinCosmeticData myCosmetic1 = new CosmeticTailTuftData() { profile = myProfile, graphic = 6, scale = 0.66f, thickness = 0.75f, count = 6, roundness = 0.4f };
                LizKinCosmeticData myCosmetic2 = new CosmeticLongShoulderScalesData() { profile = myProfile, graphic = 3, scale = 0.66f, count = 12, roundness = 0.6f, minSize = 0.4f, start = 0.2f };

                myProfile.cosmetics.Add(myCosmetic);
                myProfile.cosmetics.Add(myCosmetic1);
                myProfile.cosmetics.Add(myCosmetic2);
                configBeingUsed.profiles.Add(myProfile);
            }
            if (!redFound)
            {
                LizKinProfileData myProfile = new LizKinProfileData();
                myProfile.profileName = "CG - Hunter";
                myProfile.appliesToList = new List<int>() { 2 };
                myProfile.effectColor = new Color(50f / 255f, 205f / 255f, 50f / 255f);

                LizKinCosmeticData myCosmetic = new CosmeticAntennaeData() { profile = myProfile };
                LizKinCosmeticData myCosmetic1 = new CosmeticTailTuftData() { profile = myProfile, graphic = 2, scale = 1.4f, thickness = 0.6f, count = 8, roundness = 0.6f, length = 0.48f };
                LizKinCosmeticData myCosmetic2 = new CosmeticLongHeadScalesData() { profile = myProfile, graphic = 2, scale = 1.4f, thickness = 0.6f, spinePos = 0.13f };

                myProfile.cosmetics.Add(myCosmetic);
                myProfile.cosmetics.Add(myCosmetic1);
                myProfile.cosmetics.Add(myCosmetic2);
                configBeingUsed.profiles.Add(myProfile);
            }
            SaveLizKinData();
        }

        internal static void SaveLizKinData()
        {
            Debug.Log("LizardSkinOI SaveLizKinData");
            if (configBeingUsed == null) return;
            string path = GetPath();
            File.WriteAllText(path, Json.Serialize(configBeingUsed));
        }

        // Element that handles the new profile tab, triggers a callback on show
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

        // Element for a tab that cannot manage panels, switching makes the previous manager save its data
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

        // Element that triggers a callback once CM is done reloading/refreshing
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

        // Callback on cosmetic managers switing
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
            if (!refreshOnNextFrame && configBeingEdited.AddDefaultProfile())
            {
                RequestRefresh();
                return;
            }
        }

        private void RequestRefresh()
        {
            Debug.Log("LizardSkinOI RequestRefresh");
            if (refreshOnNextFrame) return;
            this.refreshOnNextFrame = true;
            if (activeManager != null) activeManager.SignalSwitchOut();
        }

        private void RequestNewPanel(LizKinCosmeticData.CosmeticPanel panel)
        {
            Debug.Log("LizardSkinOI RequestNewPanel");
            this.panelsToAdd.Add(panel);
        }
        private void RequestPanelRemoval(LizKinCosmeticData.CosmeticPanel panel)
        {
            Debug.Log("LizardSkinOI RequestPanelRemoval");
            this.panelsToRemove.Add(panel);
        }

        // full-tab element that manages a cosmetics profile for an associated profileData
        internal class ProfileManager : UIelement
        {
            internal LizardSkinOI lizardSkinOI;
            internal LizKinProfileData profileData;
            private OpTab opTab;
            private EventfulTextBox nameBox;
            private EventfulResourceSelector appliesToModeSelector;
            private EventfulResourceSelector appliesToSelectorSelector;
            private EventfulCheckBox appliesTo0;
            private OpLabel appliesTo0Label;
            private EventfulCheckBox appliesTo1;
            private OpLabel appliesTo1Label;
            private EventfulCheckBox appliesTo2;
            private OpLabel appliesTo2Label;
            private EventfulCheckBox appliesTo3;
            private OpLabel appliesTo3Label;
            private EventfulTextBox appliesToInput;
            private OpTinyColorPicker effectColorPicker;
            private EventfulCheckBox overrideBaseCkb;
            private OpTinyColorPicker baseColorPicker;
            private EventfulFloatSlider previewRotationSlider;
            private MenuCosmeticsAdaptor cosmeticsPreview;
            private OpScrollBox cosmeticsBox;
            private List<GroupPanel> cosmPanels;
            private AddCosmeticPanelPanel addPanelPanel;
            private bool organizePending;

            public ProfileManager(LizardSkinOI lizardSkinOI, LizKinProfileData lizKinProfileData, OpTab opTab) : base(new Vector2(0, 0), new Vector2(600, 600))
            {
                this.lizardSkinOI = lizardSkinOI;
                this.profileData = lizKinProfileData;
                this.opTab = opTab;

                // Profile management row
                OpSimpleImageButton arrowDown;
                opTab.AddItems(new OpLabel(profileMngmtPos, new Vector2(40, 24), text: "Profile:", alignment: FLabelAlignment.Left),
                    this.nameBox = new EventfulTextBox(profileMngmtPos + new Vector2(50, 0), 100, "", defaultValue: profileData.profileName) { description = "Rename this profile" },
                    new OpSimpleImageButton(profileMngmtPos + new Vector2(160, 0), new Vector2(24, 24), "btnProfileMvUp", "LizKinArrow") { description = "Move this profile up" },
                    arrowDown = new OpSimpleImageButton(profileMngmtPos + new Vector2(190, 0), new Vector2(24, 24), "btnProfileMvDown", "LizKinArrow") { description = "Move this profile down" },
                    new OpSimpleImageButton(profileMngmtPos + new Vector2(220, 0), new Vector2(24, 24), "btnProfileDuplicate", "LizKinDuplicate") { description = "Duplicate this profile" },
                    new OpSimpleImageButton(profileMngmtPos + new Vector2(250, 0), new Vector2(24, 24), "btnProfileDelete", "LizKinDelete") { description = "Delete this profile" }
                    );
                arrowDown.sprite.scaleY *= -1;
                this.nameBox.OnValueChangedEvent += NameBox_OnValueChanged;

                // Filters
                opTab.AddItems(new OpLabel(characterMngmtPos + new Vector2(0, 0), new Vector2(40, 24), text: "Applies to:", alignment: FLabelAlignment.Left),
                    appliesToModeSelector = new EventfulResourceSelector(characterMngmtPos + new Vector2(40, 30), 110, "", typeof(LizKinProfileData.ProfileAppliesToMode), profileData.appliesToMode.ToString()) { description = "How complicated should the selection filter be..." },
                    appliesToSelectorSelector = new EventfulResourceSelector(characterMngmtPos + new Vector2(80, 0), 100, "", typeof(LizKinProfileData.ProfileAppliesToSelector), profileData.appliesToSelector.ToString()) { description = "Filter by Difficulty (story-mode), Character (arena) or Player-number..." },

                    appliesTo0 = new EventfulCheckBox(characterMngmtPos + new Vector2(0, -30), ""),
                    appliesTo0Label = new OpLabel(characterMngmtPos + new Vector2(40, -30), new Vector2(40, 24), text: "Survivor", alignment: FLabelAlignment.Left),
                    appliesTo1 = new EventfulCheckBox(characterMngmtPos + new Vector2(100, -30), ""),
                    appliesTo1Label = new OpLabel(characterMngmtPos + new Vector2(140, -30), new Vector2(40, 24), text: "Monk", alignment: FLabelAlignment.Left),
                    appliesTo2 = new EventfulCheckBox(characterMngmtPos + new Vector2(0, -60), ""),
                    appliesTo2Label = new OpLabel(characterMngmtPos + new Vector2(40, -60), new Vector2(40, 24), text: "Hunter", alignment: FLabelAlignment.Left),
                    appliesTo3 = new EventfulCheckBox(characterMngmtPos + new Vector2(100, -60), ""),
                    appliesTo3Label = new OpLabel(characterMngmtPos + new Vector2(140, -60), new Vector2(40, 24), text: "Nightcat", alignment: FLabelAlignment.Left),

                    appliesToInput = new EventfulTextBox(characterMngmtPos + new Vector2(10, -30), 160, "", defaultValue: "-1") { allowSpace = true, description = "Which indexes to apply to, comma-separated, everything being zero-indexed, or -1 for all" }
                    );

                appliesToModeSelector.OnValueChangedEvent += FiltersSelector_OnValueChangedEvent;
                appliesToSelectorSelector.OnValueChangedEvent += FiltersSelector_OnValueChangedEvent;

                appliesTo0.OnValueChangedEvent += FiltersValues_OnValueChangedEvent;
                appliesTo1.OnValueChangedEvent += FiltersValues_OnValueChangedEvent;
                appliesTo2.OnValueChangedEvent += FiltersValues_OnValueChangedEvent;
                appliesTo3.OnValueChangedEvent += FiltersValues_OnValueChangedEvent;
                appliesToInput.OnValueChangedEvent += FiltersValues_OnValueChangedEvent;

                FiltersConformToConfig();

                // Colors
                opTab.AddItems(new OpLabel(colorMngmtPos + new Vector2(80, 0), new Vector2(40, 24), text: "Effect Color:", alignment: FLabelAlignment.Right),
                new OpLabel(colorMngmtPos + new Vector2(80, -30), new Vector2(40, 24), text: "Override Base Color:", alignment: FLabelAlignment.Right),
                this.overrideBaseCkb = new EventfulCheckBox(colorMngmtPos + new Vector2(130, -30), "", profileData.overrideBaseColor) { description = "Use a different Base Color than the slugcat's default color" });
                
                // see iHaveChildren
                this.effectColorPicker = new OpTinyColorPicker(colorMngmtPos + new Vector2(130, 0), "", OpColorPicker.ColorToHex(profileData.effectColor)) { description = "Pick the Effect Color for the highlights" };
                effectColorPicker.AddSelfAndChildrenToTab(opTab);
                this.baseColorPicker = new OpTinyColorPicker(colorMngmtPos + new Vector2(160, -30), "", OpColorPicker.ColorToHex(profileData.baseColorOverride)) { description = "Pick the Base Color for the cosmetics" };
                baseColorPicker.AddSelfAndChildrenToTab(opTab);

                overrideBaseCkb.OnValueChangedEvent += ColorStuff_OnChanged;
                effectColorPicker.OnValueChangedEvent += ColorStuff_OnChanged;
                effectColorPicker.OnFrozenUpdate += KeepPreviewUpdated;
                baseColorPicker.OnValueChangedEvent += ColorStuff_OnChanged;
                baseColorPicker.OnFrozenUpdate += KeepPreviewUpdated;

                // Preview pannel 
                EventfulImageButton refreshBtn;
                opTab.AddItems(
                    new OpRect(previewPanelPos + new Vector2(0, -420), new Vector2(220, 420)),
                    cosmeticsPreview = new MenuCosmeticsAdaptor(previewPanelPos + new Vector2(110, -100), profileData),
                    previewRotationSlider = new EventfulFloatSlider(previewPanelPos + new Vector2(30, -45), "", new Vector2(-1, 1), 160),
                    refreshBtn = new EventfulImageButton(previewPanelPos + new Vector2(5, -29), new Vector2(24,24), "", "LizKinReload")
                    );

                previewRotationSlider.OnValueChangedEvent += PreviewRotationSlider_OnChanged;
                previewRotationSlider.OnFrozenUpdate += KeepPreviewUpdated;

                refreshBtn.OnSignal += RefreshPreview;
                refreshBtn.OnFrozenUpdate += KeepPreviewUpdated;

                // Cosmetics Panenl
                //Debug.Log("Cosmetic panel start");
                cosmPanels = new List<GroupPanel>();
                //Debug.Log("Cosmetic box make");
                cosmeticsBox = new OpScrollBox(cosmeticsPanelPos, new Vector2(370, 540), 0, hasSlideBar: false);
                //Debug.Log("Cosmetic box add");
                opTab.AddItems(cosmeticsBox);

                //Debug.Log("cosmeticsBox.contentSize is " + cosmeticsBox.GetContentSize());

                //Debug.Log("add pannel make");

                addPanelPanel = new AddCosmeticPanelPanel(new Vector2(5, 0), 360f);
                //Debug.Log("add pannel add");

                addPanelPanel.AddSelfAndChildrenToScroll(cosmeticsBox);

                addPanelPanel.OnAdd += AddPanelPanel_OnAdd;
                addPanelPanel.OnPaste += AddPanelPanel_OnPaste;

                //Debug.Log("make pannels");

                MakeCosmEditPannels();

                //OrganizePannels();
                this.organizePending = true;

                //Debug.Log("ProfileTabManager done");
            }

            private void FiltersValues_OnValueChangedEvent()
            {
                FiltersGrabConfig();
            }

            private void FiltersSelector_OnValueChangedEvent()
            {
                FiltersGrabConfig();
                FiltersConformToConfig();
            }

            Vector2 profileMngmtPos => new Vector2(15, 570);
            Vector2 characterMngmtPos => new Vector2(380, 540);
            Vector2 colorMngmtPos => new Vector2(380, 450);
            Vector2 previewPanelPos => new Vector2(380, 420);
            Vector2 cosmeticsPanelPos => new Vector2(0, 0);

            private void NameBox_OnValueChanged()
            {
                profileData.profileName = nameBox.value;
                lizardSkinOI.DataChanged();
            }

            private void AddPanelPanel_OnAdd()
            {
                profileData.AddEmptyCosmetic();
                lizardSkinOI.DataChanged();
                LizKinCosmeticData.CosmeticPanel panel = profileData.cosmetics[profileData.cosmetics.Count - 1].MakeEditPanel(this);
                cosmeticsPreview.Reset();
                cosmPanels.Add(panel);
                lizardSkinOI.RequestNewPanel(panel);
            }

            private void AddPanelPanel_OnPaste()
            {
                if(LizardSkinOI.cosmeticOnClipboard != null ) DuplicateCosmetic(LizardSkinOI.cosmeticOnClipboard);
            }

            internal void SetClipboard(LizKinCosmeticData data)
            {
                LizardSkinOI.cosmeticOnClipboard = LizKinCosmeticData.Clone(data); // release profile ref, detach from original
            }

            internal void DuplicateCosmetic(LizKinCosmeticData data)
            {
                profileData.cosmetics.Add(LizKinCosmeticData.Clone(data));
                profileData.cosmetics[profileData.cosmetics.Count - 1].profile = profileData;
                lizardSkinOI.DataChanged();
                cosmeticsPreview.Reset();
                LizKinCosmeticData.CosmeticPanel panel = profileData.cosmetics[profileData.cosmetics.Count - 1].MakeEditPanel(this);
                cosmPanels.Add(panel);
                lizardSkinOI.RequestNewPanel(panel);
            }

            internal void DeleteCosmetic(LizKinCosmeticData.CosmeticPanel panel)
            {
                profileData.cosmetics.Remove(panel.data);
                lizardSkinOI.DataChanged();
                panel.data.profile = null;
                cosmeticsPreview.Reset();
                cosmPanels.Remove(panel);
                lizardSkinOI.RequestPanelRemoval(panel);
            }

            internal void MakeCosmEditPannels()
            {
                Debug.Log("LizardSkinOI MakeCosmEditPannels");
                foreach (LizKinCosmeticData cosmeticData in profileData.cosmetics)
                {
                    //Debug.Log("Makin new panel");

                    LizKinCosmeticData.CosmeticPanel panel = cosmeticData.MakeEditPanel(this);
                    //CosmeticEditPanel editPanel = new CosmeticEditPanel(new Vector2(5, 0), new Vector2(360, 100));Moving child element
                    cosmPanels.Add(panel);
                    panel.AddSelfAndChildrenToScroll(cosmeticsBox);
                }
                //Debug.Log("MakeCosmEditPannels done");
            }

            internal void OrganizePannels()
            {
                Debug.Log("LizardSkinOI OrganizePannels");
                float cosmEditPanelMarginVert = 6;
                float totalheight = cosmEditPanelMarginVert + addPanelPanel.size.y;
                foreach (LizKinCosmeticData.CosmeticPanel panel in cosmPanels)
                {
                    totalheight += panel.size.y + cosmEditPanelMarginVert;
                }

                // these two loops could happen at once.
                totalheight = Mathf.Max(totalheight, cosmeticsBox.GetContentSize());

                if (totalheight != cosmeticsBox.GetContentSize()) cosmeticsBox.SetContentSize(totalheight);


                float topleftpos = cosmEditPanelMarginVert/2;
                foreach (LizKinCosmeticData.CosmeticPanel panel in cosmPanels)
                {
                    
                    panel.topLeft = new Vector2(3, totalheight - topleftpos);
                    // Debug.Log("Moved panel to " + (totalheight - topleftpos));
                    topleftpos += panel.size.y + cosmEditPanelMarginVert;
                }

                // Debug.Log("Moved add panel to " + (totalheight - topleftpos));
                addPanelPanel.topLeft = new Vector2(3, totalheight - topleftpos);

                //Debug.Log("OrganizePannels done");
            }


            internal void ChangeCosmeticType(LizKinCosmeticData.CosmeticPanel panel, LizKinCosmeticData.CosmeticInstanceType newType)
            {
                Debug.Log("LizardSkinOI ChangeCosmeticType");
                LizKinCosmeticData newCosmetic = LizKinCosmeticData.MakeCosmeticOfType(newType);
                newCosmetic.ReadFromOther(panel.data);
                newCosmetic.profile = profileData;

                profileData.cosmetics[profileData.cosmetics.IndexOf(panel.data)] = newCosmetic;
                panel.data.profile = null; // Cosmetic is dead
                cosmeticsPreview.Reset(); // Preview cannot use dead cosms
                lizardSkinOI.DataChanged();

                LizKinCosmeticData.CosmeticPanel newPanel = newCosmetic.MakeEditPanel(this);
                cosmPanels[cosmPanels.IndexOf(panel)] = newPanel;
                lizardSkinOI.RequestPanelRemoval(panel);
                lizardSkinOI.RequestNewPanel(newPanel);
            }

            // Called from OI
            internal void AddPanel(LizKinCosmeticData.CosmeticPanel panel)
            {
                panel.AddSelfAndChildrenToScroll(cosmeticsBox);
                OrganizePannels();
            }
            // Called from OI
            internal void RemovePanel(LizKinCosmeticData.CosmeticPanel panel)
            {
                panel.DestroySelfAndChildren();
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

            internal void RefreshPreview()
            {
                cosmeticsPreview.Reset();
            }

            internal void KeepPreviewUpdated(float dt)
            {
                cosmeticsPreview.Update(dt);
            }

            private void PreviewRotationSlider_OnChanged()
            {
                cosmeticsPreview.SetRotation(previewRotationSlider.valueFloat);
            }

            private void ColorStuff_OnChanged()
            {
                profileData.effectColor = effectColorPicker.valuecolor;
                profileData.overrideBaseColor = overrideBaseCkb.valueBool;
                profileData.baseColorOverride = baseColorPicker.valuecolor;
                lizardSkinOI.DataChanged();
            }

            public override void Update(float dt)
            {
                if (organizePending)
                {
                    OrganizePannels();
                    organizePending = false;
                }

                base.Update(dt);

                //if (profileData.appliesToMode != (LizKinProfileData.ProfileAppliesToMode)Enum.Parse(typeof(LizKinProfileData.ProfileAppliesToMode), appliesToModeSelector.value)
                //    || profileData.appliesToSelector != (LizKinProfileData.ProfileAppliesToSelector)Enum.Parse(typeof(LizKinProfileData.ProfileAppliesToSelector), appliesToSelectorSelector.value))
                //{
                //    // config change
                //    FiltersGrabConfig();
                //    // might need layout change
                //    FiltersConformToConfig();
                //    lizardSkinOI.DataChanged();
                //} else FiltersGrabConfig();

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
                if (opTab.name != profileData.profileName)
                {
                    lizardSkinOI.RequestRefresh();
                }
            }

            internal void Signal(UItrigger trigger, string signal)
            {
                throw new NotImplementedException("Cannot handle signal: " + signal);
            }

            private void FiltersGrabConfig()
            {
                Debug.Log("FiltersGrabConfig");
                //Debug.Log("profileData.appliesToList was " + String.Join(", ", profileData.appliesToList.Select(n => n.ToString()).ToArray()));
                LizKinProfileData.ProfileAppliesToMode previousMode = profileData.appliesToMode;
                LizKinProfileData.ProfileAppliesToMode newMode = (LizKinProfileData.ProfileAppliesToMode)Enum.Parse(typeof(LizKinProfileData.ProfileAppliesToMode), appliesToModeSelector.value);
                profileData.appliesToMode = newMode;
                profileData.appliesToSelector = (LizKinProfileData.ProfileAppliesToSelector)Enum.Parse(typeof(LizKinProfileData.ProfileAppliesToSelector), appliesToSelectorSelector.value);

                List<int> previousList = profileData.appliesToList;
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
                            if (int.TryParse(rawsingle.Trim(), out int myint)) profileData.appliesToList.Add(myint);
                        }

                        break;
                }
                //Debug.Log("profileData.appliesToList now is  " + String.Join(", ", profileData.appliesToList.Select(n => n.ToString()).ToArray()));
                if(previousMode != newMode || String.Join(", ", previousList.Select(n => n.ToString()).ToArray()) != String.Join(", ", profileData.appliesToList.Select(n => n.ToString()).ToArray()) ) lizardSkinOI.DataChanged(); // This could probably be optimized
            }


            private void FiltersConformToConfig()
            {
                Debug.Log("FiltersConformToConfig");
                //Debug.Log("profileData.appliesToList was " + String.Join(", ", profileData.appliesToList.Select(n => n.ToString()).ToArray()));
                LizKinProfileData.ProfileAppliesToMode currentMode = profileData.appliesToMode;
                appliesTo0._newvalue = (profileData.appliesToList.Contains(-1) || profileData.appliesToList.Contains(0)) ? "true" : "false";
                appliesTo1._newvalue = (profileData.appliesToList.Contains(-1) || profileData.appliesToList.Contains(1)) ? "true" : "false";
                appliesTo2._newvalue = (profileData.appliesToList.Contains(-1) || profileData.appliesToList.Contains(2)) ? "true" : "false";
                appliesTo3._newvalue = (profileData.appliesToList.Contains(-1) || profileData.appliesToList.Contains(3)) ? "true" : "false";
                appliesTo0.OnChange();
                appliesTo1.OnChange();
                appliesTo2.OnChange();
                appliesTo3.OnChange();
                appliesToInput._newvalue = String.Join(", ", profileData.appliesToList.Select(n => n.ToString()).ToArray());
                appliesToInput.OnChange();
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
                        appliesToSelectorSelector._newvalue = LizKinProfileData.ProfileAppliesToSelector.Character.ToString();
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
        }

        internal interface IHaveChildren
        {
            void AddSelfAndChildrenToTab(OpTab tab);
            void AddSelfAndChildrenToScroll(OpScrollBox scroll);
            //void RemoveSelfAndChildrenFromTab(OpTab tab);
            //void RemoveSelfAndChildrenFromScroll(OpScrollBox scroll);
            void DestroySelfAndChildren();

        }

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

            public void DestroySelfAndChildren()
            {
                foreach (UIelement child in children)
                {
                    if (child is IHaveChildren) (child as IHaveChildren).DestroySelfAndChildren();
                    else OpTab.DestroyItems(child);
                }
                OpTab.DestroyItems(this);
            }

            public override void OnChange()
            {

                base.OnChange();
                if (originalPositions.Count == 0) return; // called from ctor before initialized
                for (int i = 0; i < children.Count; i++)
                {
                    children[i].pos = topLeft + originalPositions[i];
                    //Debug.Log("Moving child element to " + (topLeft + originalPositions[i]));
                }
            }

            public Vector2 topLeft { get { return GetPos() + new Vector2(0, size.y); } set { pos = value - new Vector2(0, size.y); OnChange(); } } // Setting this should call onchange and move children
        }

        internal delegate void OnValueChangedHandler();
        internal delegate void OnButtonSignalHandler();
        public delegate void OnFrozenUpdateHandler(float dt);

        internal class EventfulButton : OpSimpleButton
        {
            public EventfulButton(Vector2 pos, Vector2 size, string signal, string text = "") : base(pos, size, signal, text)
            {
            }
            
            public event OnButtonSignalHandler OnSignal;
            public override void Signal()
            {
                if (!string.IsNullOrEmpty(signal)) base.Signal();
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
                if(!string.IsNullOrEmpty(signal)) base.Signal();
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

        class FlatColorPicker : OpColorPicker
        {
            private FlatColorPicker(Vector2 pos, string key, string defaultHex = "FFFFFF") : base(pos, key, defaultHex) { }

            public override void OnChange()
            {
                Vector2 oldPos = this._pos;
                this._pos = Vector2.zero;
                base.OnChange();
                this._pos = oldPos;
                if (!ctor || !_init) { return; }
                this.myContainer.SetPosition(this.ScreenPos);
            }

            public static OpColorPicker MakeFlatColorpicker(Vector2 pos, string key, string defaultHex = "FFFFFF")
            {
                FContainer container = new FContainer();
                FContainer pgctr = OptionScript.configMenu.pages[0].Container;
                OptionScript.configMenu.pages[0].Container = container;
                FlatColorPicker pkr = new FlatColorPicker(pos, key, defaultHex);
                OptionScript.configMenu.pages[0].Container = pgctr;
                FContainer pfkcontainer = (FContainer)typeof(OpColorPicker).GetField("myContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(pkr);
                pfkcontainer.AddChildAtIndex(container, 0);
                return pkr;
            }
        }

        internal class OpTinyColorPicker : EventfulButton, IHaveChildren
        {
            private OpColorPicker colorPicker;
            private bool currentlyPicking;
            const float mouseTimeout = 0.5f;
            float mouseOutCounter;

            public OpTinyColorPicker(Vector2 pos, string signal, string defaultHex) : base(pos, new Vector2(24, 24), signal)
            {
                //this.colorPicker = new OpColorPicker(pos + new Vector2(-60, 24), "", defaultHex);
                this.colorPicker = FlatColorPicker.MakeFlatColorpicker(pos + new Vector2(-60, 24), "", defaultHex);

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
                //scroll.AddItems(this,
                //    colorPicker);
                scroll.AddItems(this);
                this.tab.AddItems(colorPicker);
                this.colorPicker.Hide();
            }

            public void DestroySelfAndChildren()
            {
                OpTab.DestroyItems(colorPicker, this);
            }

            public override void Signal()
            {
                // base.Signal();
                if (!currentlyPicking)
                {
                    //this.colorPicker.pos = (this.inScrollBox ? (this.GetPos() + scrollBox.GetPos()) : this.GetPos()) + new Vector2(-60, 24);
                    this.colorPicker.pos = (this.inScrollBox ? (this.GetPos() + scrollBox.GetPos() + new Vector2(0f, scrollBox.ScrollOffset)) : this.GetPos()) + new Vector2(-60, 24);
                    colorPicker.Show();
                    currentlyPicking = true;
                }
                else
                {
                    currentlyPicking = false;
                    colorFill = colorPicker.valueColor;
                    OnValueChangedEvent?.Invoke();
                    colorPicker.Hide();
                    //if (!string.IsNullOrEmpty(signal)) base.Signal(); // uses eventful now, has to trigger event
                    base.Signal();
                }
            }

            public event OnValueChangedHandler OnValueChangedEvent;

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
                    OnValueChangedEvent?.Invoke();
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

            public event OnValueChangedHandler OnValueChangedEvent;
            public override string value
            {
                set
                {
                    bool change = value != _value;
                    base.value = value;
                    if (change) OnValueChangedEvent?.Invoke();
                }
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

            public event OnValueChangedHandler OnValueChangedEvent;
            public override string value
            {
                set
                {
                    bool change = value != _value;
                    base.value = value;
                    if (change) OnValueChangedEvent?.Invoke();
                }
            }
        }

        internal class EventfulTextBox : OpTextBox
        {
            //public EventfulTextBox(Vector2 pos, float sizeX, string key, string defaultValue = "TEXT", Accept accept = Accept.StringASCII) : base(pos, sizeX, key, defaultValue, accept)
            public EventfulTextBox(Vector2 pos, float sizeX, string key, string defaultValue = "TEXT") : base(pos, sizeX, key, "")
            {
                // Attempt at fixing bug that happens when defaultvalue gets trimmed in the ctor, causes nullref in CM 1454?
                this._value = defaultValue;
                //this._lastValue = defaultValue;
                typeof(EventfulTextBox).GetField("_lastValue", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this, defaultValue);
                this.defaultValue = defaultValue;
                OnChange();
            }

            public event OnValueChangedHandler OnValueChangedEvent;
            public override string value
            {
                set
                {
                    bool change = value != _value;
                    base.value = value;
                    if (change) OnValueChangedEvent?.Invoke();
                }
            }

            public string _newvalue
            {
                set
                {
                    _value = value;
                }
            }

            public event OnFrozenUpdateHandler OnFrozenUpdate;
            public override void Update(float dt)
            {
                bool wasHeld = held;
                string preupvalue = value;
                base.Update(dt);
                if (value != preupvalue) OnValueChangedEvent?.Invoke();
                if (wasHeld && held) OnFrozenUpdate?.Invoke(dt);
            }
        }

        internal class EventfulCheckBox : OpCheckBox
        {
            public EventfulCheckBox(Vector2 pos, string key, bool defaultBool = false) : base(pos, key, defaultBool)
            {
            }

            public EventfulCheckBox(float posX, float posY, string key, bool defaultBool = false) : base(posX, posY, key, defaultBool)
            {
            }

            public event OnValueChangedHandler OnValueChangedEvent;
            public override string value
            {
                set
                {
                    bool change = value != _value;
                    base.value = value;
                    if (change) OnValueChangedEvent?.Invoke();
                }
            }

            public string _newvalue
            {
                set
                {
                    _value = value;
                }
            }
        }

        internal class EventfulUpdown : OpUpdown
        {
            public EventfulUpdown(Vector2 pos, float sizeX, string key, int defaultInt) : base(pos, sizeX, key, defaultInt)
            {
            }

            public EventfulUpdown(Vector2 pos, float sizeX, string key, float defaultFloat, byte decimalNum = 1) : base(pos, sizeX, key, defaultFloat, decimalNum)
            {
            }

            public event OnValueChangedHandler OnValueChangedEvent;
            public override string value
            {
                set
                {
                    bool change = value != _value;
                    base.value = value;
                    if (change && _value == value &&
                        !(bool)typeof(OpTextBox).GetField("_keyboardOn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).GetValue(this)
                        ) OnValueChangedEvent?.Invoke();
                }
            }

            public event OnFrozenUpdateHandler OnFrozenUpdate;
            public override void Update(float dt)
            {
                bool wasHeld = held;
                base.Update(dt);

                if (wasHeld && held) OnFrozenUpdate?.Invoke(dt);
            }
        }

        internal class EventfulResourceSelector : OpResourceSelector
        {
            public EventfulResourceSelector(Vector2 pos, float width, string key, Type enumType, string defaultName = "") : base(pos, width, key, enumType, defaultName)
            {

            }

            public EventfulResourceSelector(Vector2 pos, float width, string key, SpecialEnum listType, string defaultName = "") : base(pos, width, key, listType, defaultName)
            {
            }

            public event OnValueChangedHandler OnValueChangedEvent;
            public override string value
            {
                set
                {
                    bool change = value != _value;
                    base.value = value;
                    if (change) OnValueChangedEvent?.Invoke();
                }
            }

            public string _newvalue
            {
                set
                {
                    _value = value;
                }
            }
        }
    }
}
