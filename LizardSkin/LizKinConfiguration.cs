

using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using RWCustom;
using System;

[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]


namespace LizardSkin
{
    // Must serialize everything to supported types,
    public interface IJsonSerializable
    {
        Dictionary<string, object> ToJson();
        void ReadFromJson(Dictionary<string, object> json, bool ignoremissing = false);
    }


    public class LizKinConfiguration : IJsonSerializable
    {
        const int version = 1;
        private const int maxProfileCount = 10;
        public List<LizKinProfileData> profiles;

        public LizKinConfiguration()
        {
            profiles = new List<LizKinProfileData>();
        }

        // TODO make these two idempotent
        public Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>()
                {
                    {"LizKinConfiguration.version", (long)version },
                    {"profiles", profiles }
                };
        }

        public void ReadFromJson(Dictionary<string, object> json, bool ignoremissing = false)
        {
            if ((long)json["LizKinConfiguration.version"] == 1)
            {
                profiles = ((List<object>)json["profiles"]).Cast<Dictionary<string, object>>().ToList().ConvertAll(
                new Converter<Dictionary<string, object>, LizKinProfileData>(LizKinProfileData.MakeFromJson));
            }

            if(!ignoremissing) throw new SerializationException("LizKinConfiguration version unsuported");
        }
        public static LizKinConfiguration MakeFromJson(Dictionary<string, object> json)
        {
            LizKinConfiguration instance = new LizKinConfiguration();
            instance.ReadFromJson(json);
            return instance;
        }

        public bool AddDefaultProfile()
        {
            if (profiles.Count >= maxProfileCount) return false;
            profiles.Add(GetDefaultProfile());
            return true;
        }

        private LizKinProfileData GetDefaultProfile()
        {
            LizKinProfileData myProfile = new LizKinProfileData();
            myProfile.profileName = "New Profile";
            return myProfile;
        }

        internal bool MoveProfileUp(LizKinProfileData profileData)
        {
            int index = profiles.IndexOf(profileData);
            if (index > 0)
            {
                profiles.RemoveAt(index);
                profiles.Insert(index - 1, profileData);
                return true;
            }
            return false;
        }

        internal bool MoveProfileDown(LizKinProfileData profileData)
        {
            int index = profiles.IndexOf(profileData);
            if (index < profiles.Count-1)
            {
                profiles.RemoveAt(index);
                profiles.Insert(index + 1, profileData);
                return true;
            }
            return false;
        }

        internal bool DuplicateProfile(LizKinProfileData profileData)
        {
            if (profiles.Count >= maxProfileCount) return false;
            profiles.Add(LizKinProfileData.Clone(profileData));
            return true;
        }

        internal bool DeleteProfile(LizKinProfileData profileData)
        {
            if (profiles.Count == 1) return false;
            profiles.Remove(profileData);
            return true;
        }

