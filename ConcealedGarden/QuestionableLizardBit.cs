using RWCustom;
using UnityEngine;
// Don't look
namespace ConcealedGarden
{
	// I said don't look
	internal class QuestionableLizardBit : LizardCosmetics.Template
	{
		// Stop what are you doing
		private LizardScale bit;
		private float inhateLength;
		private float ivInfluence;
		private int graphic = 0;
		private float graphicHeight;
		private bool colored = true;
		private float happiness;

		public static void Apply()
		{
			// You're not gonna like it
			On.LizardGraphics.ctor += LizardGraphics_ctor;
		}
		private static void LizardGraphics_ctor(On.LizardGraphics.orig_ctor orig, LizardGraphics self, PhysicalObject ow)
		{
			orig(self, ow);
			// But we're doing it
			//Debug.Log("Lizord create");
			if (!self.lizard.room.game.IsStorySession) return;
			//Debug.Log("is story");
			World.CreatureSpawner spawner = self.lizard.room.game.world.GetSpawner(self.lizard.abstractCreature.ID);
			//Debug.Log("region name is " + (self.lizard.room.game.session as StoryGameSession).saveState.regionStates[spawnRegion].regionName);
			if (spawner != null && (spawner.region < 0 || spawner.region >= (self.lizard.room.game.session as StoryGameSession).saveState.regionStates.Length
				|| (self.lizard.room.game.session as StoryGameSession).saveState.regionStates[spawner.region].regionName != "CG"))
				return;
			if (spawner == null && self.lizard?.room?.world != null && self.lizard.room.world.name != "CG") return;
			int seed = UnityEngine.Random.seed;
			UnityEngine.Random.seed = self.lizard.abstractCreature.ID.RandomSeed;
			float inhateLength = 15f;
			float ivInfluence = 1f;
			bool shouldHaveAQuestionableBit = false;
			if ((self.lizard.Template.type == CreatureTemplate.Type.RedLizard) && UnityEngine.Random.value < 0.7f)
			{
				shouldHaveAQuestionableBit = true;
				inhateLength = 22f;
				ivInfluence = 0.5f;
			}
			if ((self.lizard.Template.type == CreatureTemplate.Type.GreenLizard) && UnityEngine.Random.value < 0.65f)
			{
				shouldHaveAQuestionableBit = true;
				inhateLength = 18f;
				ivInfluence = 0.6f;
			}
			if ((self.lizard.Template.type == CreatureTemplate.Type.CyanLizard || self.lizard.Template.type == CreatureTemplate.Type.YellowLizard || self.lizard.Template.type == CreatureTemplate.Type.BlackLizard || self.lizard.Template.type == CreatureTemplate.Type.WhiteLizard) && UnityEngine.Random.value < 0.55f)
			{
				shouldHaveAQuestionableBit = true;
				inhateLength = 12.5f;
				if (self.lizard.Template.type == CreatureTemplate.Type.YellowLizard) inhateLength *= 0.8f;
				ivInfluence = 1f;
			}
			if ((self.lizard.Template.type == CreatureTemplate.Type.BlueLizard) && UnityEngine.Random.value < 0.5f)
			{
				shouldHaveAQuestionableBit = true;
				inhateLength = 8f;
				ivInfluence = 0.8f;
			}
			if ((self.lizard.Template.type == CreatureTemplate.Type.PinkLizard) && UnityEngine.Random.value < 0.4f)
			{
				shouldHaveAQuestionableBit = true;
				inhateLength = 10f;
				ivInfluence = 1.5f;
			}
			if (self.lizard.Template.type == CreatureTemplate.Type.Salamander && ((self.blackSalamander && UnityEngine.Random.value < 0.8f) || (!self.blackSalamander && UnityEngine.Random.value < 0.3f)))
			{
				shouldHaveAQuestionableBit = true;
				inhateLength = 12f;
				ivInfluence = 1.2f;
			}
			//Debug.Log("Lizord has a QuestionableLizardBit? " + shouldHaveAQuestionableBit);
			if (shouldHaveAQuestionableBit) self.AddCosmetic(self.startOfExtraSprites + self.extraSprites, new QuestionableLizardBit(self, self.startOfExtraSprites + self.extraSprites, inhateLength, ivInfluence));
			UnityEngine.Random.seed = seed;
		}

