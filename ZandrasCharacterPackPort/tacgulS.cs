using MonoMod.RuntimeDetour;
using SlugBase;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
    internal class tacgulS : SlugBaseCharacter
	{
        private Hook h;

        public tacgulS() : base("zcptacguls", FormatVersion.V1, 0, true) {
            On.Menu.Menu.ctor += Menu_ctor;
            On.Menu.DreamScreen.GetDataFromGame += DreamScreen_GetDataFromGame;
            On.Menu.SleepAndDeathScreen.GetDataFromGame += SleepAndDeathScreen_GetDataFromGame;
        }

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

        public override string DisplayName => "tacgulS";
		public override string Description => @".tacgulS a tsuJ
.secneuqesnoc eht wonk t'nod llits ew tub ,daeh sti tih evah ot smees yug elttil sihT";

        public override bool HasDreams => true;


        protected override void Disable()
		{
			On.RoomCamera.ctor -= RoomCamera_ctor;

            h.Undo();
            h.Free();
            h = null;
        }

		protected override void Enable()
		{
            On.RoomCamera.ctor += RoomCamera_ctor;

            h = new Hook(typeof(UnityEngine.Shader).GetMethod("SetGlobalVector", new System.Type[]{ typeof(string), typeof(Vector4) }),
                typeof(tacgulS).GetMethod("SetGlobalVector"), this);
		}

        public delegate void orig_SetGlobalVector(string a, Vector4 b);
        public void SetGlobalVector(orig_SetGlobalVector orig, string name, Vector4 vals)
        {
            if (name == "_spriteRect")
            {
                //float f = vals[2];
                //vals[2] = vals[0];
                //vals[0] = f;
                float ratio = 1f - (1f / Futile.screen.pixelWidth);
                //float offset = 1400 - Futile.screen.pixelWidth;
                vals[2] = -vals[2] + ratio;
                vals[0] = -vals[0] + ratio;
            }

            //vector = "effective campos"
            //-vector + campos = -displacement of camera (related to image) = displacement of level related to stage
            //Shader.SetGlobalVector("_spriteRect", new 
            //    Vector4(
            //    (-vector.x - 0.5f + this.CamPos(this.currentCameraPosition).x) / this.sSize.x,
            //    (-vector.y + 0.5f + this.CamPos(this.currentCameraPosition).y) / this.sSize.y,
            //    (-vector.x - 0.5f + this.levelGraphic.width + this.CamPos(this.currentCameraPosition).x) / this.sSize.x,
            //    (-vector.y + 0.5f + this.levelGraphic.height + this.CamPos(this.currentCameraPosition).y) / this.sSize.y));

            orig(name, vals);
        }

        private void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
        {
			orig(self, game, cameraNumber);
            foreach (var c in self.SpriteLayers)
            {
				c.ScaleAroundPointRelative(self.sSize/2f, -1, 1);
            }
        }
    }
}