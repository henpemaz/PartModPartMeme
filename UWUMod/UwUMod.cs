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
using MonoMod.RuntimeDetour;
using System.IO;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

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

        static UwUMod()
        {
            
        }

        public UwUMod()
        {
            this.ModID = "UwUMod";
            this.Version = "1.1";
            this.author = "Henpemaz";
        }
        public override void OnLoad()
        {
            base.OnLoad();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            On.FLabel.CreateTextQuads += new On.FLabel.hook_CreateTextQuads(FWabew_CweateTextQuads_HK);
            On.HUD.DialogBox.Message.ctor += new On.HUD.DialogBox.Message.hook_ctor(DiawogueBox_Message_ctow_HK);
            On.SSOracleBehavior.ThrowOutBehavior.NewAction += new On.SSOracleBehavior.ThrowOutBehavior.hook_NewAction(SSOwacweBehaviow_ThrowOutBehavior_NewAction_HK);

            On.Menu.MenuIllustration.LoadFile_1 += new On.Menu.MenuIllustration.hook_LoadFile_1(MenuIwwustwation_WoadFiwe_HK);

            On.RainWorld.Start += RainWorld_Start;
        }

        public static void SwapWogs()
        {
            if (File.Exists("exceptionLog.txt"))
            {
                File.AppendAllText("exceptionWog.txt", UwUSpwitUwUAndJoin(File.ReadAllText("exceptionLog.txt")));
                File.Delete("exceptionLog.txt");
            }
            if (File.Exists("consoleLog.txt"))
            {
                File.AppendAllText("consoweWog.txt", UwUSpwitUwUAndJoin(File.ReadAllText("consoleLog.txt")));
                File.Delete("consoleLog.txt");
            }
        }

        private void RainWorld_Start(On.RainWorld.orig_Start owig, RainWorld sewf)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                IntPtr window = GetActiveWindow();
                GetWindowText(window, sb, sb.Capacity);
                SetWindowText(window, UWUfyStwing(sb.ToString()));

                File.Delete("exceptionWog.txt");
                File.Delete("consoweWog.txt");
                if (!File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "nyoUwUWog.txt")) && !File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "noUwULog.txt")))
                {
                    // Wogs get a bit messy duwing de twansition >w<
                    new Hook(typeof(Console).GetMethod("Write", new Type[] { typeof(string) }), typeof(UwUMod).GetMethod("UwU_ConsoweWwite"));
                    foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    {

                        if (asm.GetName().Name == "ConfigMachine")
                        {
                            Debug.Log("UWU found ConfigMachinye");
                            var typeref = asm.GetType("CompletelyOptional.InternalTranslator");
                            if (typeref != null && typeref.GetMethod("Translate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) != null)
                            {
                                new Hook(typeref.GetMethod("Translate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), typeof(UwUMod).GetMethod("UwU_ConfigMachinyeTwanswate"));
                            }
                            typeref = asm.GetType("OptionalUI.OpLabel");
                            if (typeref != null && typeref.GetConstructor(new Type[] { typeof(Vector2), typeof(Vector2), typeof(string), typeof(FLabelAlignment), typeof(bool) }) != null)
                            {
                                new Hook(typeref.GetConstructor(new Type[] { typeof(Vector2), typeof(Vector2), typeof(string), typeof(FLabelAlignment), typeof(bool) }), typeof(UwUMod).GetMethod("UwU_ConfigMachinyeOpWabew"));
                            }
                        }
                        if(asm.GetName().Name == "Partiality")
                        {
                            Debug.Log("UWU found Pawtiawity");
                            var typeref = asm.GetType("Partiality.Modloader.ModManager");
                            if (typeref != null && typeref.GetMethod("HandleLog", new Type[] { typeof(string), typeof(string), typeof(LogType) }) != null)
                            {
                                new Hook(typeref.GetMethod("HandleLog", new Type[] { typeof(string), typeof(string), typeof(LogType) }), typeof(UwUMod).GetMethod("UwU_ModManagewWogHandwew"));
                                SwapWogs();
                            }
                        }
                        if (asm.GetName().Name == "LogFix")
                        {
                            Debug.Log("UWU found WogFix");
                            var typeref = asm.GetType("LogFix.LogFix");
                            if (typeref != null && typeref.GetMethod("HandleLog", new Type[] { typeof(string), typeof(string), typeof(LogType) }) != null)
                            {
                                new Hook(typeref.GetMethod("HandleLog", new Type[] { typeof(string), typeof(string), typeof(LogType) }), typeof(UwUMod).GetMethod("UwU_WogFixWogHandwew"));
                                SwapWogs();
                            }
                        }
                        if (asm.GetName().Name == "BepInEx")
                        {
                            Debug.Log("UWU found BepInEx");
                            var typeref = asm.GetType("BepInEx.Logging.ManualLogSource");
                            if (typeref != null && typeref.GetMethod("Log", new Type[] { typeof(LogType), typeof(object) }) != null)
                            {
                                new Hook(typeref.GetMethod("Log", new Type[] { typeof(LogType), typeof(object) }), typeof(UwUMod).GetMethod("UwU_ManuawWogSuvwceWog"));
                            }
                            if (typeref != null && typeref.GetConstructor(new Type[] { typeof(string) }) != null)
                            {
                                new Hook(typeref.GetConstructor(new Type[] { typeof(string) }), typeof(UwUMod).GetMethod("UwU_ManuawWogSuvwce"));
                            }
                        }
                        if (asm.GetName().Name == "CustomRegions")
                        {
                            Debug.Log("UWU found CustomWegions");
                            var typeref = asm.GetType("CustomRegions.Mod.CustomWorldMod");
                            if (typeref != null && typeref.GetMethod("Log", new Type[] { typeof(string) }) != null)
                            {
                                new Hook(typeref.GetMethod("Log", new Type[] { typeof(string) }), typeof(UwUMod).GetMethod("UwU_CWSWog"));
                            }
                            if (File.Exists("customWorldLog.txt"))
                            {
                                File.Delete("customWowwdWog.txt");
                                File.WriteAllText("customWowwdWog.txt", UwUSpwitUwUAndJoin(File.ReadAllText("customWorldLog.txt")));
                                File.Delete("customWorldLog.txt");
                            }
                        }
                        if (asm.GetName().Name == "JollyCoop")
                        {
                            Debug.Log("UWU found JowwyCoop");
                            var typeref = asm.GetType("JollyCoop.JollyScript");
                            if (typeref != null && typeref.GetMethod("JollyLog", new Type[] { typeof(string) }) != null)
                            {
                                new Hook(typeref.GetMethod("JollyLog", new Type[] { typeof(string) }), typeof(UwUMod).GetMethod("UwU_CWSWog")); // same signature reduce, reuse, recycle
                            }
                            if (File.Exists("jollyLog.txt"))
                            {
                                File.Delete("jowwyWog.txt");
                                File.WriteAllText("jowwyWog.txt", UwUSpwitUwUAndJoin(File.ReadAllText("jollyLog.txt")));
                                File.Delete("jollyLog.txt");
                            }
                        }
                        //Debug.LogError("loaded " + asm.GetName().Name);
                    }
                    Debug.Log("UWU Logs registered successfully! Create noUwULog.txt in the game directory to disable it.");
                }
                else
                {
                    Debug.Log("UWU Logs disabled. Remove noUwULog.txt or nyoUwUWog.txt in the game directory to enable it.");
                }
            }
            finally
            {
                owig(sewf);
                try
                {
                    if (!File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "nyoOwO.txt")) && !File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "noOwO.txt")))
                    {
                        CustomAtlasLoader.LoadCustomAtlas("owoface", Assembly.GetExecutingAssembly().GetManifestResourceStream("UwUMod.wesuwwces.owoface.png"), Assembly.GetExecutingAssembly().GetManifestResourceStream("UwUMod.wesuwwces.owoface.txt"));
                        Debug.Log("OwO face loaded! Create noOwO.txt in the game directory to disable it.");
                    }
                    else
                    {
                        Debug.Log("OwO face disabled. Remove noOwO.txt or nyoOwO.txt in the game directory to enable it.");
                    }
                }
                catch
                {
                    // pass
                }
            }
        }

        public delegate string OwigConfigMachinyeTwanswate(string owigstwing);
        public static string UwU_ConfigMachinyeTwanswate(OwigConfigMachinyeTwanswate owig, string owigstwing)
        {
            string wetuwnvaw = owig(owigstwing);
            //Debug.Log("UWU I'm in!!! - " + wetuwnvaw);
            if(wetuwnvaw == "There was an issue initializing OptionInterface.")
                wetuwnvaw = "[[OOPSIE WOOPSIE!! UwU We made a fucky wucky!! A wittwe\nfucko boingo! The code monkeys at our headquawtews\nare wowking VEWY HAWD to fix this!]]";
            return wetuwnvaw;
        }

        public delegate void OwigConfigMachinyeOpWabew(OptionalUI.OpLabel sewf, Vector2 pos, Vector2 size, string text, FLabelAlignment awignment, bool bigText);
        public static void UwU_ConfigMachinyeOpWabew(OwigConfigMachinyeOpWabew owig, OptionalUI.OpLabel sewf, Vector2 pos, Vector2 size, string text, FLabelAlignment awignment, bool bigText)
        {
            if (bigText && text == ":(")
                owig(sewf, new Vector2(pos.x - 40, pos.y), size, "UnU", awignment, bigText);
            else owig(sewf, pos, size, text, awignment, bigText);
        }

        public delegate void OwigWog(string wogText);
        public static void UwU_CWSWog(OwigWog owig, string wogText)
        {
            if (!File.Exists(RWCustom.Custom.RootFolderDirectory() + "customWowwdWog.txt"))
            {
                CustomRegions.Mod.CustomWorldMod.CreateCustomWorldLog();
            }

            try
            {
                using (StreamWriter file = new StreamWriter(RWCustom.Custom.RootFolderDirectory() + "customWowwdWog.txt", true))
                {
                    file.WriteLine(UwUSpwitUwUAndJoin(wogText));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public delegate void OwigManuawWogSuvwce(BepInEx.Logging.ManualLogSource sewf, string suvwceNyame);
        public static void UwU_ManuawWogSuvwce(OwigManuawWogSuvwce owig, BepInEx.Logging.ManualLogSource sewf, string suvwceNyame)
        {
            owig(sewf, UWUfyStwing(suvwceNyame));
        }

        public delegate void OwigManuawWogSuvwceWog(BepInEx.Logging.ManualLogSource sewf, BepInEx.Logging.LogLevel wevew, object data);
        public static void UwU_ManuawWogSuvwceWog(OwigManuawWogSuvwceWog owig, BepInEx.Logging.ManualLogSource sewf, BepInEx.Logging.LogLevel wevew, object data)
        {
            owig(sewf, wevew, UwUSpwitUwUAndJoin(data.ToString()));
        }

        public delegate void OwigModManagewWogHandwew(ModManager modManagew, string wogStwing, string stackTwace, LogType type);
        public static void UwU_ModManagewWogHandwew(OwigModManagewWogHandwew owig, ModManager modManagew, string wogStwing, string stackTwace, LogType type)
        {
            //owig(modManagew, UwUSpwitUwUAndJoin(wogStwing), UwUSpwitUwUAndJoin(stackTwace), type);
            if (type == LogType.Error || type == LogType.Exception)
            {
                File.AppendAllText("exceptionWog.txt", UwUSpwitUwUAndJoin(wogStwing) + Environment.NewLine);
                File.AppendAllText("exceptionWog.txt", UwUSpwitUwUAndJoin(stackTwace) + Environment.NewLine);
                return;
            }
            File.AppendAllText("consoweWog.txt", UwUSpwitUwUAndJoin(wogStwing) + Environment.NewLine);
        }
        public delegate void OwigWogFixWogHandwew(LogFix.LogFix wogFix, string wogStwing, string stackTwace, LogType type);
        public static void UwU_WogFixWogHandwew(OwigWogFixWogHandwew owig, LogFix.LogFix wogFix, string wogStwing, string stackTwace, LogType type)
        {
            //owig(modManagew, UwUSpwitUwUAndJoin(wogStwing), UwUSpwitUwUAndJoin(stackTwace), type);
            if (type == LogType.Error || type == LogType.Exception)
            {
                File.AppendAllText("exceptionWog.txt", UwUSpwitUwUAndJoin(wogStwing) + Environment.NewLine);
                File.AppendAllText("exceptionWog.txt", UwUSpwitUwUAndJoin(stackTwace) + Environment.NewLine);
                return;
            }
            File.AppendAllText("consoweWog.txt", UwUSpwitUwUAndJoin(wogStwing) + Environment.NewLine);
        }

        //public delegate void OwigWogFixWogHandwew(BepInEx.BaseUnityPlugin sewf, string wogStwing, string stackTwace, LogType type);
        //public static void UwU_WogFixWogHandwew(OwigWogFixWogHandwew owig, BepInEx.BaseUnityPlugin sewf, string wogStwing, string stackTwace, LogType type)
        //{
        //    //owig(modManagew, UwUSpwitUwUAndJoin(wogStwing), UwUSpwitUwUAndJoin(stackTwace), type);
        //    if (type == LogType.Error || type == LogType.Exception)
        //    {
        //        File.AppendAllText("exceptionWog.txt", UwUSpwitUwUAndJoin(wogStwing) + Environment.NewLine);
        //        File.AppendAllText("exceptionWog.txt", UwUSpwitUwUAndJoin(stackTwace) + Environment.NewLine);
        //        return;
        //    }
        //    File.AppendAllText("consoweWog.txt", UwUSpwitUwUAndJoin(wogStwing) + Environment.NewLine);
        //}

        public delegate void OwigConsoweWwite(string vawue);
        public static void UwU_ConsoweWwite(OwigConsoweWwite owig, string vawue)
        {
            owig(UwUSpwitUwUAndJoin(vawue));
        }

        private static string UwUSpwitUwUAndJoin(string owig)
        {
            if (string.IsNullOrEmpty(owig)) return owig;
            string[] stwings = Regex.Split(owig, Environment.NewLine);
            for (int i = 0; i < stwings.Length; i++)
            {
                stwings[i] = UWUfyStwing(PwepwocessDiawoge(stwings[i]));
            }
            return string.Join(Environment.NewLine, stwings);
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
            if (string.IsNullOrEmpty(owig)) return owig;
            //Debug.Log("uwufying: " + owig + " -> " + uwu_simpwe.Aggregate(uwu_wegex.Aggregate(owig, (current, value) => Regex.Replace(current, value.Key, value.Value)), (current, value) => current.Replace(value.Key, value.Value)));
            return uwu_simpwe.Aggregate(uwu_wegex.Aggregate(owig, (cuwwent, vawue) => Regex.Replace(cuwwent, vawue.Key, vawue.Value)), (cuwwent, vawue) => cuwwent.Replace(vawue.Key, vawue.Value));
        }


        static char[] sepawatows = { '-', '.', ' ' };

        public static string PwepwocessDiawoge(string owig)
        {
            if (string.IsNullOrEmpty(owig)) return owig;
            // Stuttew
            int fiwstSepawatow = owig.IndexOfAny(sepawatows);
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
                owig = owig.TrimEnd(sepawatows);
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
                //if (Futile.atlasManager.GetAtlasWithName(iwwust.fileName) != null)
                //{
                //    return;
                //}
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
