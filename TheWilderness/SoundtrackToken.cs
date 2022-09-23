using RWCustom;
using System;
using UnityEngine;

namespace TheWilderness
{
    internal class SoundtrackToken : CollectToken
    {
        public class SoundtrackTokenData : CollectTokenData { public SoundtrackTokenData(PlacedObject pobj) : base(pobj, true) { } }

        public SoundtrackToken(Room room, PlacedObject placedObj) : base(room, placedObj)
        {
        }

        internal static void Apply()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("TWSoundtrackToken", typeof(SoundtrackToken), typeof(SoundtrackTokenData), typeof(DevInterface.ResizeableObjectRepresentation)));

            On.CollectToken.DrawSprites += CollectToken_DrawSprites;
        }

        private static void CollectToken_DrawSprites(On.CollectToken.orig_DrawSprites orig, CollectToken self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (sLeaser.deleteMeNextFrame) return;

            Color color = Color.Lerp(Color.Lerp(self.GoldCol(Mathf.Lerp(self.lastGlitch, self.glitch, timeStacker)), new Color(0.08f, 0.13f, 0.92f), 0.8f), Color.white, 0.4f);
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                var prealpha = sLeaser.sprites[i].alpha;
                sLeaser.sprites[i].color = color;
                sLeaser.sprites[i].alpha = prealpha;
            }
            var pos = sLeaser.sprites[self.GoldSprite].GetPosition();
            if (!sLeaser.sprites[self.LineSprite(0)].isVisible) return;
            Vector2 a = pos + new Vector2(-3f, -2f);
            Vector2 b = a + new Vector2(3f, 14f);

            sLeaser.sprites[self.LineSprite(0)].x = a.x;
            sLeaser.sprites[self.LineSprite(0)].y = a.y;
            sLeaser.sprites[self.LineSprite(0)].scaleY = Vector2.Distance(a, b);
            sLeaser.sprites[self.LineSprite(0)].rotation = Custom.AimFromOneVectorToAnother(a, b);

            a = b + new Vector2(-2f, 1f);
            sLeaser.sprites[self.LineSprite(1)].x = a.x;
            sLeaser.sprites[self.LineSprite(1)].y = a.y;
            sLeaser.sprites[self.LineSprite(1)].scaleY = Vector2.Distance(a, b);
            sLeaser.sprites[self.LineSprite(1)].rotation = Custom.AimFromOneVectorToAnother(a, b);

            b = a + new Vector2(-2f, -1f);
            sLeaser.sprites[self.LineSprite(2)].x = a.x;
            sLeaser.sprites[self.LineSprite(2)].y = a.y;
            sLeaser.sprites[self.LineSprite(2)].scaleY = Vector2.Distance(a, b);
            sLeaser.sprites[self.LineSprite(2)].rotation = Custom.AimFromOneVectorToAnother(a, b);

            a = b + new Vector2(-1f, 0f);
            sLeaser.sprites[self.LineSprite(3)].x = a.x;
            sLeaser.sprites[self.LineSprite(3)].y = a.y;
            sLeaser.sprites[self.LineSprite(3)].scaleY = Vector2.Distance(a, b);
            sLeaser.sprites[self.LineSprite(3)].rotation = Custom.AimFromOneVectorToAnother(a, b);
        }

        public override void Destroy()
        {
            base.Destroy();
            this.anythingUnlocked = false;

            this.room.game.cameras[0].hud.textPrompt.AddMessage("New soundtrack pieces unlocked", 20, 160, true, true);
        }
    }
}