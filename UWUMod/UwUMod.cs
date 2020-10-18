using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;
using UnityEngine;
using System.Reflection;

using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Runtime.InteropServices;

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

namespace UwUMod
{

    public class UwUMod : PartialityMod
    {
        [DllImport("user32.dll", EntryPoint = "SetWindowText")]
        public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);

        [DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);

        public UwUMod()
        {
            this.ModID = "UwUMod";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            StringBuilder sb = new StringBuilder();
            IntPtr window = GetActiveWindow();
            GetWindowText(window, sb, sb.Capacity);
            SetWindowText(window, UWUfyStwing(sb.ToString()));

            On.FLabel.CreateTextQuads += new On.FLabel.hook_CreateTextQuads(FWabew_CweateTextQuads_HK);
            On.HUD.DialogBox.Message.ctor += new On.HUD.DialogBox.Message.hook_ctor(DiawogueBox_Message_ctow_HK);
            On.SSOracleBehavior.ThrowOutBehavior.NewAction += new On.SSOracleBehavior.ThrowOutBehavior.hook_NewAction(SSOwacweBehaviow_ThrowOutBehavior_NewAction_HK);

            On.Menu.MenuIllustration.LoadFile_1 += new On.Menu.MenuIllustration.hook_LoadFile_1(MenuIwwustwation_WoadFiwe_HK);
        }

        private static Dictionary<string, string> uwu_simpwe = new Dictionary<string, string>()
        {
            { @"R", @"W" },
            { @"r", @"w" },
            { @"L", @"W" },
            { @"l", @"w" },
            { @"OU", @"UW" },
            { @"Ou", @"Uw" },
            { @"ou", @"uw" },
            { @"TH", @"D" },
            { @"Th", @"D" },
            { @"th", @"d" },

        };
        private static Dictionary<string, string> uwu_wegex = new Dictionary<string, string>()
        {
            { @"N([AEIOU])", @"NY$1" },
            { @"N([aeiou])", @"Ny$1" },
            { @"n([aeiou])", @"ny$1" },
            { @"T[Hh]\b", @"F" },
            { @"th\b", @"f" },
            { @"T[Hh]([UI][^sS])", @"F$1" },
            { @"th([ui][^sS])", @"f$1" },
            { @"OVE\b", @"UV" },
            { @"Ove\b", @"Uv" },
            { @"ove\b", @"uv" },
        };

        public static string UWUfyStwing(string owig)
        {
            //Debug.Log("uwufying: " + owig + " -> " + uwu_simpwe.Aggregate(uwu_wegex.Aggregate(owig, (current, value) => Regex.Replace(current, value.Key, value.Value)), (current, value) => current.Replace(value.Key, value.Value)));
            return uwu_simpwe.Aggregate(uwu_wegex.Aggregate(owig, (cuwwent, vawue) => Regex.Replace(cuwwent, vawue.Key, vawue.Value)), (cuwwent, vawue) => cuwwent.Replace(vawue.Key, vawue.Value));
        }


        static char[] separators = { '-', '.', ' ' };

        public static string PwepwocessDiawoge(string owig)
        {
            // Stuttew
            int fiwstSepawatow = owig.IndexOfAny(separators);
            if (owig.StartsWith("Oh"))
            {
                owig = "Uh" + owig.Substring(2);
            }
            else if (owig.Length > 3 && (fiwstSepawatow < 0 || fiwstSepawatow > 5) && UnityEngine.Random.value < 0.25f)
            {
                Match fiwstPhoneticVowew = Regex.Match(owig, @"[aeiouyngAEIOUYNG]");
                Match fiwstAwfanum = Regex.Match(owig, @"\w");
                if (fiwstPhoneticVowew.Success && fiwstPhoneticVowew.Index < 5)
                {
                    owig = owig.Substring(0, fiwstPhoneticVowew.Index + 1) + "-" + owig.Substring(fiwstAwfanum.Index);
                }
            }

            // Standawd wepwacemens
            bool hasFace = false;
            owig = owig.Replace("what is that", "whats this");
            if (owig.IndexOf("What is that") != -1)
            {
                owig = owig.Replace("What is that", "OWO whats this");
                hasFace = true;
            }
            owig = owig.Replace("Little", "Widdow");
            owig = owig.Replace("little", "widdow");
            if(owig.IndexOf("!") != -1)
            {
                owig = Regex.Replace(owig, @"(!+)", @"$1 >w<");
                hasFace = true;
            }

            // Pwetty faces UwU
            if (owig.EndsWith("?") || (!hasFace && UnityEngine.Random.value < 0.2f))
            {
                owig = owig.TrimEnd(separators);
                switch (UnityEngine.Random.Range(0, 10))
                {
                    case 0:
                        owig += " uwu";
                        break;
                    case 1:
                        owig += " owo";
                        break;
                    case 2:
                        owig += " UwU";
                        break;
                    case 3:
                        owig += " OwO";
                        break;
                    case 4:
                        owig += " >w<";
                        break;
                    case 5:
                        owig += " ^w^";
                        break;
                    case 6:
                    case 7:
                        owig += " UwU";
                        break;
                    default:
                        owig += "~";
                        break;
                }
            }
            return owig;
        }


