using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Partiality.Modloader;


using System.Security;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using UnityEngine;

[assembly: IgnoresAccessChecksTo("Assembly-CSharp")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
        {
            AssemblyName = assemblyName;
        }

        public string AssemblyName { get; }
    }
}


namespace LizardSkin
{
    public class LizardSkin : PartialityMod
    {

        public LizardSkin()
        {
            this.ModID = "LizardSkin";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static LizardSkin instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            PlayerGraphicsCosmeticsAdaptor.ApplyHooksToPlayerGraphics();

            // if FancySlugcats present
            Type fpg = Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats");
            if(fpg != null)
            {
                Debug.LogError("LizardSkin: FOUND FancyPlayerGraphics");
                FancyPlayerGraphicsCosmeticsAdaptor.ApplyHooksToFancyPlayerGraphics();
            }
            else
            {
                Debug.LogError("LizardSkin: NOT FOUND FancyPlayerGraphics");
            }
            // 

            Type jollypg = Type.GetType("JollyCoop.PlayerGraphicsHK, JollyCoop");
            if (jollypg != null)
            {
                Debug.LogError("LizardSkin: FOUND Jolly");
                PlayerGraphicsCosmeticsAdaptor.ApplyHooksToJollyPlayerGraphicsHK();
            }
            else
            {
                Debug.LogError("LizardSkin: NOT FOUND Jolly");
            }

            Type custail_ref = Type.GetType("CustomTail.CustomTail, CustomTail");
            if (custail_ref != null)
            {
                Debug.LogError("LizardSkin: FOUND CustomTail");
                // No hookies
            }
            else
            {
                Debug.LogError("LizardSkin: NOT FOUND CustomTail");
            }

            Type colorfoot_ref = Type.GetType("Colorfoot.LegMod, Colorfoot");
            if (colorfoot_ref != null)
            {
                Debug.LogError("LizardSkin: FOUND Colorfoot");
                PlayerGraphicsCosmeticsAdaptor.ApplyHooksToColorfootPlayerGraphicsPatch();
            }
            else
            {
                Debug.LogError("LizardSkin: NOT FOUND Colorfoot");
            }
        }
    }
}
