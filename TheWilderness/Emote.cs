using System;
using System.Collections;
using UnityEngine;

namespace TheWilderness
{
    internal class Emote : HUD.HudPart
    {
        private RainWorldGame game;
        Player player;
        private readonly RoomCamera camera;
        private FSprite sprite;
        private int counter;
        private float lastlife;
        private float life;
        private Vector2 lastpos;
        private Vector2 pos;

        public Emote(string sprite, Player player, HUD.HUD hud, RoomCamera camera) : base(hud)
        {
            hud.AddPart(this);

            hud.PlaySound(SoundID.Slugcat_Ghost_Appear);
            this.game = player.abstractCreature.world.game;
            this.player = player;
            this.camera = camera;
            this.sprite = new FSprite(sprite)
            {
                anchorY = 0,
                scale = 0.25f,
                alpha = 0f
            };
            hud.fContainers[0].AddChild(this.sprite);
            this.life = 0f;
        }

        public override void ClearSprites()
        {
            this.sprite.RemoveFromContainer();
        }

        public override void Update()
        {
            this.counter++;
            this.lastlife = life;
            this.life = Mathf.Min(Mathf.InverseLerp(0, 60, counter), Mathf.InverseLerp(240, 180, counter));
            this.lastpos = pos;
            if (this.player.room == null && camera.room == player.abstractCreature.Room.realizedRoom)
            {
                Vector2? vector = game.shortcuts.OnScreenPositionOfInShortCutCreature(camera.room, player);
                if (vector != null)
                {
                    this.pos = Vector2.Lerp(pos, vector.Value + new Vector2(0f, 60f) - camera.pos, 0.2f);
                }
            }
            else
            {
                this.pos = Vector2.Lerp(player.bodyChunks[0].pos, player.bodyChunks[1].pos, 0.33333334f) + new Vector2(0f, 60f) - camera.pos;
            }
        }

        public override void Draw(float timeStacker)
        {
            sprite.SetPosition(Vector2.Lerp(lastpos, pos, timeStacker));
            sprite.alpha = Mathf.Lerp(lastlife,life, timeStacker);
        }

        internal static void Apply()
        {
            On.Player.ctor += Player_ctor;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            world.game.rainWorld.StartCoroutine(EmoteInput(self));
        }

        // engine rate input because I hate anything else really
        private static IEnumerator EmoteInput(Player self)
        {
            while (!self.slatedForDeletetion && (self.abstractCreature?.world?.game?.processActive ?? false))
            {
                if (self.playerState.playerNumber == 1 && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha1))
                {
                    new Emote("greeting_t", self, self.abstractCreature.world.game.cameras[0].hud, self.abstractCreature.world.game.cameras[0]);
                    Debug.LogError("spawned emote!!!");
                }
                if (self.playerState.playerNumber == 0 && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha2))
                {
                    new Emote("happy_t", self, self.abstractCreature.world.game.cameras[0].hud, self.abstractCreature.world.game.cameras[0]);
                    Debug.LogError("spawned emote!!!");
                }
                if (self.playerState.playerNumber == 1 && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha3))
                {
                    new Emote("confused_t", self, self.abstractCreature.world.game.cameras[0].hud, self.abstractCreature.world.game.cameras[0]);
                    Debug.LogError("spawned emote!!!");
                }
                yield return null;
            }
            yield return null;
        }
    }
}