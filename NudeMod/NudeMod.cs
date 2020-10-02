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
            this.Version = "2.0";
            this.author = "henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            On.OracleGraphics.Gown.Color += new On.OracleGraphics.Gown.hook_Color(OracleGraphics_Gown_Color_Patch);
            On.CustomDecal.LoadFile += new On.CustomDecal.hook_LoadFile(CustomDecal_LoadFile_Patch);
        }

        public static Color OracleGraphics_Gown_Color_Patch(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown instance, float f)
        {
            Color color = orig.Invoke(instance, f);
            color.a = 0f;
            return color;
        }

        static string[] embeddedFiles = {
            "UNQ - Plate1",
            "UNQ - Plate2",
            "UNQ - Plate3",
            "UNQ - Plate4",
            "UNQ - Plate5"
            };

        public static void CustomDecal_LoadFile_Patch(On.CustomDecal.orig_LoadFile orig, CustomDecal instance, string fileName)
        {   
            if (embeddedFiles.Contains(fileName))
            {
                if (Futile.atlasManager.GetAtlasWithName(fileName) != null)
                {
                    return;
                }
                Texture2D texture2D = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                texture2D.wrapMode = TextureWrapMode.Clamp;
                texture2D.anisoLevel = 0;
                texture2D.filterMode = FilterMode.Point;
                System.IO.Stream stream = typeof(NudeMod).Assembly.GetManifestResourceStream("NudeMod." + fileName + ".png");
                byte[] bytes = new byte[stream.Length];
                stream.Read(bytes, 0, (int)stream.Length);
                texture2D.LoadImage(bytes);
                HeavyTexturesCache.LoadAndCacheAtlasFromTexture(fileName, texture2D);
            }
            else
            {
                orig(instance, fileName);
            }
        }


    }
}
