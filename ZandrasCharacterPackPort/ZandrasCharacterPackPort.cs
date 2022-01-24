using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using Menu;
using SlugBase;
using System.Collections.Generic;

[assembly: AssemblyTrademark("Zandra & Henpemaz")]

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ZandrasCharacterPackPort
{
    [BepInEx.BepInPlugin("henpemaz.zandrascharacterpackport", "ZandrasCharacterPack", "1.1")]
    public class ZandrasCharacterPackPort : BepInEx.BaseUnityPlugin
    {
        public string author = "Zandra, Henpemaz";
        public static ZandrasCharacterPackPort instance;

        public void OnEnable()
        {
            instance = this;
            PlayerManager.RegisterCharacter(new Kineticat());
            PlayerManager.RegisterCharacter(new Aquaria());
            PlayerManager.RegisterCharacter(new VultCat());
            PlayerManager.RegisterCharacter(new KarmaCat());
            PlayerManager.RegisterCharacter(new Skittlecat());
            PlayerManager.RegisterCharacter(new VVVVVCat());
            PlayerManager.RegisterCharacter(new PseudoWingcat());
            PlayerManager.RegisterCharacter(new tacgulS());
        }
    }

    // Utils

    internal static class Utils
    {
        public static T Target<T>(this WeakReference self) { return (T)self?.Target; }

        // Start of game utils

        internal static void GiveSurvivor(RainWorldGame game, params string[] shelters)
        {
            var survivor = game.GetStorySession.saveState.deathPersistentSaveData.winState.GetTracker(WinState.EndgameID.Survivor, true) as WinState.IntegerTracker;
            survivor.SetProgress(survivor.max);
            survivor.lastShownProgress = survivor.progress;
            foreach(var s in shelters)
            {
                game.rainWorld.progression.miscProgressionData.SaveDiscoveredShelter(s);
            }
        }

        public static void PlacePlayers(Room room, int[,] places)//RWCustom.IntVector2[] places)
        {
            int nplaces = places.GetLength(0);
            for (int i = 0; i < room.game.Players.Count; i++)
            {
                room.game.Players[i].pos = new WorldCoordinate(room.abstractRoom.index, places[i % nplaces, 0], places[i % nplaces, 1], -1); ;
            }
        }

        public class CameraMan : UpdatableAndDeletable
        {
            private readonly int target;

            public CameraMan(int target)
            {
                this.target = target;
            }
            public override void Update(bool eu)
            {
                base.Update(eu);
                if (slatedForDeletetion || room == null) return;

                if (this.room.game.cameras[0].room == this.room)
                {
                    if (this.room.game.cameras[0].currentCameraPosition != target)
                    {
                        this.room.game.cameras[0].MoveCamera(target);
                        Destroy();
                    }
                }
            }
        }

        public class Messenger : UpdatableAndDeletable
        {
            public class Message
            {
                public readonly string text;
                public readonly int delay;
                public readonly int time;
                public readonly bool darken;
                public readonly bool hideui;

                public Message(string text, int delay, int time, bool darken, bool hideui)
                {
                    this.text = text;
                    this.delay = delay;
                    this.time = time;
                    this.darken = darken;
                    this.hideui = hideui;
                }

            }
            public List<Message> messages = new List<Message>();
            public Messenger(string text, int delay, int frames, bool darken, bool hideui)
            {
                this.messages.Add(new Message(text, delay, frames, darken, hideui));
            }
            public Messenger(List<Message> messages)
            {
                this.messages = messages;
            }
            public override void Update(bool eu)
            {
                base.Update(eu);
                if (this.slatedForDeletetion || this.room == null) return;
                if (this.messages.Count == 0) { this.Destroy(); return; }

                if (this.room.game.session.Players[0].realizedCreature != null && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.textPrompt != null && this.room.game.cameras[0].hud.textPrompt.messages.Count < 1)
                {
                    var curr = messages.Unshift();
                    this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate(curr.text), curr.delay, curr.time, curr.darken, curr.hideui);
                }
            }
        }

        // Behavior Utils

        public abstract class SlugbaseBehavior
        {
            protected SlugBaseCharacter slugChar;

            public SlugbaseBehavior(SlugBaseCharacter slugChar) { this.slugChar = slugChar; }

            /// <summary>
            /// Defaults to no op.
            /// </summary>
            public virtual void Enable() { }

            /// <summary>
            /// Defaults to no op.
            /// </summary>
            public virtual void Disable() { }

            ~SlugbaseBehavior() { slugChar = null; }
        }

        public class PebblesKarmaOnce : SlugbaseBehavior
        {
            public PebblesKarmaOnce(SlugBaseCharacter slugChar) : base(slugChar) { }

            public override void Enable()
            {
                //base.Enable();
                On.SSOracleBehavior.Update += SSOracleBehavior_Update;
            }

            public override void Disable()
            {
                //base.Disable();
                On.SSOracleBehavior.Update -= SSOracleBehavior_Update;
            }

            private void SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
            {
                orig(self, eu);

                if (!slugChar.MultiInstance || !slugChar.IsMe(self.oracle.room.game)) return;

                if (!self.oracle.Consious)
                {
                    return;
                }
                // copypasted behav with changes!
                if (self.action == SSOracleBehavior.Action.General_GiveMark)
                {
                    if (self.inActionCounter == 299) // will be 300 next frame
                    {
                        if (!self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.pebblesHasIncreasedRedsKarmaCap)
                        {
                            self.player.mainBodyChunk.vel += RWCustom.Custom.RNV() * 10f;
                            self.player.bodyChunks[1].vel += RWCustom.Custom.RNV() * 10f;
                            self.player.Stun(40);
                            (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark = true;
                            bool under9 = self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.karmaCap < 9;
                            self.oracle.room.game.GetStorySession.saveState.IncreaseKarmaCapOneStep();
                            self.oracle.room.game.GetStorySession.saveState.deathPersistentSaveData.pebblesHasIncreasedRedsKarmaCap = true;
                        }

                        (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma = (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap;
                        for (int l = 0; l < self.oracle.room.game.cameras.Length; l++)
                        {
                            if (self.oracle.room.game.cameras[l].hud.karmaMeter != null)
                            {
                                self.oracle.room.game.cameras[l].hud.karmaMeter.UpdateGraphic();
                            }
                        }
                        for (int m = 0; m < 20; m++)
                        {
                            self.oracle.room.AddObject(new Spark(self.player.mainBodyChunk.pos, RWCustom.Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                        }
                        self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 1f);
                        self.inActionCounter++; // skip default
                    }
                }
            }
        }
    }
}