		public QuestionableLizardBit(LizardGraphics self, int v, float inhateLength, float ivInfluence) : base(self, v)
		{
            this.spritesOverlap = SpritesOverlap.Behind;
            //this.spritesOverlap = SpritesOverlap.InFront;
            this.graphicHeight = Futile.atlasManager.GetElementWithName("LizardScaleA" + this.graphic).sourcePixelSize.y;
			this.colored = true;
			this.bit = new LizardScale(this);
			this.inhateLength =  inhateLength;
			//this.inhateLength = 50;// inhateLength;
			this.ivInfluence = ivInfluence;

			this.minLength = (inhateLength / 5f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.dominance, 0.5f, 1f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.sympathy, 0.3f, 1f);
			this.maxLength = inhateLength
				* InfluenceOf(Mathf.Max((lGraphics.lizard.abstractCreature.personality.energy + lGraphics.lizard.abstractCreature.personality.dominance) / 2f, lGraphics.lizard.abstractCreature.personality.dominance), 1.0f, 6f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.dominance, 0.3f, 0.9f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.energy, 0.2f, 1.5f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.sympathy, 0.1f, 1.5f);

			this.minWidth = (inhateLength / 40f) 
				* ((lGraphics.iVars.fatness + lGraphics.iVars.tailFatness) / 2f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.dominance, 0.2f, 1f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.sympathy, 0.5f, 1f);
			this.maxWidth = (0.15f + 0.85f * inhateLength / graphicHeight)
				* ((lGraphics.iVars.fatness + lGraphics.iVars.tailFatness) / 2f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.energy, 0.3f, 1.3f)
				* InfluenceOf(lGraphics.lizard.abstractCreature.personality.sympathy, 0.2f, 1.1f);

			UpdateSize();
			LogSize();

			this.numberOfSprites = 2;
		}

		private int throbCounter;
        private Vector2 lastTarget;
		private float minLength;
		private float maxLength;
		private float minWidth;
		private float maxWidth;

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
				Debug.Log("Liz inhate length is " + inhateLength);

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

		private void UpdateSize()
		{
			bit.length = Mathf.Lerp(minLength, maxLength, Custom.SCurve(happiness, 0.8f)); 
			bit.width = Mathf.Lerp(minWidth, maxWidth, Mathf.Pow(happiness, 0.5f));
		}

		private LizardGraphics.LizardSpineData GetAttachedPos(float timeStacker)
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

		private float InfluenceOf(float value, float scale, float exp)
		{
			if (value == 0.5f) return 1;
			if (exp == 1f) return 1 - (ivInfluence * scale) + (ivInfluence * scale * 2f * value);
			return 1 - (ivInfluence * scale) + (ivInfluence * scale * 2f * (((value * 2f) - 1f) * ((Mathf.Pow(Mathf.Abs((value * 2f) - 1f), exp)) / Mathf.Abs((value * 2f) - 1f)) + 1f) / 2f);
		}

		public override void Update()
		{
			if (happiness < this.lGraphics.showDominance * 0.8f)
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
				sLeaser.sprites[i] = new FSprite("LizardScaleA" + this.graphic, true);
				sLeaser.sprites[i].scaleY = this.bit.length / this.graphicHeight;
				sLeaser.sprites[i].anchorY = 0.1f;
				sLeaser.sprites[i].anchorX = 0.3f;
				if (this.colored)
				{
					sLeaser.sprites[i + 1] = new FSprite("LizardScaleB" + this.graphic, true);
					sLeaser.sprites[i + 1].scaleY = this.bit.length / this.graphicHeight;
					sLeaser.sprites[i + 1].anchorY = 0.1f;
					sLeaser.sprites[i + 1].anchorX = 0.3f;
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
					sLeaser.sprites[i + 1].color = this.lGraphics.lizard.Template.type == CreatureTemplate.Type.WhiteLizard ? this.lGraphics.HeadColor(1f) : Color.Lerp(this.lGraphics.HeadColor(1f), this.lGraphics.effectColor, 0.3f);
                }
			}
			base.ApplyPalette(sLeaser, rCam, palette);
		}
	}
}