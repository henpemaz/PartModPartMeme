using DevInterface;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using static ManagedPlacedObjects.PlacedObjectsManager;

namespace ManagedPlacedObjects
{
    public static class Examples
    {
        internal static void PlacedObjectsExample()
        {
            // Registers a type with a loooooot of fields
            List<ManagedField> fields = new List<ManagedField>
            {
                new FloatField("f1", 0f, 1f, 0.2f, 0.01f, ManagedFieldWithPanel.ControlType.slider, "Float Slider"),
                new FloatField("f2", 0f, 1f, 0.5f, 0.1f, ManagedFieldWithPanel.ControlType.button, "Float Button"),
                new FloatField("f3", 0f, 1f, 0.8f, 0.1f, ManagedFieldWithPanel.ControlType.arrows, "Float Arrows"),
                new FloatField("f4", 0f, 1f, 0.8f, 0.1f, ManagedFieldWithPanel.ControlType.text, "Float Text"),

                new BooleanField("b1", false, ManagedFieldWithPanel.ControlType.slider, "Bool Slider"),
                new BooleanField("b2", true, ManagedFieldWithPanel.ControlType.button, "Bool Button"),
                new BooleanField("b3", false, ManagedFieldWithPanel.ControlType.arrows, "Bool Arrows"),
                new BooleanField("b4", true, ManagedFieldWithPanel.ControlType.text, "Bool Text"),

                new EnumField("e1", typeof(PlacedObject.Type), PlacedObject.Type.None, new System.Enum[] { PlacedObject.Type.BlueToken, PlacedObject.Type.GoldToken }, ManagedFieldWithPanel.ControlType.slider, "Enum Slider"),
                new EnumField("e2", typeof(PlacedObject.Type), PlacedObject.Type.Mushroom, null, ManagedFieldWithPanel.ControlType.button, "Enum Button"),
                new EnumField("e3", typeof(PlacedObject.Type), PlacedObject.Type.SuperStructureFuses, null, ManagedFieldWithPanel.ControlType.arrows, "Enum Arrows"),
                new EnumField("e4", typeof(PlacedObject.Type), PlacedObject.Type.GhostSpot, null, ManagedFieldWithPanel.ControlType.text, "Enum Text"),

                new IntegerField("i1", 0, 10, 1, ManagedFieldWithPanel.ControlType.slider, "Integer Slider"),
                new IntegerField("i2", 0, 10, 2, ManagedFieldWithPanel.ControlType.button, "Integer Button"),
                new IntegerField("i3", 0, 10, 3, ManagedFieldWithPanel.ControlType.arrows, "Integer Arrows"),
                new IntegerField("i4", 0, 10, 3, ManagedFieldWithPanel.ControlType.text, "Integer Text"),

                new StringField("str1", "your text here", "String"),

                new Vector2Field("vf1", Vector2.one, Vector2Field.VectorReprType.line),
                new Vector2Field("vf2", Vector2.one, Vector2Field.VectorReprType.circle),
                new Vector2Field("vf3", Vector2.one, Vector2Field.VectorReprType.rect),

                new IntVector2Field("ivf1", new RWCustom.IntVector2(1, 1), IntVector2Field.IntVectorReprType.line),
                new IntVector2Field("ivf2", new RWCustom.IntVector2(1, 1), IntVector2Field.IntVectorReprType.tile),
                new IntVector2Field("ivf3", new RWCustom.IntVector2(1, 1), IntVector2Field.IntVectorReprType.fourdir),
                new IntVector2Field("ivf4", new RWCustom.IntVector2(1, 1), IntVector2Field.IntVectorReprType.eightdir),
                new IntVector2Field("ivf5", new RWCustom.IntVector2(1, 1), IntVector2Field.IntVectorReprType.rect)
            };

            // Data serialization and UI are taken care of by the manageddata and managedrepresentation types
            // And that's about it, now sillyobject will receive a placedobject with manageddata and that data will have all these fields
            RegisterFullyManagedObjectType(fields.ToArray(), typeof(SillyObject));
            // Trust me I just spared you from writing some 300 lines of code with this.

            // A type with no object, no data, no repr, just for marking places
            ManagedObjectType curiousObjectLocation = new ManagedObjectType("CuriousObjectLocation", null, null, null);
            RegisterManagedObject(curiousObjectLocation);
            // Could also be done with RegisterEmptyObjectType("CuriousObjectLocation", null, null);

            // Registers my self implemented Manager
            // It handles spawning its object, data and representation 
            RegisterManagedObject(new CuriousObjectType());
            // Could also be achieved with RegisterManagedObject(new ManagedObjectType("CuriousObject", typeof(CuriousObjectType.CuriousObject), typeof(CuriousObjectType.CuriousData), typeof(CuriousObjectType.CuriousRepresentation)));
            // but at the expense of some extra reflection calls
        }

        // Juuuuust an object, yet, we can place it. Data and UI are generated automatically
        internal class SillyObject : UpdatableAndDeletable
        {
            private readonly PlacedObject placedObject;

            public SillyObject(PlacedObject pObj, Room room)
            {
                this.room = room;
                this.placedObject = pObj;
                UnityEngine.Debug.Log("SillyObject create");
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                if (room.game.clock % 100 == 0)
                    Debug.Log("SillyObject vf1.x is " + (placedObject.data as ManagedData).GetValue<Vector2>("vf1").x);
            }
        }

