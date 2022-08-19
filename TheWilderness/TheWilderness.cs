using System.Security;
using System.Security.Permissions;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

[assembly: AssemblyTrademark("Henpemaz")]

namespace TheWilderness
{
	[BepInEx.BepInPlugin("henpemaz.thewildernessmod", "The Wilderness", "1.0")]
	public class TheWilderness : BepInEx.BaseUnityPlugin
	{
		public void OnEnable()
		{
			On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
			On.Room.Loaded += Room_Loaded;
		}

		private void Room_Loaded(On.Room.orig_Loaded orig, Room self)
		{
			orig(self);
			if (self.game == null) return;
			for (int k = 0; k < self.roomSettings.effects.Count; k++)
			{
				if (self.roomSettings.effects[k].type == EnumExt_CGWaterFallEffect.TWClouds2)
				{
					self.AddObject(new TWClouds(self, self.roomSettings.effects[k]));
				}
			}
		}

		public static class EnumExt_CGWaterFallEffect
		{
#pragma warning disable 0649
			public static RoomSettings.RoomEffect.Type TWClouds;
			public static RoomSettings.RoomEffect.Type TWClouds2;
#pragma warning restore 0649
		}

		private void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera self)
		{
			orig(self);
			if (self.room != null && self.fullScreenEffect == null)
			{
				if (self.room.roomSettings.GetEffectAmount(EnumExt_CGWaterFallEffect.TWClouds) > 0f)
				{
					LoadGraphic("clouds1", false, false);
					self.SetUpFullScreenEffect("Foreground");
					self.fullScreenEffect.shader = self.game.rainWorld.Shaders["Cloud"];
					self.fullScreenEffect.SetElementByName("clouds1");
					self.lightBloomAlphaEffect = EnumExt_CGWaterFallEffect.TWClouds;
					self.lightBloomAlpha = self.room.roomSettings.GetEffectAmount(EnumExt_CGWaterFallEffect.TWClouds);
					self.fullScreenEffect.color = new UnityEngine.Color(self.lightBloomAlpha, 1f - Mathf.Pow(self.lightBloomAlpha, 0.5f), 1f - Mathf.Pow(self.lightBloomAlpha, 0.4f));
					Shader.SetGlobalVector("_AboveCloudsAtmosphereColor", self.currentPalette.fogColor);
				}
			}
		}

		public void LoadGraphic(string elementName, bool crispPixels, bool clampWrapMode)
		{
			if (Futile.atlasManager.GetAtlasWithName(elementName) != null)
			{
				return;
			}
			WWW www = new WWW(string.Concat(new object[]
			{
			"file:///",
			RWCustom.Custom.RootFolderDirectory(),
			"Assets",
			Path.DirectorySeparatorChar,
			"Futile",
			Path.DirectorySeparatorChar,
			"Resources",
			Path.DirectorySeparatorChar,
			"Illustrations",
			Path.DirectorySeparatorChar,
			elementName,
			".png"
			}));
			Texture2D texture2D = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			texture2D.wrapMode = ((!clampWrapMode) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp);
			if (crispPixels)
			{
				texture2D.anisoLevel = 0;
				texture2D.filterMode = FilterMode.Point;
			}
			www.LoadImageIntoTexture(texture2D);
			HeavyTexturesCache.LoadAndCacheAtlasFromTexture(elementName, texture2D);
		}

		public class TWClouds : BackgroundScene
		{
			public TWClouds(Room room, RoomSettings.RoomEffect effect) : base(room)
			{
				this.effect = effect;
				base.LoadGraphic("clouds1", false, false);
				base.LoadGraphic("clouds2", false, false);
				base.LoadGraphic("clouds3", false, false);
				int num = 5;
				for (int i = 0; i < num; i++)
				{
					float cloudDepth = (float)i / (float)(3f);
					this.AddElement(new TWClouds.Cloud(this, new Vector2(0f, 0f), Mathf.Pow(cloudDepth, 1.2f), i));
				}
			}

