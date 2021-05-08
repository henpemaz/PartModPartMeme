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

            PlacedObjectsManager.Apply();
        }
    }
}
