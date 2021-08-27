//using System; // Fuck off I want to use Random
using UnityEngine;

namespace ConcealedGarden
{
    internal class YellowThoughtsAdaptor
    {
        // Make yellow lizards to 'talk' on mark + cg-tf
        internal static void Apply()
        {
            On.YellowAI.ctor += YellowAI_ctor;
            On.YellowAI.Update += YellowAI_Update;
            //On.YellowAI.RecieveInfoOnCritter += YellowAI_RecieveInfoOnCritter;
        }

        // Using weakref dict, avoid cyclic key
        // Pass 'self' on all methods, don't hold ref
        static readonly AttachedField<YellowAI, YellowThoughtsAdaptor> LizardThought = new AttachedField<YellowAI, YellowThoughtsAdaptor>();
        private int noTalk;

        private static void YellowAI_ctor(On.YellowAI.orig_ctor orig, YellowAI self, ArtificialIntelligence AI)
        {
            orig(self, AI);
            LizardThought[self] = new YellowThoughtsAdaptor();
        }

        private static void YellowAI_Update(On.YellowAI.orig_Update orig, YellowAI self)
        {
            YellowThoughtsAdaptor @this = LizardThought[self];
            @this?.Update(self);
            orig(self);
        }

        //private static void YellowAI_RecieveInfoOnCritter(On.YellowAI.orig_RecieveInfoOnCritter orig, YellowAI self, Lizard packMember, Tracker.CreatureRepresentation rep)
        //{
        //    YellowThoughtsAdaptor @this = LizardThought[self];
        //    @this?.RecieveInfoOnCritter(self, packMember, rep);
        //    orig(self, packMember, rep);
        //}

        class MessagePool
        {
            private string[] messages;
            private int lastIndex;

            public MessagePool(string[] messages)
            {
                this.messages = messages;
                this.lastIndex = -1;
            }

            public string get
            {
                get
                {
                    int roll;
                    while ((roll = Random.Range(0, messages.Length)) == lastIndex) ; // Keep rolling
                    lastIndex = roll;
                    return messages[roll];
                }
            }
        }

        static MessagePool IdleMessage = new MessagePool(new string[] { "Hmm...", "Yup.", "...", ":)", "(:" });
        static MessagePool HuntMessage = new MessagePool(new string[] { "Attack!", "Food!", "Get them!", ">:O", "Prey!" });
        static MessagePool FleeMessage = new MessagePool(new string[] { "Aah!", "Run!", "Monster!", ">o<'", "Get away!" });
        static MessagePool RainMessage = new MessagePool(new string[] { "The rain...", "Shelter", "Run...", "Rain is coming..." });
        static MessagePool StowMessage = new MessagePool(new string[] { "Yum!", "Delicious", "Got them...", "Aha...", ">:3c" });
        static MessagePool FistyMessage = new MessagePool(new string[] { "Grrr!", "You...", ">:O", "Wraa", "Grrr...", "Tsc" });
        static MessagePool DisappointedMessage = new MessagePool(new string[] { "Hmm", "Ouch", "):", ":(", ">-<", "Grr..." });
        static MessagePool NoiseMessage = new MessagePool(new string[] { "What?", "Something", ":o", "Where...", "What was that...", "I heard something" });
        static MessagePool FriendMessage = new MessagePool(new string[] { "You!", "Friend!", "Master!", "Together...", "(:", "<3" });
        