        // Some other objects, this time we're registering type, object, data and representation
        public static class EnumExt_ManagedPlacedObjects
        {
            public static PlacedObject.Type CuriousObject;
            public static PlacedObject.Type CuriousObjectLocation;
        }

        // An empty placedobject for making places in a room
        // Oooops we don't even have to extend it for that, just call MangedObjectType with your type
        //internal class CuriousObjectLocation : ManagedObjectType
        //{
        //    public CuriousObjectLocation() : base(EnumExt_ManagedPlacedObjects.CuriousObjectLocation, null, null, null)
        //    {
        //    }
        //}

        // A very curious object, part managed part manual
        // Overriding the base class here was optional, it's just that flexible lol
        internal class CuriousObjectType : ManagedObjectType
        {
            // Ignore the stuff in the baseclass and write your own if you want
            public CuriousObjectType() : base("CuriousObject", null, null, null) // this could have been (PlacedObjects.CuriousObject, typeof(CuriousObject), typeof(...)...)
            {
            }

            // Override at your own risk ? the default behaviour works just fine, but maybe you know what you're doign
            protected override PlacedObject.Type GetObjectType()
            {
                return EnumExt_ManagedPlacedObjects.CuriousObject;
            }

            public override PlacedObject.Data MakeEmptyData(PlacedObject pObj)
            {
                return new CuriousData(pObj);
            }

            public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
            {
                return new CuriousObject(placedObject, room);
            }

            public override PlacedObjectRepresentation MakeRepresentation(PlacedObject pObj, ObjectsPage objPage)
            {
                return new CuriousRepresentation(GetObjectType(), objPage, pObj);
            }

            // Our curious and useful object
            class CuriousObject : UpdatableAndDeletable, IDrawable
            {
                private readonly PlacedObject placedObject;
                private readonly List<PlacedObject> otherPlaces;

                public CuriousObject(PlacedObject placedObject, Room room)
                {
                    this.placedObject = placedObject;
                    this.room = room;
                    otherPlaces = new List<PlacedObject>();

                    // Finds aditional info from other objects
                    foreach (PlacedObject pobj in room.roomSettings.placedObjects)
                    {
                        if (pobj.type == EnumExt_ManagedPlacedObjects.CuriousObjectLocation && pobj.active)
                            otherPlaces.Add(pobj);
                    }

                    UnityEngine.Debug.Log("CuriousObject started and found " + otherPlaces.Count + " location");
                }
                 // IDrawable stuff
                public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
                {
                    if (newContatiner == null) newContatiner = rCam.ReturnFContainer("Midground");
                    for (int i = 0; i < sLeaser.sprites.Length; i++)
                    {
                        newContatiner.AddChild(sLeaser.sprites[i]);
                    }
                }

                public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
                {
                    for (int i = 0; i < sLeaser.sprites.Length; i++)
                    {
                        sLeaser.sprites[i].color = PlayerGraphics.SlugcatColor(UnityEngine.Random.Range(0, 4));
                    }
                }

                public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
                {
                    for (int i = 0; i < sLeaser.sprites.Length; i++)
                    {
                        sLeaser.sprites[i].SetPosition(otherPlaces[i].pos - camPos);
                        sLeaser.sprites[i].scale = (this.placedObject.data as CuriousData).GetValue<float>("scale");
                        sLeaser.sprites[i].rotation = (this.placedObject.data as CuriousData).rotation;
                    }
                }

                public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
                {
                    sLeaser.sprites = new FSprite[otherPlaces.Count];
                    for (int i = 0; i < sLeaser.sprites.Length; i++)
                    {
                        sLeaser.sprites[i] = new FSprite("HeadA0");
                    }
                    this.AddToContainer(sLeaser, rCam, null);
                }
            }

            // The data for our curious object
            // We declare a managed field called "scale", and we create another one called rotation because why not
            class CuriousData : ManagedData
            {
                public float rotation;

                public CuriousData(PlacedObject owner) : base(owner, new ManagedField[] { new FloatField("scale", 0.1f, 10f, 1f, displayName:"Scale") })
                {
                    this.rotation = UnityEngine.Random.value * 360f;
                }

                // Serialization has to include our manual field
                public override string ToString()
                {
                    //Debug.Log("CuriousData serializing as " + base.ToString() + "~" + rotation);
                    return base.ToString() + "~" + rotation;
                }

                public override void FromString(string s)
                {
                    //Debug.Log("CuriousData deserializing from "+ s);
                    base.FromString(s);
                    string[] arr = Regex.Split(s, "~");
                    rotation = float.Parse(arr[fields.Length + (needsControlPanel ? 2 : 0) + 0]); // a bit iffy
                    //Debug.Log("CuriousData got rotation = " + rotation);
                }
            }

            // Representation... ManagedData takes care of creating controls for managed fields
            // but we have one unmanaged field to control
            class CuriousRepresentation : ManagedRepresentation
            {
                public CuriousRepresentation(PlacedObject.Type placedType, ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
                {

                }

                public override void Update()
                {
                    base.Update();
                    if (UnityEngine.Input.GetKey("b")) return;
                    (pObj.data as CuriousData).rotation = RWCustom.Custom.VecToDeg(this.owner.mousePos - absPos);
                }
            }
        }

        // And that was it, 3 somewhat functional objects in some... 220 lines of code.
    }
}