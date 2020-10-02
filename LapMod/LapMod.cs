using Partiality.Modloader;
using System;
using RWCustom;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LapMod
{
    public class LapMod : PartialityMod
    {
        public LapMod()
        {
            this.ModID = "LapMod";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public static LapMod instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking codes would go here

            On.ShortcutHandler.Update += ShortcutHandler_Update_patch;

            LapMod.instance = this;
        }
        static int wantsNextRoomCounter = 0;
        static int enteredFromNode = -1;

        static void ShortcutHandler_Update_patch(On.ShortcutHandler.orig_Update orig, ShortcutHandler instance)
        {
            RainWorld rw = UnityEngine.Object.FindObjectOfType<RainWorld>();
            RainWorldGame game = rw.processManager.currentMainLoop as RainWorldGame;
            
            if (Input.GetKey(game.manager.rainWorld.options.controls[0].KeyboardMap))
            {
                // Debug.Log("player wants out");
                wantsNextRoomCounter = 40;
            }
            else if (wantsNextRoomCounter > 0)
            {
                wantsNextRoomCounter--;
            }
            for (int i = instance.transportVessels.Count - 1; i >= 0; i--)
            {
                if (instance.transportVessels[i].room.realizedRoom != null && instance.transportVessels[i].creature is Player && !game.IsArenaSession) // Found Player
                {
                    //Debug.Log("found player in pipe");
                    if (instance.transportVessels[i].wait <= 0) // About to move
                    {
                        //Debug.Log("about to move");
                        Room realizedRoom = instance.transportVessels[i].room.realizedRoom;
                        IntVector2 pos = ShortcutHandler.NextShortcutPosition(instance.transportVessels[i].pos, instance.transportVessels[i].lastPos, realizedRoom);
                        if (realizedRoom.GetTile(pos).shortCut == 2) // About to exit
                        {
                            //Debug.Log("about to exit");
                            // Looping back
                            int num = Array.IndexOf<IntVector2>(realizedRoom.exitAndDenIndex, pos);
                            if (wantsNextRoomCounter <= 0 && enteredFromNode > -1 && !instance.transportVessels[i].room.shelter && !instance.transportVessels[i].room.gate && instance.transportVessels[i].room.connections.Length > 1)
                            {
                                //Debug.Log("looping");
                                realizedRoom.PlaySound(SoundID.Player_Tick_Along_In_Shortcut, 0f, 1f, 1f);

                                instance.transportVessels[i].PushNewLastPos(instance.transportVessels[i].pos);
                                instance.transportVessels[i].pos = pos;

                                instance.transportVessels[i].creature.abstractCreature.pos.abstractNode = num;

                                Debug.Log("LapMod: redirecting vessel");
                                if (instance.transportVessels[i].room.connections.Length > 0)
                                {
                                    if (num >= instance.transportVessels[i].room.connections.Length)
                                    {
                                        instance.transportVessels[i].PushNewLastPos(instance.transportVessels[i].pos);
                                        instance.transportVessels[i].pos = pos;
                                        Debug.Log("faulty room exit");
                                    }
                                    else
                                    {
                                        int num2 = instance.transportVessels[i].room.connections[num];
                                        if (num2 <= -1)
                                        {
                                            break; // Huh
                                        }
                                        instance.transportVessels[i].entranceNode = enteredFromNode;
                                        //instance.transportVessels[i].room = instance.game.world.GetAbstractRoom(num2);
                                        instance.betweenRoomsWaitingLobby.Add(instance.transportVessels[i]);
                                    }
                                }
                                instance.transportVessels.RemoveAt(i);
                            }
                            else // About to enter new room, store info
                            {
                                Debug.Log("LapMod: passing through");
                                if (num < instance.transportVessels[i].room.connections.Length)
                                {
                                    int num2 = instance.transportVessels[i].room.connections[num];
                                    enteredFromNode = game.world.GetAbstractRoom(num2).ExitIndex(instance.transportVessels[i].room.index);
                                }
                            }
                        }
                    }

                }

            }

            orig(instance);
        }
    }
}
