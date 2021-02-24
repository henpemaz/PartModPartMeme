using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DevelopmentBuild
{
    public class DevelopmentBuildMod : PartialityMod
    {
        public DevelopmentBuildMod()
        {
            this.ModID = "DevelopmentBuildMod";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static DevelopmentBuildMod instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre
            On.RainWorld.LoadSetupValues += RainWorld_LoadSetupValues;
            

        }

        private RainWorldGame.SetupValues RainWorld_LoadSetupValues(On.RainWorld.orig_LoadSetupValues orig, bool distributionBuild)
        {
            RainWorld rw = UnityEngine.Object.FindObjectOfType<RainWorld>();
            if(rw != null)
            {
                rw.buildType = RainWorld.BuildType.Development;

                if (!System.IO.File.Exists(RWCustom.Custom.RootFolderDirectory() + "setup.txt"))
                {
					using (System.IO.StreamWriter streamWriter = System.IO.File.CreateText(RWCustom.Custom.RootFolderDirectory() + "setup.txt"))
					{
                        streamWriter.Write(defaultsetup);
					}
				}
            }

            return orig(false);
        }
        private const string defaultsetup = "starting room: SU_C04" +
"\nplayer 2 active: 0" + // 0;
"\npink: 0" + // 1;
"\ngreen: 0" + // 2;
"\nblue: 0" + // 3;
"\nwhite: 0" + // 4;
"\nspears: 0" + // 5;
"\nflies: 0" + // 6;
"\nleeches: 0" + // 7;
"\nsnails: 0" + // 8;
"\nvultures: 0" + // 9;
"\nlantern mice: 0" + // 10;
"\ncicadas: 0" + // 11;
"\npalette: 0" + // 12;
"\nlizard laser eyes: 0" + // 13;
"\nplayer invincibility: 0" + // 14;
"\ncycle time min in seconds: 400" + // 15;
"\ncycle time max in seconds: 800" + // 59;
"\nflies to win: 4" + // 16;
"\nworld creatures spawn: 1" + // 17;
"\ndon't bake: 1" + // 18;
"\nwidescreen: 0" + // 19;
"\nstart screen: 1" + // 20;
"\ncycle startup: 1" + // 21;
"\nfull screen: 0" + // 22;
"\nyellow: 0" + // 23;
"\nred: 0" + // 24;
"\nspiders: 0" + // 25;
"\nplayer glowing: 0" + // 26;
"\ngarbage worms: 0" + // 27;
"\njet fish: 0" + // 28;
"\nblack: 0" + // 29;
"\nsea leeches: 0" + // 30;
"\nsalamanders: 0" + // 31;
"\nbig eels: 0" + // 32;
"\ndefault settings screen: 0" + // 33;
"\nplayer 1 active: 1" + // 34;
"\ndeer: 0" + // 35;
"\ndev tools active: 0" + // 36;
"\ndaddy long legs: 0" + // 37;
"\ntube worms: 0" + // 38;
"\nbro long legs: 0" + // 39;
"\ntentacle plants: 0" + // 40;
"\npole mimics: 0" + // 41;
"\nmiros birds: 0" + // 42;
"\nload game: 1" + // 43;
"\nmulti use gates: 0" + // 44;
"\ntemple guards: 0" + // 45;
"\ncentipedes: 0" + // 46;
"\nworld: 1" + // 47;
"\ngravity flicker cycle min: 8" + // 48;
"\ngravity flicker cycle max: 18" + // 49;
"\nreveal map: 0" + // 50;
"\nscavengers: 0" + // 51;
"\nscavengers shy: 0" + // 52;
"\nscavenger like player: 0" + // 53;
"\ncentiwings: 0" + // 54;
"\nsmall centipedes: 0" + // 55;
"\nload progression: 1" + // 56;
"\nlungs: 128" + // 57;
"\nplay music: 1" + // 58;
"\ncheat karma: 0" + // 60;
"\nload all ambient sounds: 0" + // 61;
"\noverseers: 0" + // 62;
"\nghosts: 0" + // 63;
"\nfire spears: 0" + // 64;
"\nscavenger lanterns: 0" + // 65;
"\nalways travel: 0" + // 66;
"\nscavenger bombs: 0" + // 67;
"\nthe mark: 0" + // 68;
"\ncustom: 0" + // 69;
"\nbig spiders: 0" + // 70;
"\negg bugs: 0" + // 71;
"\nsingle player character: -1" + // 72;
"\nneedle worms: 0" + // 73;
"\nsmall needle worms: 0" + // 74;
"\nspitter spiders: 0" + // 75;
"\ndropwigs: 0" + // 76;
"\ncyan: 0" + // 77;
"\nking vultures: 0" + // 78;
"\nlog spawned creatures: 0" + // 79;
"\nred centipedes: 0" + // 80;
"\nproceed lineages: 0" + // 81;
"\n";
    }


}
