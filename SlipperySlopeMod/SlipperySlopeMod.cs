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

namespace SlipperySlopeMod
{
    public partial class SlipperySlopeMod : PartialityMod
    {
        public SlipperySlopeMod()
        {
            this.ModID = "SlipperySlopeMod";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static SlipperySlopeMod instance;

        public override void OnEnable()
        {
            base.OnEnable();

            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("SlipperySlope",
                typeof(SlipperySlope), typeof(SlipperySlope.SlipperySlopeData), typeof(PlacedObjectsManager.ManagedRepresentation)));
        }
    }
}
