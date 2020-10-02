using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScavsHaveBadAim
{
    public class ScavsHaveBadAim : PartialityMod
    {
        public ScavsHaveBadAim()
        {
            instance = this;
            this.ModID = "ScavsHaveBadAim";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public static ScavsHaveBadAim instance; // Not necessary, but useful if you are planning to support Config Machine


        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking codes would go here
            // These two do exactly the same
            // On.ScavengerAI.ViolenceTypeAgainstCreature += new On.ScavengerAI.hook_ViolenceTypeAgainstCreature(Patched_ViolenceTypeAgainstCreature);
            On.ScavengerAI.ViolenceTypeAgainstCreature += Patched_ViolenceTypeAgainstCreature;
            ScavsHaveBadAim.instance = this;
        }
        
        static ScavengerAI.ViolenceType Patched_ViolenceTypeAgainstCreature(On.ScavengerAI.orig_ViolenceTypeAgainstCreature orig, ScavengerAI instance, Tracker.CreatureRepresentation critRep)
        {
            // These two do exactly the same
            //ScavengerAI.ViolenceType result = orig.Invoke(instance, critRep);
            ScavengerAI.ViolenceType result = orig(instance, critRep);

            if (critRep.representedCreature.realizedCreature != null && critRep.representedCreature.realizedCreature is Player)
            {
                if (result == ScavengerAI.ViolenceType.Lethal)
                {
                    result = ScavengerAI.ViolenceType.Warning;
                }
            }
            return result;
        }
    }
}
