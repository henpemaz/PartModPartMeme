using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal class YellowTalk
    {
        internal static void Apply()
        {
            On.YellowAI.RecieveInfoOnCritter += YellowAI_RecieveInfoOnCritter;
        }

        private static void YellowAI_RecieveInfoOnCritter(On.YellowAI.orig_RecieveInfoOnCritter orig, YellowAI self, Lizard packMember, Tracker.CreatureRepresentation rep)
        {
            orig(self, packMember, rep);
            if (UnityEngine.Random.value < 0.2f) TryMakePopup(self.lizard.room, self.lizard.mainBodyChunk.pos + UnityEngine.Random.insideUnitCircle*50f, rep.representedCreature.creatureTemplate.name);
        }

        public static void TryMakePopup(Room room, Vector2 position, string message)
        {
            HUD.HUD hud;
            for (int i = 0; i < room.game.cameras.Length; i++)
            {
                if ((hud = room.game.cameras[i].hud) != null 
                    && hud.owner is Player
                    && room.game.cameras[i].followAbstractCreature?.Room == room.abstractRoom)
                {
                    hud.parts.Add(new PlacedDialogueBox(hud, room.game.cameras[i], room, position, message)); // Added directly so not to sit as main dialoguebox
                }
            }
        }
        public class PlacedDialogueBox : HUD.DialogBox
        {
            private Room room;
            private RoomCamera camera;
            private readonly Vector2 position;

            public PlacedDialogueBox(HUD.HUD hud, RoomCamera camera, Room room, Vector2 position, string message) : base(hud)
            {
                this.camera = camera;
                this.room = room;
                this.position = position;
                this.NewMessage(message, 120);
            }
            public override void Update()
            {
                base.Update();
                if (camera.room != this.room || this.messages.Count == 0)
                {
                    this.slatedForDeletion = true;
                    this.room = null;
                    this.camera = null;
                    return;
                }
                Vector2 relPos = position - camera.pos;
                this.messages[0].yPos = relPos.y;
                this.messages[0].xOrientation = relPos.x / this.hud.rainWorld.screenSize.x;
            }
            public override void ClearSprites()
            {
                base.ClearSprites();
                this.label?.RemoveFromContainer(); // duhh bugfix
            }
        }
    }
}