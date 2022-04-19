using System.Security;
using System.Security.Permissions;
using System.Reflection;
using SlugBase;

[assembly: AssemblyTrademark("Henpemaz")]

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
