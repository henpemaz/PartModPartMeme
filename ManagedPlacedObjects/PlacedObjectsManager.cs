using DevInterface;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace ManagedPlacedObjects
{
    public static class PlacedObjectsManager
    {
        #region HOOKS
        private static bool _hooked = false;
        private static void Apply()
        {
            if (_hooked) return;
            _hooked = true;

            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData_Patch;
            On.Room.Loaded += Room_Loaded_Patch;
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep_Patch;
            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;

            //PlacedObjectsExample();
        }

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

        private static void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if (self.devUI == null)
            {
                ManagedStringControl.activeStringControl = null;     // remove string control focus when dev tools are closed
            }
        }

        #endregion HOOKS

        #region NATIVEDETOURS
        private static bool _stringsdetoured = false;
        private static void SetupInputDetours()
        {
            if (_stringsdetoured) return;
            _stringsdetoured = true;

            System.Reflection.BindingFlags bindingFlags =
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            System.Reflection.MethodBase getKeyMethod;
            System.Reflection.MethodBase captureInputMethod;

            getKeyMethod = typeof(Input).GetMethod("GetKey", new Type[] { typeof(string) });
            captureInputMethod = typeof(PlacedObjectsManager)
                    .GetMethod("CaptureInput", bindingFlags, null, new Type[] { typeof(string) }, null);
            inputDetour_string = new NativeDetour(getKeyMethod, captureInputMethod);

            getKeyMethod = typeof(Input).GetMethod("GetKey", new Type[] { typeof(KeyCode) });
            captureInputMethod = typeof(PlacedObjectsManager)
                    .GetMethod("CaptureInput", bindingFlags, null, new Type[] { typeof(KeyCode) }, null);
            inputDetour_code = new NativeDetour(getKeyMethod, captureInputMethod);
        }

#pragma warning disable IDE0051 // Reflection, dearling
        private static NativeDetour inputDetour_string;
        private static NativeDetour inputDetour_code;

        private static void UndoInputDetours()
        {
            inputDetour_string.Undo();
            inputDetour_code.Undo();
        }

        private static bool CaptureInput(string key)
        {
            key = key.ToUpper();
            KeyCode code = (KeyCode)Enum.Parse(typeof(KeyCode), key);
            return CaptureInput(code);
        }
#pragma warning restore IDE0051 // Remove unused private members

        private static bool CaptureInput(KeyCode code)
        {
            bool res;

            if (ManagedStringControl.activeStringControl == null)
            {
                res = Orig_GetKey(code);
            }
            else
            {
                if (code == KeyCode.Escape)
                {
                    res = Orig_GetKey(code);
                }
                else
                {
                    res = false;
                }
            }

            return res;
        }

        private static bool Orig_GetKey(KeyCode code)
        {
            inputDetour_code.Undo();
            bool res = Input.GetKey(code);
            inputDetour_code.Apply();
            return res;
        }

        #endregion NATIVEDETOURS

        #region INTERNALS
        private static readonly List<ManagedObjectType> managedObjectTypes = new List<ManagedObjectType>();
        private static ManagedObjectType GetManagerForType(PlacedObject.Type tp)
        {
            foreach (var manager in managedObjectTypes)
            {
                if (tp == manager.GetObjectType() && tp != PlacedObject.Type.None) return manager;
            }
            return null;
        }

        private static PlacedObject.Type DeclareOrGetEnum(string name)
        {
            // enum handling is delayed because of how enumextend works
            // bee needs to let mods add enums during onload or this will be always super annoying to use
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name is empty");
            // nope, this crashes because enumextender hasnt readied its pants yet
            //if (PastebinMachine.EnumExtender.EnumExtender.declarations.Count > 0) PastebinMachine.EnumExtender.EnumExtender.ExtendEnumsAgain();
            PlacedObject.Type tp;
            try
            {
                tp = (PlacedObject.Type)Enum.Parse(typeof(PlacedObject.Type), name);
            }
            catch
            {
                PastebinMachine.EnumExtender.EnumExtender.AddDeclaration(typeof(PlacedObject.Type), name);
                PastebinMachine.EnumExtender.EnumExtender.ExtendEnumsAgain();
                tp = (PlacedObject.Type)Enum.Parse(typeof(PlacedObject.Type), name);
            }

            return tp;
        }
        #endregion INTERNALS

        /// <summary>
        /// Register a managed type to handle object, data and repr initialization during room load and devtools hooks
        /// </summary>
        /// <param name="obj"></param>
        public static void RegisterManagedObject(ManagedObjectType obj)
        {
            Apply();
            managedObjectTypes.Add(obj);
        }

        // Some shorthands

        /// <summary>
        /// Shorthand for registering a FullyManagedObjectType.
        /// Wraps an UpdateableAndDeletable into a managed type with managed data and UI.
        /// Can also be used with a null type to spawn a Managed data+representation with no object on room.load
        /// If the object isn't null its Constructor should take (Room, PlacedObject) or (PlacedObject, Room).
        /// </summary>
        /// <param name="managedFields"></param>
        /// <param name="type">An UpdateableAndDeletable</param>
        /// <param name="name">Optional enum-name for your object, otherwise infered from type. Can be an enum already created with Enumextend. Do NOT use enum.ToString() on an enumextend'd enum, it wont work during Init() or Load()</param>
        public static void RegisterFullyManagedObjectType(ManagedField[] managedFields, Type type, string name=null)
        {
            if (string.IsNullOrEmpty(name)) name = type.Name;
            
            ManagedObjectType fullyManaged = new FullyManagedObjectType(name, type, managedFields);
            RegisterManagedObject(fullyManaged);
        }

        /// <summary>
        /// Shorthand for registering a ManagedObjectType with no actual object.
        /// Creates an empty data-holding placed object.
        /// Data and Repr must work well together (typically rep tries to cast data to a specific type to use it).
        /// Either can be left null, so no data or no specific representation will be created for the placedobject.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="dataType"></param>
        /// <param name="reprType"></param>
        public static void RegisterEmptyObjectType(string name, Type dataType, Type reprType)
        {
            ManagedObjectType emptyObjectType = new ManagedObjectType(name, null, dataType, reprType);
            RegisterManagedObject(emptyObjectType);
        }

        #region MANAGED
        /// <summary>
        /// Main class for managed object types.
        /// Make-calls CAN return null, causing the object to not be created, or have no data, or use the default handle representation.
        /// This class can be used to simply handle the room/devtools hooks.
        /// Call <see cref="RegisterManagedObject(ManagedObjectType)"/> to register your manager
        /// </summary>
        public class ManagedObjectType
        {
            protected PlacedObject.Type placedType;
            protected readonly string name;
            protected readonly Type objectType;
            protected readonly Type dataType;
            protected readonly Type reprType;
            protected readonly bool singleInstance;

            public ManagedObjectType(string name, Type objectType, Type dataType, Type reprType, bool singleInstance=false)
            {
                this.placedType = default; // type parsing deferred until actualy used
                this.name = name;
                this.objectType = objectType;
                this.dataType = dataType;
                this.reprType = reprType;
                this.singleInstance = singleInstance;
            }

            /// <summary>
            /// The PlacedObject.Type this is the manager for.
            /// </summary>
            public virtual PlacedObject.Type GetObjectType()
            {
                return placedType == default ? placedType = DeclareOrGetEnum(name) : placedType;
            }

            /// <summary>
            /// Called from Room.Loaded hook
            /// </summary>
            public virtual UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room)
            {
                if (objectType == null) return null;

                if (singleInstance) // Only one per room
                {
                    UpdatableAndDeletable instance = null;
                    foreach (var item in room.updateList)
                    {
                        if (item.GetType().IsAssignableFrom(objectType))
                        {
                            instance = item;
                            break;
                        }
                    }
                    if (instance != null) return null;
                }

                try { return (UpdatableAndDeletable)Activator.CreateInstance(objectType, new object[] { room, placedObject }); }
                catch
                {
                    try { return (UpdatableAndDeletable)Activator.CreateInstance(objectType, new object[] { placedObject, room }); }
                    catch
                    {
                        try { return (UpdatableAndDeletable)Activator.CreateInstance(objectType, new object[] { room }); } // Objects that scan room for data or no data;
                        catch { throw new ArgumentException("ManagedObjectType.MakeObject : objectType must have a constructor like (Room room, PlacedObject pObj) or (PlacedObject pObj, Room room) or (Room room)"); }

                    }
                }
            }

            /// <summary>
            /// Called from PlacedObject.GenerateEmptyData hook
            /// </summary>
            public virtual PlacedObject.Data MakeEmptyData(PlacedObject pObj)
            {
                if (dataType == null) return null;

                try { return (PlacedObject.Data)Activator.CreateInstance(dataType, new object[] { pObj }); }
                catch
                {
                    try { return (PlacedObject.Data)Activator.CreateInstance(dataType, new object[] { pObj, PlacedObject.LightFixtureData.Type.RedLight }); } // Redlights man
                    catch { throw new ArgumentException("ManagedObjectType.MakeEmptyData : dataType must have a constructor like (PlacedObject pObj)"); }
                }
            }

            /// <summary>
            /// Called from ObjectsPage.CreateObjRep hook
            /// </summary>
            public virtual PlacedObjectRepresentation MakeRepresentation(PlacedObject pObj, ObjectsPage objPage)
            {
                if (reprType == null) return null;

                try { return (PlacedObjectRepresentation)Activator.CreateInstance(reprType, new object[] { objPage.owner, placedType.ToString() + "_Rep", objPage, pObj, placedType.ToString() }); }
                catch
                {
                    try { return (PlacedObjectRepresentation)Activator.CreateInstance(reprType, new object[] { objPage.owner, placedType.ToString() + "_Rep", objPage, pObj, placedType.ToString(), false }); } // Resizeables man
                    catch { throw new ArgumentException("ManagedObjectType.MakeRepresentation : reprType must have a constructor like (DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name)"); }
                }
            }
        }

        /// <summary>
        /// Class for managing a wraped UpdateableAndDeletable object
        /// Uses the fully managed data and representation types
        /// </summary>
        public class FullyManagedObjectType : ManagedObjectType
        {
            protected readonly ManagedField[] managedFields;

            public FullyManagedObjectType(string name, Type objectType, ManagedField[] managedFields, bool singleInstance = false) : base(name, objectType, null, null, singleInstance)
            {
                this.managedFields = managedFields;
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
        /// A field to handle serialization and generate UI for data
        /// A field is merely a recipe/interface, the actual data is stored in the ManagedData data object for each pObj
        /// </summary>
        public abstract class ManagedField
        {
            public readonly string key;
            private readonly object _defaultValue;
            public virtual object DefaultValue => _defaultValue; // I SUSPECT one day someone will run into a situation with enums where the default value doesn't exist at initialization. Enumextend moment. Lets be nice to that poor soul.

            public ManagedField(string key, object defaultValue)
            {
                this.key = key;
                this._defaultValue = defaultValue;
            }

            /// <summary>
            /// Serialization method called from ManagedData
            /// </summary>
            public virtual string ToString(object value) => value.ToString();

            /// <summary>
            /// Deserialization method called from ManagedData
            /// </summary>
            public abstract object FromString(string str);

            /// <summary>
            /// Wether this field spawns a control panel node or not. Inherit ManagedFieldWithPanel for actually creating them.
            /// </summary>
            public virtual bool NeedsControlPanel { get => this is ManagedFieldWithPanel; }

            /// <summary>
            /// Create an aditional DevUINode for manipulating this field. Inherit a PositionedDevUINode if you need to create several nodes.
            /// </summary>
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
            protected readonly ControlType control;
            public readonly string displayName;

            protected ManagedFieldWithPanel(string key, object defaultValue, ControlType control = ControlType.none, string displayName = null) : base(key, defaultValue)
            {
                this.control = control;
                this.displayName = displayName ?? key;
            }
            public enum ControlType
            {
                none,
                slider,
                arrows,
                button,
                text
            }

            /// <summary>
            /// Used internally for control panel display. Consumed by MakeControls to expand the panel and space controls.
            /// </summary>
            public virtual Vector2 PanelUiSizeMinusName { get => SizeOfPanelNode() + new Vector2(SizeOfLargestDisplayValue(), 0f); }

            /// <summary>
            /// Used internally for control panel display. Consumed by PanelUiSizeMinusName and final UI.
            /// </summary>
            public abstract float SizeOfLargestDisplayValue();

            /// <summary>
            /// Approx size of the UI minus displayname and valuedisplay width. Consumed by PanelUiSizeMinusName.
            /// </summary>
            protected virtual Vector2 SizeOfPanelNode()
            {
                switch (control)
                {
                    case ControlType.slider:
                        return new Vector2(116f, 20f);

                    case ControlType.arrows:
                        return new Vector2(52f, 20f);

                    case ControlType.text:
                    case ControlType.button:
                        return new Vector2(12f, 20f);

                    default:
                        break;
                }
                return new Vector2(0f, 20f);
            }

            /// <summary>
            /// Used internally for control panel display. Consumed by MakeControls to make controls aligned.
            /// </summary>
            public virtual float SizeOfDisplayname()
            {
                return HUD.DialogBox.meanCharWidth * (displayName.Length + 2);
            }

            /// <summary>
            /// Used internally for building the control panel display.
            /// </summary>
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
                    case ControlType.text:
                        return new ManagedStringControl(this, managedData, panel, sizeOfDisplayname);
                }
                return null;
            }

            /// <summary>
            /// Used internally for control panel display.
            /// </summary>
            public virtual string DisplayValueForNode(PositionedDevUINode node, ManagedData data)
            {
                // field tostring, but fields can format on their own
                return ToString(data.GetValue<object>(key));
            }

            /// <summary>
            /// Used internally for text input parsing.
            /// </summary>
            public virtual void ParseFromText(PositionedDevUINode node, ManagedData data, string newValue)
            {
                data.SetValue(key, this.FromString(newValue));
            }
        }


        /// <summary>
        /// Managed data type, handles managed fields
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
                    valuesByKey[field.key] = field.DefaultValue;
                    if (field.NeedsControlPanel) this.needsControlPanel = true;
                }
            }

            public readonly ManagedField[] fields;
            protected readonly Dictionary<string, ManagedField> fieldsByKey;
            protected readonly Dictionary<string, object> valuesByKey;
            public readonly bool needsControlPanel;
            public Vector2 panelPos;

            // I was thinking of extracting these to an interface but I'm not sure if it achieves anything if everything is properly inheritable ?
            public virtual T GetValue<T>(string fieldName)
            {
                return (T)valuesByKey[fieldName];
            }

            public virtual void SetValue<T>(string fieldName, T value)
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
        }

        public class ManagedRepresentation : PlacedObjectRepresentation
        {
            protected readonly PlacedObject.Type placedType;
            protected readonly Dictionary<string, DevUINode> managedNodes; // Unused for now, but seems convenient for specialization
            protected ManagedControlPanel panel; // Unused for now, but seems convenient for specialization

            public ManagedRepresentation(PlacedObject.Type placedType, ObjectsPage objPage, PlacedObject pObj) : base(objPage.owner, placedType.ToString() + "_Rep", objPage, pObj, placedType.ToString())
            {
                this.placedType = placedType;
                this.pObj = pObj;

                this.managedNodes = new Dictionary<string, DevUINode>();
                MakeControls();
            }

            protected virtual void MakeControls()
            {
                ManagedData data = pObj.data as ManagedData;
                if (data.needsControlPanel)
                {
                    ManagedControlPanel panel = new ManagedControlPanel(this.owner, "ManagedControlPanel", this, data.panelPos, Vector2.zero, pObj.type.ToString());
                    this.panel = panel;
                    this.subNodes.Add(panel);
                    Vector2 uiSize = new Vector2(0f, 0f);
                    Vector2 uiPos = new Vector2(3f, 3f);
                    float largestDisplayname = 0f;
                    for (int i = 0; i < data.fields.Length; i++) // up down
                    {
                        if (data.fields[i] is ManagedFieldWithPanel field && field.NeedsControlPanel)
                        {
                            largestDisplayname = Mathf.Max(largestDisplayname, field.SizeOfDisplayname());
                        }
                    }

                    for (int i = data.fields.Length - 1; i >= 0; i--) // down up
                    {
                        if (data.fields[i] is ManagedFieldWithPanel field && field.NeedsControlPanel)
                        {
                            PositionedDevUINode node = field.MakeControlPanelNode(data, panel, largestDisplayname);
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

                for (int i = 0; i < data.fields.Length; i++)
                {
                    ManagedField field = data.fields[i];
                    DevUINode node = field.MakeAditionalNodes(data, this);
                    if (node != null)
                    {
                        this.subNodes.Add(node);
                        this.managedNodes[field.key] = node;
                    }
                }
            }
        }

        public class ManagedControlPanel : Panel
        {
            protected readonly ManagedRepresentation managedRepresentation;
            public Dictionary<string, DevUINode> managedNodes; // Added externally. Unused for now, but seems convenient for specialization
            protected readonly int lineSprt;

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
                (this.managedRepresentation.pObj.data as ManagedData).panelPos = this.pos; // mfw no "data with panel" intermediate class
            }
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
            protected readonly float min;
            protected readonly float max;
            protected readonly float increment;

            public FloatField(string key, float min, float max, float defaultValue, float increment = 0.1f, ControlType control = ControlType.slider, string displayName = null) : base(key, defaultValue, control, displayName)
            {
                this.min = min;
                this.max = max;
                this.increment = increment;
            }

            public override object FromString(string str)
            {
                return Mathf.Clamp(float.Parse(str),min, max);
            }

            protected virtual int NumberOfDecimals()
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

            public virtual float FactorOf(ManagedData data)
            {
                return ((max - min) == 0) ? 0f : (((float)data.GetValue<float>(key) - min) / (max - min));
            }

            public virtual void NewFactor(ManagedData data, float factor)
            {
                data.SetValue<float>(key, min + factor * (max - min));
            }

            public virtual void Next(ManagedData data)
            {
                float val = data.GetValue<float>(key) + increment;
                if (val > max) val = min;
                data.SetValue<float>(key, val);
            }

            public virtual void Prev(ManagedData data)
            {
                float val = data.GetValue<float>(key) - increment;
                if (val < min) val = max;
                data.SetValue<float>(key, val);
            }

            public override void ParseFromText(PositionedDevUINode node, ManagedData data, string newValue)
            {
                float val = float.Parse(newValue);
                if (val < min || val > max) throw new ArgumentException();
                base.ParseFromText(node, data, newValue);
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

            public virtual float FactorOf(ManagedData data)
            {
                return data.GetValue<bool>(key) ? 1f : 0f;
            }

            public virtual void NewFactor(ManagedData data, float factor)
            {
                data.SetValue(key, factor > 0.5f);
            }

            public virtual void Next(ManagedData data)
            {
                data.SetValue(key, !data.GetValue<bool>(key));
            }

            public virtual void Prev(ManagedData data)
            {
                data.SetValue(key, !data.GetValue<bool>(key));
            }
        }

        public class EnumField : ManagedFieldWithPanel, IIterablePanelField, IInterpolablePanelField
        {
            protected readonly Type type;
            protected Enum[] _possibleValues;

            public EnumField(string key, Type type, Enum defaultValue, Enum[] possibleValues = null, ControlType control = ControlType.none, string displayName = null) : base(key, (possibleValues != null && !possibleValues.Contains(defaultValue))? possibleValues[0] : defaultValue, control, displayName)
            {
                this.type = type;
                this._possibleValues = possibleValues;
            }

            protected virtual Enum[] PossibleValues // We defer this listing so enumextend can do its magic.
            {
                get
                {
                    if (_possibleValues == null) _possibleValues = Enum.GetValues(type).Cast<Enum>().ToArray();
                    return _possibleValues;
                }
            }

            public override object FromString(string str)
            {
                Enum fromstring = (Enum)Enum.Parse(type, str);
                return PossibleValues.Contains(fromstring) ? fromstring : PossibleValues[0];
            }

            public override float SizeOfLargestDisplayValue()
            {
                int longestEnum = PossibleValues.Aggregate<Enum, int>(0, (longest, next) =>
                        next.ToString().Length > longest ? next.ToString().Length : longest);
                return HUD.DialogBox.meanCharWidth * longestEnum + 2;
            }

            public virtual float FactorOf(ManagedData data)
            {
                return (float)Array.IndexOf(PossibleValues, data.GetValue<Enum>(key)) / (float)(PossibleValues.Length - 1);
            }

            public virtual void NewFactor(ManagedData data, float factor)
            {
                data.SetValue<Enum>(key, PossibleValues[Mathf.RoundToInt(factor * (PossibleValues.Length - 1))]);
            }

            public virtual void Next(ManagedData data)
            {
                data.SetValue<Enum>(key, PossibleValues[(Array.IndexOf(PossibleValues, data.GetValue<Enum>(key)) + 1) % PossibleValues.Length]);
            }

            public virtual void Prev(ManagedData data)
            {
                data.SetValue<Enum>(key, PossibleValues[(Array.IndexOf(PossibleValues, data.GetValue<Enum>(key)) - 1 + PossibleValues.Length) % PossibleValues.Length]);
            }

            public override void ParseFromText(PositionedDevUINode node, ManagedData data, string newValue)
            {
                Enum fromstring;
                try
                {
                    fromstring = (Enum)Enum.Parse(type, newValue);
                }
                catch (Exception)
                {
                    foreach (Enum val in PossibleValues)
                    {
                        if (val.ToString().ToLowerInvariant() == newValue.ToLowerInvariant())
                        {
                            // This check is flawed if we have for instance "aa" and "AAA" it becomes impossible to type in the second one
                            // But honestly who would name their enums like that...
                            data.SetValue(key, val);
                            return;
                        }
                    }
                    throw;
                }
                if (!PossibleValues.Contains(fromstring)) throw new ArgumentException();

                data.SetValue(key, fromstring);
                //base.ParseFromText(node, data, newValue);
            }
        }

        public class IntegerField : ManagedFieldWithPanel, IIterablePanelField, IInterpolablePanelField
        {
            protected readonly int min;
            protected readonly int max;

            public IntegerField(string key, int min, int max, int defaultValue, ControlType control = ControlType.arrows, string displayName = null) : base(key, defaultValue, control, displayName)
            {
                this.min = min;
                this.max = max;
            }

            public override object FromString(string str)
            {
                return Mathf.Clamp(int.Parse(str), min, max);
            }

            public override float SizeOfLargestDisplayValue()
            {
                return HUD.DialogBox.meanCharWidth * ((Mathf.Max(Mathf.Abs(min), Mathf.Abs(max))).ToString().Length + 2);
            }

            public virtual float FactorOf(ManagedData data)
            {
                return (max - min == 0) ? 0f : (data.GetValue<int>(key) - min) / (float)(max - min);
            }

            public virtual void NewFactor(ManagedData data, float factor)
            {
                data.SetValue<int>(key, Mathf.RoundToInt(min + factor * (max - min)));
            }

            public virtual void Next(ManagedData data)
            {
                int val = data.GetValue<int>(key) + 1;
                if (val > max) val = min;
                data.SetValue<int>(key, val);
            }

            public virtual void Prev(ManagedData data)
            {
                int val = data.GetValue<int>(key) - 1;
                if (val < min) val = max;
                data.SetValue<int>(key, val);
            }

            public override void ParseFromText(PositionedDevUINode node, ManagedData data, string newValue)
            {
                int val = int.Parse(newValue);
                if (val < min || val > max) throw new ArgumentException();
                base.ParseFromText(node, data, newValue);
            }
        }

        public class StringField : ManagedFieldWithPanel
        {
            public StringField(string key, string defaultValue, string displayName = null) : base(key, defaultValue, ControlType.text, displayName)
            {

            }

            public override object FromString(string str)
            {
                return str;
            }

            public override string ToString(object value)
            {
                return value.ToString(); // should already be a string
            }

            public override float SizeOfLargestDisplayValue()
            {
                return HUD.DialogBox.meanCharWidth * 25; // No character limit but this is the expected reasonable max anyone would be using ?
            }
        }

        public class Vector2Field : ManagedField
        {
            protected readonly VectorReprType repr;

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
                return string.Join("^", new string[] { vec.x.ToString(), vec.y.ToString() });
            }

            public override DevUINode MakeAditionalNodes(ManagedData managedData, ManagedRepresentation managedRepresentation)
            {
                return new ManagedVectorHandle(this, managedData, managedRepresentation, repr);
            }
        }

        public class IntVector2Field : ManagedField
        {
            protected readonly IntVectorReprType repr;

            public IntVector2Field(string key, RWCustom.IntVector2 defaultValue, IntVectorReprType repr = IntVectorReprType.line) : base(key, defaultValue)
            {
                this.repr = repr;
            }

            public enum IntVectorReprType
            {
                none,
                line,
                tile,
                fourdir,
                eightdir,
                rect,
            }

            public override object FromString(string str)
            {
                string[] arr = Regex.Split(str, "\\^");
                return new RWCustom.IntVector2(int.Parse(arr[0]), int.Parse(arr[1]));
            }

            public override string ToString(object value)
            {
                RWCustom.IntVector2 vec = (RWCustom.IntVector2)value;
                return string.Join("^", new string[] { vec.x.ToString(), vec.y.ToString() });
            }

            public override DevUINode MakeAditionalNodes(ManagedData managedData, ManagedRepresentation managedRepresentation)
            {
                return new ManagedIntHandle(this, managedData, managedRepresentation, repr);
            }
        }

        #endregion FIELDS

        #region CONTROLS

        public class ManagedVectorHandle : Handle // All-in-one super handle
        {
            protected readonly Vector2Field field;
            protected readonly ManagedData data;
            protected readonly Vector2Field.VectorReprType reprType;
            protected readonly int line = -1;
            protected readonly int circle = -1;
            protected readonly int[] rect;

            public ManagedVectorHandle(Vector2Field field, ManagedData managedData, ManagedRepresentation repr, Vector2Field.VectorReprType reprType) : base(repr.owner, field.key, repr, managedData.GetValue<Vector2>(field.key))
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

                if (line >= 0)
                {
                    base.MoveSprite(line, this.absPos);
                    this.fSprites[line].scaleY = pos.magnitude;
                    //this.fSprites[line].rotation = RWCustom.Custom.AimFromOneVectorToAnother(this.absPos, (parentNode as PositionedDevUINode).absPos); // but why
                    this.fSprites[line].rotation = RWCustom.Custom.VecToDeg(-pos);
                }
                if (circle >= 0)
                {
                    base.MoveSprite(circle, (parentNode as PositionedDevUINode).absPos);
                    this.fSprites[circle].scale = pos.magnitude / 8f;
                    this.fSprites[circle].alpha = 2f / pos.magnitude;
                }
                if (rect != null)
                {

                    Vector2 leftbottom = Vector2.zero;
                    Vector2 topright = Vector2.zero;
                    // rectgrid abandoned

                    leftbottom = (parentNode as PositionedDevUINode).absPos + leftbottom;
                    topright = absPos + topright;
                    Vector2 size = (topright - leftbottom);

                    base.MoveSprite(rect[0], leftbottom);
                    this.fSprites[rect[0]].scaleY = size.y;// + size.y.Sign();
                    base.MoveSprite(rect[1], leftbottom);
                    this.fSprites[rect[1]].scaleX = size.x;// + size.x.Sign();
                    base.MoveSprite(rect[2], (topright));
                    this.fSprites[rect[2]].scaleY = -size.y;// - size.y.Sign();
                    base.MoveSprite(rect[3], (topright));
                    this.fSprites[rect[3]].scaleX = -size.x;// - size.x.Sign();
                    base.MoveSprite(rect[4], leftbottom);
                    this.fSprites[rect[4]].scaleX = size.x;// + size.x.Sign();
                    this.fSprites[rect[4]].scaleY = size.y;// + size.y.Sign();
                }
            }

            public override void SetColor(Color col)
            {
                base.SetColor(col);
                if (line >= 0)
                {
                    this.fSprites[line].color = col;
                }
                if (circle >= 0)
                {
                    this.fSprites[circle].color = col;
                }

                if (rect != null)
                {
                    for (int i = 0; i < rect.Length; i++)
                    {
                        this.fSprites[rect[i]].color = col;
                    }
                }
            }
        }

        public class ManagedIntHandle : Handle // All-in-one super handle 2
        {
            protected readonly IntVector2Field field;
            protected readonly ManagedData data;
            protected readonly IntVector2Field.IntVectorReprType reprType;
            protected readonly int pixel = -1;
            protected readonly int[] rect;

            public ManagedIntHandle(IntVector2Field field, ManagedData managedData, ManagedRepresentation repr, IntVector2Field.IntVectorReprType reprType) : base(repr.owner, field.key, repr, managedData.GetValue<RWCustom.IntVector2>(field.key).ToVector2()*20f)
            {
                this.field = field;
                this.data = managedData;
                this.reprType = reprType;
                switch (reprType)
                {
                    case IntVector2Field.IntVectorReprType.line:
                    case IntVector2Field.IntVectorReprType.tile:
                    case IntVector2Field.IntVectorReprType.fourdir:
                    case IntVector2Field.IntVectorReprType.eightdir:
                        this.pixel = this.fSprites.Count;
                        this.fSprites.Add(new FSprite("pixel", true));
                        owner.placedObjectsContainer.AddChild(this.fSprites[pixel]);
                        this.fSprites[pixel].MoveBehindOtherNode(this.fSprites[0]); // attention to detail

                        if (reprType == IntVector2Field.IntVectorReprType.tile)
                        {
                            this.fSprites[pixel].alpha = 0.25f;
                            this.fSprites[pixel].scale = 20f;
                        }

                        this.fSprites[pixel].anchorX = 0;
                        this.fSprites[pixel].anchorY = 0;

                        if (reprType == IntVector2Field.IntVectorReprType.fourdir || reprType == IntVector2Field.IntVectorReprType.eightdir)
                        {
                            this.fSprites[0].SetElementByName("Menu_Symbol_Arrow");
                            this.fSprites[0].scale = 0.75f;
                        }

                        break;
                    case IntVector2Field.IntVectorReprType.rect:
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
                // absolute so we're aligned with room tiles
                if (reprType == IntVector2Field.IntVectorReprType.fourdir)
                {
                    float dir = RWCustom.Custom.VecToDeg(newPos);
                    dir -= ((dir + 45f + 360f) % 90f) - 45f;
                    newPos = RWCustom.Custom.DegToVec(dir) * 20f;
                }
                else if (reprType == IntVector2Field.IntVectorReprType.eightdir)
                {
                    float dir = RWCustom.Custom.VecToDeg(newPos);
                    dir -= ((dir + 22.5f + 360f) % 45f) - 22.5f;
                    newPos = RWCustom.Custom.DegToVec(dir);
                    if (Mathf.Abs(newPos.x) > 0.5f) newPos.x = Mathf.Sign(newPos.x);
                    if (Mathf.Abs(newPos.y) > 0.5f) newPos.y = Mathf.Sign(newPos.y);
                    newPos *= 20f;
                }
                Vector2 parentPos = (this.parentNode as PositionedDevUINode).absPos;
                Vector2 roompos = newPos + parentPos;
                RWCustom.IntVector2 ownIntPos = new RWCustom.IntVector2(Mathf.FloorToInt(roompos.x / 20f), Mathf.FloorToInt(roompos.y / 20f));
                RWCustom.IntVector2 parentIntPos = new RWCustom.IntVector2(Mathf.FloorToInt(parentPos.x / 20f), Mathf.FloorToInt(parentPos.y / 20f));
                // relativize again
                ownIntPos -= parentIntPos;
                newPos = ownIntPos.ToVector2()*20f;

                data.SetValue<RWCustom.IntVector2>(field.key, ownIntPos);
                base.Move(newPos); // calls refresh
            }

            public override void Refresh()
            {
                base.Refresh();
                pos = data.GetValue<RWCustom.IntVector2>(field.key).ToVector2()*20f;

                if (pixel >= 0)
                {
                    switch (reprType)
                    {

                        case IntVector2Field.IntVectorReprType.tile:
                            base.MoveSprite(pixel, new Vector2(Mathf.FloorToInt(absPos.x / 20f), Mathf.FloorToInt(absPos.y / 20f))*20f);
                            break;
                        case IntVector2Field.IntVectorReprType.line:
                        case IntVector2Field.IntVectorReprType.fourdir:
                        case IntVector2Field.IntVectorReprType.eightdir:
                            if (reprType == IntVector2Field.IntVectorReprType.fourdir || reprType == IntVector2Field.IntVectorReprType.eightdir)
                                this.fSprites[0].rotation = RWCustom.Custom.VecToDeg(pos);

                            base.MoveSprite(pixel, this.absPos);
                            this.fSprites[pixel].scaleY = pos.magnitude;
                            this.fSprites[pixel].rotation = RWCustom.Custom.VecToDeg(-pos);
                            break;
                    }
                }
                
                if (rect != null)
                {
                    Vector2 parentPos = (this.parentNode as PositionedDevUINode).absPos;
                    Vector2 roompos = absPos;
                    RWCustom.IntVector2 ownIntPos = new RWCustom.IntVector2(Mathf.FloorToInt(roompos.x / 20f), Mathf.FloorToInt(roompos.y / 20f));
                    RWCustom.IntVector2 parentIntPos = new RWCustom.IntVector2(Mathf.FloorToInt(parentPos.x / 20f), Mathf.FloorToInt(parentPos.y / 20f));

                    Vector2 leftbottom = new Vector2(Mathf.Min(ownIntPos.x, parentIntPos.x) * 20f, Mathf.Min(ownIntPos.y, parentIntPos.y) * 20f);
                    Vector2 topright = new Vector2(Mathf.Max(ownIntPos.x, parentIntPos.x) * 20f + 20f, Mathf.Max(ownIntPos.y, parentIntPos.y) * 20f + 20f);
                    // rectgrid revived

                    Vector2 size = (topright - leftbottom);

                    base.MoveSprite(rect[0], leftbottom);
                    this.fSprites[rect[0]].scaleY = size.y;// + size.y.Sign();
                    base.MoveSprite(rect[1], leftbottom);
                    this.fSprites[rect[1]].scaleX = size.x;// + size.x.Sign();
                    base.MoveSprite(rect[2], (topright));
                    this.fSprites[rect[2]].scaleY = -size.y;// - size.y.Sign();
                    base.MoveSprite(rect[3], (topright));
                    this.fSprites[rect[3]].scaleX = -size.x;// - size.x.Sign();
                    base.MoveSprite(rect[4], leftbottom);
                    this.fSprites[rect[4]].scaleX = size.x;// + size.x.Sign();
                    this.fSprites[rect[4]].scaleY = size.y;// + size.y.Sign();
                }
            }

            public override void SetColor(Color col)
            {
                base.SetColor(col);
                if (pixel >= 0)
                {
                    this.fSprites[pixel].color = col;
                }

                if (rect != null)
                {
                    for (int i = 0; i < rect.Length; i++)
                    {
                        this.fSprites[rect[i]].color = col;
                    }
                }
            }
        }

        public class ManagedSlider : Slider
        {
            protected readonly ManagedFieldWithPanel field;
            protected readonly IInterpolablePanelField interpolable;
            protected readonly ManagedData data;

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
            protected readonly Button button;
            protected readonly ManagedFieldWithPanel field;
            protected readonly IIterablePanelField iterable;
            protected readonly ManagedData data;
            public ManagedButton(ManagedFieldWithPanel field, ManagedData data, ManagedControlPanel panel, float sizeOfDisplayname) : base(panel.owner, field.key, panel, Vector2.zero)
                
            {
                this.field = field;
                this.iterable = field as IIterablePanelField;
                if (iterable == null) throw new ArgumentException("Field must implement IIterablePanelField");
                this.data = data;
                this.subNodes.Add(new DevUILabel(owner, "Title", this, new Vector2(0f, 0f), sizeOfDisplayname, field.displayName));
                this.subNodes.Add(this.button = new Button(owner, "Button", this, new Vector2(sizeOfDisplayname + 10f, 0f), field.SizeOfLargestDisplayValue(), field.DisplayValueForNode(this, data)));
            }

            public virtual void Signal(DevUISignalType type, DevUINode sender, string message) // from button
            {
                iterable.Next(data);
                this.Refresh();
            }

            public override void Refresh()
            {
                this.button.Text = field.DisplayValueForNode(this, data);
                base.Refresh();
            }
        }

        public class ManagedArrowSelector : IntegerControl
        {
            protected readonly ManagedFieldWithPanel field;
            protected readonly IIterablePanelField iterable;
            protected readonly ManagedData data;

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

        public class ManagedStringControl : PositionedDevUINode
        {
            public static ManagedStringControl activeStringControl = null;

            protected readonly ManagedFieldWithPanel field;
            protected readonly ManagedData data;
            protected bool clickedLastUpdate = false;

            public ManagedStringControl(ManagedFieldWithPanel field, ManagedData data,
                    ManagedControlPanel panel, float sizeOfDisplayname)
                    : base(panel.owner, "ManagedStringControl", panel, Vector2.zero)
            {
                SetupInputDetours();

                this.field = field;
                this.data = data;

                subNodes.Add(new DevUILabel(owner, "Title", this, new Vector2(0, 0), sizeOfDisplayname, field.displayName));
                subNodes.Add(new DevUILabel(owner, "Text", this, new Vector2(60, 0), 136, ""));

                Text = field.DisplayValueForNode(this, data);

                DevUILabel textLabel = (this.subNodes[1] as DevUILabel);
                textLabel.pos.x = sizeOfDisplayname + 10f;
                textLabel.size.x = field.SizeOfLargestDisplayValue();
                textLabel.fSprites[0].scaleX = textLabel.size.x;
            }

            protected virtual string Text
            {
                get
                {
                    return subNodes[1].fLabels[0].text;
                }
                set
                {
                    subNodes[1].fLabels[0].text = value;
                }
            }
            
            public override void Refresh()
            {
                // No data refresh until the transaction is complete :/
                // TrySet happens on input and focus loss
                base.Refresh();
            }

            public override void Update()
            {
                if (owner.mouseClick && !clickedLastUpdate)
                {
                    if ((subNodes[1] as RectangularDevUINode).MouseOver && activeStringControl != this)
                    {
                        // replace whatever instance/null that was focused
                        activeStringControl = this;
                        subNodes[1].fLabels[0].color = new Color(0.1f, 0.4f, 0.2f);
                    }
                    else if (activeStringControl == this)
                    {
                        // focus lost
                        TrySetValue(Text, true);
                        activeStringControl = null;
                        subNodes[1].fLabels[0].color = Color.black;
                    }

                    clickedLastUpdate = true;
                }
                else if (!owner.mouseClick)
                {
                    clickedLastUpdate = false;
                }

                if (activeStringControl == this)
                {
                    foreach (char c in Input.inputString)
                    {
                        if (c == '\b')
                        {
                            if (Text.Length != 0)
                            {
                                Text = Text.Substring(0, Text.Length - 1);
                                TrySetValue(Text, false);
                            }
                        }
                        else if (c == '\n' || c == '\r')
                        {
                            // should lose focus
                            TrySetValue(Text, true);
                            activeStringControl = null;
                            subNodes[1].fLabels[0].color = Color.black;
                        }
                        else
                        {
                            Text += c;
                            TrySetValue(Text, false);
                        }
                    }
                }
            }

            protected virtual void TrySetValue(string newValue, bool endTransaction) {
                try
                {
                    field.ParseFromText(this, data, newValue);
                    subNodes[1].fLabels[0].color = new Color(0.1f, 0.4f, 0.2f); // positive feedback
                }
                catch (Exception)
                {
                    subNodes[1].fLabels[0].color = Color.red; // negative fedback
                }
                if (endTransaction)
                {
                    Text = field.DisplayValueForNode(this, data);
                    subNodes[1].fLabels[0].color = Color.black;
                    Refresh();
                }
            }
        }

        #endregion CONTROLS
    }
}