using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ManagedPlacedObjects;
using System.Security;
using System.Security.Permissions;
using System.Reflection;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace ElectricArcsMod
{
    public class ElectricArcsMod : PartialityMod
    {
        public ElectricArcsMod()
        {
            this.ModID = "ElectricArcsMod";
            this.Version = "1.0";
            this.author = "Henpemaz";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            ElectricArcs.Register();
        }
    }
}
