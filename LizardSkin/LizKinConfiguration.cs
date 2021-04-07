

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

    public interface IJsonSerializable
    {
        Dictionary<string, object> ToJson();
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

        public Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>()
                {
                    {"LizKinConfiguration.version", (long)version },
                    {"profiles", profiles }
                };
        }
        public static LizKinConfiguration FromJson(Dictionary<string, object> json)
        {
            if ((long)json["LizKinConfiguration.version"] == 1)
            {
                LizKinConfiguration instance = new LizKinConfiguration()
                {
                    profiles = ((List<object>)json["profiles"]).Cast<Dictionary<string, object>>().ToList().ConvertAll(
                    new Converter<Dictionary<string, object>, LizKinProfileData>(LizKinProfileData.FromJson))
                };

                return instance;
            }

            throw new SerializationException("LizKinConfiguration version unsuported");
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
        public static LizKinProfileData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["LizKinProfileData.version"] == 1)
            {
                LizKinProfileData instance = new LizKinProfileData()
                {
                    profileName = (string)json["profileName"],
                    appliesToMode = (ProfileAppliesToMode)(long)json["appliesToMode"],
                    appliesToSelector = (ProfileAppliesToSelector)(long)json["appliesToSelector"],
                    appliesToList = (json["appliesToList"] as List<object>).ConvertAll(Convert.ToInt64).ConvertAll(Convert.ToInt32),
                    effectColor = OptionalUI.OpColorPicker.HexToColor((string)json["effectColor"]),
                    overrideBaseColor = (bool)json["overrideBaseColor"],
                    baseColorOverride = OptionalUI.OpColorPicker.HexToColor((string)json["baseColorOverride"]),
                    cosmetics = ((List<object>)json["cosmetics"]).Cast<Dictionary<string, object>>().ToList().ConvertAll(
                    new Converter<Dictionary<string, object>, LizKinCosmeticData>(LizKinCosmeticData.FromJson)),
                };

                foreach (LizKinCosmeticData cosmetic in instance.cosmetics)
                {
                    cosmetic.profile = instance;
                }
                return instance;
            }
            
            throw new SerializationException("LizKinProfileData version unsuported");
        }

        internal static LizKinProfileData Clone(LizKinProfileData instance)
        {
            return LizKinProfileData.FromJson(instance.ToJson());
        }

        public Color baseColor(ICosmeticsAdaptor iGraphics, float y) => overrideBaseColor ? baseColorOverride : iGraphics.BodyColorFallback(y);

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
        public Color baseColor(ICosmeticsAdaptor iGraphics, float y) => overrideBaseColor ? baseColorOverride : profile.baseColor(iGraphics, y);

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

        public static Dictionary<string, object> ToJsonConverter(LizKinCosmeticData instance)
        {
            return instance.ToJson();
        }
        public static LizKinCosmeticData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["LizKinCosmeticData.version"] == 1)
            {
                LizKinCosmeticData instance;
                switch ((CosmeticInstanceType)(long)json["instanceType"])
                {
                    case CosmeticInstanceType.Antennae:
                        instance = CosmeticAntennaeData.FromJson(json);
                        break;
                    case CosmeticInstanceType.AxolotlGills:
                        instance = CosmeticAxolotlGillsData.FromJson(json);
                        break;
                    case CosmeticInstanceType.BumpHawk:
                        instance = CosmeticBumpHawkData.FromJson(json);
                        break;
                    case CosmeticInstanceType.JumpRings:
                        instance = CosmeticJumpRingsData.FromJson(json);
                        break;
                    case CosmeticInstanceType.LongHeadScales:
                        instance = CosmeticLongHeadScalesData.FromJson(json);
                        break;
                    case CosmeticInstanceType.LongShoulderScales:
                        instance = CosmeticLongShoulderScalesData.FromJson(json);
                        break;
                    case CosmeticInstanceType.ShortBodyScales:
                        instance = CosmeticShortBodyScalesData.FromJson(json);
                        break;
                    case CosmeticInstanceType.SpineSpikes:
                        instance = CosmeticSpineSpikesData.FromJson(json);
                        break;
                    case CosmeticInstanceType.TailFin:
                        instance = CosmeticTailFinData.FromJson(json);
                        break;
                    case CosmeticInstanceType.TailGeckoScales:
                        instance = CosmeticTailGeckoScalesData.FromJson(json);
                        break;
                    case CosmeticInstanceType.TailTuft:
                        instance = CosmeticTailTuftData.FromJson(json);
                        break;
                    case CosmeticInstanceType.Whiskers:
                        instance = CosmeticWhiskersData.FromJson(json);
                        break;
                    case CosmeticInstanceType.WingScales:
                        instance = CosmeticWingScalesData.FromJson(json);
                        break;
                    default:
                        throw new SerializationException("LizKinCosmeticData CosmeticInstanceType not found");
                }

                instance.seed = (int)(long)json["seed"];
                instance.overrideEffectColor = (bool)json["overrideEffectColor"];
                instance.effectColorOverride = OptionalUI.OpColorPicker.HexToColor((string)json["effectColorOverride"]);
                instance.overrideBaseColor = (bool)json["overrideBaseColor"];
                instance.baseColorOverride = OptionalUI.OpColorPicker.HexToColor((string)json["baseColorOverride"]);

                return instance;
            }
            else throw new SerializationException("LizKinCosmeticData version unsuported");
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
            this.tintColor = new Color(0.9f, 0.3f, 0.3f);
        }

        public override CosmeticInstanceType instanceType => CosmeticInstanceType.Antennae;

        public static new CosmeticAntennaeData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticAntennaeData.version"] == 1) return new CosmeticAntennaeData()
            {
                length = (float)(double)json["length"],
                alpha = (float)(double)json["alpha"],
                tintColor = OptionalUI.OpColorPicker.HexToColor((string)json["baseColorOverride"])
        };
            throw new SerializationException("CosmeticAntennaeData version unsuported");
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
    }


    internal class CosmeticAxolotlGillsData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.AxolotlGills;

        public static new CosmeticAxolotlGillsData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticAxolotlGillsData.version"] == 1) return new CosmeticAxolotlGillsData()
            {

            };
            throw new SerializationException("CosmeticAxolotlGillsData version unsuported");
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

        public static new CosmeticBumpHawkData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticBumpHawkData.version"] == 1) return new CosmeticBumpHawkData()
            {

            };
            throw new SerializationException("CosmeticBumpHawkData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticBumpHawkData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }


    internal class CosmeticJumpRingsData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.JumpRings;

        public static new CosmeticJumpRingsData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticJumpRingsData.version"] == 1) return new CosmeticJumpRingsData()
            {

            };
            throw new SerializationException("CosmeticJumpRingsData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticJumpRingsData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }


    internal class CosmeticLongHeadScalesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.LongHeadScales;

        public static new CosmeticLongHeadScalesData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticLongHeadScalesData.version"] == 1) return new CosmeticLongHeadScalesData()
            {

            };
            throw new SerializationException("CosmeticLongHeadScalesData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticLongHeadScalesData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }


    internal class CosmeticLongShoulderScalesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.LongShoulderScales;

        public static new CosmeticLongShoulderScalesData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticLongShoulderScalesData.version"] == 1) return new CosmeticLongShoulderScalesData()
            {

            };
            throw new SerializationException("CosmeticLongShoulderScalesData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticLongShoulderScalesData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
    internal class CosmeticShortBodyScalesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.ShortBodyScales;

        public static new CosmeticShortBodyScalesData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticShortBodyScalesData.version"] == 1) return new CosmeticShortBodyScalesData()
            {

            };
            throw new SerializationException("CosmeticShortBodyScalesData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticShortBodyScalesData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
    internal class CosmeticSpineSpikesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.SpineSpikes;

        public static new CosmeticSpineSpikesData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticSpineSpikesData.version"] == 1) return new CosmeticSpineSpikesData()
            {

            };
            throw new SerializationException("CosmeticSpineSpikesData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticSpineSpikesData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
    internal class CosmeticTailFinData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.TailFin;

        public static new CosmeticTailFinData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticTailFinData.version"] == 1) return new CosmeticTailFinData()
            {

            };
            throw new SerializationException("CosmeticTailFinData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticTailFinData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
    internal class CosmeticTailGeckoScalesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.TailGeckoScales;

        public static new CosmeticTailGeckoScalesData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticTailGeckoScalesData.version"] == 1) return new CosmeticTailGeckoScalesData()
            {

            };
            throw new SerializationException("CosmeticTailGeckoScalesData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticTailGeckoScalesData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
    internal class CosmeticTailTuftData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.TailTuft;

        public bool veryCute;

        public static new CosmeticTailTuftData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticTailTuftData.version"] == 1) return new CosmeticTailTuftData()
            {
                veryCute = (bool)json["veryCute"]
            };
            throw new SerializationException("CosmeticTailTuftData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticTailTuftData.version", (long)version },
                    {"veryCute", veryCute }
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
    internal class CosmeticWhiskersData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.Whiskers;

        public static new CosmeticWhiskersData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticWhiskersData.version"] == 1) return new CosmeticWhiskersData()
            {

            };
            throw new SerializationException("CosmeticWhiskersData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticWhiskersData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
    internal class CosmeticWingScalesData : LizKinCosmeticData
    {
        const int version = 1;
        public override CosmeticInstanceType instanceType => CosmeticInstanceType.WingScales;

        public static new CosmeticWingScalesData FromJson(Dictionary<string, object> json)
        {
            if ((long)json["CosmeticWingScalesData.version"] == 1) return new CosmeticWingScalesData()
            {

            };
            throw new SerializationException("CosmeticWingScalesData version unsuported");
        }

        public override Dictionary<string, object> ToJson()
        {
            return base.ToJson().Concat(new Dictionary<string, object>()
                {
                    {"CosmeticWingScalesData.version", (long)version },
                }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
