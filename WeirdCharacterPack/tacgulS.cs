using MonoMod.RuntimeDetour;
using SlugBase;
using System;
using UnityEngine;

namespace WeirdCharacterPack
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
            // stores a weakreference to the game and uses that.
            On.RoomCamera.ctor += RoomCamera_ctor;

            new Hook(typeof(UnityEngine.Shader).GetMethod("SetGlobalVector", new System.Type[] { typeof(string), typeof(Vector4) }),
                typeof(tacgulS).GetMethod("SetGlobalVector"), this);
            On.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage; ;
        }

        private void SlugcatPage_AddImage(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_AddImage orig, Menu.SlugcatSelectMenu.SlugcatPage self, bool ascended)
        {
            orig(self, ascended);

            if (PlayerManager.GetCustomPlayer(self.slugcatNumber) == this)
            {
                foreach (var item in self.slugcatImage.depthIllustrations)
                {
                    item.sprite.ScaleAroundPointRelative(UnityEngine.Vector2.zero, -1f, 1f);
                    item.pos.x = self.menu.manager.rainWorld.screenSize.x - item.pos.x;
                    item.lastPos.x = self.menu.manager.rainWorld.screenSize.x - item.lastPos.x;
                }
            }
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

        // Fixup for mouse and flipped menus
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

        // moved most to perma hooks and gameref
        protected override void Disable()
        {
            On.Player.checkInput -= Player_checkInput;
            On.Water.DrawSprites -= Water_DrawSprites;
            On.RoomCamera.DrawUpdate -= RoomCamera_DrawUpdate;
            On.HUD.Map.ctor -= Map_ctor;
        }

        protected override void Enable()
        {
            On.Player.checkInput += Player_checkInput;
            On.Water.DrawSprites += Water_DrawSprites;
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.HUD.Map.ctor += Map_ctor;
        }

        private void Map_ctor(On.HUD.Map.orig_ctor orig, HUD.Map self, HUD.HUD hud, HUD.Map.MapData mapData)
        {
            orig(self, hud, mapData);

            if (hud.rainWorld.processManager.currentMainLoop is RainWorldGame g && IsMe(g) && self.inFrontContainer != null)
            {
                self.inFrontContainer.scaleX = -1;
                self.inFrontContainer.x = hud.rainWorld.screenSize.x;
            }
        }

        private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);

            if (IsMe(self.game))
            {
                self.levelGraphic.x-=0.5f;
                self.backgroundGraphic.x-= 0.5f;
            }
        }

        private void Water_DrawSprites(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if(rCam.room != null && IsMe(rCam.game) && sLeaser.sprites != null && sLeaser.sprites.Length > 1)
            {
                var deepwater = sLeaser.sprites[1];
                if (deepwater._color != new Color(0f, 0f, 0f)) deepwater._color.r = 1f - deepwater._color.r;
            }
        }

        private void Player_checkInput(On.Player.orig_checkInput orig, Player self)
        {
            orig(self);
            if ((IsMe(self) && !IsMe(self.abstractCreature.Room.world.game)) ||(!IsMe(self) && IsMe(self.abstractCreature.Room.world.game))) // if the game isnt mirrored, mirror the inputs
            {
                self.input[0].x *= -1;
                self.input[0].downDiagonal *= -1;
                self.input[0].analogueDir.x *= -1;
            }
        }

        public delegate void orig_SetGlobalVector(string a, Vector4 b);
        public void SetGlobalVector(orig_SetGlobalVector orig, string name, Vector4 vals)
        {
            if (name == "_spriteRect" && IsMe(gameRef.Target<RainWorldGame>()))
            {
                float ratio = 1f - (0.5f / Futile.screen.pixelWidth);
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
                    //c.ScaleAroundPointRelative(self.sSize / 2f, -1, 1);
                    c.scaleX = -1;
                    c.x = self.sSize.x;
                }
            }
        }

        private WeakReference gameRef = new WeakReference(null);
    }
}