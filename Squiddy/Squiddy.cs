using System.Security;
using System.Security.Permissions;
using System.Reflection;
using SlugBase;
using UnityEngine;

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
        void Update() // debug thinghies
        {
            if (Input.GetKeyDown("1"))
            {
                if (GameObject.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is RainWorldGame game)
                    Debug.LogError(game.cameras[0].virtualMicrophone.listenerPoint);
            }
        }
    }
}
