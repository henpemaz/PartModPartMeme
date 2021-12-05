using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using System.Security;
using System.Security.Permissions;
using Partiality.Modloader;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace TestMod
{
    public class TestMod : PartialityMod
    {
        public TestMod()
        {
            this.ModID = "TestMod";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public static TestMod instance;

        public override void OnEnable()
        {
            base.OnEnable();
            // Hooking code goose hre

            managedfruitpom.PlacedObjectsManager.RegisterManagedObject(new ManagedFruit());
        }

        public class ManagedFruit : managedfruitpom.PlacedObjectsManager.ManagedObjectType
        {
            public ManagedFruit() : base("froot", null, typeof(PlacedObject.ConsumableObjectData), typeof(DevInterface.ConsumableRepresentation)){}

            public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
            {
                int m = room.roomSettings.placedObjects.IndexOf(placedObject);
                if (!(room.game.session is StoryGameSession) || !(room.game.session as StoryGameSession).saveState.ItemConsumed(room.world, false, room.abstractRoom.index, m))
                {
                    AbstractPhysicalObject abstractPhysicalObject = new AbstractConsumable(room.world, AbstractPhysicalObject.AbstractObjectType.DangleFruit, null, room.GetWorldCoordinate(room.roomSettings.placedObjects[m].pos), room.game.GetNewID(), room.abstractRoom.index, m, room.roomSettings.placedObjects[m].data as PlacedObject.ConsumableObjectData);
                    (abstractPhysicalObject as AbstractConsumable).isConsumed = false;
                    room.abstractRoom.entities.Add(abstractPhysicalObject);
                }
                return null;
            }
        }
    }
}
