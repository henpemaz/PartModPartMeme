using MonoMod.RuntimeDetour;
using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewGamePassageMod
{
    public class NewGamePassageMod : PartialityMod
    {
        

        public NewGamePassageMod()
        {
            this.ModID = "NewGamePassageMod";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            On.ProcessManager.RequestMainProcessSwitch += processswitch_hk;
            On.Menu.SlideShow.CommunicateWithUpcomingProcess += slideshowcomms_hk;

            On.Menu.FastTravelScreen.GetAccessibleShelterNamesOfRegion += accessibleshelters_hk;
            On.Menu.FastTravelScreen.Update += ftsupdate_hk;

            On.PlayerProgression.LoadMapTexture_1 += loadmap_hk;


            //On.RWInput.PlayerInput += rwinput_playerinput_hk;
            //new Hook(typeof(UnityEngine.Input).GetMethod("GetKey", new Type[] { typeof(string) }), typeof(KeybindsRandomizer).GetMethod("input_GetKey_str_hk"));
            //NativeDetour GetAxisRawDetour = new NativeDetour(typeof(UnityEngine.Input).GetMethod("GetAxisRaw"), typeof(KeybindsRandomizer).GetMethod("GetAxisRaw_hk"));
            //GetAxisRaw_orig = GetAxisRawDetour.GenerateTrampoline<Func<string, float>>();
        }

        private void loadmap_hk(On.PlayerProgression.orig_LoadMapTexture_1 orig, PlayerProgression self, int regionIndex)
        {
            if (self.rainWorld.setup.revealMap == true)
            {
                return; // The map will reveal itself to you, no need to load :)
            }
            orig(self, regionIndex);
        }

        private void ftsupdate_hk(On.Menu.FastTravelScreen.orig_Update orig, Menu.FastTravelScreen self)
        {
            bool oldval = self.manager.rainWorld.setup.revealMap;
            self.manager.rainWorld.setup.revealMap = true;
            orig(self);
            self.manager.rainWorld.setup.revealMap = oldval;

        }

        private List<string> accessibleshelters_hk(On.Menu.FastTravelScreen.orig_GetAccessibleShelterNamesOfRegion orig, Menu.FastTravelScreen self, string regionAcronym)
        {
            List<string> lst = new List<string>();
            if (self.activeWorld != null)
            {
                int num = 0;
                while (num < self.activeWorld.NumberOfRooms)
                {
                    AbstractRoom abstractRoom = self.activeWorld.GetAbstractRoom(self.activeWorld.firstRoomIndex + num);
                    if (abstractRoom.shelter)
                    {
                        lst.Add(abstractRoom.name);
                    }
                    num++;
                }
            }
            // does not return null on empty ;)
            return lst;
        }

        private void slideshowcomms_hk(On.Menu.SlideShow.orig_CommunicateWithUpcomingProcess orig, Menu.SlideShow self, MainLoopProcess nextProcess)
        {
            orig(self, nextProcess);

            // From SlugcatSelect.comms
            if (nextProcess.ID == ProcessManager.ProcessID.FastTravelScreen)
            {
                (nextProcess as Menu.FastTravelScreen).initiateCharacterFastTravel = true;
            }
        }

        private void processswitch_hk(On.ProcessManager.orig_RequestMainProcessSwitch orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == ProcessManager.ProcessID.Game && ((self.currentMainLoop.ID == ProcessManager.ProcessID.SlugcatSelect && self.menuSetup.startGameCondition == ProcessManager.MenuSetup.StoryGameInitCondition.New) || (self.currentMainLoop.ID == ProcessManager.ProcessID.SlideShow && (self.nextSlideshow == Menu.SlideShow.SlideShowID.WhiteIntro || self.nextSlideshow == Menu.SlideShow.SlideShowID.YellowIntro))))
            {
                ID = ProcessManager.ProcessID.FastTravelScreen;
            }
            orig(self, ID);
        }


    }
}
