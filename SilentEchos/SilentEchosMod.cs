using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SilentEchos
{
    public class SilentEchosMod : PartialityMod
    {
        public SilentEchosMod()
        {
            this.ModID = "SilentEchosMod";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();

            On.Ghost.Update += Ghost_Update;
        }

        private void Ghost_Update(On.Ghost.orig_Update orig, Ghost self, bool eu)
        {
            orig(self, eu);
            self.onScreenCounter = UnityEngine.Mathf.Min(self.onScreenCounter, 1);
        }
    }
}
