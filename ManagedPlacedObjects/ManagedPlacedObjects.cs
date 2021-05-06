﻿using DevInterface;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;

namespace ManagedPlacedObjects
{
    public static class ManagedPlacedObjects
    {
        public static void ApplyHooks()
        {
            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData_Patch;
            On.Room.Loaded += Room_Loaded_Patch;
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep_Patch;

            MySillyExample();
        }

        #region HOOKS
        private static void ObjectsPage_CreateObjRep_Patch(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
        {
            orig(self, tp, pObj);
            ManagedObjectType manager = GetManagerForType(tp);
            if (manager == null) return;

            if (pObj == null) pObj = self.RoomSettings.placedObjects[self.RoomSettings.placedObjects.Count - 1];
            DevInterface.PlacedObjectRepresentation placedObjectRepresentation = manager.MakeRepresentation(pObj, self);
            if (placedObjectRepresentation == null) return;

            DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)self.tempNodes.Pop();
            self.subNodes.Pop();
            old.ClearSprites();
            self.tempNodes.Add(placedObjectRepresentation);
            self.subNodes.Add(placedObjectRepresentation);
        }

        private static void Room_Loaded_Patch(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            ManagedObjectType manager;
            for (int i = 0; i < self.roomSettings.placedObjects.Count; i++)
            {
                if (self.roomSettings.placedObjects[i].active)
                {
                    if ((manager = GetManagerForType(self.roomSettings.placedObjects[i].type)) != null)
                    {
                        UpdatableAndDeletable obj = manager.MakeObject(self.roomSettings.placedObjects[i], self);
                        if (obj == null) continue;
                        self.AddObject(obj);
                    }
                }
            }
        }

