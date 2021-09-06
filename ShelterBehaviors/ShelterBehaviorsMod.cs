using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using Partiality.Modloader;
using ShelterBehaviors.POM;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ShelterBehaviors
{
    /// <summary>
    /// Main partmod class.
    /// </summary>
    public class ShelterBehaviorsMod : PartialityMod
    {

        public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/5/2";
        public int version = 2;
        public string keyE = "AQAB";
        public string keyN = "uwptqosDNjimqNbRwCtJIKBXFsvYZN+b7yl668ggY46j+2Zlm/+L9TpypF6Bhu85CKnkY7ffFCQixTSzumdXrz1WVD0PTvoKDAp33U/loKHoAe/rs3HwdaOAdpug//rIGDmtwx56DC05NiLYKVRf4pS3yM1xN39Rr2at/RmAxdamKLUnoJtHRwx2eGsoKq5dmPZ7BKTmF/49N6eFUvUXEF9evPRfAdPH9bYAMNx0QS3G6SYC0IQj5zWm4FnY1C57lmvZxQgqEZDCVgadphJAjsdVAk+ZruD0O8X/dqXiIBSdEjZsvs4VDsjEF8ekHoon2UZnMEd6XocIK4CBqJ9HCMGaGZusnwhtVsGyMur1Go4w0CXDH3L5mKhcEm/V7Ik2RV5/Z2Kz8555fO7/9UiDC9vh5kgk2Mc04iJa9rcWSMfrwzrnvzHZzKnMxpmc4XoSqiExVEVJszNMKqgPiQGprkfqCgyK4+vbeBSXx3Ftalncv9acU95qxrnbrTqnyPWAYw3BKxtsY4fYrXjsR98VclsZUFuB/COPTI/afbecDHy2SmxI05ZlKIIFE/+yKJrY0T/5cT/d8JEzHvTNLOtPvC5Ls1nFsBqWwKcLHQa9xSYSrWk8aetdkWrVy6LQOq5dTSD4/53Tu0ZFIvlmPpBXrgX8KJN5LqNMmml5ab/W7wE=";

        public ShelterBehaviorsMod()

        {
            this.ModID = "ShelterBehaviorsMod";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;

        }
        public static ShelterBehaviorsMod instance;

        /// <summary>
        /// Makes creatures <see cref="ShelterBehaviorManager.CycleSpawnPosition"/> on <see cref="AbstractCreature.RealizeInRoom"/>
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="instance"></param>
        public static void CreatureShuffleHook (On.AbstractCreature.orig_RealizeInRoom orig, AbstractCreature instance)
        {
            var mngr = instance.Room.realizedRoom?.updateList?.FirstOrDefault(x => x is ShelterBehaviorManager) as ShelterBehaviorManager;
            mngr?.CycleSpawnPosition();
            orig(instance);

        }
        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre
            On.AbstractCreature.RealizeInRoom += CreatureShuffleHook;

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
                new PlacedObjectsManager.IntegerField("ini", 0, 400, 120, PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider, displayName:"Initial wait"),
                new PlacedObjectsManager.IntegerField("ouf", 0, 400, 120, PlacedObjectsManager.ManagedFieldWithPanel.ControlType.slider, displayName:"Open up anim"),
                new PlacedObjectsManager.BooleanField("ani", false, displayName:"Animate Water"),

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
