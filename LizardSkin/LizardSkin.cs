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
using OptionalUI;

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

        public static OptionalUI.OptionInterface LoadOI()
        {
            return new MyOI();
        }
        internal class MyOI : OptionalUI.OptionInterface
        {
            public MyOI() : base(mod: LizardSkin.instance)
            {

            }

            public override void Initialize()
            {
                base.Initialize();

                this.Tabs = new OptionalUI.OpTab[1];
                this.Tabs[0] = new OptionalUI.OpTab();

                OpContainer myContainer = new MenuCosmeticsAdaptor(new Vector2(300, 300));


                this.Tabs[0].AddItems(myContainer);

                OpLabel myLabel = new OpLabel(new Vector2(300, 300), new Vector2(60, 30), "Lizcat preview :)");

                this.Tabs[0].AddItems(myLabel);
            }

            public override void Update(float dt)
            {
                base.Update(dt);
            }
        }


        internal static Type fpg;
        internal static Type jolly_ref;
        internal static Type custail_ref;
        internal static Type colorfoot_ref;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            PlayerGraphicsCosmeticsAdaptor.ApplyHooksToPlayerGraphics();

            // store type to check for instances
            fpg = Type.GetType("FancySlugcats.FancyPlayerGraphics, FancySlugcats");
            jolly_ref = Type.GetType("JollyCoop.PlayerGraphicsHK, JollyCoop");
            custail_ref = Type.GetType("CustomTail.CustomTail, CustomTail");
            colorfoot_ref = Type.GetType("Colorfoot.LegMod, Colorfoot");

            if (fpg != null)
            {
                Debug.Log("LizardSkin: FOUND FancyPlayerGraphics");
                FancyPlayerGraphicsCosmeticsAdaptor.ApplyHooksToFancyPlayerGraphics();
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND FancyPlayerGraphics");
            }

            if (jolly_ref != null)
            {
                Debug.Log("LizardSkin: FOUND Jolly");
                PlayerGraphicsCosmeticsAdaptor.ApplyHooksToJollyPlayerGraphicsHK();
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND Jolly");
            }

            if (custail_ref != null)
            {
                Debug.Log("LizardSkin: FOUND CustomTail");
                // No hookies :)
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND CustomTail");
            }

            if (colorfoot_ref != null)
            {
                Debug.Log("LizardSkin: FOUND Colorfoot");
                PlayerGraphicsCosmeticsAdaptor.ApplyHooksToColorfootPlayerGraphicsPatch();
            }
            else
            {
                Debug.Log("LizardSkin: NOT FOUND Colorfoot");
            }
        }
    }
}