        private static void PlacedObject_GenerateEmptyData_Patch(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
        {
            orig(self);
            ManagedObjectType manager = GetManagerForType(self.type);
            if (manager != null)
            {
                self.data = manager.MakeEmptyData(self);
            }
        }
        #endregion HOOKS

        #region EXAMPLE

        internal static void MySillyExample()
        {
            List<ManagedField> fields = new List<ManagedField>();

            fields.Add(new FloatField("f1", 0f, 1f, 0.2f, 0.01f, ManagedFieldWithPanel.ControlType.slider, "Float Slider"));
            fields.Add(new FloatField("f2", 0f, 1f, 0.5f, 0.1f, ManagedFieldWithPanel.ControlType.button, "Float Button"));
            fields.Add(new FloatField("f3", 0f, 1f, 0.8f, 0.1f, ManagedFieldWithPanel.ControlType.arrows, "Float Arrows"));

            fields.Add(new BooleanField("b1", false, ManagedFieldWithPanel.ControlType.slider, "Bool Slider"));
            fields.Add(new BooleanField("b2", true, ManagedFieldWithPanel.ControlType.button, "Bool Button"));
            fields.Add(new BooleanField("b3", false, ManagedFieldWithPanel.ControlType.arrows, "Bool Arrows"));

            fields.Add(new EnumField("e1", typeof(PlacedObject.Type), PlacedObject.Type.None, null, ManagedFieldWithPanel.ControlType.slider, "Enum Slider"));
            fields.Add(new EnumField("e2", typeof(PlacedObject.Type), PlacedObject.Type.Mushroom, null, ManagedFieldWithPanel.ControlType.button, "Enum Button"));
            fields.Add(new EnumField("e3", typeof(PlacedObject.Type), PlacedObject.Type.SuperStructureFuses, null, ManagedFieldWithPanel.ControlType.arrows, "Enum Arrows"));

            fields.Add(new IntegerField("i1", 0, 10, 1, ManagedFieldWithPanel.ControlType.slider, "Integer Slider"));
            fields.Add(new IntegerField("i2", 0, 10, 2, ManagedFieldWithPanel.ControlType.button, "Integer Button"));
            fields.Add(new IntegerField("i3", 0, 10, 3, ManagedFieldWithPanel.ControlType.arrows, "Integer Arrows"));

            fields.Add(new Vector2Field("vf1", Vector2.one, Vector2Field.VectorReprType.line));
            fields.Add(new Vector2Field("vf2", Vector2.one, Vector2Field.VectorReprType.circle));
            fields.Add(new Vector2Field("vf3", Vector2.one, Vector2Field.VectorReprType.rect));

            MakeFullyManagedObjectType(fields.ToArray(), typeof(SillyObject));

            RegisterManagedObject(new CuriousObjectLocation());
            RegisterManagedObject(new CuriousObjectType());
        }


        // Juuuuust an object, yet, we can place it. Data and UI are generated automatically
        internal class SillyObject : UpdatableAndDeletable
        {
            private PlacedObject placedObject;

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
        internal class CuriousObjectLocation : ManagedObjectType
        {
            public override PlacedObject.Type GetObjectType()
            {
                return EnumExt_ManagedPlacedObjects.CuriousObjectLocation;
            }

            public override PlacedObject.Data MakeEmptyData(PlacedObject pObj)
            {
                return null;
            }

            public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
            {
                return null;
            }

            public override PlacedObjectRepresentation MakeRepresentation(PlacedObject pObj, ObjectsPage objPage)
            {
                return null;
            }
        }

        // A very curious object, part managed part manual
        internal class CuriousObjectType : ManagedObjectType
        {
            public override PlacedObject.Type GetObjectType()
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
                private PlacedObject placedObject;

                private List<PlacedObject> otherPlaces;

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

        // And that was it, 3 somewhat functional objects in some... 211 lines of code.
        #endregion EXAMPLE

        #region MANAGED
        // How objects are registered and stored
        private static List<ManagedObjectType> managedObjectTypes = new List<ManagedObjectType>();

        /// <summary>
        /// Register a managed type to handle room and devtool hooks
        /// </summary>
        /// <param name="obj"></param>
        public static void RegisterManagedObject(ManagedObjectType obj)
        {
            managedObjectTypes.Add(obj);
        }

        private static ManagedObjectType GetManagerForType(PlacedObject.Type tp)
        {
            foreach (var objtype in managedObjectTypes)
            {
                if (objtype.GetObjectType() == tp) return objtype;
            }
            return null;
        }

        /// <summary>
        /// Wraps an UpdateableAndDeletable into a managed type with managed data and UI
        /// Must have a constructor that takes (Room, PlacedObject) or (PlacedObject, Room)
        /// </summary>
        /// <param name="managedFields"></param>
        /// <param name="type"></param>
        public static void MakeFullyManagedObjectType(ManagedField[] managedFields, Type type)
        {
            PastebinMachine.EnumExtender.EnumExtender.AddDeclaration(typeof(PlacedObject.Type), type.Name);
            ManagedObjectType fullyManaged = new FullyManagedObjectType(type, managedFields);
            RegisterManagedObject(fullyManaged);
        }

        /// <summary>
        /// Main class for managed object types
        /// Make-calls CAN return null, causing the object to not be created, or have no data, or use the default handle representation
        /// This class can be used without the managed fields, data and representations to simply handle the room/devtools hooks
        /// Call <see cref="RegisterManagedObject(ManagedObjectType)"/> to register your manager
        /// </summary>
        public abstract class ManagedObjectType // This could be an interface I think
        {
            public abstract PlacedObject.Type GetObjectType();

            public abstract UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room);

            public abstract PlacedObjectRepresentation MakeRepresentation(PlacedObject pObj, ObjectsPage objPage);

            public abstract PlacedObject.Data MakeEmptyData(PlacedObject pObj);
        }


        /// <summary>
        /// Class for managing a wraped UpdateableAndDeletable object
        /// Uses the fully managed data and representation types
        /// </summary>
        public class FullyManagedObjectType : ManagedObjectType
        {
            private PlacedObject.Type placedType;
            private readonly Type objectType;
            private readonly ManagedField[] managedFields;

            public FullyManagedObjectType(Type objectType, ManagedField[] managedFields)
            {
                this.objectType = objectType;
                this.managedFields = managedFields;
                placedType = default;
            }

            public override PlacedObject.Type GetObjectType()
            {
                // unable to access placedType at startup time because enumextend runs later on ???
                if (placedType == default) placedType = (PlacedObject.Type)Enum.Parse(typeof(PlacedObject.Type), objectType.Name);
                return placedType;
            }

            public override UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
            {
                System.Reflection.ConstructorInfo info = objectType.GetConstructor(System.Reflection.BindingFlags.Default, null, new Type[] { typeof(PlacedObject), typeof(Room) }, null);
                if(info == null) info = objectType.GetConstructor(System.Reflection.BindingFlags.Default, null, new Type[] { typeof(Room), typeof(PlacedObject) }, null);
                return (UpdatableAndDeletable)info.Invoke(new object[] {placedObject, room });
            }

            public override PlacedObject.Data MakeEmptyData(PlacedObject pObj)
            {
                return new ManagedData(pObj, managedFields);
            }

            public override PlacedObjectRepresentation MakeRepresentation(PlacedObject pObj, ObjectsPage objPage)
            {
                return new ManagedRepresentation(GetObjectType(), objPage, pObj);
            }
        }


        /// <summary>
        /// A field that can handle serialization and generate UI
        /// This field is merely a recipe, the actual data is stored in the data object for each pObj
        /// </summary>
        public abstract class ManagedField
        {
            public string key;
            public object defaultValue;

            public ManagedField(string key, object defaultValue)
            {
                this.key = key;
                this.defaultValue = defaultValue;
            }

            public virtual string ToString(object value) => value.ToString();
            public abstract object FromString(string str);

            public virtual bool NeedsControlPanel { get => this is ManagedFieldWithPanel; }

            public virtual DevUINode MakeAditionalNodes(ManagedData managedData, ManagedRepresentation managedRepresentation)
            {
                return null;
            }
        }

        /// <summary>
        /// A field that generates a control-panel control
        /// </summary>
        public abstract class ManagedFieldWithPanel : ManagedField
        {
            private readonly ControlType control;
            public readonly string displayName;

            public enum ControlType
            {
                none,
                slider,
                arrows,
                button
            }

            protected ManagedFieldWithPanel(string key, object defaultValue, ControlType control = ControlType.none, string displayName = null) : base(key, defaultValue)
            {
                this.control = control;
                this.displayName = displayName ?? key;
            }

            public virtual Vector2 PanelUiSizeMinusName { get => SizeOfPanelNode() + new Vector2(SizeOfLargestDisplayValue(), 0f); }

            /// <summary>
            /// Approx size of the UI minus displayname and valuedisplay width
            /// </summary>
            /// <returns></returns>
            public virtual Vector2 SizeOfPanelNode()
            {
                switch (control)
                {
                    case ControlType.slider:
                        return new Vector2(118f, 20f);

                    case ControlType.arrows:
                        return new Vector2(54f, 20f);

                    case ControlType.button:
                        return new Vector2(14f, 20f);

                    default:
                        break;
                }
                return new Vector2(0f, 20f);
            }

            public virtual float SizeOfDisplayname()
            {
                return HUD.DialogBox.meanCharWidth * (displayName.Length + 2);
            }

            public abstract float SizeOfLargestDisplayValue();


            public virtual PositionedDevUINode MakeControlPanelNode(ManagedData managedData, ManagedControlPanel panel, float sizeOfDisplayname)
            {
                switch (control)
                {
                    case ControlType.slider:
                        return new ManagedSlider(this, managedData, panel, sizeOfDisplayname);
                    case ControlType.arrows:
                        return new ManagedArrowSelector(this, managedData, panel, sizeOfDisplayname);
                    case ControlType.button:
                        return new ManagedButton(this, managedData, panel, sizeOfDisplayname);
                }
                return null;
            }

            public virtual string DisplayValueForNode(PositionedDevUINode node, ManagedData data)
            {
                // field tostring
                return ToString(data.GetValue<object>(key));
            }
        }

        /// <summary>
        /// Managed data type, handles managed fields and the coordinates the generation of the panel UI
        /// </summary>
        public class ManagedData : PlacedObject.Data
        {
            public ManagedData(PlacedObject owner, ManagedField[] fields) : base(owner)
            {
                this.fields = fields;
                this.fieldsByKey = new Dictionary<string, ManagedField>();
                this.valuesByKey = new Dictionary<string, object>();

                panelPos = new Vector2(100, 50);

                this.needsControlPanel = false;
                foreach (var field in fields)
                {
                    if (fieldsByKey.ContainsKey(field.key)) throw new FormatException("fields with duplicated names are not a good idea sir");
                    fieldsByKey[field.key] = field;
                    valuesByKey[field.key] = field.defaultValue;
                    if (field.NeedsControlPanel) this.needsControlPanel = true;
                }
            }

            protected readonly ManagedField[] fields;
            protected readonly Dictionary<string, ManagedField> fieldsByKey;
            protected readonly Dictionary<string, object> valuesByKey;
            public readonly bool needsControlPanel;
            public Vector2 panelPos;

            public T GetValue<T>(string fieldName)
            {
                return (T)valuesByKey[fieldName];
            }

            public void SetValue<T>(string fieldName, T value)
            {
                valuesByKey[fieldName] = (object) value;
            }

            public override void FromString(string s)
            {
                string[] array = Regex.Split(s, "~");
                int datastart = 0;
                if (needsControlPanel)
                {
                    this.panelPos = new Vector2(float.Parse(array[0]), float.Parse(array[1]));
                    datastart = 2;
                }

                for (int i = 0; i < fields.Length; i++)
                {
                    valuesByKey[fields[i].key] = fields[i].FromString(array[datastart+i]);
                }
            }

            public override string ToString()
            {
                return (needsControlPanel ? (panelPos.x.ToString() + "~" + panelPos.y.ToString() + "~") : "") + string.Join("~", Array.ConvertAll(fields, f => f.ToString(valuesByKey[f.key])));
            }

            internal virtual void MakeControls(ManagedRepresentation managedRepresentation, ObjectsPage objPage, PlacedObject pObj)
            {
                if (needsControlPanel)
                {
                    ManagedControlPanel panel = new ManagedControlPanel(managedRepresentation.owner, "ManagedControlPanel", managedRepresentation, this.panelPos, Vector2.zero, pObj.type.ToString());
                    managedRepresentation.subNodes.Add(panel);
                    Vector2 uiSize = new Vector2(0f, 0f);
                    Vector2 uiPos = new Vector2(3f, 3f);
                    float largestDisplayname = 0f;
                    for (int i = 0; i < fields.Length; i++) // up down
                    {
                        ManagedFieldWithPanel field = fields[i] as ManagedFieldWithPanel;
                        if (field != null && field.NeedsControlPanel)
                        {
                            largestDisplayname = Mathf.Max(largestDisplayname, field.SizeOfDisplayname());
                        }
                    }

                    for (int i = fields.Length - 1; i >= 0; i--) // down up
                    {
                        ManagedFieldWithPanel field = fields[i] as ManagedFieldWithPanel;
                        if (field != null && field.NeedsControlPanel)
                        {
                            PositionedDevUINode node = field.MakeControlPanelNode(this, panel, largestDisplayname);
                            panel.managedNodes[field.key] = node;
                            panel.subNodes.Add(node);
                            node.pos = uiPos;
                            uiSize.x = Mathf.Max(uiSize.x, field.PanelUiSizeMinusName.x);
                            uiSize.y += field.PanelUiSizeMinusName.y;
                            uiPos.y += field.PanelUiSizeMinusName.y;
                        }
                    }
                    panel.size = uiSize + new Vector2(3 + largestDisplayname, 1);
                }

                for (int i = 0; i < fields.Length; i++)
                {
                    ManagedField field = fields[i];
                    DevUINode node = field.MakeAditionalNodes(this, managedRepresentation);
                    if (node != null)
                    {
                        managedRepresentation.subNodes.Add(node);
                        managedRepresentation.managedNodes[field.key] = node;
                    }
                }
            }
        }


        // These ended up rather empty huh, most of the magic happens on ManagedData
        public class ManagedRepresentation : PlacedObjectRepresentation
        {
            private PlacedObject.Type placedType;
            private ObjectsPage objPage;
            public Dictionary<string, DevUINode> managedNodes;

            public ManagedRepresentation(PlacedObject.Type placedType, ObjectsPage objPage, PlacedObject pObj) : base(objPage.owner, placedType.ToString() + "_Rep", objPage, pObj, placedType.ToString())
            {
                this.placedType = placedType;
                this.objPage = objPage;
                this.pObj = pObj;

                this.managedNodes = new Dictionary<string, DevUINode>();
                (pObj.data as ManagedData).MakeControls(this, objPage, pObj);
            }
        }

        public class ManagedControlPanel : Panel
        {
            public Dictionary<string, DevUINode> managedNodes;
            private readonly int lineSprt;

            public ManagedControlPanel(DevUI owner, string IDstring, ManagedRepresentation parentNode, Vector2 pos, Vector2 size, string title) : base(owner, IDstring, parentNode, pos, size, title)
            {
                managedRepresentation = parentNode;
                managedNodes = new Dictionary<string, DevUINode>();

                this.fSprites.Add(new FSprite("pixel", true));
                owner.placedObjectsContainer.AddChild(this.fSprites[this.lineSprt = this.fSprites.Count - 1]);
                this.fSprites[lineSprt].anchorY = 0f;
            }
            public override void Refresh()
            {
                base.Refresh();
                Vector2 bottomLeft = collapsed ? nonCollapsedAbsPos + new Vector2(0f, size.y) : this.absPos;
                base.MoveSprite(lineSprt, bottomLeft);
                this.fSprites[lineSprt].scaleY = collapsed ? (this.pos + new Vector2(0f, size.y)).magnitude : this.pos.magnitude;
                this.fSprites[lineSprt].rotation = RWCustom.Custom.AimFromOneVectorToAnother(bottomLeft, (parentNode as PositionedDevUINode).absPos);
                (this.managedRepresentation.pObj.data as ManagedData).panelPos = this.pos;
            }

            public ManagedRepresentation managedRepresentation { get; }
        }
        #endregion MANAGED

        #region FIELDS
        // Interfaces for controls
        public interface IInterpolablePanelField // sliders
        {
            float FactorOf(ManagedData data);
            void NewFactor(ManagedData data, float factor);
        }

        public interface IIterablePanelField // buttons, arrows
        {
            void Next(ManagedData data);
            void Prev(ManagedData data);
        }


        public class FloatField : ManagedFieldWithPanel, IInterpolablePanelField, IIterablePanelField
        {
            private readonly float min;
            private readonly float max;
            private readonly float increment;

            public FloatField(string key, float min, float max, float defaultValue, float increment = 0.1f, ControlType control = ControlType.slider, string displayName = null) : base(key, defaultValue, control, displayName)
            {
                this.min = min;
                this.max = max;
                this.increment = increment;
            }

            public override object FromString(string str)
            {
                return float.Parse(str);
            }

            private int NumberOfDecimals()
            {
                // fix decimals from https://stackoverflow.com/a/30205131
                decimal dec = new decimal(increment);
                dec = Math.Abs(dec); //make sure it is positive.
                dec -= (int)dec;     //remove the integer part of the number.
                int decimalPlaces = 0;
                while (dec > 0)
                {
                    decimalPlaces++;
                    dec *= 10;
                    dec -= (int)dec;
                }
                return decimalPlaces;
            }

            public override float SizeOfLargestDisplayValue()
            {
                return HUD.DialogBox.meanCharWidth*(Mathf.FloorToInt(Mathf.Max(Mathf.Abs(min), Mathf.Abs(max))).ToString().Length + 2 + NumberOfDecimals());
            }

            public override string DisplayValueForNode(PositionedDevUINode node, ManagedData data)
            {
                //return base.DisplayValueForNode(node, data);
                // fix too many decimals
                return data.GetValue<float>(key).ToString("N" + NumberOfDecimals());
            }

            public float FactorOf(ManagedData data)
            {
                return ((max - min) == 0) ? 0f : (((float)data.GetValue<float>(key) - min) / (max - min));
            }

            public void NewFactor(ManagedData data, float factor)
            {
                data.SetValue<float>(key, min + factor * (max - min));
            }

            public void Next(ManagedData data)
            {
                float val = data.GetValue<float>(key) + increment;
                if (val > max) val = min;
                data.SetValue<float>(key, val);
            }

            public void Prev(ManagedData data)
            {
                float val = data.GetValue<float>(key) - increment;
                if (val < min) val = max;
                data.SetValue<float>(key, val);
            }
        }


        public class BooleanField : ManagedFieldWithPanel, IIterablePanelField, IInterpolablePanelField
        {
            public BooleanField(string key, bool defaultValue, ControlType control = ControlType.button, string displayName = null) : base(key, defaultValue, control, displayName)
            {
            }

            public override object FromString(string str)
            {
                return bool.Parse(str);
            }

            public override float SizeOfLargestDisplayValue()
            {
                return HUD.DialogBox.meanCharWidth * 5;
            }

            public float FactorOf(ManagedData data)
            {
                return data.GetValue<bool>(key) ? 1f : 0f;
            }

            public void NewFactor(ManagedData data, float factor)
            {
                data.SetValue(key, factor > 0.5f);
            }

            public void Next(ManagedData data)
            {
                data.SetValue(key, !data.GetValue<bool>(key));
            }

            public void Prev(ManagedData data)
            {
                data.SetValue(key, !data.GetValue<bool>(key));
            }
        }

        public class EnumField : ManagedFieldWithPanel, IIterablePanelField, IInterpolablePanelField
        {
            private Enum[] _possibleValues;

            public EnumField(string key, Type type, Enum defaultValue, Enum[] possibleValues = null, ControlType control = ControlType.none, string displayName = null) : base(key, defaultValue, control, displayName)
            {
                this.type = type;
                // Man, System.Array is trash, why are Enums so bad
                this._possibleValues = possibleValues;
            }

            public Type type { get; }
            public Enum[] possibleValues // We defer this listing so stuff like enumextend can do its magic.
            {
                get
                {
                    if (_possibleValues == null) _possibleValues = Enum.GetValues(type).Cast<Enum>().ToArray();
                    return _possibleValues;
                }
            }

            public override object FromString(string str)
            {
                return Enum.Parse(type, str);
            }

            public override float SizeOfLargestDisplayValue()
            {
                int longestEnum = possibleValues.Aggregate<Enum, int>(0, (longest, next) =>
                        next.ToString().Length > longest ? next.ToString().Length : longest);
                return HUD.DialogBox.meanCharWidth * longestEnum;
            }

            public float FactorOf(ManagedData data)
            {
                return (float)Array.IndexOf(possibleValues, data.GetValue<Enum>(key)) / (float)(possibleValues.Length - 1);
            }

            public void NewFactor(ManagedData data, float factor)
            {
                data.SetValue<Enum>(key, possibleValues[Mathf.RoundToInt(factor * (possibleValues.Length - 1))]);
            }

            public void Next(ManagedData data)
            {
                data.SetValue<Enum>(key, possibleValues[(Array.IndexOf(possibleValues, data.GetValue<Enum>(key)) + 1) % possibleValues.Length]);
            }

            public void Prev(ManagedData data)
            {
                data.SetValue<Enum>(key, possibleValues[(Array.IndexOf(possibleValues, data.GetValue<Enum>(key)) - 1 + possibleValues.Length) % possibleValues.Length]);
            }
        }

        public class IntegerField : ManagedFieldWithPanel, IIterablePanelField, IInterpolablePanelField
        {
            private readonly int min;
            private readonly int max;

            public IntegerField(string key, int min, int max, int defaultValue, ControlType control = ControlType.arrows, string displayName = null) : base(key, defaultValue, control, displayName)
            {
                this.min = min;
                this.max = max;
            }

            public override object FromString(string str)
            {
                return int.Parse(str);
            }

            public override float SizeOfLargestDisplayValue()
            {
                return HUD.DialogBox.meanCharWidth * ((Mathf.Max(Mathf.Abs(min), Mathf.Abs(max))).ToString().Length + 2);
            }

            public float FactorOf(ManagedData data)
            {
                return (max - min == 0) ? 0f : (data.GetValue<int>(key) - min) / (float)(max - min);
            }

            public void NewFactor(ManagedData data, float factor)
            {
                data.SetValue<int>(key, Mathf.RoundToInt(min + factor* (max - min)));
            }

            public void Next(ManagedData data)
            {
                int val = data.GetValue<int>(key) + 1;
                if (val > max) val = min;
                data.SetValue<int>(key, val);
            }

            public void Prev(ManagedData data)
            {
                int val = data.GetValue<int>(key) - 1;
                if (val < min) val = max;
                data.SetValue<int>(key, val);
            }
        }


        public class Vector2Field : ManagedField
        {
            private readonly VectorReprType repr;

            public Vector2Field(string key, Vector2 defaultValue, VectorReprType repr = VectorReprType.line) : base(key, defaultValue)
            {
                this.repr = repr;
            }

            public enum VectorReprType
            {
                none,
                line,
                circle,
                rect,
            }

            public override object FromString(string str)
            {
                string[] arr = Regex.Split(str, "\\^");
                return new Vector2(float.Parse(arr[0]), float.Parse(arr[1]));
            }

            public override string ToString(object value)
            {
                Vector2 vec = (Vector2)value;
                return string.Join("^", new string[]{ vec.x.ToString(), vec.y.ToString() });
            }

            public override DevUINode MakeAditionalNodes(ManagedData managedData, ManagedRepresentation managedRepresentation)
            {
                return new ManagedHandle(this, managedData, managedRepresentation, repr);
            }
        }
        #endregion FIELDS

        #region CONTROLS

        private class ManagedHandle : Handle // All-in-one super handle
        {
            private Vector2Field field;
            private ManagedData data;
            private readonly Vector2Field.VectorReprType reprType;
            private int line = -1;
            private int circle = -1;
            private int[] rect;

            public ManagedHandle(Vector2Field field, ManagedData managedData, ManagedRepresentation repr, Vector2Field.VectorReprType reprType) : base(repr.owner, field.key, repr, managedData.GetValue<Vector2>(field.key))
            {
                this.field = field;
                this.data = managedData;
                this.reprType = reprType;
                switch (reprType)
                {
                    case Vector2Field.VectorReprType.circle:
                    case Vector2Field.VectorReprType.line:
                        this.line = this.fSprites.Count;
                        this.fSprites.Add(new FSprite("pixel", true));
                        owner.placedObjectsContainer.AddChild(this.fSprites[line]);
                        this.fSprites[line].anchorY = 0;
                        if (reprType != Vector2Field.VectorReprType.circle)
                            break;
                    //case Vector2Field.VectorReprType.circle:
                        this.circle = this.fSprites.Count;
                        this.fSprites.Add(new FSprite("Futile_White", true));
                        owner.placedObjectsContainer.AddChild(this.fSprites[circle]);
                        this.fSprites[circle].shader = owner.room.game.rainWorld.Shaders["VectorCircle"];
                        break;
                    case Vector2Field.VectorReprType.rect:
                        this.rect = new int[5];
                        for (int i = 0; i < 5; i++)
                        {
                            this.rect[i] = this.fSprites.Count;
                            this.fSprites.Add(new FSprite("pixel", true));
                            owner.placedObjectsContainer.AddChild(this.fSprites[rect[i]]);
                            this.fSprites[rect[i]].anchorX = 0f;
                            this.fSprites[rect[i]].anchorY = 0f;
                        }
                        this.fSprites[rect[4]].alpha = 0.05f;
                        break;
                    default:
                        break;
                }
            }

            public override void Move(Vector2 newPos)
            {
                data.SetValue<Vector2>(field.key, newPos);
                base.Move(newPos); // calls refresh
            }

            public override void Refresh()
            {
                base.Refresh();
                pos = data.GetValue<Vector2>(field.key);

                if(line >= 0)
                {
                    base.MoveSprite(line, this.absPos);
                    this.fSprites[line].scaleY = pos.magnitude;
                    //this.fSprites[line].rotation = RWCustom.Custom.AimFromOneVectorToAnother(this.absPos, (parentNode as PositionedDevUINode).absPos); // but why
                    this.fSprites[line].rotation = RWCustom.Custom.VecToDeg(-pos);
                }
                if (circle >= 0)
                {
                    base.MoveSprite(circle, (parentNode as PositionedDevUINode).absPos);
                    this.fSprites[circle].scale = pos.magnitude/8f;
                    this.fSprites[circle].alpha = 2f / pos.magnitude;
                }
                if(rect != null)
                {

                    Vector2 leftbottom = Vector2.zero;
                    Vector2 topright = Vector2.zero;
                    // rectgrid abandoned

                    leftbottom = (parentNode as PositionedDevUINode).absPos + leftbottom;
                    topright = absPos + topright;
                    Vector2 size = (topright - leftbottom);

                    base.MoveSprite(1, leftbottom);
                    this.fSprites[1].scaleY = size.y;// + size.y.Sign();
                    base.MoveSprite(2, leftbottom);
                    this.fSprites[2].scaleX = size.x;// + size.x.Sign();
                    base.MoveSprite(3, (topright));
                    this.fSprites[3].scaleY = -size.y;// - size.y.Sign();
                    base.MoveSprite(4, (topright));
                    this.fSprites[4].scaleX = -size.x;// - size.x.Sign();
                    base.MoveSprite(5, leftbottom);
                    this.fSprites[5].scaleX = size.x;// + size.x.Sign();
                    this.fSprites[5].scaleY = size.y;// + size.y.Sign();
                }
            }
        }


        public class ManagedSlider : Slider
        {
            public ManagedFieldWithPanel field { get; }

            private IInterpolablePanelField interpolable;

            public ManagedData data { get; }

            public ManagedSlider(ManagedFieldWithPanel field, ManagedData data, ManagedControlPanel panel, float sizeOfDisplayname) : base(panel.owner, field.key, panel, Vector2.zero, field.displayName, false, sizeOfDisplayname)
            {
                this.field = field;
                this.interpolable = field as IInterpolablePanelField;
                if (interpolable == null) throw new ArgumentException("Field must implement IInterpolablePanelField");
                this.data = data;

                DevUILabel numberLabel = (this.subNodes[1] as DevUILabel);
                numberLabel.pos.x = sizeOfDisplayname + 10f;
                numberLabel.size.x = field.SizeOfLargestDisplayValue();
                numberLabel.fSprites[0].scaleX = numberLabel.size.x;

                // hacky hack for nubpos
                this.titleWidth = sizeOfDisplayname + numberLabel.size.x - 16f;
            }

            public override void NubDragged(float nubPos)
            {
                interpolable.NewFactor(data, nubPos);
                // this.managedControlPanel.managedRepresentation.Refresh(); // is this relevant ?
                this.Refresh();
            }

            public override void Refresh()
            {
                base.Refresh();
                float value = interpolable.FactorOf(data);
                base.NumberText = field.DisplayValueForNode(this, data);
                base.RefreshNubPos(value);
            }
        }

        public class ManagedButton : PositionedDevUINode, IDevUISignals
        {
            private Button button;

            public ManagedFieldWithPanel field { get; }

            private IIterablePanelField iterable;

            public ManagedData data { get; }
            public ManagedButton(ManagedFieldWithPanel field, ManagedData data, ManagedControlPanel panel, float sizeOfDisplayname) : base(panel.owner, field.key, panel, Vector2.zero)
                
            {
                this.field = field;
                this.iterable = field as IIterablePanelField;
                if (iterable == null) throw new ArgumentException("Field must implement IIterablePanelField");
                this.data = data;
                this.subNodes.Add(new DevUILabel(owner, "Title", this, new Vector2(0f, 0f), sizeOfDisplayname, field.displayName));
                this.subNodes.Add(this.button = new Button(owner, "Button", this, new Vector2(sizeOfDisplayname + 10f, 0f), field.SizeOfLargestDisplayValue(), field.defaultValue.ToString()));

            }

            public void Signal(DevUISignalType type, DevUINode sender, string message) // from button
            {
                iterable.Next(data);
                // no right-click suport :(
                this.Refresh();
            }

            public override void Refresh()
            {
                this.button.Text = field.DisplayValueForNode(this, data);
                base.Refresh();
            }
        }

        internal class ManagedArrowSelector : IntegerControl
        {
            private ManagedFieldWithPanel field;
            private IIterablePanelField iterable;
            private ManagedData data;

            public ManagedArrowSelector(ManagedFieldWithPanel field, ManagedData managedData, ManagedControlPanel panel, float sizeOfDisplayname) : base(panel.owner, "ManagedArrowSelector", panel, Vector2.zero, field.displayName )
            {
                this.field = field;
                this.iterable = field as IIterablePanelField;
                if (iterable == null) throw new ArgumentException("Field must implement IIterablePanelField");
                this.data = managedData;

                DevUILabel titleLabel = (this.subNodes[0] as DevUILabel);
                titleLabel.size.x = sizeOfDisplayname;
                titleLabel.fSprites[0].scaleX = sizeOfDisplayname;

                DevUILabel numberLabel = (this.subNodes[1] as DevUILabel);
                numberLabel.pos.x = sizeOfDisplayname + 30f;
                numberLabel.size.x = field.SizeOfLargestDisplayValue();
                numberLabel.fSprites[0].scaleX = numberLabel.size.x;

                ArrowButton arrowL = (this.subNodes[2] as ArrowButton);
                arrowL.pos.x = sizeOfDisplayname + 10f;

                ArrowButton arrowR = (this.subNodes[3] as ArrowButton);
                arrowR.pos.x = numberLabel.pos.x + numberLabel.size.x + 4f;
            }

            public override void Increment(int change)
            {
                if(change == 1)
                {
                    iterable.Next(data);
                }
                else if (change == -1)
                {
                    iterable.Prev(data);
                }

                this.Refresh();
            }

            public override void Refresh()
            {
                NumberLabelText = field.DisplayValueForNode(this, data);
                base.Refresh();
            }
        }
        #endregion CONTROLS
    }
}