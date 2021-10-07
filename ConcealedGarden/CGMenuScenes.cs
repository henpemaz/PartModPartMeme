using System;
using System.Collections.Generic;
using System.IO;

namespace ConcealedGarden
{
    internal class CGMenuScenes
    {
        internal static void Apply()
        {
            On.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;
            On.Menu.SleepAndDeathScreen.AddBkgIllustration += SleepAndDeathScreen_AddBkgIllustration;
        }


        private static string VanillaPath(string scene, string sprite) => "Scenes" + Path.DirectorySeparatorChar + scene + Path.DirectorySeparatorChar + sprite;
        private static readonly string replacementFolder = "Scenes" + Path.DirectorySeparatorChar + "CGMenuAlt";
        private static readonly Dictionary<string, string> transfurrReplacemens = new Dictionary<string, string>()
        {
            {VanillaPath("Sleep Screen - White", "Sleep - 2 - White"), "Sleep - 2 - White - tf"},
            {VanillaPath("Sleep Screen - Yellow", "Sleep - 2 - Yellow"), "Sleep - 2 - Yellow - tf"},
            {VanillaPath("Sleep Screen - Red", "Sleep - 2 - Red"), "Sleep - 2 - Red - tf"},
            {VanillaPath("Slugcat - White", "White Slugcat - 2"), "White Slugcat - 2 - tf"},
            {VanillaPath("Slugcat - Yellow", "Yellow Slugcat - 1"), "Yellow Slugcat - 1 - tf"},
            {VanillaPath("Slugcat - Red", "Red Slugcat - 1"), "Red Slugcat - 1 - tf"},
        };

        private static void SlugcatPage_AddImage(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_AddImage orig, Menu.SlugcatSelectMenu.SlugcatPage self, bool ascended)
        {
            orig(self, ascended);
            string pdata = ConcealedGardenProgression.progression.GetProgDataOfSlugcat(self.colorName);
            Dictionary<string, object> storedPp;
            if (!string.IsNullOrEmpty(pdata) && (storedPp = (Dictionary<string, object>)Json.Deserialize(pdata)) != null)
            {
                ConcealedGardenProgression progOfCat = new ConcealedGardenProgression(persDict: storedPp);
                if (progOfCat.transfurred)
                {
                    foreach(var i in self.slugcatImage.depthIllustrations)
                    {
                        if(transfurrReplacemens.TryGetValue(i.folderName + Path.DirectorySeparatorChar + i.fileName, out string replacement))
                        {
                            i.fileName = replacement;
                            i.folderName = replacementFolder;
                            i.LoadFile(i.folderName);
                            i.sprite.SetElementByName(i.fileName);
                        }
                    }
                }
            }
        }

        private static void SleepAndDeathScreen_AddBkgIllustration(On.Menu.SleepAndDeathScreen.orig_AddBkgIllustration orig, Menu.SleepAndDeathScreen self)
        {
            orig(self);
            string pdata = ConcealedGardenProgression.progression.GetProgDataOfSlugcat(self.manager.rainWorld.progression.PlayingAsSlugcat);
            if (!string.IsNullOrEmpty(pdata) && (Json.Deserialize(pdata) is Dictionary<string, object> storedPp))
            {
                ConcealedGardenProgression progOfCat = new ConcealedGardenProgression(persDict: storedPp);
                if (progOfCat.transfurred)
                {
                    foreach (var i in self.scene.depthIllustrations)
                    {
                        if (transfurrReplacemens.TryGetValue(i.folderName + Path.DirectorySeparatorChar + i.fileName, out string replacement))
                        {
                            i.fileName = replacement;
                            i.folderName = replacementFolder;
                            i.LoadFile(i.folderName);
                            i.sprite.SetElementByName(i.fileName);
                        }
                    }
                }
            }
        }
    }
}