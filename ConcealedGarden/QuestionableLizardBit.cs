using RWCustom;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
// Don't look
namespace ConcealedGarden
{
	// I said don't look
	internal class QuestionableLizardBit : LizardCosmetics.Template
	{
		// Stop, what are you doing
		private static bool allOfThem = false;
		private static bool everyRegion = false;
		private static bool arenaToo = false;
		private static bool veryHappy = false;
		private static int ohMy = 0;
		private static bool coloredBits = false;
		private static Color bitColor;

		public static void Apply()
		{
            // You're not gonna like it
#pragma warning disable CS0219 // shush, stop raising warnings
            string str = "Disclaimer: only applies if NudeMod is ON ;o";
            string str2 = "Anyone who finds this knows exactly what they're in for";
#pragma warning restore CS0219
            On.RainWorld.Start += RainWorld_Start;
			
		}

		private static void InternalApply()
        {
			On.LizardGraphics.ctor += LizardGraphics_ctor;
			On.RainWorldGame.ctor += RainWorldGame_ctor;
		}

        private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            try
            {
                bool found = false;
                // UnityEngine.Debug.Log("NudeMod searching...");
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.GetName().Name == "NudeMod")
                    {
                        found = true;
                        UnityEngine.Debug.Log("NudeMod FOUND");
                        InternalApply();
                        break;
                    }
                }
                if (!found) UnityEngine.Debug.Log("NudeMod NOT FOUND");
            }
            finally
            {
				orig(self);
            }
		}

        private static void RainWorldGame_ctor(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
			// First some cheatcodes
			allOfThem = File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "allOfThem.txt"));
			everyRegion = File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "everyRegion.txt"));
			arenaToo = File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "arenaToo.txt"));
			veryHappy = File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "veryHappy.txt"));
			ohMy = 0;
			foreach (FileInfo file in new DirectoryInfo(Custom.RootFolderDirectory()).GetFiles("*.txt", SearchOption.TopDirectoryOnly))
			{
				if (file.Name.StartsWith("ve") && file.Name.EndsWith("ryBig.txt"))
				{
					int e = 0;
					while (file.Name[1 + e] == 'e') e++;
					if (e == file.Name.Length - 10 && e > ohMy) ohMy = e;
				}
			}
			coloredBits = false;
			if (File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "bitsColor.txt")))
            {
                try
                {
					string colorcode = File.ReadAllText(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "bitsColor.txt"));
					if (colorcode.StartsWith("#")) colorcode = colorcode.Substring(1);
					if (colorcode.Length != 6) throw new Exception();
					bitColor = new Color(
						Convert.ToInt32(colorcode.Substring(0, 2), 16) / 255f,
						Convert.ToInt32(colorcode.Substring(2, 2), 16) / 255f,
						Convert.ToInt32(colorcode.Substring(4, 2), 16) / 255f);
					coloredBits = true;
				}
                catch{}
            }
			orig(self, manager);
		}

        private static void LizardGraphics_ctor(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
		{
			orig(self, ow);
			// We're doing it
			if (!self.lizard.room.game.IsStorySession && !arenaToo) return;
			if (self.lizard.room.game.IsStorySession && !everyRegion)
            {
				World.CreatureSpawner spawner = self.lizard.room.game.world.GetSpawner(self.lizard.abstractCreature.ID);
				if (spawner != null && (spawner.region < 0 || spawner.region >= (self.lizard.room.game.session as StoryGameSession).saveState.regionStates.Length
					|| (self.lizard.room.game.session as StoryGameSession).saveState.regionStates[spawner.region].regionName != "CG"))
					return;
				if (spawner == null && self.lizard?.room?.world != null && self.lizard.room.world.name != "CG") return;
			}

			int seed = UnityEngine.Random.seed;
			UnityEngine.Random.seed = self.lizard.abstractCreature.ID.RandomSeed;
			float innateLength = 0f;
			float ivInfluence = 0f;
			bool shouldHaveAQuestionableBit = allOfThem;
			bool alreadyRolled = allOfThem;

			if (!string.IsNullOrEmpty(self.lizard.abstractCreature.spawnData) && self.lizard.abstractCreature.spawnData[0] == '{')
			{
				string[] array = self.lizard.abstractCreature.spawnData.Substring(1, self.lizard.abstractCreature.spawnData.Length - 2).Split(new char[]
				{
					','
				});
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].Length > 0)
					{
						string[] array2 = array[i].Split(new char[]
						{
							':'
						});
						string text = array2[0].Trim().ToLowerInvariant();
						if (text == "male" && !alreadyRolled)
                        {
							shouldHaveAQuestionableBit = array2.Length <= 1 || UnityEngine.Random.value < float.Parse(array2[1]);
							alreadyRolled = true;
						}
					}
				}
			}

			if ((self.lizard.Template.type == CreatureTemplate.Type.RedLizard) && (UnityEngine.Random.value < 0.7f && !alreadyRolled || shouldHaveAQuestionableBit) )
			{
				shouldHaveAQuestionableBit = true;
				innateLength = 22f;
				ivInfluence = 0.5f;
			}
			if ((self.lizard.Template.type == CreatureTemplate.Type.GreenLizard) && (UnityEngine.Random.value < 0.65f && !alreadyRolled || shouldHaveAQuestionableBit))
			{
				shouldHaveAQuestionableBit = true;
				innateLength = 18f;
				ivInfluence = 0.6f;
			}
			if ((self.lizard.Template.type == CreatureTemplate.Type.CyanLizard || self.lizard.Template.type == CreatureTemplate.Type.YellowLizard || self.lizard.Template.type == CreatureTemplate.Type.BlackLizard || self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard) && (UnityEngine.Random.value < 0.55f && !alreadyRolled || shouldHaveAQuestionableBit))
			{
				shouldHaveAQuestionableBit = true;
				innateLength = 12.5f;
				if (self.lizard.Template.type == CreatureTemplate.Type.YellowLizard) innateLength *= 0.8f;
				ivInfluence = 1f;
			}
			if ((self.lizard.Template.type == CreatureTemplate.Type.BlueLizard) && (UnityEngine.Random.value < 0.5f && !alreadyRolled || shouldHaveAQuestionableBit))
			{
				shouldHaveAQuestionableBit = true;
				innateLength = 9f;
				ivInfluence = 0.8f;
			}
			if ((self.lizard.Template.type == CreatureTemplate.Type.PinkLizard) && (UnityEngine.Random.value < 0.4f && !alreadyRolled || shouldHaveAQuestionableBit))
			{
				shouldHaveAQuestionableBit = true;
				innateLength = 10f;
				ivInfluence = 1.6f;
			}
			if (self.lizard.Template.type == CreatureTemplate.Type.Salamander && (((self.blackSalamander && UnityEngine.Random.value < 0.8f) || (!self.blackSalamander && UnityEngine.Random.value < 0.3f)) && !alreadyRolled || shouldHaveAQuestionableBit))
			{
				shouldHaveAQuestionableBit = true;
				innateLength = 12f;
				ivInfluence = 1.2f;
			}
			//Debug.Log("Lizord has a QuestionableLizardBit? " + shouldHaveAQuestionableBit);
			if (shouldHaveAQuestionableBit) self.AddCosmetic(self.startOfExtraSprites + self.extraSprites, new QuestionableLizardBit(self, self.startOfExtraSprites + self.extraSprites, innateLength, ivInfluence));
			UnityEngine.Random.seed = seed;
		}

		protected LizardScale bit;
		protected float innateLength;
		protected float ivInfluence;
        protected float happy;
        protected int graphic = 0;
		protected float graphicHeight;
		protected bool colored = true;
		protected float happiness;

		public QuestionableLizardBit(LizardGraphics self, int v, float innateLength, float ivInfluence) : base(self, v)
		{
            this.spritesOverlap = SpritesOverlap.Behind;
            //this.spritesOverlap = SpritesOverlap.InFront;
            this.graphicHeight = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic).sourcePixelSize.y;
			this.colored = true;
			this.bit = new LizardScale(this);
			this.innateLength =  innateLength;
			this.ivInfluence = ivInfluence;
			this.happy = 0f;

			if ( !string.IsNullOrEmpty(self.lizard.abstractCreature.spawnData) && self.lizard.abstractCreature.spawnData[0] == '{')
			{
				string[] array = self.lizard.abstractCreature.spawnData.Substring(1, self.lizard.abstractCreature.spawnData.Length - 2).Split(new char[]
				{
					','
				});
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].Length > 0)
					{
						string[] array2 = array[i].Split(new char[]
						{
							':'
						});
						string text = array2[0].Trim().ToLowerInvariant();
						if (text == "happy") happy = array2.Length > 1 ? float.Parse(array2[1]) : 1f;
						if (text == "innate") innateLength = array2.Length > 1 ? float.Parse(array2[1]) : innateLength*1.3f;

					}
				}
			}
			if (veryHappy) happy = 1f;
			innateLength *= Mathf.Pow(1.3f, ohMy);

			this.minLength = (innateLength / 5f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.dominance, 0.5f, 1f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.sympathy, 0.3f, 1f);
			this.maxLength = innateLength
				* InfluenceOf(Mathf.Max((lGraphics.lizard.abstractCreature.personality.energy + lGraphics.lizard.abstractCreature.personality.dominance) / 2f, lGraphics.lizard.abstractCreature.personality.dominance), 1.0f, 6f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.dominance, 0.3f, 0.9f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.energy, 0.2f, 1.5f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.sympathy, 0.1f, 1.5f);

			this.minWidth = (innateLength / 40f) 
				* ((lGraphics.iVars.fatness + lGraphics.iVars.tailFatness) / 2f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.dominance, 0.2f, 1f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.sympathy, 0.5f, 1f);
			this.maxWidth = (0.15f + 0.85f * innateLength / graphicHeight)
				* ((lGraphics.iVars.fatness + lGraphics.iVars.tailFatness) / 2f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.energy, 0.3f, 1.3f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.sympathy, 0.2f, 1.1f);

			UpdateSize();
			LogSize();

			this.numberOfSprites = 2;
		}

		protected int throbCounter;
        protected Vector2 lastTarget;
		protected float minLength;
		protected float maxLength;
		protected float minWidth;
		protected float maxWidth;

		private bool logSize = false;
		private static bool logInfluenceMaths = false;

		private void LogSize()
		{
			if (logSize)
			{
				if (logInfluenceMaths)
				{
					float oldInf = ivInfluence;
					ivInfluence = 1f;
					for (int i = 0; i < 11; i++)
					{
						Debug.Log("Lizbits influence of " + (float)i / 10f + " at expo " + 0.2f + " and scale 0.5 is " + InfluenceOf((float)i / 10f, 0.5f, 0.2f));
					}
					for (int i = 0; i < 11; i++)
					{
						Debug.Log("Lizbits influence of " + (float)i / 10f + " at expo " + 0.5f + " and scale 0.5 is " + InfluenceOf((float)i / 10f, 0.5f, 0.5f));
					}
					for (int i = 0; i < 11; i++)
					{
						Debug.Log("Lizbits influence of " + (float)i / 10f + " at expo " + 1f + " and scale 0.5 is " + InfluenceOf((float)i / 10f, 0.5f, 1f));
					}
					for (int i = 0; i < 11; i++)
					{
						Debug.Log("Lizbits influence of " + (float)i / 10f + " at expo " + 1.5f + " and scale 0.5 is " + InfluenceOf((float)i / 10f, 0.5f, 1.5f));
					}
					for (int i = 0; i < 11; i++)
					{
						Debug.Log("Lizbits influence of " + (float)i / 10f + " at expo " + 3f + " and scale 0.5 is " + InfluenceOf((float)i / 10f, 0.5f, 3f));
					}
					ivInfluence = oldInf;

					for (int i = 0; i < 20000; i++)
					{
						AbstractCreature.Personality personality = new AbstractCreature.Personality(new EntityID(-1, i));
						float wouldBeBonus = InfluenceOf(Mathf.Max((personality.energy + personality.dominance) / 2f, personality.dominance), 1f, 6f);
						if (wouldBeBonus > 1.3f)
						{
							Debug.Log("Liz ID " + i + " would have a large bonus of " + wouldBeBonus);
							Debug.Log("Liz dominance " + personality.dominance + " energy " + personality.energy);

						}
						if (wouldBeBonus > 1.1f && wouldBeBonus <= 1.3f)
						{
							Debug.Log("Liz ID " + i + " would have a bonus of " + wouldBeBonus);
							Debug.Log("Liz dominance " + personality.dominance + " energy " + personality.energy);
						}
					}
					logInfluenceMaths = false;
				}

				happiness = 1f;
				UpdateSize();
				Debug.Log("Liz type is " + lGraphics.lizard.Template.type);
				Debug.Log("Liz innate length is " + innateLength);

				Debug.Log("Liz dominance is " + lGraphics.lizard.abstractCreature.personality.dominance);
				Debug.Log("Liz energy is " + lGraphics.lizard.abstractCreature.personality.energy);
				Debug.Log("Liz symp is " + lGraphics.lizard.abstractCreature.personality.sympathy);

				Debug.Log("Liz bit is long " + bit.length);
				Debug.Log("Liz bit is wide " + bit.width);
				happiness = 0f;
				UpdateSize();
				logSize = false;
			}
		}

		protected virtual void UpdateSize()
		{
			bit.length = Mathf.Lerp(minLength, maxLength, Custom.SCurve(happiness, 0.8f)); 
			bit.width = Mathf.Lerp(minWidth, maxWidth, Mathf.Pow(happiness, 0.5f));
		}

		protected virtual LizardGraphics.LizardSpineData GetAttachedPos(float timeStacker)
		{
			float body = this.lGraphics.lizard.bodyChunkConnections[0].distance + this.lGraphics.lizard.bodyChunkConnections[1].distance;
			float s1 = (body - 2.5f) / (body + lGraphics.tailLength);
			LizardGraphics.LizardSpineData frontPos = this.lGraphics.SpinePosition(s1, timeStacker);
			float s2 = (body + 2.5f) / (body + lGraphics.tailLength);
			LizardGraphics.LizardSpineData backPos = this.lGraphics.SpinePosition(s2, timeStacker);
			//backPos.depthRotation = 1; // debug
			frontPos.outerPos = (frontPos.pos + backPos.pos)/2f - (0.97f - (1.5f / frontPos.rad) * maxWidth) * frontPos.rad * frontPos.perp * frontPos.depthRotation;
			return frontPos;
		}

		protected float InfluenceOf(float value, float scale, float exp)
		{
			if (value == 0.5f) return 1;
			if (exp == 1f) return 1 - (ivInfluence * scale) + (ivInfluence * scale * 2f * value);
			return 1 - (ivInfluence * scale) + (ivInfluence * scale * 2f * (((value * 2f) - 1f) * ((Mathf.Pow(Mathf.Abs((value * 2f) - 1f), exp)) / Mathf.Abs((value * 2f) - 1f)) + 1f) / 2f);
		}

		public override void Update()
		{
			if (happiness < happy) happiness = happy;
			else if (happiness < this.lGraphics.showDominance * 0.8f)
			{
				if (UnityEngine.Input.GetKey("l")) Debug.Log("liz happy coz showing off");
				happiness = RWCustom.Custom.LerpAndTick(happiness, this.lGraphics.showDominance * 0.8f, 0.008f, 0.002f);
			}
			else if (lGraphics.lizard.AI != null && lGraphics.lizard.AI.friendTracker != null && lGraphics.lizard.AI.friendTracker.friend != null
				&& lGraphics.lizard.AI.friendTracker.followClosestFriend
				&& lGraphics.lizard.AI.friendTracker.friendMovingCounter == 0
				&& lGraphics.lizard.AI.behavior == LizardAI.Behavior.FollowFriend
				&& lGraphics.lizard.AI.friendTracker.RunSpeed() == 0f)
			{
				if (UnityEngine.Input.GetKey("l")) Debug.Log("liz happy coz friend");
				float dist = lGraphics.lizard.AI.friendTracker.friend.abstractCreature.pos.Tile.FloatDist(lGraphics.lizard.AI.creature.pos.Tile);
				float distFactor = Mathf.Clamp01((dist - 2f) / 12f);
				happiness = RWCustom.Custom.LerpAndTick(happiness, 1 - 0.6f*distFactor, 0.002f, 0.0008f - 0.0006f * distFactor);
			}
			else if (lGraphics.lizard.AI != null && happiness < this.lGraphics.lizard.AI.excitement * 0.6f && this.lGraphics.lizard.AI.CombinedFear < 0.2f)
			{
				if (UnityEngine.Input.GetKey("l")) Debug.Log("liz happy coz excited");
				happiness = RWCustom.Custom.LerpAndTick(happiness, this.lGraphics.lizard.AI.excitement * 0.6f, 0.011f, 0.008f);
			}
			else
			{
				if (UnityEngine.Input.GetKey("l")) Debug.Log("liz unhappy");
				this.happiness -= lGraphics.lizard.AI.panic > 0 ? 0.02f
					: (lGraphics.lizard.AI != null && (this.lGraphics.lizard.AI.CombinedFear > 0.2f) || this.lGraphics.lizard.AI.behavior == LizardAI.Behavior.Flee) ? 0.01f 
					: (lGraphics.lizard.AI != null && this.lGraphics.lizard.AI.behavior == LizardAI.Behavior.Injured) ? 0.005f
					: 0.0018f;
			}
			this.happiness = Mathf.Clamp01(this.happiness);
			UpdateSize();

			LizardGraphics.LizardSpineData backPos = GetAttachedPos(1f);
			// animate
			Vector2 dir = Vector2.Lerp(-backPos.dir,
				-backPos.dir * (-0.2f + happiness * 0.2f
					+ (0.5f) * Mathf.Pow(Mathf.Clamp01(happiness - 0.2f) / 0.8f, 1.5f)
					+ (bit.length / graphicHeight) * Mathf.Pow(Mathf.Clamp01(happiness - 0.2f) / 0.8f, 0.5f)
					+ 6f * Mathf.Clamp01(0.2f - happiness))
				- backPos.perp * Mathf.Sign(backPos.depthRotation) * (bit.width * 0.7f) * (1f + (lGraphics.iVars.fatness - lGraphics.iVars.tailFatness) * 0.25f),
				Mathf.Abs(backPos.depthRotation)).normalized;

			Vector2 target = backPos.outerPos + dir * (bit.length + happiness);
			this.bit.vel += Vector2.ClampMagnitude(target - this.bit.pos, Mathf.Lerp(0.5f, 10f, happiness)) * 0.8f * (0.5f + 0.5f * happiness);
			this.bit.vel *= 0.95f;
			this.throbCounter--;
			if ((target - lastTarget).magnitude > 1.5f) throbCounter = Mathf.Max(throbCounter, 60 - Mathf.FloorToInt(40 * happiness));
			if (throbCounter < 0
				&& happiness > 0.6f
				&& UnityEngine.Random.value < (0.016f - throbCounter * 0.008f)
				)
			{
				throbCounter = 80 - Mathf.FloorToInt(50 * happiness);
				this.bit.vel += -backPos.perp * backPos.depthRotation * (happiness - 0.5f) * 4f;
			}
			lastTarget = target;
			this.bit.ConnectToPoint(backPos.outerPos, this.bit.length, true, 0f, new Vector2(0f, 0f), 0f, 0f);
			this.bit.Update();
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			for (int i = this.startSprite; i >= this.startSprite; i--)
			{
                sLeaser.sprites[i] = new FSprite("LizardScaleA" + this.graphic, true)
                {
                    scaleY = this.bit.length / this.graphicHeight,
                    anchorY = 0.1f,
                    anchorX = 0.3f
                };
                if (this.colored)
				{
                    sLeaser.sprites[i + 1] = new FSprite("LizardScaleB" + this.graphic, true)
                    {
                        scaleY = this.bit.length / this.graphicHeight,
                        anchorY = 0.1f,
                        anchorX = 0.3f
                    };
                }
			}
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			for (int i = this.startSprite; i >= this.startSprite; i--)
			{
				LizardGraphics.LizardSpineData backPos = GetAttachedPos(timeStacker);
				sLeaser.sprites[i].x = backPos.outerPos.x - camPos.x;
				sLeaser.sprites[i].y = backPos.outerPos.y - camPos.y;
				sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(backPos.outerPos, Vector2.Lerp(this.bit.lastPos, this.bit.pos, timeStacker));
				sLeaser.sprites[i].scaleY = this.bit.length / this.graphicHeight;
				sLeaser.sprites[i].scaleX = this.bit.width * Mathf.Sign(backPos.depthRotation);
				if (this.colored)
				{
					sLeaser.sprites[i + 1].x = backPos.outerPos.x - camPos.x;
					sLeaser.sprites[i + 1].y = backPos.outerPos.y - camPos.y;
					sLeaser.sprites[i + 1].rotation = Custom.AimFromOneVectorToAnother(backPos.outerPos, Vector2.Lerp(this.bit.lastPos, this.bit.pos, timeStacker));
					sLeaser.sprites[i + 1].scaleY = this.bit.length / this.graphicHeight;
					sLeaser.sprites[i + 1].scaleX = this.bit.width * Mathf.Sign(backPos.depthRotation);
				}
			}
			//if (this.lGraphics.lizard.Template.type == CreatureTemplate.Type.WhiteLizard)
			//{
			//	this.ApplyPalette(sLeaser, rCam, this.palette);
			//}
			this.ApplyPalette(sLeaser, rCam, this.palette);
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
		{
			for (int i = this.startSprite; i >= this.startSprite; i--)
			{
				sLeaser.sprites[i].color = this.lGraphics.BodyColor(0.5f);
				if (this.colored)
                {
					if (coloredBits)
						sLeaser.sprites[i + 1].color = bitColor;
					else
						sLeaser.sprites[i + 1].color = this.lGraphics.lizard.Template.type == CreatureTemplate.Type.WhiteLizard ? this.lGraphics.HeadColor(1f) : Color.Lerp(this.lGraphics.HeadColor(1f), this.lGraphics.effectColor, 0.3f);
                }
			}
			base.ApplyPalette(sLeaser, rCam, palette);
		}
	}
}