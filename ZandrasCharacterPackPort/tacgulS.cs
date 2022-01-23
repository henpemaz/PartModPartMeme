using MonoMod.RuntimeDetour;
using SlugBase;
using System;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
    internal class tacgulS : SlugBaseCharacter
	{
        public tacgulS() : base("zcptacguls", FormatVersion.V1, 0, true) {
            On.Menu.Menu.ctor += Menu_ctor;
            On.Menu.DreamScreen.GetDataFromGame += DreamScreen_GetDataFromGame;
            On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;

            new Hook(typeof(Menu.RectangularMenuObject).GetProperty("MouseOver").GetGetMethod(),
                typeof(tacgulS).GetMethod("RectangularMenuObject_MouseOver"), this);

            // moved here because unable to hit this hook in arena mode
            On.RoomCamera.ctor += RoomCamera_ctor;

            new Hook(typeof(UnityEngine.Shader).GetMethod("SetGlobalVector", new System.Type[] { typeof(string), typeof(Vector4) }),
                typeof(tacgulS).GetMethod("SetGlobalVector"), this);
        }


        public delegate bool orig_MouseOver(Menu.RectangularMenuObject self);
        public bool RectangularMenuObject_MouseOver(orig_MouseOver orig, Menu.RectangularMenuObject self)
        {
            var p = self.menu.manager.rainWorld.progression;
            if ((IsMe(p.currentSaveState) || (p.currentSaveState == null && IsMe(p.starvedSaveState))) 
                || (self.menu is Menu.KarmaLadderScreen k && IsMe(k.saveState)) 
                || (self.menu is Menu.DreamScreen d && IsMe(d.fromGameDataPackage?.saveState)))
            {
                Vector2 screenPos = new Vector2(Futile.screen.pixelWidth - self.ScreenPos.x, self.ScreenPos.y);
                return self.menu.mousePosition.x < screenPos.x && self.menu.mousePosition.y > screenPos.y && self.menu.mousePosition.x > screenPos.x - self.size.x && self.menu.mousePosition.y < screenPos.y + self.size.y;
            }
            return orig(self);
        }

        public override string DisplayName => "tacgulS";
        public override string Description => @".tacgulS a tsuJ
.secneuqesnoc eht wonk t'nod llits ew tub ,daeh sti tih evah ot smees yug elttil sihT";

        public override bool HasDreams => true;


        // flips menus around based on current playthrough
        private void Menu_ctor(On.Menu.Menu.orig_ctor orig, Menu.Menu self, ProcessManager manager, ProcessManager.ProcessID ID)
        {
            orig(self, manager, ID);
            var p = manager.rainWorld.progression;
            if (IsMe(p.currentSaveState) || (p.currentSaveState == null && IsMe(p.starvedSaveState)))
            {
                self.container.ScaleAroundPointRelative(manager.rainWorld.screenSize / 2f, -1, 1);
            }
        }

        // bugfix for dream -> sleep because progression.currentsavestate is reset
        private void SleepAndDeathScreen_GetDataFromGame(On.Menu.SleepAndDeathScreen.orig_GetDataFromGame orig, Menu.SleepAndDeathScreen self, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            orig(self, package);
            var p = self.manager.rainWorld.progression;
            if (!(IsMe(p.currentSaveState) || (p.currentSaveState == null && IsMe(p.starvedSaveState))) && IsMe(package?.saveState))
            {
                self.container.ScaleAroundPointRelative(self.manager.rainWorld.screenSize / 2f, -1, 1);
            }
        }

        // bugfix for dream because progression.currentsavestate is reset
        private void DreamScreen_GetDataFromGame(On.Menu.DreamScreen.orig_GetDataFromGame orig, Menu.DreamScreen self, DreamsState.DreamID dreamID, Menu.KarmaLadderScreen.SleepDeathScreenDataPackage package)
        {
            orig(self, dreamID, package);
            var p = self.manager.rainWorld.progression;
            if (!(IsMe(p.currentSaveState) || (p.currentSaveState == null && IsMe(p.starvedSaveState))) && IsMe(package?.saveState))
            {
                self.container.ScaleAroundPointRelative(self.manager.rainWorld.screenSize / 2f, -1, 1);
            }
        }

  //      protected override void Disable()
		//{
		//	On.RoomCamera.ctor -= RoomCamera_ctor;

  //          h.Undo();
  //          h.Free();
  //          h = null;
  //      }

		//protected override void Enable()
		//{
            
		//}

        public delegate void orig_SetGlobalVector(string a, Vector4 b);
        public void SetGlobalVector(orig_SetGlobalVector orig, string name, Vector4 vals)
        {
            if (name == "_spriteRect" && IsMe(gameRef.Target<RainWorldGame>()))
            {
                float ratio = 1f - (1f / Futile.screen.pixelWidth);
                vals[2] = -vals[2] + ratio;
                vals[0] = -vals[0] + ratio;
            }
            orig(name, vals);
        }

        private void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
			orig(self, game, cameraNumber);
            gameRef.Target = game;// nifty
            if (IsMe(game))
            {
                foreach (var c in self.SpriteLayers)
                {
                    c.ScaleAroundPointRelative(self.sSize / 2f, -1, 1);
                }
            }
        }

        private WeakReference gameRef = new WeakReference(null);
    }
}