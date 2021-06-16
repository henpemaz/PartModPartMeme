using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;


using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Reflection;

[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}


namespace LizardSkin
{
    public class LizardSkin : PartialityMod
    {

        public LizardSkin()
        {
            this.ModID = "LizardSkin";
            this.Version = "0.1";
            this.author = "Henpemaz";

            instance = this;
        }

        public static LizardSkin instance;

        public static OptionalUI.OptionInterface LoadOI()
        {
            return new LizardSkinOI();
        }

        internal static Type fpg_ref;
        internal static Type jolly_ref;
        internal static Type custail_ref;
        internal static Type colorfoot_ref;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            PlayerGraphicsCosmeticsAdaptor.ApplyHooksToPlayerGraphics();

            On.RainWorld.Start += RainWorld_Start_hk;

            // Json goes brrrr
            On.Json.Serializer.SerializeOther += Serializer_SerializeOther;

            TestSerialization();
        }

        static int CGProgressionStep = -1; // -1 unset 0 init 1 progressed;
        public static void SetCGProgression(int step)
        {
            CGProgressionStep = step;
        }

        private void RainWorld_Start_hk(On.RainWorld.orig_Start orig, RainWorld self)
        {
            // Find conflicting mods
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "FancySlugcats")
                {
                    fpg_ref = asm.GetType("FancySlugcats.FancyPlayerGraphics");
                }
                else
                if (asm.GetName().Name == "JollyCoop")
                {
                    jolly_ref = asm.GetType("JollyCoop.PlayerGraphicsHK");
                }
                else
                if (asm.GetName().Name == "CustomTail")
                {
                    custail_ref = asm.GetType("CustomTail.CustomTail");
                }
                else
                if (asm.GetName().Name == "Colorfoot")
                {
                    colorfoot_ref = asm.GetType("Colorfoot.LegMod");
                }
            }

            if (fpg_ref != null)
            {
                Debug.Log("LizardSkin: FOUND FancyPlayerGraphics");
                try
                {
                    FancyPlayerGraphicsCosmeticsAdaptor.ApplyHooksToFancyPlayerGraphics();
                    Debug.Log("LizardSkin: FancyPlayerGraphics hooks applied");
                }
                catch (Exception e)
                {
                    Debug.LogError("LizardSkin: ERROR hooking FancyPlayerGraphics, integration disabled, send logs to henpe");
                    fpg_ref = null;
                    Debug.LogException(e);
                }
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND FancyPlayerGraphics, integration disabled");
            }

            if (jolly_ref != null)
            {
                Debug.Log("LizardSkin: FOUND Jolly");
                try
                {
                    PlayerGraphicsCosmeticsAdaptor.ApplyHooksToJollyPlayerGraphicsHK();
                    Debug.Log("LizardSkin: Jolly hooks applied");
                }
                catch (Exception e)
                {
                    Debug.LogError("LizardSkin: ERROR hooking Jolly, integration disabled, send logs to henpe");
                    jolly_ref = null;
                    Debug.LogException(e);
                }
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND Jolly, integration disabled");
            }

            if (custail_ref != null)
            {
                Debug.Log("LizardSkin: FOUND CustomTail, no hooks to apply");
                // No hookies needed, good mod :)
                // ... or is it... trimesh bugfixing will tell 
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND CustomTail, integration disabled");
            }

            if (colorfoot_ref != null)
            {
                Debug.Log("LizardSkin: FOUND Colorfoot");
                try
                {
                    PlayerGraphicsCosmeticsAdaptor.ApplyHooksToColorfootPlayerGraphicsPatch();
                    Debug.Log("LizardSkin: Colorfoot hooks applied");
                }
                catch (Exception e)
                {
                    Debug.LogError("LizardSkin: ERROR hooking Colorfoot, integration disabled, send logs to henpe");
                    colorfoot_ref = null;
                    Debug.LogException(e);
                }
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND Colorfoot, integration disabled");
            }

            orig(self);
            CustomAtlasLoader.LoadCustomAtlas("LizKinIcons.png", Assembly.GetExecutingAssembly().GetManifestResourceStream("LizardSkin.Resources.LizKinIcons.png"), Assembly.GetExecutingAssembly().GetManifestResourceStream("LizardSkin.Resources.LizKinIcons.txt"));
        }

        private static void TestSerialization()
        {
            Debug.Log("LizardSkin: Serialization tests start");
            LizKinConfiguration myConfig = new LizKinConfiguration();
            myConfig.AddDefaultProfile();

            string serialized = Json.Serialize(myConfig);

            LizKinConfiguration deserialized = LizKinConfiguration.MakeFromJson(Json.Deserialize(serialized) as Dictionary<string, object>);

            string serialized2 = Json.Serialize(deserialized);

            if (serialized != serialized2) throw new System.Runtime.Serialization.SerializationException("LizardSkin: Reserialization check failed");

            LizKinConfiguration cloned = LizKinConfiguration.Clone(myConfig);

            string serialized3 = Json.Serialize(cloned);

            if (serialized2 != serialized3) throw new System.Runtime.Serialization.SerializationException("LizardSkin: Clone check failed");

            Debug.Log("LizardSkin: Serialization tests ok");
        }

        private static void Serializer_SerializeOther(On.Json.Serializer.orig_SerializeOther orig, Json.Serializer self, object value)
        {
            if (value is IJsonSerializable) self.SerializeObject((value as IJsonSerializable).ToJson());
            else orig(self, value);
        }

        internal static List<LizKinCosmeticData> GetCosmeticsForSlugcat(int name, int slugcatCharacter, int playerNumber)
        {
            if(LizardSkinOI.configuration == null)
            {
                LizardSkinOI.LoadLizKinData();
            }
            return LizardSkinOI.configuration.GetCosmeticsForSlugcat(name, slugcatCharacter, playerNumber);
        }
    }
}