        internal List<LizKinCosmeticData> GetCosmeticsForSlugcat(int difficulty, int character, int player)
        {
            List<LizKinCosmeticData> cosmetics = new List<LizKinCosmeticData>();
            foreach (LizKinProfileData profile in profiles)
            {
                if (profile.MatchesSlugcat(difficulty, character, player))
                {
                    cosmetics.AddRange(profile.cosmetics);
                }
            }
            return cosmetics;
        }
    }

    public class LizKinProfileData : IJsonSerializable
    {
        const int version = 1;

        public string profileName;

        public enum ProfileAppliesToMode
        {
            Basic = 1,
            Advanced,
            VeryAdvanced
        }

        public ProfileAppliesToMode appliesToMode;

        public enum ProfileAppliesToSelector
        {
            Character = 1,
            Difficulty,
            Player
        }

        public ProfileAppliesToSelector appliesToSelector;

        public List<int> appliesToList;

        public Color effectColor;

        public bool overrideBaseColor;

        public Color baseColorOverride;

        public List<LizKinCosmeticData> cosmetics;

        internal LizKinProfileData()
        {
            profileName = "New Profile";
            appliesToMode = ProfileAppliesToMode.Basic;
            appliesToSelector = ProfileAppliesToSelector.Character;
            appliesToList = new List<int>() { -1 };
            effectColor = Custom.HSL2RGB(UnityEngine.Random.value, UnityEngine.Random.value * 0.2f + 0.8f, UnityEngine.Random.value * 0.2f + 0.8f);
            overrideBaseColor = false;
            baseColorOverride = Color.white;
            cosmetics = new List<LizKinCosmeticData>();
        }

        public Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>()
                {
                    {"LizKinProfileData.version", (long)version },
                    {"profileName", profileName },
                    {"appliesToMode", (long) appliesToMode },
                    {"appliesToSelector", (long) appliesToSelector },
                    {"appliesToList", appliesToList.ConvertAll(Convert.ToInt64).Cast<object>().ToList()},
                    {"effectColor", OptionalUI.OpColorPicker.ColorToHex(effectColor) },
                    {"overrideBaseColor", overrideBaseColor },
                    {"baseColorOverride", OptionalUI.OpColorPicker.ColorToHex(baseColorOverride) },
                    {"cosmetics", cosmetics.ConvertAll(
                    new Converter<LizKinCosmeticData, Dictionary<string, object>>(LizKinCosmeticData.ToJsonConverter)).Cast<object>().ToList() },
                };
        }

        public virtual void ReadFromJson(Dictionary<string, object> json, bool ignoremissing = false)
        {
            if (json.ContainsKey("LizKinProfileData.version"))
            {
                if ((long)json["LizKinProfileData.version"] == 1)
                {
                    profileName = (string)json["profileName"];
                    appliesToMode = (ProfileAppliesToMode)(long)json["appliesToMode"];
                    appliesToSelector = (ProfileAppliesToSelector)(long)json["appliesToSelector"];
                    appliesToList = (json["appliesToList"] as List<object>).ConvertAll(Convert.ToInt64).ConvertAll(Convert.ToInt32);
                    effectColor = OptionalUI.OpColorPicker.HexToColor((string)json["effectColor"]);
                    overrideBaseColor = (bool)json["overrideBaseColor"];
                    baseColorOverride = OptionalUI.OpColorPicker.HexToColor((string)json["baseColorOverride"]);
                    cosmetics = ((List<object>)json["cosmetics"]).Cast<Dictionary<string, object>>().ToList().ConvertAll(
                    new Converter<Dictionary<string, object>, LizKinCosmeticData>(LizKinCosmeticData.MakeFromJson));

                    foreach (LizKinCosmeticData cosmetic in cosmetics)
                    {
                        cosmetic.profile = this;
                    }

                    return;
                }
            }
            if(!ignoremissing) throw new SerializationException("LizKinProfileData version unsuported");
        }

        public static LizKinProfileData MakeFromJson(Dictionary<string, object> json)
        {
            // just one type of profile for now
            LizKinProfileData instance = new LizKinProfileData();
            instance.ReadFromJson(json);
            return instance;
        }

        // Hmmmmmmm
        internal static LizKinProfileData Clone(LizKinProfileData instance)
        {
            return LizKinProfileData.MakeFromJson(instance.ToJson());
        }

        public Color GetBaseColor(ICosmeticsAdaptor iGraphics, float y) => overrideBaseColor ? baseColorOverride : iGraphics.BodyColorFallback(y);

        internal bool MatchesSlugcat(int difficulty, int character, int player)
        {
            if (appliesToList.Contains(-1)) return true;

            switch (appliesToSelector)
            {
                case ProfileAppliesToSelector.Character:
                    return appliesToList.Contains(character);
                case ProfileAppliesToSelector.Difficulty:
                    return appliesToList.Contains(difficulty);
                case ProfileAppliesToSelector.Player:
                    return appliesToList.Contains(player);
            }
            return false;
        }

        internal void AddEmptyCosmetic()
        {
            this.cosmetics.Add(new CosmeticTailTuftData(){profile = this});
        }
    }

    public abstract class LizKinCosmeticData : IJsonSerializable
    {

        const int version = 1;

        public LizKinProfileData profile;

        public enum CosmeticInstanceType
        {
            Antennae = 1,
            AxolotlGills,
            BumpHawk,
            JumpRings,
            LongHeadScales,
            LongShoulderScales,
            ShortBodyScales,
            SpineSpikes,
            TailFin,
            TailGeckoScales,
            TailTuft,
            Whiskers,
            WingScales
        }

        public abstract CosmeticInstanceType instanceType { get; }
        public Color effectColor => overrideEffectColor ? effectColorOverride : profile.effectColor;
        public Color GetBaseColor(ICosmeticsAdaptor iGraphics, float y) => overrideBaseColor ? baseColorOverride : profile.GetBaseColor(iGraphics, y);

        public int seed;

        public bool overrideEffectColor;

        public Color effectColorOverride;

        public bool overrideBaseColor;

        public Color baseColorOverride;

        public LizKinCosmeticData()
        {
            seed = (int)(10000 * UnityEngine.Random.value);
            effectColorOverride = Color.red;
            baseColorOverride = Color.red;
        }

        public virtual Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>()
                {
                    {"LizKinCosmeticData.version", (long)version },
                    {"instanceType", (long) instanceType },
                    {"seed", (long) seed },
                    {"overrideEffectColor", overrideEffectColor },
                    {"effectColorOverride", OptionalUI.OpColorPicker.ColorToHex(effectColorOverride) },
                    {"overrideBaseColor", overrideBaseColor },
                    {"baseColorOverride", OptionalUI.OpColorPicker.ColorToHex(baseColorOverride) },
                };
        }

        // linq go brr
        public static Dictionary<string, object> ToJsonConverter(LizKinCosmeticData instance)
        {
            return instance.ToJson();
        }

        public virtual void ReadFromJson(Dictionary<string, object> json, bool ignoremissing=false)
        {
            if (json.ContainsKey("LizKinCosmeticData.version"))
            {
                if ((long)json["LizKinCosmeticData.version"] == 1)
                {
                    seed = (int)(long)json["seed"];
                    overrideEffectColor = (bool)json["overrideEffectColor"];
                    effectColorOverride = OptionalUI.OpColorPicker.HexToColor((string)json["effectColorOverride"]);
                    overrideBaseColor = (bool)json["overrideBaseColor"];
                    baseColorOverride = OptionalUI.OpColorPicker.HexToColor((string)json["baseColorOverride"]);

                    return;
                }
            }
            if(!ignoremissing) throw new SerializationException("LizKinCosmeticData version unsuported");
        }


        public static LizKinCosmeticData MakeFromJson(Dictionary<string, object> json)
        {
            LizKinCosmeticData instance = MakeCosmeticOfType((CosmeticInstanceType)(long)json["instanceType"]);
            instance.ReadFromJson(json);
            return instance;
        }

        internal static LizKinCosmeticData MakeCosmeticOfType(CosmeticInstanceType newType)
        {
            switch (newType)
            {
                case CosmeticInstanceType.Antennae:
                    return new CosmeticAntennaeData();
                case CosmeticInstanceType.AxolotlGills:
                    return new CosmeticAxolotlGillsData();
                case CosmeticInstanceType.BumpHawk:
                    return new CosmeticBumpHawkData();
                case CosmeticInstanceType.JumpRings:
                    return new CosmeticJumpRingsData();
                case CosmeticInstanceType.LongHeadScales:
                    return new CosmeticLongHeadScalesData();
                case CosmeticInstanceType.LongShoulderScales:
                    return new CosmeticLongShoulderScalesData();
                case CosmeticInstanceType.ShortBodyScales:
                    return new CosmeticShortBodyScalesData();
                case CosmeticInstanceType.SpineSpikes:
                    return new CosmeticSpineSpikesData();
                case CosmeticInstanceType.TailFin:
                    return new CosmeticTailFinData();
                case CosmeticInstanceType.TailGeckoScales:
                    return new CosmeticTailGeckoScalesData();
                case CosmeticInstanceType.TailTuft:
                    return new CosmeticTailTuftData();
                case CosmeticInstanceType.Whiskers:
                    return new CosmeticWhiskersData();
                case CosmeticInstanceType.WingScales:
                    return new CosmeticWingScalesData();
                default:
                    throw new ArgumentException("Unsupported instance type");
            }
        }

        internal virtual void ReadFromOther(LizKinCosmeticData other)
        {
            ReadFromJson(other.ToJson(), ignoremissing: true);
        }

        internal static LizKinCosmeticData Clone(LizKinCosmeticData instance)
        {
            return LizKinCosmeticData.MakeFromJson(instance.ToJson());
        }

        virtual internal CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new CosmeticPanel(this, manager);
        }

        virtual internal void ReadEditPanel(CosmeticPanel panel)
        {
            //throw new NotImplementedException("Data wasn't read");
            this.seed = panel.seedBox.valueInt;
            this.overrideEffectColor = panel.effectCkb.valueBool;
            this.effectColorOverride = panel.effectColorPicker.valuecolor;
            this.overrideBaseColor = panel.baseCkb.valueBool;
            this.baseColorOverride = panel.baseColorPicker.valuecolor;
        }

        //abstract
        internal class CosmeticPanel : LizardSkinOI.GroupPanel
        {
            const float pannelWidth = 360f;
            private LizardSkinOI.EventfulComboBox typeBox;
            private LizardSkinOI.ProfileManager manager;
            internal LizKinCosmeticData data;

            internal LizardSkinOI.EventfulTextBox seedBox;
            internal LizardSkinOI.EventfulCheckBox effectCkb;
            internal LizardSkinOI.OpTinyColorPicker effectColorPicker;
            internal LizardSkinOI.EventfulCheckBox baseCkb;
            internal LizardSkinOI.OpTinyColorPicker baseColorPicker;

            protected virtual float pannelHeight => 360f;
            //protected
            internal CosmeticPanel(LizKinCosmeticData data, LizardSkinOI.ProfileManager manager, float height=56) : base(Vector2.zero, new Vector2(pannelWidth, height))
            {

                this.data = data;
                this.manager = manager;
                // Group panel Y coordinates are top-to-bottom
                // add type selector
                this.typeBox = new LizardSkinOI.EventfulComboBox(new Vector2(3, -27), 140, "", Enum.GetNames(typeof(LizKinCosmeticData.CosmeticInstanceType)), data.instanceType.ToString());
                typeBox.OnChangeEvent += TypeBox_OnChangeEvent;
                children.Add(typeBox);
                // add basic buttons
                LizardSkinOI.EventfulImageButton btnClip = new LizardSkinOI.EventfulImageButton(new Vector2(150, -27), new Vector2(24, 24), "", "LizKinClipboard");
                btnClip.OnSignal += () => { this.manager.SetClipboard(data); };
                children.Add(btnClip);
                LizardSkinOI.EventfulImageButton btnDuplicate = new LizardSkinOI.EventfulImageButton(new Vector2(180, -27), new Vector2(24, 24), "", "LizKinDuplicate");
                btnDuplicate.OnSignal += () => { this.manager.DuplicateCosmetic(data); };
                children.Add(btnDuplicate);
                LizardSkinOI.EventfulImageButton btnDelete = new LizardSkinOI.EventfulImageButton(new Vector2(210, -27), new Vector2(24, 24), "", "LizKinDelete");
                btnDelete.OnSignal += () => { this.manager.DeleteCosmetic(this); };
                children.Add(btnDelete);

                // seeeeed
                children.Add(new OptionalUI.OpLabel(new Vector2(240, -27), new Vector2(69, 24), "Seed:", FLabelAlignment.Right));
                children.Add(seedBox = new LizardSkinOI.EventfulTextBox(new Vector2(312, -27), 45, "", data.seed.ToString()));
                seedBox.OnChangeEvent += DataChangedRefreshNeeded;
                seedBox.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                // Second row
                // color overrides
                children.Add(new OptionalUI.OpLabel(new Vector2(3, -53), new Vector2(97, 24), "Effect Override:", FLabelAlignment.Right));
                children.Add(effectCkb = new LizardSkinOI.EventfulCheckBox(new Vector2(105, -53), "", data.overrideEffectColor));
                effectCkb.OnChangeEvent += DataChanged;
                children.Add(effectColorPicker = new LizardSkinOI.OpTinyColorPicker(new Vector2(135, -53), "", OptionalUI.OpColorPicker.ColorToHex(data.effectColorOverride)));
                effectColorPicker.OnChanged += DataChanged;
                effectColorPicker.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OptionalUI.OpLabel(new Vector2(180, -53), new Vector2(100, 24), "Base Override:", FLabelAlignment.Right));
                children.Add(baseCkb = new LizardSkinOI.EventfulCheckBox(new Vector2(285, -53), "", data.overrideBaseColor));
                baseCkb.OnChangeEvent += DataChanged;
                children.Add(baseColorPicker = new LizardSkinOI.OpTinyColorPicker(new Vector2(315, -53), "", OptionalUI.OpColorPicker.ColorToHex(data.baseColorOverride)));
                baseColorPicker.OnChanged += DataChanged;
                baseColorPicker.OnFrozenUpdate += TriggerUpdateWhileFrozen;

            }

            protected void TriggerUpdateWhileFrozen(float dt)
            {
                manager.KeepPreviewUpdated(dt);
            }

            protected virtual void DataChanged()
            {
                this.data.ReadEditPanel(this);

            }
            protected virtual void DataChangedRefreshNeeded()
            {
                this.data.ReadEditPanel(this);
                manager.RefreshPreview();

            }

            private void TypeBox_OnChangeEvent()
            {
                if (data.instanceType.ToString() == typeBox.value) return;

                // Not sure what happens here
                // do we try and clone a common ancestor of data to keep some of the info ?
                if (data.profile == null) return; // Already removed, combox dying triggers onchange :/
                manager.ChangeCosmeticType(this, (CosmeticInstanceType) Enum.Parse(typeof(CosmeticInstanceType), typeBox.value));
            }
        }
    }



    internal class CosmeticAntennaeData : LizKinCosmeticData
    {
        const int version = 1;
        public float length;
        public float alpha;
        public Color tintColor;

        public CosmeticAntennaeData()
        {
            length = 0.5f;
            alpha = 0.45f;
            tintColor = new Color(0.9f, 0.3f, 0.3f);
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.Antennae;

        public override void ReadFromJson(Dictionary<string, object> json, bool ignoremissing = false)
        {
            base.ReadFromJson(json, ignoremissing);
            if (json.ContainsKey("CosmeticAntennaeData.version"))
            {
                if ((long)json["CosmeticAntennaeData.version"] == 1)
                {
                    length = (float)(double)json["length"];
                    alpha = (float)(double)json["alpha"];
                    tintColor = OptionalUI.OpColorPicker.HexToColor((string)json["tintColor"]);
                    return;
                }
            }
            if(!ignoremissing)throw new SerializationException("CosmeticAntennaeData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                {"CosmeticAntennaeData.version", (long)version },
                {"length", (double)length },
                {"alpha", (double)alpha },
                {"tintColor",  OptionalUI.OpColorPicker.ColorToHex(tintColor)}
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        internal override CosmeticPanel MakeEditPanel(LizardSkinOI.ProfileManager manager)
        {
            return new AntennaePanel(this, manager);
        }

        internal override void ReadEditPanel(CosmeticPanel panel)
        {
            base.ReadEditPanel(panel);
            this.length = (panel as AntennaePanel).lengthControl.valueFloat;
            this.alpha = (panel as AntennaePanel).alphaControl.valueFloat;
            this.tintColor = (panel as AntennaePanel).tintPicker.valuecolor;
        }

        internal class AntennaePanel : CosmeticPanel
        {
            internal LizardSkinOI.EventfulUpdown lengthControl;
            internal LizardSkinOI.EventfulUpdown alphaControl;
            internal LizardSkinOI.OpTinyColorPicker tintPicker;

            internal AntennaePanel(CosmeticAntennaeData data, LizardSkinOI.ProfileManager manager, float height = 88) : base(data, manager, height)
            {
                children.Add(new OptionalUI.OpLabel(new Vector2(5, -81), new Vector2(60, 24), "Length:", FLabelAlignment.Right));
                children.Add(this.lengthControl = new LizardSkinOI.EventfulUpdown(new Vector2(70, -85), 55, "", data.length, 2));
                lengthControl.SetRange(0f, 1f);
                lengthControl.OnChangeEvent += DataChangedRefreshNeeded;
                lengthControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OptionalUI.OpLabel(new Vector2(120, -81), new Vector2(60, 24), "Alpha:", FLabelAlignment.Right));
                children.Add(this.alphaControl = new LizardSkinOI.EventfulUpdown(new Vector2(185, -85), 55, "", data.alpha, 2));
                alphaControl.SetRange(0f, 1f);
                alphaControl.OnChangeEvent += DataChangedRefreshNeeded;
                alphaControl.OnFrozenUpdate += TriggerUpdateWhileFrozen;

                children.Add(new OptionalUI.OpLabel(new Vector2(245, -81), new Vector2(60, 24), "Tint:", FLabelAlignment.Right));
                children.Add(tintPicker = new LizardSkinOI.OpTinyColorPicker(new Vector2(310, -83), "", OptionalUI.OpColorPicker.ColorToHex(data.tintColor)));
                tintPicker.OnChanged += DataChanged;
                tintPicker.OnFrozenUpdate += TriggerUpdateWhileFrozen;
                tintPicker.OnSignal += DataChangedRefreshNeeded;
            }
        }

    }
    internal class CosmeticAxolotlGillsData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.AxolotlGills;

        public override void ReadFromJson(Dictionary<string, object> json, bool ignoremissing = false)
        {
            base.ReadFromJson(json, ignoremissing);
            if (json.ContainsKey("CosmeticAxolotlGillsData.version"))
            {
                if ((long)json["CosmeticAxolotlGillsData.version"] == 1)
                {

                    return;
                }
            }
            if (!ignoremissing) throw new SerializationException("CosmeticAxolotlGillsData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticAxolotlGillsData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
    internal class CosmeticBumpHawkData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.BumpHawk;

    }
    internal class CosmeticJumpRingsData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.JumpRings;

    }
    internal class CosmeticLongHeadScalesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.LongHeadScales;

    }
    internal class CosmeticLongShoulderScalesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.LongShoulderScales;

    }
    internal class CosmeticShortBodyScalesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.ShortBodyScales;

    }
    internal class CosmeticSpineSpikesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.SpineSpikes;

    }
    internal class CosmeticTailFinData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.TailFin;

    }
    internal class CosmeticTailGeckoScalesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.TailGeckoScales;

    }
    internal class CosmeticTailTuftData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.TailTuft;

    }
    internal class CosmeticWhiskersData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.Whiskers;

    }
    internal class CosmeticWingScalesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.WingScales;

    }
}