			private float CloudDepth(float f)
			{
				return Mathf.Lerp(this.cloudsStartDepth, this.cloudsEndDepth, f);
			}

			public RoomSettings.RoomEffect effect;

			public float cloudsStartDepth = 5f;

			public float cloudsEndDepth = 40f;

			public class Cloud : BackgroundScene.BackgroundSceneElement
			{
				public Cloud(TWClouds aboveCloudsScene, Vector2 pos, float cloudDepth, int index) : base(aboveCloudsScene, pos, aboveCloudsScene.CloudDepth(cloudDepth))
				{
					this.randomOffset = UnityEngine.Random.value;
					this.index = index;
					this.cloudDepth = cloudDepth;
				}
				public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
				{
					this.skyColor = palette.skyColor;
				}

				public float randomOffset;

				public Color skyColor;

				public int index;

				public TWClouds AboveCloudsScene
				{
					get
					{
						return this.scene as TWClouds;
					}
				}

				public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
				{
					Shader.SetGlobalVector("_AboveCloudsAtmosphereColor", rCam.currentPalette.fogColor);
					sLeaser.sprites = new FSprite[2];
					sLeaser.sprites[0] = new FSprite("pixel", true);
					sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["Background"];
					sLeaser.sprites[0].anchorY = 0f;
					sLeaser.sprites[0].scaleX = 1400f;
					sLeaser.sprites[0].x = 683f;
					sLeaser.sprites[0].y = 0f;
					sLeaser.sprites[1] = new FSprite("clouds" + (this.index % 3 + 1).ToString(), true);
					sLeaser.sprites[1].shader = rCam.game.rainWorld.Shaders["Cloud"];
					sLeaser.sprites[1].anchorY = 1f;
					this.AddToContainer(sLeaser, rCam, null);
				}

				public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
				{
					//float y = this.scene.RoomToWorldPos(rCam.room.cameraPositions[rCam.currentCameraPosition]).y;
					//float num = Mathf.InverseLerp(this.AboveCloudsScene.startAltitude, this.AboveCloudsScene.endAltitude, y);
					float num = AboveCloudsScene.effect.amount;
					float num2 = this.cloudDepth;
					if (num > 0.5f)
					{
						num2 = Mathf.Lerp(num2, 1f, Mathf.InverseLerp(0.5f, 1f, num) * 0.5f);
					}
					this.depth = Mathf.Lerp(this.AboveCloudsScene.cloudsStartDepth, this.AboveCloudsScene.cloudsEndDepth, num2);
					float num3 = Mathf.Lerp(10f, 2f, num2);
					float num4 = base.DrawPos(camPos, rCam.hDisplace).y;
					num4 += Mathf.Lerp(Mathf.Pow(this.cloudDepth, 0.75f), Mathf.Sin(this.cloudDepth * 3.1415927f), 0.5f) * Mathf.InverseLerp(0.5f, 0f, num) * 600f;
					num4 -= Mathf.InverseLerp(0.18f, 0.1f, num) * Mathf.Pow(1f - this.cloudDepth, 3f) * 100f;
					float num5 = Mathf.Lerp(1f, Mathf.Lerp(0.75f, 0.25f, num), num2);
					num4 += -100f + 200f * num2;
					num5 = 1f;
					sLeaser.sprites[0].scaleY = num4 - 150f * num3 * num5;
					sLeaser.sprites[1].scaleY = num5 * num3;
					sLeaser.sprites[1].scaleX = num3;
					sLeaser.sprites[1].color = new Color(num2 * 0.75f, this.randomOffset, Mathf.Lerp(num5, 1f, 0.5f), 1f);
					sLeaser.sprites[1].x = 683f;
					sLeaser.sprites[1].y = num4;
					sLeaser.sprites[0].color = Color.Lerp(this.skyColor, rCam.currentPalette.fogColor, num2 * 0.75f);
					base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
				}

				private float cloudDepth;
			}
		}
	}
}
