using Partiality.Modloader;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GuardianSemaphore
{
    public class GuardianSemaphore : PartialityMod
    {
        public GuardianSemaphore()
        {
            this.ModID = "GuardianSemaphore";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking codes would go here

            On.Room.Update += Update_hook;
        }

        public static void Update_hook(On.Room.orig_Update orig, Room instance)
        {
            orig(instance);
            if (instance.game.cameras[0].room == instance && instance.abstractRoom != null && instance.abstractRoom.index == 559)//SB_B03
            {
                Color color = new Color(1f, 1f, 1f);
                AbstractRoom d03 = instance.game.overWorld.activeWorld.GetAbstractRoom(565);
                if(d03.realizedRoom != null)
                {
                    color = new Color(0f, 1f, 0f);
                    for (int i = 0; i < instance.game.world.loadingRooms.Count; i++)
                    {
                        if (instance.game.world.loadingRooms[i].room.abstractRoom.index == d03.index)
                        {
                            color = new Color(1f, 0f, 0f);
                            break;
                        }
                    }
                }
                instance.game.cameras[0].shortcutGraphics.ColorEntrance(instance.abstractRoom.ExitIndex(565 /*SB_D03*/), color);
            }
        }
    }
}
