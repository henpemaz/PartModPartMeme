using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Security;
using System.Security.Permissions;
using Partiality.Modloader;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace TestMod
{
    public class TestMod : PartialityMod
    {
        public TestMod()
        {
            this.ModID = "TestMod";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static TestMod instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            On.RainWorld.Start += RainWorld_Start;
            On.RoomCamera.Update += RoomCamera_Update;
        }

        private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            orig(self);
            if (UnityEngine.Input.GetKeyDown("k"))
            {
                UnityEngine.Debug.LogError(self.currentPalette);
                UnityEngine.Debug.LogError(self.currentPalette.darkness);
                UnityEngine.Debug.LogError(self.currentPalette.blackColor);

            }
        }

        private void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            
            orig(self);

            UnityEngine.Debug.LogError("start search");
            var targetHead = IntColor(23, 26, 20);
            var targetBody = IntColor(120, 38, 21);

            var refDark = 0.4196078f;
            var refBlack = new UnityEngine.Color(0.086f, 0.075f, 0.110f);

            var tolerance = 1/255f;

            for (int i = 1000; i < 100000; i++)
            {
                var sg = MakeAScav(i);

                //  from palette
                sg.darkness = refDark;
                sg.blackColor = refBlack;

                var gotHead = sg.BlendedHeadColor;
                var gotBody = sg.BlendedBodyColor;

                if(ColorsClose(gotHead, targetHead, tolerance) && ColorsClose(gotBody, targetBody, tolerance))
                {
                    UnityEngine.Debug.LogError("scac id " + i);
                }
            }

            UnityEngine.Debug.LogError("end search");
        }

        private UnityEngine.Color IntColor(int r, int g, int b)
        {
            return new UnityEngine.Color(r / 255f, g / 255f, b / 255f);
        }

        private bool ColorsClose(UnityEngine.Color a, UnityEngine.Color b, float tol)
        {
            return (UnityEngine.Mathf.Abs(a.r - b.r) < tol && UnityEngine.Mathf.Abs(a.g - b.g) < tol && UnityEngine.Mathf.Abs(a.b - b.b) < tol);
        }

        static ScavengerGraphics MakeAScav(int seed)
        {
            var ID = new EntityID(-1, seed);
            var absscac = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(AbstractCreature)) as AbstractCreature;
            absscac.ID = ID;
            absscac.personality = new AbstractCreature.Personality(ID);
            var realscac = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Scavenger)) as Scavenger;
            realscac.abstractPhysicalObject = absscac;
            var scacgraphics = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ScavengerGraphics)) as ScavengerGraphics;
            scacgraphics.scavenger = realscac;

            UnityEngine.Random.seed = ID.RandomSeed;
            scacgraphics.iVars = new ScavengerGraphics.IndividualVariations(realscac);
            scacgraphics.GenerateColors();
            return scacgraphics;
        }
    }
}
