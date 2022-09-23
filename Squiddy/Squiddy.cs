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
    [BepInEx.BepInPlugin("henpemaz.squiddymod", "Squiddy", "0.5")]
    public class Squiddy : BepInEx.BaseUnityPlugin
    {
        public string author = "Henpemaz";
        public static Squiddy instance;

        public void OnEnable()
        {
            instance = this;
            PlayerManager.RegisterCharacter(new SquiddyBase());
            //On.VoidSea.VoidWorm.MainWormBehavior.SuperSwim += MainWormBehavior_SuperSwim;
            //On.VoidSea.VoidWorm.Swim += VoidWorm_Swim;
        }

        //private void VoidWorm_Swim(On.VoidSea.VoidWorm.orig_Swim orig, VoidSea.VoidWorm self)
        //{
        //    orig(self);
        //    orig(self);
        //    orig(self);
        //    orig(self);
        //    orig(self);
        //    orig(self);
        //    orig(self);
        //    orig(self);
        //    orig(self);
        //    orig(self);
        //    orig(self);
        //}

        //private void MainWormBehavior_SuperSwim(On.VoidSea.VoidWorm.MainWormBehavior.orig_SuperSwim orig, VoidSea.VoidWorm.MainWormBehavior self, float add)
        //{
        //    orig(self, add * 10);
        //}

        void Update() // debug thinghies
        {
            //if (Input.GetKeyDown("1"))
            //{
            //    if (GameObject.FindObjectOfType<RainWorld>()?.processManager?.currentMainLoop is RainWorldGame game)
            //        Debug.LogError(game.cameras[0].virtualMicrophone.listenerPoint);
            //}
        }
    }
}
