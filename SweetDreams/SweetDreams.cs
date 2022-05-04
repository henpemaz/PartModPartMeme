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
using RWCustom;
using System.Collections;

[assembly: AssemblyTrademark("Intikus, Tealppup & Henpemaz")]

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: System.Runtime.CompilerServices.SuppressIldasm()]

namespace SweetDreams
{
    [BepInEx.BepInPlugin("henpemaz.sweetdreams", "SweetDreams", "1.0")]
    public class SweetDreams : BepInEx.BaseUnityPlugin
    {
        public string author = "Intikus, Tealppup & Henpemaz";
        public static SweetDreams instance;

        public void OnEnable()
        {
            instance = this;

            // magic happens here
            On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;

            On.RainWorldGame.Update += RainWorldGame_Update;
        }

        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            // testing
            orig(self);
            if (self.clock > 80 && self.Players[0].Room.realizedRoom != null && self.Players[0].Room.realizedRoom.readyForAI && self.manager.upcomingProcess == null) self.Win(false);
        }

        private void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, SleepAndDeathScreen self, KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            orig(self, package);
            // sleep and death, but we only care about sleep
            if (self.IsSleepScreen)
            {
                // check for friend in shelter
                var friendInShelter = package.sessionRecord.friendInDen;
                // if friend found, load image
                if(friendInShelter != null)
                {

                }
                // new images is loaded on top so not to change indexes

                var slugstration = self.scene.depthIllustrations.First(v => v.fileName.StartsWith("Sleep - 2 - White") || v.fileName.StartsWith("Sleep - 2 - Yellow") || v.fileName.StartsWith("Sleep - 2 - Red"));
                //todo handle slugcat not found!!


                var slugsprite = slugstration.sprite;
                var slugdepth = slugstration.depth;
                MenuDepthIllustration top;
                MenuDepthIllustration bottom;
                //self.scene.AddIllustration(bottom = new MenuDepthIllustration(self, self.scene, "", "Pink white under", new Vector2(720, 122), slugdepth, MenuDepthIllustration.MenuShader.Normal));
                //self.scene.AddIllustration(top = new MenuDepthIllustration(self, self.scene, "", "Pink white over", new Vector2(704, 118), slugdepth, MenuDepthIllustration.MenuShader.Normal));
                self.scene.AddIllustration(bottom = new MenuDepthIllustration(self, self.scene, "", "greenbody", new Vector2(640, 157), slugdepth+0.25f, MenuDepthIllustration.MenuShader.Normal));
                self.scene.AddIllustration(top = new MenuDepthIllustration(self, self.scene, "", "greentail", new Vector2(635, 75), slugdepth - 0.125f, MenuDepthIllustration.MenuShader.Normal));

                bottom.sprite.MoveBehindOtherNode(slugsprite);
                top.sprite.MoveInFrontOfOtherNode(slugsprite);

                // move images to the right layer, wether that's on top of below slugcat

            }
        }
    }
}
