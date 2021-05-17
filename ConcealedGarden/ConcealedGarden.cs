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

namespace ConcealedGarden
{
    public partial class ConcealedGarden : PartialityMod
    {
        public ConcealedGarden()
        {
            this.ModID = "ConcealedGarden";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static ConcealedGarden instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            ElectricArcs.Register();

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.FloatField("rmin", 0, 1, 0.1f, 0.001f),
                new PlacedObjectsManager.FloatField("rmax", 0, 1, 0.3f, 0.001f),
                new PlacedObjectsManager.FloatField("gmin", 0, 1, 0.05f, 0.001f),
                new PlacedObjectsManager.FloatField("gmax", 0, 1, 0.2f, 0.001f),
                new PlacedObjectsManager.FloatField("bmin", 0, 1, 0.5f, 0.001f),
                new PlacedObjectsManager.FloatField("bmax", 0, 1, 0.25f, 0.001f),
                new PlacedObjectsManager.FloatField("stiff", 0, 1, 0.5f, 0.01f),
                new PlacedObjectsManager.IntegerField("ftc", 0, 400, 120, PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider),
            }, typeof(OrganicShelter.OrganicShelterCoordinator), "OrganicShelterCoordinator");

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.Vector2Field("size", new UnityEngine.Vector2(40,40), PlacedObjectsManager.Vector2Field.VectorReprType.circle),
                new PlacedObjectsManager.Vector2Field("dest", new UnityEngine.Vector2(0,50), PlacedObjectsManager.Vector2Field.VectorReprType.line),
                new PlacedObjectsManager.FloatField("stiff", 0, 1, 0.5f, 0.01f),
            }, null, "OrganicLockPart");

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.Vector2Field("size", new UnityEngine.Vector2(-100,100), PlacedObjectsManager.Vector2Field.VectorReprType.circle),
                new PlacedObjectsManager.FloatField("sizemin", 1, 200, 12f, 1f),
                new PlacedObjectsManager.FloatField("sizemax", 1, 200, 20f, 1f),
                new PlacedObjectsManager.FloatField("depth", -100, 100, 4f, 1f),
                new PlacedObjectsManager.FloatField("density", 0, 5, 0.5f, 0.01f),
                new PlacedObjectsManager.FloatField("stiff", 0, 1, 0.5f, 0.01f),
                new PlacedObjectsManager.FloatField("spread", 0, 20f, 2f, 0.1f),
                new PlacedObjectsManager.IntegerField("seed", 0, 9999, 0),
            }, null, "OrganicLining");

            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("LifeSimProjectionSegment",
                typeof(LifeSimProjection), typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation), singleInstance: true));
            //PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("LifeSimProjectionPulser",
            //    typeof(LifeSimProjection), typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation), singleInstance: true));
            //PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("LifeSimProjectionKiller",
            //    typeof(LifeSimProjection), typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation), singleInstance: true));

            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("CosmeticLeaves",
                typeof(CosmeticLeaves), typeof(CosmeticLeaves.CosmeticLeavesObjectData), typeof(PlacedObjectsManager.ManagedRepresentation)));

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.BooleanField("noleft", false, displayName:"No Left Door"),
                new PlacedObjectsManager.BooleanField("noright", false, displayName:"No Right Door"),
                new PlacedObjectsManager.BooleanField("nowater", false, displayName:"No Water, stoopid"),
            }, typeof(CGGateFix), "CGGateFix");


            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == "NudeMod")
                {
                    QuestionableLizardBit.Apply();
                }
            }

            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("SlipperySlope",
                typeof(SlipperySlope), typeof(SlipperySlope.SlipperySlopeData), typeof(PlacedObjectsManager.ManagedRepresentation)));
        }
    }
}
