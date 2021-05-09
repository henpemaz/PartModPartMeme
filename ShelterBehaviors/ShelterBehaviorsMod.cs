using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using Partiality.Modloader;
using ManagedPlacedObjects;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShelterBehaviors
{
    public class ShelterBehaviorsMod : PartialityMod
    {
        public ShelterBehaviorsMod()

        {
            this.ModID = "ShelterBehaviorsMod";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;

        }

        public static ShelterBehaviorsMod instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            //PlacedObjectsManager.ApplyHooks();

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]{
                new PlacedObjectsManager.BooleanField("nvd", true, displayName:"No Vanilla Door"),
                new PlacedObjectsManager.BooleanField("htt", false, displayName:"Hold To Trigger"),
                new PlacedObjectsManager.IntegerField("htts", 1, 10, 4, displayName:"HTT Trigger Speed"),
                new PlacedObjectsManager.BooleanField("cs", false, displayName:"Consumable Shelter"),
                new PlacedObjectsManager.IntegerField("csmin", -1, 30, 3, displayName:"Consum. Cooldown Min"),
                new PlacedObjectsManager.IntegerField("csmax", 0, 30, 6, displayName:"Consum. Cooldown Max"),
                new PlacedObjectsManager.IntegerField("ftt", 0, 400, 20, PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Trigger"),
                new PlacedObjectsManager.IntegerField("fts", 0, 400, 40, PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Sleep"),
                new PlacedObjectsManager.IntegerField("ftsv", 0, 400, 60, PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Starvation"),
                new PlacedObjectsManager.IntegerField("ftw", 0, 400, 120, PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider, displayName:"Frames to Win"),
                }, typeof(ShelterBehaviorManager), "ShelterBhvrManager");

            

            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]{
                //new PlacedObjectsManager.BooleanField("httt", false, displayName: "HTT Tutorial"),
                new PlacedObjectsManager.IntegerField("htttcd", -1, 12, 6, displayName: "HTT Tut. Cooldown"), }
                , typeof(ShelterBehaviorManager.HoldToTriggerTutorialObject), "ShelterBhvrHTTTutorial");

            //PlacedObjectsManager.RegisterEmptyObjectType("ShelterBhvrPlacedDoor", typeof()) TODO directional data and rep;
            PlacedObjectsManager.RegisterFullyManagedObjectType(new PlacedObjectsManager.ManagedField[]{
                new PlacedObjectsManager.IntVector2Field("dir", new RWCustom.IntVector2(0,1), PlacedObjectsManager.IntVector2Field.IntVectorReprType.fourdir), }
            , null, "ShelterBhvrPlacedDoor");

            PlacedObjectsManager.RegisterEmptyObjectType("ShelterBhvrTriggerZone", typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
            PlacedObjectsManager.RegisterEmptyObjectType("ShelterBhvrNoTriggerZone", typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
            PlacedObjectsManager.RegisterEmptyObjectType("ShelterBhvrSpawnPosition", null, null); // No data required :)
        }

        public static class EnumExt_ShelterBehaviorsMod
        {
            public static PlacedObject.Type ShelterBhvrManager;
            public static PlacedObject.Type ShelterBhvrPlacedDoor;
            public static PlacedObject.Type ShelterBhvrTriggerZone;
            public static PlacedObject.Type ShelterBhvrNoTriggerZone;
            public static PlacedObject.Type ShelterBhvrHTTTutorial;
            public static PlacedObject.Type ShelterBhvrSpawnPosition;
        }
    }
}
