using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ManagedPlacedObjects
{
    /// <summary>
    /// YOU DO NOT NEED TO BUILD OR SHIP THIS MOD
    /// It's available so that you can try the examples
    /// Copy PlacedObjectManager to your project in order to use it. RENAME its namespace to avoid a nasty Monomod bug
    /// Alternatively, you can comment out the "Examples.PlacedObjectsExample();" line and build and use this mod as a dependency, but that's unadvised.
    /// </summary>
    public class ManagedPlacedObjectsMod : PartialityMod
    {
        public ManagedPlacedObjectsMod()
        {
            this.ModID = "ManagedPlacedObjectsMod";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static ManagedPlacedObjectsMod instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            //PlacedObjectsManager.Apply(); // no longer necessary, auto applied when anything uses it
            //Examples.PlacedObjectsExample();
        }
    }
}
