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
        static ZandrasCharacterPackPort instance;

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

    internal static class WeakRefExt
    {
        public static T Target<T>(this WeakReference self) { return (T)self?.Target; }
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
                    this.room.game.cameras[0].followAbstractCreature = room.game.Players[0];
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
}