        // We-we wook this guy because _text is set in way too many pwaces besides of the .text access UwU
        public static void FWabew_CweateTextQuads_HK(On.FLabel.orig_CreateTextQuads owig, FLabel instance)
        {
            if (instance._doesTextNeedUpdate || instance._numberOfFacetsNeeded == 0) // Stawt conditions ow text changed
            {
                instance._text = UWUfyStwing(instance._text);
            }
            owig(instance);
        }

        public static void DiawogueBox_Message_ctow_HK(On.HUD.DialogBox.Message.orig_ctor owig, HUD.DialogBox.Message instance, string text, float xOwientation, float yPos, int extwaWinger)
        {
            text = PwepwocessDiawoge(text);
            owig(instance, text, xOwientation, yPos, extwaWinger);
            // De duwbweus awe pwetty big owo
            int duwbweus = UWUfyStwing(text).Count(f => f == 'w' || f == 'W');
            instance.longestLine = 1 + (int) Math.Floor(RWCustom.Custom.LerpMap(duwbweus, 0, text.Length, instance.longestLine*0.95f, instance.longestLine*1.5f));
        }

        public static void SSOwacweBehaviow_ThrowOutBehavior_NewAction_HK(On.SSOracleBehavior.ThrowOutBehavior.orig_NewAction owig, SSOracleBehavior.ThrowOutBehavior instance, SSOracleBehavior.Action owdAction, SSOracleBehavior.Action newAction)
        {
            owig(instance, owdAction, newAction);
            if (newAction == SSOracleBehavior.Action.ThrowOut_KillOnSight)
            {
                instance.dialogBox.Interrupt("PEWISH", 0);
            }
        }

        static string[] embeddedFiwes = {
            "CompetitiveShadow",
            "CompetitiveTitle",
            "Intro_Roll_A",
            "Intro_Roll_B",
            "Intro_Roll_C",
            "MainTitle",
            "MainTitle2",
            "MainTitleBevel",
            "MainTitleShadow",
            "MultiplayerPortrait01",
            "MultiplayerPortrait11",
            "MultiplayerPortrait21",
            "MultiplayerPortrait31",
            "SandboxShadow",
            "SandboxTitle",
            "Title_CC",
            "Title_CC_Shadow",
            "Title_DS",
            "Title_DS_Shadow",
            "Title_GW",
            "Title_GW_Shadow",
            "Title_HI",
            "Title_HI_Shadow",
            "Title_LF",
            "Title_LF_Shadow",
            "Title_SB",
            "Title_SB_Shadow",
            "Title_SH",
            "Title_SH_Shadow",
            "Title_SI",
            "Title_SI_Shadow",
            "Title_SL",
            "Title_SL_Shadow",
            "Title_SS",
            "Title_SS_Shadow",
            "Title_SU",
            "Title_SU_Shadow",
            "Title_UW",
            "Title_UW_Shadow"
            };

        public static void MenuIwwustwation_WoadFiwe_HK(On.Menu.MenuIllustration.orig_LoadFile_1 owig, Menu.MenuIllustration iwwust, string fowdew)
        {
            Debug.Log("UWU MenuIwwustwation_WoadFiwe_HK");
            if (fowdew == "Illustrations" && embeddedFiwes.Contains(iwwust.fileName))
            {
                Debug.Log("UWU loaded " + iwwust.fileName + " from UwUMod");
                if (Futile.atlasManager.GetAtlasWithName(iwwust.fileName) != null)
                {
                    return;
                }
                iwwust.texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                iwwust.texture.wrapMode = TextureWrapMode.Clamp;
                if (iwwust.crispPixels)
                {
                    iwwust.texture.anisoLevel = 0;
                    iwwust.texture.filterMode = FilterMode.Point;
                }
                System.IO.Stream stweam = Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(UwUMod).Namespace + ".wesuwwces." + iwwust.fileName + ".png");
                byte[] bytes = new byte[stweam.Length];
                stweam.Read(bytes, 0, (int)stweam.Length);
                iwwust.texture.LoadImage(bytes);
                HeavyTexturesCache.LoadAndCacheAtlasFromTexture(iwwust.fileName, iwwust.texture);
            }
            else
            {
                Debug.Log("UWU ignored file " + iwwust.fileName);
                owig(iwwust, fowdew);
            }
        }

    }

}
