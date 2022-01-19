using System;
using System.Collections.Generic;
using System.Linq;
//using System.Reflection.Emit;
using Mono.Cecil.Cil;
using System.Text;
using MonoMod.Cil;
using UnityEngine;
using System.Reflection;

namespace HurricaneMod
{
    [BepInEx.BepInPlugin("henpemaz.hurricanemodport", "HurricaneMod", "0.2.2")]
    public class HurricaneMod : BepInEx.BaseUnityPlugin
    {
        public string author = "Mikronaut, Henpemaz";
        static HurricaneMod instance;

        private static AttachedField<LizardBreedParams, bool> lizardBreedParamsisLightning = new AttachedField<LizardBreedParams, bool>();

        // this was poorly written
        //private static AttachedField<LizardBreedParams, string> breedParamslizardType = new AttachedField<LizardBreedParams, string>();

        // this was unused
        private static AttachedField<DaddyLongLegs, bool> daddyLongLegsisRed = new AttachedField<DaddyLongLegs, bool>();

        public void OnEnable()
        {
            instance = this;

            On.RainWorld.Start += RainWorld_Start;

            On.LizardBreeds.BreedTemplate += LizardBreeds_BreedTemplate; // this needs here
        }

        private CreatureTemplate LizardBreeds_BreedTemplate(On.LizardBreeds.orig_BreedTemplate orig, CreatureTemplate.Type type, CreatureTemplate lizardAncestor, CreatureTemplate pinkTemplate, CreatureTemplate blueTemplate, CreatureTemplate greenTemplate)
        {
            var retval = orig(type, lizardAncestor, pinkTemplate, blueTemplate, greenTemplate);
            LizardBreedParams lizardBreedParams = (LizardBreedParams)retval.breedParameters;
            // inhale... copy
            // exhale... paste
            // trim whats unchanged (took a lot of patience)
            lizardBreedParamsisLightning[lizardBreedParams] = false;
			switch (type)
			{
				case CreatureTemplate.Type.PinkLizard:
					lizardBreedParams.biteDelay = 6;
					lizardBreedParams.biteChance = 0.75f;
					lizardBreedParams.baseSpeed = 5.1f;
					lizardBreedParams.bodyMass = 2.6f;
					lizardBreedParams.danger = 0.36f;
					lizardBreedParams.noGripSpeed = 0.9f;
					break;
				case CreatureTemplate.Type.GreenLizard:
					lizardBreedParams.biteDelay = 8;
					lizardBreedParams.biteChance = 0.5f;
					lizardBreedParams.baseSpeed = 8.4f;
					lizardBreedParams.bodyMass = 9.4f;
					lizardBreedParams.danger = 0.36f;
					lizardBreedParams.noGripSpeed = 0.15f;
					break;
				case CreatureTemplate.Type.BlueLizard:
					lizardBreedParams.biteDelay = 7;
					lizardBreedParams.biteChance = 0.6f;
					lizardBreedParams.baseSpeed = 4f;
					lizardBreedParams.bodyMass = 2.1f;
					lizardBreedParams.danger = 0.28f;
					lizardBreedParams.noGripSpeed = 0.6f;
					break;
				case CreatureTemplate.Type.YellowLizard:
					lizardBreedParams.biteDelay = 9;
					lizardBreedParams.biteChance = 0.33333334f;
					lizardBreedParams.baseSpeed = 5.1f;
					lizardBreedParams.bodyMass = 2.1f;
					lizardBreedParams.danger = 0.32f;
					lizardBreedParams.noGripSpeed = 0.3f;
					break;
				case CreatureTemplate.Type.WhiteLizard:
					lizardBreedParams.biteDelay = 8;
					lizardBreedParams.baseSpeed = 4.8f;
					lizardBreedParams.bodyMass = 2.6f;
					lizardBreedParams.danger = 0.4f;
					lizardBreedParams.noGripSpeed = 0.3f;
					break;
				case CreatureTemplate.Type.RedLizard:
					lizardBreedParams.baseSpeed = 6.3f;
					lizardBreedParams.bodyMass = 3.9f;
					lizardBreedParams.danger = 0.64f;
					lizardBreedParams.noGripSpeed = 0.75f;
					break;
				case CreatureTemplate.Type.BlackLizard:
					lizardBreedParams.biteDelay = 5;
					lizardBreedParams.baseSpeed = 4.9f;
					lizardBreedParams.bodyMass = 2.5f;
					lizardBreedParams.danger = 0.36f;
					lizardBreedParams.noGripSpeed = 0.3f;
					break;
				case CreatureTemplate.Type.Salamander:
					lizardBreedParams.baseSpeed = 3.9f;
					lizardBreedParams.bodyMass = 2.6f;
					lizardBreedParams.danger = 0.32f;
					lizardBreedParams.noGripSpeed = 0.3f;
					break;
			}
			return retval;
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            IL.LizardCosmetics.SpineSpikes.ctor += SpineSpikes_ctor;
            On.CreatureTemplate.ctor += CreatureTemplate_ctor;
            On.DaddyLongLegs.ctor += DaddyLongLegs_ctor;
            IL.DaddyLongLegs.Stun += DaddyLongLegs_Stun;

            On.Lizard.ctor += Lizard_ctor;
            IL.Lizard.Collide += Lizard_Collide;
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(typeof(OverseerGraphics).GetProperty("MainColor").GetGetMethod(), new ILContext.Manipulator(OverseerGraphics_MainColor));
            On.RainWorld.LoadSetupValues += RainWorld_LoadSetupValues;
            On.SLOracleSwarmer.BitByPlayer += SLOracleSwarmer_BitByPlayer;
            IL.Snail.Click += Snail_Click;

            orig(self);
        }

