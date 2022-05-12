using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using System.Collections.Generic;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour.HookGen;
using Triton;

[assembly: AssemblyTrademark("Henpemaz")]

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace LuaMods
{
    [BepInEx.BepInPlugin("henpemaz.luamods", "LuaMods", "1.0")]
    public class LuaMods : BepInEx.BaseUnityPlugin
    {
        public string author = "Henpemaz";
        public static LuaMods instance;

        private Lua lua;

        public void OnEnable()
        {
            instance = this;

            try
            {
                // From here https://github.com/kevzhao2/triton-old/
                lua = new Lua();
                lua.ImportNamespace("On");
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            
        }

        public void Update()
        {
            if (Input.GetKeyDown("l"))
            {
                lua.DoString(System.IO.File.ReadAllText("./script.lua"));
            }

        }


        //private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        //{
        //    orig(self);
        //    UnityEngine.Debug.Log("About to run some LUA");
        //    using (var lua = new Lua())
        //    {
        //        lua.DoString("print('Hello, world!')");
        //        lua.DoString(@"
        //using 'System'
        //using 'UnityEngine'
        //Console.WriteLine('A string in Lua')
        //Debug.Log('Hello from Lua')");
        //    }
        //}

    }
}
