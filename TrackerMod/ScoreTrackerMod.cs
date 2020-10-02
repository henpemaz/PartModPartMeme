using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ScoreTrackerMod
{
    public class ScoreTrackerMod : PartialityMod
    {
        public ScoreTrackerMod()
        {
            this.ModID = "ScoreTrackerMod";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public static ScoreTrackerMod instance; // Not necessary, but useful if you are planning to support Config Machine

        // Kill points reference table
        private static int[] killScores = new int[Enum.GetNames(typeof(MultiplayerUnlocks.SandboxUnlockID)).Length];
        // Kill math
        static int pointsFromSaveStateKills(List<KeyValuePair<IconSymbol.IconSymbolData, int>> kills)
        {
            if(kills == null)
            {
                return 0;
            }
            int sum = 0;
            foreach (KeyValuePair<IconSymbol.IconSymbolData, int> kill in kills)
            {
                if (CreatureSymbol.DoesCreatureEarnATrophy(kill.Key.critType))
                {
                    sum += killScores[(int)MultiplayerUnlocks.SandboxUnlockForSymbolData(kill.Key)] * kill.Value;
                }
            }
            return sum;
        }

        static int pointsFromCycleKills(List<PlayerSessionRecord.KillRecord> kills)
        {
            if (kills == null)
            {
                return 0;
            }
            int sum = 0;
            foreach (PlayerSessionRecord.KillRecord kill in kills)
            {
                if (CreatureSymbol.DoesCreatureEarnATrophy(kill.symbolData.critType))
                {
                    sum += killScores[(int)MultiplayerUnlocks.SandboxUnlockForSymbolData(kill.symbolData)];
                }
            }
            return sum;
        }

        // Stored references to menu objects, to be disposed when the Menu closes
        // think of it as "additional fields" for Menu.SleepScreenKills
        private static WeakReference cyclePointsTickerRef;
        private static WeakReference totalPointsTickerRef;
        private static WeakReference cyclesPlayedTickerRef;
        private static WeakReference averagePointsTickerRef;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking codes would go here

            On.Menu.SleepScreenKills.ctor += SleepScreenKills_ctor_patch;
            On.Menu.SleepScreenKills.Update += SleepScreenKills_Update_patch;

            ScoreTrackerMod.instance = this;

            // from Menu.StoryGameStatisticsScreen:16
            for (int i = 0; i < ScoreTrackerMod.killScores.Length; i++)
            {
                ScoreTrackerMod.killScores[i] = 1;
            }
            Menu.SandboxSettingsInterface.DefaultKillScores(ref ScoreTrackerMod.killScores);
            ScoreTrackerMod.killScores[0] = 1;
        }


        static void SleepScreenKills_ctor_patch(On.Menu.SleepScreenKills.orig_ctor orig, Menu.SleepScreenKills instance, Menu.Menu menu, Menu.MenuObject owner, Vector2 pos, List<PlayerSessionRecord.KillRecord> killsData)
        {
            orig(instance, menu, owner, pos, killsData);

            //Debug.Log("SleepScreenKills_ctor_patch called");

            //Vector2 vector = new Vector2(1200f, -30f);
            // Screen-size aware positioning
            Vector2 vector = new Vector2(-(menu as Menu.KarmaLadderScreen).LeftHandButtonsPosXAdd + (menu as Menu.KarmaLadderScreen).ContinueAndExitButtonsXPos -130f, -30f);
            int cyclePoints = pointsFromCycleKills(killsData);
            Menu.StoryGameStatisticsScreen.LabelTicker cyclePointsTicker = new Menu.StoryGameStatisticsScreen.LabelTicker(menu, instance, vector + new Vector2(0f, 0f), cyclePoints, 0, menu.Translate("Points :"));
            instance.subObjects.Add(cyclePointsTicker);
            cyclePointsTickerRef = new WeakReference(cyclePointsTicker);
            cyclePointsTicker.nameLabel.label.alpha = 0.33f;
            cyclePointsTicker.nameLabel.pos.x = -120f;
            cyclePointsTicker.numberLabel.label.alpha = 0.4f;

            int totalPoints = (menu as Menu.KarmaLadderScreen).saveState == null ? 0 : pointsFromSaveStateKills((menu as Menu.KarmaLadderScreen).saveState.kills);
            Menu.StoryGameStatisticsScreen.LabelTicker totalPointsTicker = new Menu.StoryGameStatisticsScreen.LabelTicker(menu, instance, vector + new Vector2(0f, -30f), totalPoints, 0, menu.Translate("Total :"));
            instance.subObjects.Add(totalPointsTicker);
            totalPointsTickerRef = new WeakReference(totalPointsTicker);
            totalPointsTicker.nameLabel.label.alpha = 0.33f;
            totalPointsTicker.nameLabel.pos.x = -120f;
            totalPointsTicker.numberLabel.label.alpha = 0.4f;

            int cyclesPlayed = (menu as Menu.KarmaLadderScreen).saveState == null ? 1 : (menu as Menu.KarmaLadderScreen).saveState.cycleNumber;
            Menu.StoryGameStatisticsScreen.LabelTicker cyclesPlayedTicker = new Menu.StoryGameStatisticsScreen.LabelTicker(menu, instance, vector + new Vector2(0f, -60f), cyclesPlayed, 0, menu.Translate("Cycles :"));
            instance.subObjects.Add(cyclesPlayedTicker);
            cyclesPlayedTickerRef = new WeakReference(cyclesPlayedTicker);
            cyclesPlayedTicker.nameLabel.label.alpha = 0.33f;
            cyclesPlayedTicker.nameLabel.pos.x = -120f;
            cyclesPlayedTicker.numberLabel.label.alpha = 0.4f;

            int averagePoints = (int) Math.Round((double)totalPoints / (double)cyclesPlayed);
            Menu.StoryGameStatisticsScreen.LabelTicker averagePointsTicker = new Menu.StoryGameStatisticsScreen.LabelTicker(menu, instance, vector + new Vector2(0f, -90f), averagePoints, 0, menu.Translate("Average :"));
            instance.subObjects.Add(averagePointsTicker);
            averagePointsTickerRef = new WeakReference(averagePointsTicker);
            averagePointsTicker.nameLabel.label.alpha = 0.33f;
            averagePointsTicker.nameLabel.pos.x = -120f;
            averagePointsTicker.numberLabel.label.alpha = 0.4f;

            delay = 0;
            done = false;
        }

        static int delay;
        static bool done;

        static void SleepScreenKills_Update_patch(On.Menu.SleepScreenKills.orig_Update orig, Menu.SleepScreenKills instance)
        {
            orig(instance);

            if (done) return;

            //Debug.Log("SleepScreenKills_Update_patch called");

            // main object delay before starting
            if (instance.wait <= 0)
            {
                // our own delay
                if(delay > 0)
                {
                    bool mp = Input.GetKey(instance.menu.manager.rainWorld.options.controls[0].KeyboardMap);
                    delay -= ((!mp) ? 1 : 4);
                }
                if (delay <= 0)
                {
                    // Grab references
                    Menu.StoryGameStatisticsScreen.LabelTicker[] tickers = new Menu.StoryGameStatisticsScreen.LabelTicker[]
                        {(cyclePointsTickerRef.Target as Menu.StoryGameStatisticsScreen.LabelTicker),
                         (totalPointsTickerRef.Target as Menu.StoryGameStatisticsScreen.LabelTicker),
                         (cyclesPlayedTickerRef.Target as Menu.StoryGameStatisticsScreen.LabelTicker),
                         (averagePointsTickerRef.Target as Menu.StoryGameStatisticsScreen.LabelTicker) };

                    foreach (Menu.StoryGameStatisticsScreen.LabelTicker ticker in tickers)
                    {
                        done = false;
                        if (ticker.visible == false)
                        {
                            ticker.Show();
                            delay += 12;
                            break;
                        }
                        else if (ticker.displayValue < ticker.getToValue)
                        {
                            ticker.Tick();

                            if (ticker.displayValue >= ticker.getToValue)
                            {
                                // done now
                                delay += 20;
                            }
                            else if (!ticker.FastTick)
                            {
                                // not done yet
                                delay += 4;
                            }
                            // FastTick, no delay
                            break;
                        }
                        done = true; // no breaks on last ticker = everything done
                    }
                }
                
                // One at a time, show and count
                // Could set up a list and increment the index as these guys fill up... too much effort tbh
                //if (cyclePointsTicker.visible == false)
                //{
                //    cyclePointsTicker.Show();
                //}
                //else if(cyclePointsTicker.displayValue < cyclePointsTicker.getToValue)
                //{
                //    cyclePointsTicker.Tick();
                //    if(cyclePointsTicker.displayValue >= cyclePointsTicker.getToValue) delay += 12;
                //}
                //else if(totalPointsTicker.visible == false)
                //{
                //    totalPointsTicker.Show();
                //}
                //else if(totalPointsTicker.displayValue < totalPointsTicker.getToValue)
                //{
                //    totalPointsTicker.Tick();
                //    if(totalPointsTicker.displayValue >= totalPointsTicker.getToValue) delay += 12;
                //}
                //else if(cyclesPlayedTicker.visible == false)
                //{
                //    cyclesPlayedTicker.Show();
                //}
                //else if(cyclesPlayedTicker.displayValue < cyclesPlayedTicker.getToValue)
                //{
                //    cyclesPlayedTicker.Tick();
                //    if(cyclesPlayedTicker.displayValue >= cyclesPlayedTicker.getToValue) delay += 12;
                //}
                //else if(averagePointsTicker.visible == false)
                //{
                //    averagePointsTicker.Show();
                //}
                //else if(averagePointsTicker.displayValue < averagePointsTicker.getToValue)
                //{
                //    averagePointsTicker.Tick();
                //    if (averagePointsTicker.displayValue >= averagePointsTicker.getToValue) delay += 12;
                //}


            }
        }

    }
}
