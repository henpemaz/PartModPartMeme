using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using Menu;
using SlugBase;
using System.Collections.Generic;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour.HookGen;

[assembly: AssemblyTrademark("Zandra & Henpemaz")]

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace Squiddy
{
    [BepInEx.BepInPlugin("henpemaz.squiddymod", "Squiddy", "1.0")]
    public class Squiddy : BepInEx.BaseUnityPlugin
    {
        public string author = "Henpemaz";
        public static Squiddy instance;

        public void OnEnable()
        {
            instance = this;
            PlayerManager.RegisterCharacter(new SquiddyBase());
        }
    }
}
