using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CoolCatTailFix
{
    public class CoolCatTailFix : PartialityMod
    {
        public CoolCatTailFix()
        {
            this.ModID = "CoolCatTailFix";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            On.PlayerGraphics.SlugcatColor += PlayerGraphics_SlugcatColor_patch;
        }

        public static Color PlayerGraphics_SlugcatColor_patch(On.PlayerGraphics.orig_SlugcatColor orig, int i)
        {
            switch (i)
            {
                case 1:
                case 2:
                case 3:
                    return orig(i);
                default:
                    return new Color32(50, 25, 60, 255);
            }
        }
    }
}
