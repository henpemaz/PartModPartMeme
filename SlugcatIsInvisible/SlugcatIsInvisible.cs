using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SlugcatIsInvisible
{
    public class SlugcatIsInvisible : PartialityMod
    {
        public SlugcatIsInvisible()
        {
            this.ModID = "SlugcatIsInvisible";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette_hk;
        }

        public void PlayerGraphics_ApplyPalette_hk(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics instance, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {

            //return new Color32(0,0,0,0);
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].color = Color.clear;
            }
        }
    }
}
