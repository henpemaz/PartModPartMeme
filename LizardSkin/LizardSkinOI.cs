

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

        private ProfileTabManager activeManager;

        public LizardSkinOI() : base(mod: LizardSkin.instance) { }

        const string modDescription =
@"LizardSkin lets you create profiles of cosmetics to be applied on your slugcat. Use the tabs on the left to edit or create profiles.

When on a profile tab, you can select which characters that profile should apply to. If more than one profile applies to a slugcat, all cosmetics found will be applied. Advanced mode lets you specify difficulty, player-number or character-number so that you can get it working with custom slugcats too.

Inside a profile you can add Cosmetics by clicking on the box with a +. Cosmetics can be reordered, copied, pasted, duplicated and deleted. You can also control the base color and effect color for your slugcat to match any custom sprites or skins.

You can pick Cosmetics of several types, edit their settings and configure randomization. When you're done customizing, hit refresh on the preview panel to see what your sluggo looks like :3";


        public override void Initialize()
        {
            base.Initialize();

            LoadLizKinData();

            if (!OptionInterface.isOptionMenu)
            {
                this.Tabs = new OpTab[1] { new OptionalUI.OpTab("Dummy") };
                return;
            }

            this.Tabs = new OptionalUI.OpTab[2 + configuration.profiles.Count];

            // Title and Instructions tab
            this.Tabs[0] = new OptionalUI.OpTab("Instructions");
            CompletelyOptional.GeneratedOI.AddBasicProfile(Tabs[0], rwMod);
            Tabs[0].AddItems(new OpLabelLong(new Vector2(50f, 470f), new Vector2(500f, 0f), modDescription, alignment: FLabelAlignment.Center, autoWrap: true), 
                new NotAManagerTab(this),
                new ReloadHandler(this));

            // detect Concealed Garden

            // ????

            ////

            // Make profile tabs
            for (int i = 0; i < configuration.profiles.Count; i++)
            {
                Tabs[i + 1] = new OptionalUI.OpTab(configuration.profiles[i].profileName);
                ProfileTabManager manager = new ProfileTabManager(this, configuration.profiles[i], Tabs[i + 1]);
                Tabs[i + 1].AddItems(manager);
            }

            // Make Add Profile tab
            Tabs[Tabs.Length-1] = new OptionalUI.OpTab("+");
            Tabs[Tabs.Length - 1].AddItems(new NewProfileHandler(this), new NotAManagerTab(this));

        }

        public override void Update(float dt)
        {
            base.Update(dt);
            if (refreshOnNextFrame)
            {
                lastSelectedTab = ConfigMenu.tabCtrler.index;
                ConfigMenu.RefreshCurrentConfig();
                refreshOnNextFrame = false;
                loadingFromRefresh = true;
            }
        }

        public override void ConfigOnChange()
        {
            base.ConfigOnChange();
            Debug.Log("LizardSkinOI configchanged!!!!");
        }

        public override void Signal(UItrigger trigger, string signal)
        {
            base.Signal(trigger, signal);

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
            if (loadingFromRefresh && lastSelectedTab < Tabs.Length - 1)
            {
                ConfigMenu.tabCtrler.index = lastSelectedTab;
            }

            loadingFromRefresh = false;
        }

        private void LoadLizKinData()
        {
            
            // read from disk


            // tbd

            if(configuration == null || configuration.profiles.Count == 0)
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

        

        private void SwitchActiveManager(ProfileTabManager profileTabManager)
        {
            if (activeManager != null)
            {
                activeManager.SignalSwitchOut();
            }
                
            activeManager = profileTabManager;

        }

        private void RequestNewProfile()
        {
            // Ignore new profile if about to reset, moving around tabs and such
            if (!refreshOnNextFrame && configuration.AddDefaultProfile())
            {
                RequestRefresh();
                return;
            }
        }

        private void RequestRefresh()
        {
            if (refreshOnNextFrame) return;
            if (activeManager != null) activeManager.SignalSwitchOut();
            this.refreshOnNextFrame = true;
        }

        private class ProfileTabManager : UIelement
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
            private List<CosmeticEditPanel> cosmPanels;
            private AddCosmeticPanelPanel addPanelPanel;

            public ProfileTabManager(LizardSkinOI lizardSkinOI, LizKinProfileData lizKinProfileData, OpTab opTab) : base(new Vector2(0, 0), new Vector2(600, 600))
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
                this.effectColorPicker = new OpTinyColorPicker(colorMngmtPos + new Vector2(130, 0), "btnProfileEffectColor", opTab, OpColorPicker.ColorToHex(profileData.effectColor)) { description = "Pick the Effect Color for the highlights" },
                new OpLabel(colorMngmtPos + new Vector2(80, -30), new Vector2(40, 24), text: "Override Base Color:", alignment: FLabelAlignment.Right),
                this.overrideBaseCkb = new OpCheckBox(colorMngmtPos + new Vector2(130, -30), "", profileData.overrideBaseColor) { description = "Use a different Base Color than the slugcat's default color" },
                this.baseColorPicker = new OpTinyColorPicker(colorMngmtPos + new Vector2(160, -30), "btnProfileBaseColor", opTab, OpColorPicker.ColorToHex(profileData.baseColorOverride)) { description = "Pick the Base Color for the cosmetics" }
                );

                effectColorPicker.OnFrozenUpdate += ColorPicker_OnFrozenUpdate;
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
                cosmPanels = new List<CosmeticEditPanel>();
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

                Debug.Log("make pannels");

                MakeCosmEditPannels();
            }

            private void AddPanelPanel_OnAdd(OpSimpleButton button)
            {
                profileData.AddEmptyCosmetic();
                MakeCosmEditPannels();
            }

            void MakeCosmEditPannels()
            {
                foreach (CosmeticEditPanel panel in cosmPanels)
                {
                    panel.RemoveSelfAndChildrenFromScroll(cosmeticsBox);
                    // Unload maybe ?
                }

                float cosmEditPanelMarginVert = 6;
                float totalheight = cosmEditPanelMarginVert + addPanelPanel.size.y;
                
                foreach (LizKinCosmeticData cosmeticData in profileData.cosmetics)
                {
                    //CosmeticEditPanel editPanel = cosmeticData.MakeEditPanel(this, 360f);
                    CosmeticEditPanel editPanel = new CosmeticEditPanel(new Vector2(5, 0), new Vector2(360, 100));
                    cosmPanels.Add(editPanel);
                    totalheight += editPanel.size.y + cosmEditPanelMarginVert;
                }

                // these two loops could happen at once.

                totalheight = Mathf.Max(totalheight, cosmeticsBox.GetContentSize());
                if (totalheight != cosmeticsBox.GetContentSize()) cosmeticsBox.SetContentSize(totalheight);

                float topleftpos = cosmEditPanelMarginVert/2;
                foreach (CosmeticEditPanel panel in cosmPanels)
                {
                    panel.topLeft = new Vector2(3, totalheight - topleftpos);
                    Debug.Log("Moved panel to " + (totalheight - topleftpos));
                    topleftpos += panel.size.y + cosmEditPanelMarginVert;
                    panel.AddSelfAndChildrenToScroll(cosmeticsBox);
                }

                Debug.Log("Moved add panel to " + (totalheight - topleftpos));
                addPanelPanel.topLeft = new Vector2(3, totalheight - topleftpos);

            }

            internal class GroupPanel : OpRect
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
                    if (children.Count > 0) tab.AddItems(children.ToArray());
                }
                public void AddSelfAndChildrenToScroll(OpScrollBox scroll)
                {
                    foreach (UIelement child in children)
                    {
                        originalPositions.Add(child.GetPos());
                        //child.pos += topLeft; // calls on change anyways
                    }
                    scroll.AddItems(this);
                    if (children.Count > 0) scroll.AddItems(children.ToArray());
                }
                public void RemoveSelfAndChildrenFromTab(OpTab tab)
                {
                    tab.RemoveItems(this);
                    tab.RemoveItems(children.ToArray());
                    children.Clear();
                }
                public void RemoveSelfAndChildrenFromScroll(OpScrollBox scroll)
                {
                    OpScrollBox.RemoveItemsFromScrollBox(this);
                    OpScrollBox.RemoveItemsFromScrollBox(children.ToArray());
                    children.Clear();
                }

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

                public Vector2 topLeft { get { return GetPos() + new Vector2(0, size.y); } set { pos = value - new Vector2(0, size.y); } } // Setting this should call onchange and move children
            }

            internal class CosmeticEditPanel : GroupPanel
            {
                public CosmeticEditPanel(Vector2 pos, Vector2 size) : base(pos, size)
                { 
                // Nothing yet, patience
                }
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

            private void RefreshBtn_OnSignal(OpSimpleButton button)
            {
                cosmeticsPreview.Reset();
            }

            private void FreezingButtons_OnFrozenUpdate(float dt)
            {
                cosmeticsPreview.Update(dt);
            }

            private void PreviewRotationSlider_OnChanged(EventfulFloatSlider slider)
            {
                cosmeticsPreview.SetRotation(slider.valueFloat);
            }

            private void ColorPicker_OnFrozenUpdate(float dt)
            {
                profileData.effectColor = effectColorPicker.valuecolor;
                profileData.overrideBaseColor = overrideBaseCkb.valueBool;
                profileData.baseColorOverride = baseColorPicker.valuecolor;
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

            public override void Show()
            {
                base.Show();
                lizardSkinOI.SwitchActiveManager(this);

                // Because OI unhid my hidden elements smh
                FiltersConformToConfig();
            }

            internal void SignalSwitchOut()
            {
                bool needsRefresh = false;
                // Save data
                if(nameBox.value != profileData.profileName)
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
        }

        public delegate void OnFrozenUpdateHandler(float dt);
        class OpTinyColorPicker : OpSimpleButton
        {
            private OpColorPicker colorPicker;
            private bool currentlyPicking;
            const float mouseTimeout = 0.5f;
            float mouseOutCounter;

            public OpTinyColorPicker(Vector2 pos, string signal, OpTab opTab, string defaultHex) : base(pos, new Vector2(24, 24), signal)
            {
                opTab.AddItems(this,
                    this.colorPicker = new OpColorPicker(pos + new Vector2(-60, 24), "", defaultHex));

                this.colorPicker.Hide();

                this.currentlyPicking = false;

                //this.sprite.scale = 20;
                this.colorFill = colorPicker.valueColor;
                this.rect.fillAlpha = 1f;
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
                    colorPicker.Hide();
                    base.Signal();
                }
            }

            public Color valuecolor => colorPicker.valueColor;

            public event OnFrozenUpdateHandler OnFrozenUpdate;

            public override void Update(float dt)
            {
                // we do a little tricking
                if (currentlyPicking && !this.MouseOver) this.held = false;
                base.Update(dt);
                if (currentlyPicking && !this.MouseOver)
                {
                    colorPicker.Update(dt);
                    OnFrozenUpdate.Invoke(dt);
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

        class EventfulFloatSlider : OpSlider
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

                if(wasHeld && held) OnFrozenUpdate.Invoke(dt);
            }

            public delegate void OnChangedHandler(EventfulFloatSlider slider);
            public event OnChangedHandler OnChanged;

            public override void OnChange()
            {
                base.OnChange();
                OnChanged.Invoke(this);
                //Debug.Log("floatval is " + valueFloat);
            }
        }

        public delegate void OnButtonSignalHandler(OpSimpleButton button);
        class EventfulButton : OpSimpleButton
        {
            public EventfulButton(Vector2 pos, Vector2 size, string signal, string text = "") : base(pos, size, signal, text)
            {
            }
            
            public event OnButtonSignalHandler OnSignal;
            public override void Signal()
            {
                base.Signal();
                OnSignal.Invoke(this);
            }

        }

        class EventfulImageButton : OpSimpleImageButton
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
                OnSignal.Invoke(this);
            }


            public event OnFrozenUpdateHandler OnFrozenUpdate;
            public override void Update(float dt)
            {
                bool wasHeld = held;
                base.Update(dt);

                if (wasHeld && held) OnFrozenUpdate.Invoke(dt);
            }

        }


    }
}