        private void Update(YellowAI self)
        {
            this.noTalk++;
            float talky = -1 + (self.AI as LizardAI).currentUtility * 0.5f + (self.AI as LizardAI).excitement * 0.4f + Mathf.Pow(Mathf.Max(((float)noTalk - 30f), 0f) / 600f, 1.5f) * 0.5f;
            talky += 0.35f * self.lizard.abstractCreature.personality.sympathy;
            //if (self.lizard.room.game.clock % 120 == 0) Debug.Log("Talky: " + talky);
            if (talky < 0f || noTalk < 20) return;

            switch ((self.AI as LizardAI).behavior)
            {
                case LizardAI.Behavior.Idle:
                    if (talky / 4f > Random.value) Speak(self, IdleMessage.get);
                    break;
                case LizardAI.Behavior.Hunt:
                    talky += 0.33f * self.lizard.abstractCreature.personality.dominance;
                    if (talky / 2f > Random.value) Speak(self, HuntMessage.get);
                    break;
                case LizardAI.Behavior.Flee:
                    if (talky / 1f > Random.value) Speak(self, FleeMessage.get);
                    break;
                case LizardAI.Behavior.Travelling:
                    if (talky / 4f > Random.value) Speak(self, IdleMessage.get);
                    break;
                case LizardAI.Behavior.EscapeRain:
                    if (talky / 4f > Random.value) Speak(self, RainMessage.get);
                    break;
                case LizardAI.Behavior.ReturnPrey:
                    if (talky / 2f > Random.value) Speak(self, StowMessage.get);
                    break;
                case LizardAI.Behavior.Injured:
                    if (talky / 3f > Random.value) Speak(self, DisappointedMessage.get);
                    break;
                case LizardAI.Behavior.Fighting:
                    if (talky / 2f > Random.value) Speak(self, FistyMessage.get);
                    break;
                case LizardAI.Behavior.Frustrated:
                    if (talky / 4f > Random.value) Speak(self, DisappointedMessage.get);
                    break;
                case LizardAI.Behavior.ActingOutMission:
                    if (talky / 4f > Random.value) Speak(self, DisappointedMessage.get);
                    break;
                case LizardAI.Behavior.Lurk:
                    // shh...
                    break;
                case LizardAI.Behavior.InvestigateSound:
                    if (talky / 2f > Random.value) Speak(self, NoiseMessage.get);
                    break;
                case LizardAI.Behavior.GoToSpitPos:
                    break;
                case LizardAI.Behavior.FollowFriend:
                    if (talky / 4f > Random.value) Speak(self, FriendMessage.get);
                    break;
                default:
                    break;
            }
        }

        private void Speak(YellowAI self, string text)
        {
            if (!(self.lizard.room.game.session is StoryGameSession SGS && SGS.saveState.deathPersistentSaveData.theMark)) return;
            if (!(ConcealedGarden.progression?.transfurred ?? false)) return;
            noTalk = Random.Range(-20, 0);
            self.communicating = Mathf.Max(self.communicating, Random.Range(4, 20));
            PlacedDialogueBox.TryMakePopup(self.lizard.room, self.lizard.mainBodyChunk.pos + Random.insideUnitCircle * 50f, text);
        }

        //private void RecieveInfoOnCritter(YellowAI self, Lizard packMember, Tracker.CreatureRepresentation rep)
        //{
        //    // throw new NotImplementedException();
        //}

        public class PlacedDialogueBox : HUD.DialogBox
        {
            private Room room;
            private RoomCamera camera;
            private readonly Vector2 position;

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

            public PlacedDialogueBox(HUD.HUD hud, RoomCamera camera, Room room, Vector2 position, string message) : base(hud)
            {
                this.camera = camera;
                this.room = room;
                this.position = position;
                Vector2 relPos = position - camera.pos;
                this.defaultYPos = relPos.y;
                this.defaultXOrientation = relPos.x / this.hud.rainWorld.screenSize.x;

                this.NewMessage(message, 60);
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
            }

            public override void ClearSprites()
            {
                base.ClearSprites();
                this.label?.RemoveFromContainer(); // duhh bugfix
            }

            public override void Draw(float timeStacker)
            {
                if (camera == null || camera.room != this.room)
                {
                    messages.Clear(); // hides and destroys
                }
                else if (this.messages.Count > 0)
                {
                    Vector2 relPos = position - camera.pos;
                    this.messages[0].yPos = relPos.y;
                    this.messages[0].xOrientation = relPos.x / this.hud.rainWorld.screenSize.x;
                }
                base.Draw(timeStacker);
            }
        }
    }
}