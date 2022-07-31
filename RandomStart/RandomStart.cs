using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;
using UnityEngine;

namespace RandomStart
{
    [BepInPlugin("henpemaz.randomstart", "RandomStart", "0.1.0")]
    public class RandomStart : BaseUnityPlugin
    {
        public void OnEnable()
        {
            On.RainWorld.Start += RainWorld_Start;
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            IL.SaveState.LoadGame += SaveState_LoadGame;
            On.OverWorld.ctor += OverWorld_ctor;
            orig(self);
        }

        private void SaveState_LoadGame(ILContext il)
        {
            var c = new ILCursor(il);
            bool patched = false;
            while (c.TryGotoNext(MoveType.After,
                i => !i.MatchLdelemRef(), // NOT the load from split array
                i => i.MatchStfld<SaveState>("denPosition")))
			{
                c.MoveBeforeLabels();
                c.Emit(OpCodes.Ldarg_2); // rwgame
                c.Emit(OpCodes.Ldstr, "RANDOM_START");
                c.Emit<RainWorldGame>(OpCodes.Stfld, "startingRoom");
                patched = true;
            }
			if (!patched) Debug.LogException(new Exception("Couldn't IL-hook SaveState_LoadGame from RandomStart")); // deffendisve progrmanig
		}

        private void OverWorld_ctor(On.OverWorld.orig_ctor orig, OverWorld self, RainWorldGame game)
        {
            if (game.startingRoom == "RANDOM_START")
            {
                game.startingRoom = null;
                Debug.Log("RANDOM_START");
                var regions = game.GetStorySession.saveState.progression.regionNames.ToList();//.Where(r => r == "CG").ToList();
                //foreach (var r in regions)
                //{
                //    Debug.Log(r);
                //}

                while (regions.Count > 0)
                {
                    var ri = UnityEngine.Random.Range(0, regions.Count);
                    var region = regions[ri];
                    regions.RemoveAt(ri);
                    var wl = new WorldLoader(game, game.GetStorySession.saveState.saveStateNumber, false, region, new Region(region, 0, 0), game.setupValues);
                    while (wl.activity <= WorldLoader.Activity.MappingRooms) { wl.Update(); }
                    if (wl.sheltersList.Count == 0) continue;
                    var si = UnityEngine.Random.Range(0, wl.sheltersList.Count);
                    var shelter = wl.roomAdder[wl.sheltersList[si] - wl.world.firstRoomIndex][0];
                    game.GetStorySession.saveState.denPosition = shelter;
                    game.startingRoom = shelter;
                    break;
                }

                Debug.Log(game.startingRoom);
            }

            orig(self, game);
        }
    }
}
