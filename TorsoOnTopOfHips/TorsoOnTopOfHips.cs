using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using Menu;
using System.Collections.Generic;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using MonoMod.RuntimeDetour.HookGen;





[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: System.Runtime.CompilerServices.SuppressIldasmAttribute()]
namespace TorsoOnTopOfHips
{
    [BepInEx.BepInPlugin("henpemaz.torsoontopofhips", "TorsoOnTopOfHips", "1.0")]
    public class TorsoOnTopOfHips : BepInEx.BaseUnityPlugin
    {
        public string author = "Henpemaz";
        public static TorsoOnTopOfHips instance;


        public void OnEnable()
        {
            instance = this;

            On.RainWorld.Start += RainWorld_Start;
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);

            FancyPlayerGraphics.AddToContainer += FancyPlayerGraphics_AddToContainer;
        }

        private void FancyPlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);

            sLeaser.sprites[5].MoveInFrontOfOtherNode(sLeaser.sprites[6]);
        }
    }

    public static class FancyPlayerGraphics
    {
        private static BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        public static event On.PlayerGraphics.hook_ctor ctor
        {
            add
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetConstructors()[0] is ConstructorInfo c)
                    HookEndpointManager.Add<On.PlayerGraphics.hook_ctor>(MethodBase.GetMethodFromHandle(c.MethodHandle), value);
            }
            remove
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetConstructors()[0] is ConstructorInfo c)
                    HookEndpointManager.Remove<On.PlayerGraphics.hook_ctor>(MethodBase.GetMethodFromHandle(c.MethodHandle), value);
            }
        }

        public static event On.PlayerGraphics.hook_InitiateSprites InitiateSprites
        {
            add
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetMethod("InitiateSprites", any) is MethodInfo m)
                    HookEndpointManager.Add<On.PlayerGraphics.hook_InitiateSprites>(MethodBase.GetMethodFromHandle(m.MethodHandle), value);
            }
            remove
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetMethod("InitiateSprites", any) is MethodInfo m)
                    HookEndpointManager.Remove<On.PlayerGraphics.hook_InitiateSprites>(MethodBase.GetMethodFromHandle(m.MethodHandle), value);
            }
        }
        public static event On.PlayerGraphics.hook_AddToContainer AddToContainer
        {
            add
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetMethod("AddToContainer", any) is MethodInfo m)
                    HookEndpointManager.Add<On.PlayerGraphics.hook_AddToContainer>(MethodBase.GetMethodFromHandle(m.MethodHandle), value);
            }
            remove
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetMethod("AddToContainer", any) is MethodInfo m)
                    HookEndpointManager.Remove<On.PlayerGraphics.hook_AddToContainer>(MethodBase.GetMethodFromHandle(m.MethodHandle), value);
            }
        }
        public static event On.PlayerGraphics.hook_ApplyPalette ApplyPalette
        {
            add
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetMethod("ApplyPalette", any) is MethodInfo m)
                    HookEndpointManager.Add<On.PlayerGraphics.hook_ApplyPalette>(MethodBase.GetMethodFromHandle(m.MethodHandle), value);
            }
            remove
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetMethod("ApplyPalette", any) is MethodInfo m)
                    HookEndpointManager.Remove<On.PlayerGraphics.hook_ApplyPalette>(MethodBase.GetMethodFromHandle(m.MethodHandle), value);
            }
        }
        public static event On.PlayerGraphics.hook_DrawSprites DrawSprites
        {
            add
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetMethod("DrawSprites", any) is MethodInfo m)
                    HookEndpointManager.Add<On.PlayerGraphics.hook_DrawSprites>(MethodBase.GetMethodFromHandle(m.MethodHandle), value);
            }
            remove
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetMethod("DrawSprites", any) is MethodInfo m)
                    HookEndpointManager.Remove<On.PlayerGraphics.hook_DrawSprites>(MethodBase.GetMethodFromHandle(m.MethodHandle), value);
            }
        }
        public static event On.PlayerGraphics.hook_Update Update
        {
            add
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetMethod("Update", any) is MethodInfo m)
                    HookEndpointManager.Add<On.PlayerGraphics.hook_Update>(MethodBase.GetMethodFromHandle(m.MethodHandle), value);
            }
            remove
            {
                if (Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats") is Type fpg && fpg.GetMethod("Update", any) is MethodInfo m)
                    HookEndpointManager.Remove<On.PlayerGraphics.hook_Update>(MethodBase.GetMethodFromHandle(m.MethodHandle), value);
            }
        }
    }
}
