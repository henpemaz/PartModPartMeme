using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using Menu;
using SlugBase;

[assembly: AssemblyTrademark("Zandra & Henpemaz")]

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ZandrasCharacterPackPort
{
    [BepInEx.BepInPlugin("henpemaz.zandrascharacterpackport", "ZandrasCharacterPack", "1.1")]
    public class ZandrasCharacterPackPort : BepInEx.BaseUnityPlugin
    {
        public string author = "Zandra, Henpemaz";
        static ZandrasCharacterPackPort instance;

        public void OnEnable()
        {
            instance = this;
            PlayerManager.RegisterCharacter(new Kineticat());
            PlayerManager.RegisterCharacter(new Aquaria());
            PlayerManager.RegisterCharacter(new VultCat());
            PlayerManager.RegisterCharacter(new KarmaCat());
            PlayerManager.RegisterCharacter(new Skittlecat());
            PlayerManager.RegisterCharacter(new VVVVVCat());
            PlayerManager.RegisterCharacter(new PseudoWingcat());
        }
    }

    internal static class WeakRefExt
    {
        public static T Target<T>(this WeakReference self) { return (T)self?.Target; }
    }
}
