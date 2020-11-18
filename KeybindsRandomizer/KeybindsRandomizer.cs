using MonoMod.RuntimeDetour;
using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KeybindsRandomizer
{
    public class KeybindsRandomizer : PartialityMod
    {
        public KeybindsRandomizer()
        {
            this.ModID = "KeybindsRandomizer";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            shuffle_inputs();
            //On.RWInput.PlayerInput += rwinput_playerinput_hk;
            new Hook(typeof(UnityEngine.Input).GetMethod("GetKey", new Type[] { typeof(string) }), typeof(KeybindsRandomizer).GetMethod("input_GetKey_str_hk"));
            new Hook(typeof(UnityEngine.Input).GetMethod("GetKey", new Type[] { typeof(UnityEngine.KeyCode) }), typeof(KeybindsRandomizer).GetMethod("input_GetKey_kc_hk"));
            //new Hook(typeof(UnityEngine.Input).GetMethod("GetAxisRaw"), typeof(KeybindsRandomizer).GetMethod("GetAxisRaw_hk"));
            new Hook(typeof(RainWorldGame).GetConstructor(new Type[] { typeof(ProcessManager) }), typeof(KeybindsRandomizer).GetMethod("RainWorldGame_hk"));

            // Thanks Slimecubed
            NativeDetour testDetour = new NativeDetour(typeof(UnityEngine.Input).GetMethod("GetAxisRaw"), typeof(KeybindsRandomizer).GetMethod("GetAxisRaw_hk"));
            GetAxisRaw_orig = testDetour.GenerateTrampoline<Func<string, float>>();
        }
        private static Func<string, float> GetAxisRaw_orig;


        //public delegate float GetAxisRaw_orig(string axisName);
        public static float GetAxisRaw_hk(string axisName)
        {
            float multiplier = 1f;
            int index = 0;
            char last = axisName.Last();
            UnityEngine.Debug.LogError("detoured method ran");
            if (last == '1') index = 1;
            else if (last == '2') index = 2;
            else if (last == '3') index = 3;
            else if (last == '4') index = 4;
            if (controlScramblers[index].flipX && (axisName.StartsWith("Horizontal") || axisName.StartsWith("DschockDpadX") || axisName.StartsWith("XboxDpadX"))) multiplier = -1f;
            if (controlScramblers[index].flipY && (axisName.StartsWith("Vertical") || axisName.StartsWith("DschockDpadY") || axisName.StartsWith("XboxDpadY"))) multiplier = -1f;
            if (controlScramblers[index].rotate)
            {
                if (axisName.StartsWith("Horizontal")) axisName = axisName.Replace("Horizontal", "Vertical");
                else if (axisName.StartsWith("DschockDpadX")) axisName = axisName.Replace("DschockDpadX", "DschockDpadY");
                else if (axisName.StartsWith("XboxDpadX")) axisName = axisName.Replace("XboxDpadX", "XboxDpadY");
                else if (axisName.StartsWith("Vertical")) axisName = axisName.Replace("Vertical", "Horizontal");
                else if (axisName.StartsWith("DschockDpadY")) axisName = axisName.Replace("DschockDpadY", "DschockDpadX");
                else if (axisName.StartsWith("XboxDpadY")) axisName = axisName.Replace("XboxDpadY", "XboxDpadX");
            }
            return multiplier * GetAxisRaw_orig(axisName);
        }

        public delegate void RainWorldGame_orig(RainWorldGame instance, ProcessManager manager);
        public static void RainWorldGame_hk(RainWorldGame_orig orig, RainWorldGame instance, ProcessManager manager)
        {
            orig(instance, manager);
            shuffle_inputs();
        }

        struct AxisScrambler
        {
            public bool flipX;
            public bool flipY;
            public bool rotate;

            public AxisScrambler(bool flipX, bool flipY, bool rotate)
            {
                this.flipX = flipX;
                this.flipY = flipY;
                this.rotate = rotate;
            }
        }

        static AxisScrambler[] controlScramblers = new AxisScrambler[5];

        private static void shuffle_inputs()
        {
            UnityEngine.KeyCode[] kbCodesShufled = kbCodes.OrderBy(x => UnityEngine.Random.value).ToArray();
            kbMapping = new Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode>();
            for (int i = 0; i < kbCodes.Length; i++)
            {
                kbMapping.Add(kbCodes[i], kbCodesShufled[i]);
            }

            UnityEngine.KeyCode[] ctrlCodesShufled = ctrlCodes.OrderBy(x => UnityEngine.Random.value).ToArray();
            UnityEngine.KeyCode[] ctrl1CodesShufled = ctrlCodes.OrderBy(x => UnityEngine.Random.value).ToArray();
            UnityEngine.KeyCode[] ctrl2CodesShufled = ctrlCodes.OrderBy(x => UnityEngine.Random.value).ToArray();
            UnityEngine.KeyCode[] ctrl3CodesShufled = ctrlCodes.OrderBy(x => UnityEngine.Random.value).ToArray();
            UnityEngine.KeyCode[] ctrl4CodesShufled = ctrlCodes.OrderBy(x => UnityEngine.Random.value).ToArray();
            ctrlMapping = new Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode>();
            ctrl1Mapping = new Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode>();
            ctrl2Mapping = new Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode>();
            ctrl3Mapping = new Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode>();
            ctrl4Mapping = new Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode>();
            for (int i = 0; i < ctrlCodes.Length; i++)
            {
                ctrlMapping.Add(ctrlCodes[i], ctrlCodesShufled[i]);
                ctrl1Mapping.Add(ctrl1Codes[i], ctrl1CodesShufled[i]);
                ctrl2Mapping.Add(ctrl2Codes[i], ctrl2CodesShufled[i]);
                ctrl3Mapping.Add(ctrl3Codes[i], ctrl3CodesShufled[i]);
                ctrl4Mapping.Add(ctrl4Codes[i], ctrl4CodesShufled[i]);
            }

            for (int i = 0; i < controlScramblers.Length; i++)
            {
                controlScramblers[i] = new AxisScrambler(UnityEngine.Random.value > 0.5f, UnityEngine.Random.value > 0.5f, UnityEngine.Random.value > 0.5f);
                //controlScramblers[i] = new AxisScrambler(false,false, true);
            }
        }


        // UNUSED
        //private Player.InputPackage rwinput_playerinput_hk(On.RWInput.orig_PlayerInput orig, int playerNumber, Options options, RainWorldGame.SetupValues setup)
        //{
        //    // TODO shuffle gamepad axis

        //    return orig(playerNumber, options, setup);
        //}


        public delegate bool input_GetKey_str_orig(string name);
        public static bool input_GetKey_str_hk(input_GetKey_str_orig orig, string name)
        {
            try
            {
                UnityEngine.KeyCode key = (UnityEngine.KeyCode)Enum.Parse(typeof(UnityEngine.KeyCode), name.ToUpper());
                return UnityEngine.Input.GetKey(key);
            }
            catch
            {

            }
            return orig(name);
        }


        public delegate bool input_GetKey_kc_orig(UnityEngine.KeyCode key);
        public static bool input_GetKey_kc_hk(input_GetKey_kc_orig orig, UnityEngine.KeyCode key)
        {
            UnityEngine.KeyCode newkey = default(UnityEngine.KeyCode);
            bool gotit = false;
            if (!bypassShuffle)
            {
                gotit = kbMapping.TryGetValue(key, out newkey);
                if (!gotit) gotit = ctrlMapping.TryGetValue(key, out newkey);
                if (!gotit) gotit = ctrl1Mapping.TryGetValue(key, out newkey);
                if (!gotit) gotit = ctrl2Mapping.TryGetValue(key, out newkey);
                if (!gotit) gotit = ctrl3Mapping.TryGetValue(key, out newkey);
                if (!gotit) gotit = ctrl4Mapping.TryGetValue(key, out newkey);
            }
            if (gotit) key = newkey;
            return orig(key);
        }

        static UnityEngine.KeyCode[] kbCodes = new UnityEngine.KeyCode[] { UnityEngine.KeyCode.Escape,
UnityEngine.KeyCode.F1,
UnityEngine.KeyCode.F2,
UnityEngine.KeyCode.F3,
UnityEngine.KeyCode.F4,
UnityEngine.KeyCode.F5,
UnityEngine.KeyCode.F6,
UnityEngine.KeyCode.F7,
UnityEngine.KeyCode.F8,
UnityEngine.KeyCode.F9,
UnityEngine.KeyCode.F10,
UnityEngine.KeyCode.F11,
UnityEngine.KeyCode.F12,
UnityEngine.KeyCode.ScrollLock,
UnityEngine.KeyCode.BackQuote,
UnityEngine.KeyCode.Alpha1,
UnityEngine.KeyCode.Alpha2,
UnityEngine.KeyCode.Alpha3,
UnityEngine.KeyCode.Alpha4,
UnityEngine.KeyCode.Alpha5,
UnityEngine.KeyCode.Alpha6,
UnityEngine.KeyCode.Alpha7,
UnityEngine.KeyCode.Alpha8,
UnityEngine.KeyCode.Alpha9,
UnityEngine.KeyCode.Alpha0,
UnityEngine.KeyCode.Minus,
UnityEngine.KeyCode.Equals,
UnityEngine.KeyCode.Backspace,
UnityEngine.KeyCode.Insert,
UnityEngine.KeyCode.Home,
UnityEngine.KeyCode.PageUp,
UnityEngine.KeyCode.Delete,
UnityEngine.KeyCode.End,
UnityEngine.KeyCode.PageDown,
UnityEngine.KeyCode.Numlock,
UnityEngine.KeyCode.KeypadDivide,
UnityEngine.KeyCode.KeypadMultiply,
UnityEngine.KeyCode.KeypadMinus,
UnityEngine.KeyCode.UpArrow,
UnityEngine.KeyCode.KeypadPlus,
UnityEngine.KeyCode.LeftArrow,
UnityEngine.KeyCode.RightArrow,
UnityEngine.KeyCode.DownArrow,
UnityEngine.KeyCode.KeypadEnter,
UnityEngine.KeyCode.Tab,
UnityEngine.KeyCode.Q,
UnityEngine.KeyCode.W,
UnityEngine.KeyCode.E,
UnityEngine.KeyCode.R,
UnityEngine.KeyCode.T,
UnityEngine.KeyCode.Y,
UnityEngine.KeyCode.U,
UnityEngine.KeyCode.I,
UnityEngine.KeyCode.O,
UnityEngine.KeyCode.P,
UnityEngine.KeyCode.LeftBracket,
UnityEngine.KeyCode.RightBracket,
UnityEngine.KeyCode.Backslash,
UnityEngine.KeyCode.CapsLock,
UnityEngine.KeyCode.A,
UnityEngine.KeyCode.S,
UnityEngine.KeyCode.D,
UnityEngine.KeyCode.Quote,
UnityEngine.KeyCode.Semicolon,
UnityEngine.KeyCode.F,
UnityEngine.KeyCode.G,
UnityEngine.KeyCode.H,
UnityEngine.KeyCode.J,
UnityEngine.KeyCode.K,
UnityEngine.KeyCode.L,
UnityEngine.KeyCode.LeftShift,
UnityEngine.KeyCode.Z,
UnityEngine.KeyCode.X,
UnityEngine.KeyCode.C,
UnityEngine.KeyCode.V,
UnityEngine.KeyCode.B,
UnityEngine.KeyCode.N,
UnityEngine.KeyCode.M,
UnityEngine.KeyCode.Comma,
UnityEngine.KeyCode.Period,
UnityEngine.KeyCode.Slash,
UnityEngine.KeyCode.RightShift,
UnityEngine.KeyCode.LeftControl,
UnityEngine.KeyCode.LeftAlt,
UnityEngine.KeyCode.Space,
//UnityEngine.KeyCode.RightAlt,
//UnityEngine.KeyCode.AltGr,
UnityEngine.KeyCode.RightControl,
UnityEngine.KeyCode.Return};
        static Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode> kbMapping;

        static UnityEngine.KeyCode[] ctrlCodes = new UnityEngine.KeyCode[] {
        UnityEngine.KeyCode.JoystickButton0,
        UnityEngine.KeyCode.JoystickButton1,
        UnityEngine.KeyCode.JoystickButton2,
        UnityEngine.KeyCode.JoystickButton3,
        UnityEngine.KeyCode.JoystickButton4,
        UnityEngine.KeyCode.JoystickButton5,
        UnityEngine.KeyCode.JoystickButton6,
        UnityEngine.KeyCode.JoystickButton7,
        UnityEngine.KeyCode.JoystickButton8,
        UnityEngine.KeyCode.JoystickButton9,
        };
        static Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode> ctrlMapping;

        static UnityEngine.KeyCode[] ctrl1Codes = new UnityEngine.KeyCode[] {
        UnityEngine.KeyCode.Joystick1Button0,
        UnityEngine.KeyCode.Joystick1Button1,
        UnityEngine.KeyCode.Joystick1Button2,
        UnityEngine.KeyCode.Joystick1Button3,
        UnityEngine.KeyCode.Joystick1Button4,
        UnityEngine.KeyCode.Joystick1Button5,
        UnityEngine.KeyCode.Joystick1Button6,
        UnityEngine.KeyCode.Joystick1Button7,
        UnityEngine.KeyCode.Joystick1Button8,
        UnityEngine.KeyCode.Joystick1Button9,
        };
        static Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode> ctrl1Mapping;

        static UnityEngine.KeyCode[] ctrl2Codes = new UnityEngine.KeyCode[] {
        UnityEngine.KeyCode.Joystick2Button0,
        UnityEngine.KeyCode.Joystick2Button1,
        UnityEngine.KeyCode.Joystick2Button2,
        UnityEngine.KeyCode.Joystick2Button3,
        UnityEngine.KeyCode.Joystick2Button4,
        UnityEngine.KeyCode.Joystick2Button5,
        UnityEngine.KeyCode.Joystick2Button6,
        UnityEngine.KeyCode.Joystick2Button7,
        UnityEngine.KeyCode.Joystick2Button8,
        UnityEngine.KeyCode.Joystick2Button9,
        };
        static Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode> ctrl2Mapping;

        static UnityEngine.KeyCode[] ctrl3Codes = new UnityEngine.KeyCode[] {
        UnityEngine.KeyCode.Joystick3Button0,
        UnityEngine.KeyCode.Joystick3Button1,
        UnityEngine.KeyCode.Joystick3Button2,
        UnityEngine.KeyCode.Joystick3Button3,
        UnityEngine.KeyCode.Joystick3Button4,
        UnityEngine.KeyCode.Joystick3Button5,
        UnityEngine.KeyCode.Joystick3Button6,
        UnityEngine.KeyCode.Joystick3Button7,
        UnityEngine.KeyCode.Joystick3Button8,
        UnityEngine.KeyCode.Joystick3Button9,
        };
        static Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode> ctrl3Mapping;

        static UnityEngine.KeyCode[] ctrl4Codes = new UnityEngine.KeyCode[] {
        UnityEngine.KeyCode.Joystick4Button0,
        UnityEngine.KeyCode.Joystick4Button1,
        UnityEngine.KeyCode.Joystick4Button2,
        UnityEngine.KeyCode.Joystick4Button3,
        UnityEngine.KeyCode.Joystick4Button4,
        UnityEngine.KeyCode.Joystick4Button5,
        UnityEngine.KeyCode.Joystick4Button6,
        UnityEngine.KeyCode.Joystick4Button7,
        UnityEngine.KeyCode.Joystick4Button8,
        UnityEngine.KeyCode.Joystick4Button9,
        };
        static Dictionary<UnityEngine.KeyCode, UnityEngine.KeyCode> ctrl4Mapping;
        private static bool bypassShuffle;
    }
}
