using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace PopupsMod
{
    public class PopupsMod : PartialityMod
    {
        public PopupsMod()
        {
            this.ModID = "PopupsMod";
            this.Version = "1.1";
            this.author = "Henpemaz";

            instance = this;
        }

        public static PopupsMod instance;

        public override void OnEnable()
        {
            base.OnEnable();

            List<PlacedObjectsManager.ManagedField> settings = new List<PlacedObjectsManager.ManagedField>
            {
                new PlacedObjectsManager.StringField("message", "message", displayName:"Message"),
                new PlacedObjectsManager.FloatField("delay", 0, 60, 0, displayName:"Delay Seconds"),
                new PlacedObjectsManager.FloatField("duration", 1, 60, 6, displayName:"Duration Seconds"),
                new PlacedObjectsManager.IntegerField("entrance", -1, 20, -1, displayName:"Entrance Requirement"),
                new PlacedObjectsManager.IntegerField("karma", 1, 10, 1, displayName:"Karma Requirement"),
                new PlacedObjectsManager.BooleanField("darken", true, displayName:"Darken Screen"),
                new PlacedObjectsManager.BooleanField("hidehud", true, displayName:"Hide HUD"),
                new PlacedObjectsManager.IntegerField("cooldown", -1, 40, 1, displayName:"Cooldown Cycles"),
            };

            PlacedObjectsManager.RegisterFullyManagedObjectType(settings.ToArray(), typeof(PopupTrigger), "RoomPopupTrigger");

            settings.Add(new PlacedObjectsManager.Vector2Field("handle", new Vector2(-100, 40), PlacedObjectsManager.Vector2Field.VectorReprType.circle));
            PlacedObjectsManager.RegisterFullyManagedObjectType(settings.ToArray(), typeof(ResizeablePopupTrigger), "ResizeablePopupTrigger");
            settings.Pop();

            settings.Add(new PlacedObjectsManager.Vector2Field("handle", new Vector2(40, 60), PlacedObjectsManager.Vector2Field.VectorReprType.rect));
            PlacedObjectsManager.RegisterFullyManagedObjectType(settings.ToArray(), typeof(RectanglePopupTrigger), "RectanglePopupTrigger");
            settings.Pop();
        }


        public class PopupTrigger : UpdatableAndDeletable
        {
            protected PlacedObject pObj;
            protected PlacedObjectsManager.ManagedData data => pObj.data as PlacedObjectsManager.ManagedData;
            protected int placedObjectIndex;
            protected bool queuedUp;
            private int delay;

            public PopupTrigger(Room room, PlacedObject pObj)
            {
                this.room = room;
                this.pObj = pObj;
                placedObjectIndex = room.roomSettings.placedObjects.IndexOf(pObj);

                if (data.GetValue<int>("cooldown") != 0 && room.game.session is StoryGameSession)
                {
                    if ((room.game.session as StoryGameSession).saveState.ItemConsumed(room.world, false, room.abstractRoom.index, placedObjectIndex))
                    {
                        this.Destroy();
                    }
                }
                this.delay = Mathf.RoundToInt(data.GetValue<float>("delay") * 40f); // implements own delay because message system is weird
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (base.slatedForDeletetion || !room.BeingViewed || !ShouldFire())
                {
                    this.delay = Mathf.RoundToInt(data.GetValue<float>("delay") * 40f);
                    return;
                }
                else delay--;

                if (delay <= 0 && this.room.game.session.Players[0].realizedCreature != null && this.room.game.cameras[0].hud != null && this.room.game.cameras[0].hud.textPrompt != null && this.room.game.cameras[0].hud.textPrompt.messages.Count < 1)
                {
                    this.room.game.cameras[0].hud.textPrompt.AddMessage(this.room.game.manager.rainWorld.inGameTranslator.Translate(data.GetValue<string>("message")), 0, Mathf.RoundToInt(data.GetValue<float>("duration") * 40f), data.GetValue<bool>("darken"), data.GetValue<bool>("hidehud"));
                    Consume();
                }
            }

            public virtual bool ShouldFire()
            {
                // Any logic, could be reworked into any/all option
                for (int i = 0; i < this.room.game.Players.Count; i++)
                {
                    int entrance = data.GetValue<int>("entrance");
                    if (this.room.game.Players[i].Room == this.room.abstractRoom
                    && (entrance < 0 || this.room.game.Players[i].pos.abstractNode == entrance)
                    && this.room.game.Players[i].realizedCreature != null
                    && !this.room.game.Players[i].realizedCreature.inShortcut
                    && (this.room.game.Players[i].realizedCreature as Player).Karma >= (data.GetValue<int>("karma") - 1)
                    && !this.room.game.GameOverModeActive) return true;
                }
                return false;
            }

            public virtual void Consume()
            {
                Debug.Log("CONSUMED: PopupObject");
                if (data.GetValue<int>("cooldown") != 0 && room.world.game.session is StoryGameSession)
                {
                    (room.world.game.session as StoryGameSession).saveState.ReportConsumedItem(room.world, false, room.abstractRoom.index, this.placedObjectIndex, data.GetValue<int>("cooldown"));
                }
                this.Destroy();
            }
        }

        class ResizeablePopupTrigger : PopupTrigger
        {
            public ResizeablePopupTrigger(Room room, PlacedObject pObj) : base(room, pObj)
            {
            }
            public override bool ShouldFire()
            {
                if (!base.ShouldFire()) return false;
                for (int i = 0; i < this.room.game.Players.Count; i++)
                {
                    if (RWCustom.Custom.DistLess(this.room.game.Players[i].realizedCreature.mainBodyChunk.pos, pObj.pos, data.GetValue<Vector2>("handle").magnitude))
                        return true;
                }
                return false;
            }
        }

        class RectanglePopupTrigger : PopupTrigger
        {
            public RectanglePopupTrigger(Room room, PlacedObject pObj) : base(room, pObj)
            {
            }
            public override bool ShouldFire()
            {
                if (!base.ShouldFire()) return false;
                Rect rect = new Rect(pObj.pos.x, pObj.pos.y, data.GetValue<Vector2>("handle").x, data.GetValue<Vector2>("handle").y);

                for (int i = 0; i < this.room.game.Players.Count; i++)
                {
                    if (rect.Contains(this.room.game.Players[i].realizedCreature.mainBodyChunk.pos))
                        return true;
                }
                return false;
            }
        }
    }
}

