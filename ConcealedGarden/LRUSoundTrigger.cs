using ManagedPlacedObjects;
using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal class LRUSoundTrigger : UpdatableAndDeletable
    {

        internal class LRUSoundTriggerData : PlacedObjectsManager.ManagedData
        {
            [PlacedObjectsManager.FloatField("intensity", 0f, 1f, 0.05f, 0.01f, displayName:"Intensity")]
            public float intensity;
            public LRUSoundTriggerData(PlacedObject owner) : base(owner, null) { }
        }

        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("LRUSoundTrigger",
                typeof(LRUSoundTrigger), typeof(LRUSoundTriggerData), typeof(PlacedObjectsManager.ManagedRepresentation)));

        }

        private readonly PlacedObject pObj;
        private LRUSoundTriggerData data => pObj.data as LRUSoundTriggerData;
        public LRUSoundTrigger(Room room, PlacedObject pObj)
        {
            this.room = room;
            this.pObj = pObj;
        }

		public override void Update(bool eu)
		{
			base.Update(eu);
			for (int i = 0; i < this.room.game.Players.Count; i++)
			{
				if (this.room.game.Players[i].realizedCreature != null && this.room.game.Players[i].realizedCreature.room == this.room)
				{
					this.Trigger();
					break;
				}
			}
		}
		private void Trigger()
		{
			var player = this.room.game.manager.musicPlayer;
			if (player == null)// || this.room.gravity > 0f)
			{
				return;
			}
			if (player.song == null || !(player.song is LRUSong))
			{
				//this.room.game.manager.musicPlayer.RequestSSSong();
				if (player.song != null && player.song is LRUSong)
				{
					return;
				}
				if (player.nextSong != null && player.nextSong is LRUSong)
				{
					return;
				}
				if (!player.manager.rainWorld.setup.playMusic)
				{
					return;
				}
				Music.Song song = new LRUSong(player);
				if (player.song == null)
				{
					player.song = song;
					player.song.playWhenReady = true;
				}
				else
				{
					player.nextSong = song;
					player.nextSong.playWhenReady = false;
				}
			}
			else if ((player.song as LRUSong).setVolume != null)
			{
				(player.song as LRUSong).setVolume = new float?(Mathf.Max((player.song as LRUSong).setVolume.Value, this.data.intensity));
			}
			else
			{
				(player.song as LRUSong).setVolume = new float?(this.data.intensity);
			}
		}

        public class LRUSong : Music.Song
        {
            public LRUSong(Music.MusicPlayer musicPlayer) : base(musicPlayer, "CG - Infinite Machines", Music.MusicPlayer.MusicContext.StoryMode)
			{
				this.priority = 1.1f;
				this.stopAtGate = true;
				this.stopAtDeath = true;
				this.fadeInTime = 120f;
				base.Loop = true;
				Debug.Log("Created LRUSong");
			}

			public override void Update()
			{
				base.Update();
				if (this.setVolume != null)
				{
					this.baseVolume = RWCustom.Custom.LerpAndTick(this.baseVolume, this.setVolume.Value, 0.005f, 0.0025f);
					this.destroyCounter = 0;
				}
				else
				{
					if (!(this.musicPlayer.manager.currentMainLoop is RainWorldGame game) || game.pauseMenu == null)
						this.destroyCounter++;
					if (this.destroyCounter > 150)
					{
						Debug.Log("Destroyed LRUSong");
						base.FadeOut(400f);
					}
				}
				this.setVolume = null;
			}

			public float? setVolume;

			public int destroyCounter;
		}
	}
}