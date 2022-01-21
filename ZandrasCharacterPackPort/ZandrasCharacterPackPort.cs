using Partiality.Modloader;
using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using Menu;
using SlugBase;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ZandrasCharacterPackPort
{
    public class ZandrasCharacterPackPort : PartialityMod
    {
        public ZandrasCharacterPackPort()
        {
            this.ModID = "Zandra's Character Pack Port";
            this.Version = "1.1";
            this.author = "Zandra & Henpemaz";

            instance = this;
        }

        public static ZandrasCharacterPackPort instance;

        public override void OnEnable()
        {
            base.OnEnable();
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
