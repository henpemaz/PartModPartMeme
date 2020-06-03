using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PartModHmaz
{
    public class BaseMod : PartialityMod
    {
        public BaseMod()
        {
            this.ModID = "BaseMod";
            this.Version = "0001";
            this.author = "henpemaz";
        }

        public static BaseScript script;

        public override void OnEnable()
        {
            base.OnEnable();
            BaseScript.mod = this;
            GameObject go = new GameObject();
            script = go.AddComponent<BaseScript>();
            script.Initialize();
        }
    }
}
