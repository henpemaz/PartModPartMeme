using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using Partiality.Modloader;


[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]


namespace DeathHook
{
    public class DeathHookMod : PartialityMod
    {
        public static string currentMovieName;
        public static bool anyCat = true;

        public DeathHookMod()
        {
            this.ModID = "DeathHookMod";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre
            On.RegionState.RainCycleTick += RegionState_RainCycleTick;
        }

        private void RegionState_RainCycleTick(On.RegionState.orig_RainCycleTick orig, RegionState self, int ticks, int foodRepBonus)
        {
            orig(self, ticks, foodRepBonus);

			if (ticks > 0)
			{
				for (int j = 0; j < self.world.NumberOfRooms; j++)
				{
					AbstractRoom abstractRoom = self.world.GetAbstractRoom(self.world.firstRoomIndex + j);
					for (int l = 0; l < abstractRoom.entitiesInDens.Count; l++)
					{
						if (abstractRoom.entitiesInDens[l] is AbstractCreature)
						{
							(abstractRoom.entitiesInDens[l] as AbstractCreature).state.CycleTick();
						}
					}
				}
			}
		}
    }
}