        private void Lizard_Collide(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchLdarg(1),
                i => i.MatchLdarg(2),
                i => i.MatchLdarg(3),
                i => i.MatchCall<PhysicalObject>("Collide")
                ))
            {
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate<Action<Lizard, int>>((self, myChunk) => {
                        //if (self.lizardParams.isLightning && self.graphicsModule != null)
                    if (lizardBreedParamsisLightning[self.lizardParams] && self.graphicsModule != null)
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            self.room.AddObject(new Spark(self.bodyChunks[myChunk].pos, RWCustom.Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
                        }
                        self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos);
                    }
                });
            }
            else Debug.LogException( new Exception("Couldn't IL-hook Lizard_Collide from Hurricane mod")); // deffendisve progrmanig
        }

        private RainWorldGame.SetupValues RainWorld_LoadSetupValues(On.RainWorld.orig_LoadSetupValues orig, bool distributionBuild)
        {
            var retval = orig(distributionBuild);
            if (distributionBuild)
            {
                retval.lungs = 96;
                retval.cycleTimeMin = 200;
                retval.cycleTimeMax = 600;
            }
            return retval;
        }

        private void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            var lizardParams = (abstractCreature.creatureTemplate.breedParameters as LizardBreedParams);
            if (abstractCreature.creatureTemplate.type == CreatureTemplate.Type.RedLizard) SetLightningLizard(lizardParams);
            orig(self, abstractCreature, world);
            if(lizardBreedParamsisLightning[lizardParams]) self.effectColor = lizardParams.standardColor;
        }

        public static void SetLightningLizard(LizardBreedParams lizardBreedParams)
        {
            if (new System.Random(Guid.NewGuid().GetHashCode()).Next(10) <= 1)
            {
                lizardBreedParams.baseSpeed = 10f;
                lizardBreedParams.biteDelay = 1;
                lizardBreedParams.biteDamage = 5f;
                lizardBreedParams.danger = 0.7f;
                lizardBreedParams.loungeDistance = 210f;
                lizardBreedParams.maxMusclePower = 14f;
                lizardBreedParams.biteHomingSpeed = 5.4f;
                lizardBreedParams.toughness = 4f;
                lizardBreedParams.stunToughness = 4f;
                lizardBreedParams.bodyMass = 13f;
                lizardBreedParams.bodySizeFac = 1.4f;
                lizardBreedParams.swimSpeed = 3f;
                lizardBreedParams.loungeSpeed = 3.2f;
                lizardBreedParams.preLoungeCrouchMovement = -0.3f;
                lizardBreedParams.loungeDelay = 60;
                lizardBreedParams.postLoungeStun = 18;
                lizardBreedParams.idleCounterSubtractWhenCloseToIdlePos = 50;
                lizardBreedParams.tailSegments = 15;
                lizardBreedParams.tamingDifficulty = 10f;
                lizardBreedParams.jawOpenAngle = 150f;
                lizardBreedParams.headSize = 1.4f;
                lizardBreedParams.attemptBiteRadius = 150f;
                lizardBreedParams.tailLengthFactor = 2.5f;
                lizardBreedParams.standardColor = new Color(0.25490195f, 0f, 0.21568628f);
                lizardBreedParams.perfectVisionAngle = Mathf.Lerp(1f, -1f, 0.5f);
                lizardBreedParams.periferalVisionAngle = Mathf.Lerp(1f, -1f, 0.9f);
                //lizardBreedParams.isLightning = true;
                lizardBreedParamsisLightning[lizardBreedParams] = true;
                lizardBreedParams.tailStiffness = 400f;
                lizardBreedParams.tailColorationExponent = 1.2f;
                lizardBreedParams.tailColorationStart = 0.1f;
                lizardBreedParams.loungeTendensy = 0.2f;
                return;
            }
            lizardBreedParams.baseSpeed = 6.3f;
            lizardBreedParams.biteDelay = 2;
            lizardBreedParams.biteDamage = 4f;
            lizardBreedParams.danger = 0.64f;
            lizardBreedParams.loungeDistance = 100f;
            lizardBreedParams.maxMusclePower = 9f;
            lizardBreedParams.biteHomingSpeed = 4.7f;
            lizardBreedParams.toughness = 3f;
            lizardBreedParams.stunToughness = 3f;
            lizardBreedParams.bodyMass = 3.9f;
            lizardBreedParams.bodySizeFac = 1.2f;
            lizardBreedParams.swimSpeed = 1.9f;
            lizardBreedParams.loungeSpeed = 1.9f;
            lizardBreedParams.preLoungeCrouchMovement = -0.2f;
            lizardBreedParams.loungeDelay = 90;
            lizardBreedParams.postLoungeStun = 20;
            lizardBreedParams.idleCounterSubtractWhenCloseToIdlePos = 10;
            lizardBreedParams.tailSegments = 11;
            lizardBreedParams.tamingDifficulty = 7f;
            lizardBreedParams.jawOpenAngle = 140f;
            lizardBreedParams.headSize = 1.2f;
            lizardBreedParams.attemptBiteRadius = 120f;
            lizardBreedParams.tailLengthFactor = 1.9f;
            lizardBreedParams.standardColor = new Color(1f, 0f, 0f);
            lizardBreedParams.perfectVisionAngle = Mathf.Lerp(1f, -1f, 0.44444445f);
            lizardBreedParams.periferalVisionAngle = Mathf.Lerp(1f, -1f, 0.7777778f);
            //lizardBreedParams.isLightning = false;
            lizardBreedParamsisLightning[lizardBreedParams] = false;
            lizardBreedParams.tailStiffness = 200f;
            lizardBreedParams.tailColorationStart = 0.3f;
            lizardBreedParams.tailColorationExponent = 2f;
            lizardBreedParams.loungeTendensy = 0.05f;
        }

        private void OverseerGraphics_MainColor(ILContext il)
        {
            bool once = false;
            var c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdcR4(0.44705883f),
                i => i.MatchLdcR4(0.9019608f),
                i => i.MatchLdcR4(0.76862746f)
                ))
            {
                once = true;
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldc_R4, 1f);
                c.Emit(OpCodes.Ldc_R4, 0f);
                c.Emit(OpCodes.Ldc_R4, 0.41568628f);
                c.RemoveRange(3);
            }
            if(!once) Debug.LogException(new Exception("Couldn't IL-hook OverseerGraphics_MainColor from Hurricane mod")); // deffendisve progrmanig
        }

        private void Snail_Click(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdloc(3),
                i => i.MatchIsinst<Leech>()
                ))
            {
                var go = il.DefineLabel();
                c.MarkLabel(go);
                c.MoveBeforeLabels();
                c.Emit(OpCodes.Ldloc_3);
                c.Emit(OpCodes.Isinst, typeof(Player));
                c.Emit(OpCodes.Brfalse, go);
                c.Emit(OpCodes.Ldloc_3);
                c.Emit(OpCodes.Callvirt, typeof(Creature).GetMethod("Die"));
            }
            else Debug.LogException(new Exception("Couldn't IL-hook Snail_Click from Hurricane mod")); // deffendisve progrmanig
        }

        private void SLOracleSwarmer_BitByPlayer(On.SLOracleSwarmer.orig_BitByPlayer orig, SLOracleSwarmer self, Creature.Grasp grasp, bool eu)
        {
            orig(self, grasp, eu);

            if(self.bites < 1)
            {
                if (self.room.game.session is StoryGameSession)
                {
                    (self.room.game.session as StoryGameSession).saveState.theGlow = false;
                }
                (grasp.grabber as Player).glowing = false;
            }
        }

        private void DaddyLongLegs_Stun(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchCall<DaddyLongLegs>("get_SizeClass"),
                i => i.MatchBrfalse(out var nogo)
                ))
            {
                var go = il.DefineLabel();
                c.MarkLabel(go);

                c.Index--; // before branch nogo

                c.Emit(OpCodes.Brtrue, go);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<DaddyLongLegs,bool>>(dll =>
                {
                    if (daddyLongLegsisRed.Get(dll))
                    {
                        return true;
                    }
                    return false;
                });
            }
            else Debug.LogException(new Exception("Couldn't IL-hook DaddyLongLegs_Stun from Hurricane mod")); // deffendisve progrmanig
        }

        private void DaddyLongLegs_ctor(On.DaddyLongLegs.orig_ctor orig, DaddyLongLegs self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.SizeClass)
            {
                self.effectColor = new Color(0.25f, 0.44f, 0f);
                self.eyeColor = self.effectColor;
            }
            else
            {
                self.effectColor = new Color(0.7f, 0.4f, 0.7f);
                self.eyeColor = new Color(0.44f, 0f, 1f);
            }
        }

        public static class EnumExt_HurricaneMod
        {
#pragma warning disable 0649
            public static CreatureTemplate.Type RedLongLegs;
#pragma warning restore 0649
        }

        private void CreatureTemplate_ctor(On.CreatureTemplate.orig_ctor orig, CreatureTemplate self, CreatureTemplate.Type type, CreatureTemplate ancestor, List<TileTypeResistance> tileResistances, List<TileConnectionResistance> connectionResistances, CreatureTemplate.Relationship defaultRelationship)
        {
            orig(self, type, ancestor, tileResistances, connectionResistances, defaultRelationship);
            if(type == EnumExt_HurricaneMod.RedLongLegs)
            {
                self.name = "Red Long Legs";
            }
        }

        private void SpineSpikes_ctor(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(i => i.MatchRet());
            if (c.TryGotoPrev(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<LizardCosmetics.SpineSpikes>("colored"),
                i => i.MatchLdcI4(0),
                i => i.MatchBle(out var _)
                ))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<LizardCosmetics.SpineSpikes>>(ss =>
                {
                    if (lizardBreedParamsisLightning.Get(ss.lGraphics.lizard.lizardParams))
                    {
                        ss.colored = 1;
                        ss.sizeRangeMin = 1;
                        ss.sizeRangeMax = 3;
                    }
                });

            }
            else Debug.LogException(new Exception("Couldn't IL-hook SpineSpikes_ctor from Hurricane mod")); // deffendisve progrmanig
        }

    }
}
