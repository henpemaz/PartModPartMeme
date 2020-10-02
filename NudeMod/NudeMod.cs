using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PartModHmaz
{
    public class NudeMod : PartialityMod
    {
        public NudeMod()
        {
            this.ModID = "NudeMod";
            this.Version = "1.1";
            this.author = "henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            On.OracleGraphics.Gown.Color += new On.OracleGraphics.Gown.hook_Color(OracleGraphics_Gown_Color_Patch);
        }

        public static Color OracleGraphics_Gown_Color_Patch(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown instance, float f)
        {
            Color color = orig.Invoke(instance, f);
            color.a = 0f;
            return color;
        }
    }
}
