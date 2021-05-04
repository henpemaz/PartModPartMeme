using DevInterface;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ShelterBehaviors
{

    static class ManagedPlacedObjects
    {
        #region HOOKS
        internal static void ApplyHooks()
        {
            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData_Patch;
            On.Room.Loaded += Room_Loaded_Patch;
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep_Patch;

            MySillyExample();
        }

        private static void ObjectsPage_CreateObjRep_Patch(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
        {
            orig(self, tp, pObj);
            ManagedObjectType manager = GetManagerForType(tp);
            if (manager != null)
            {
                DevInterface.PlacedObjectRepresentation old = (DevInterface.PlacedObjectRepresentation)self.tempNodes.Pop();
                self.subNodes.Pop();
                old.ClearSprites();
                DevInterface.PlacedObjectRepresentation placedObjectRepresentation = manager.MakeRepresentation(old.pObj, self);
                self.tempNodes.Add(placedObjectRepresentation);
                self.subNodes.Add(placedObjectRepresentation);
            }
        }

        private static void Room_Loaded_Patch(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            ManagedObjectType manager;
            for (int i = 0; i < self.roomSettings.placedObjects.Count; i++)
            {
                if (self.roomSettings.placedObjects[i].active)
                {
                    if ((manager = ManagedPlacedObjects.GetManagerForType(self.roomSettings.placedObjects[i].type)) != null)
                    {
                        self.AddObject(manager.MakeObject(self.roomSettings.placedObjects[i], self));
                    }
                }
            }
        }

        private static void PlacedObject_GenerateEmptyData_Patch(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
        {
            ManagedObjectType manager = GetManagerForType(self.type);
            if (manager != null)
            {
                self.data = manager.MakeEmptyData(self);
            }
        }

        #endregion HOOKS

        public static void MySillyExample()
        {
            List<ManagedField> fields = new List<ManagedField>();
            for (int i = 0; i < 10; i++)
            {
                ManagedField field = new FloatField("MyFloat" + i, 0f, (float)i + 1, (float)i);
                fields.Add(field);
            }

            MakeManagedObjectType(fields.ToArray(), typeof(SillyObject));
        }

        internal class SillyObject : UpdatableAndDeletable
        {
            private PlacedObject placedObject;

            public SillyObject(PlacedObject pObj, Room room)
            {
                this.room = room;
                this.placedObject = pObj;
                UnityEngine.Debug.Log("SillyObject create, data as follows");
                for (int i = 0; i < 10; i++)
                {
                    string field = "MyFloat" + i;
                    UnityEngine.Debug.Log(field + " as " + (placedObject.data as ManagedData).GetValue<float>(field));
                }
                //UnityEngine.Debug.Log("SillyObject create, data has MyFloat0 as " + (placedObject.data as ManagedData).GetValue<float>("MyFloat0"));
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                UnityEngine.Debug.Log("SillyObject update, now DIE");
                Destroy();
            }
        }


        private static List<ManagedObjectType> managedObjectTypes = new List<ManagedObjectType>();
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
        internal static void MakeManagedObjectType(ManagedField[] managedFields, Type type)
        {
            try
            {
                PastebinMachine.EnumExtender.EnumExtender.AddDeclaration(typeof(PlacedObject.Type), type.Name);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error extending enums " + e);
            }
            ManagedObjectType fullyManaged = new FullyManagedObjectType(type, managedFields);
            RegisterManagedObject(fullyManaged);
        }

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
                return (UpdatableAndDeletable) objectType.GetConstructor(System.Reflection.BindingFlags.Default, null, new Type[]{ typeof(PlacedObject), typeof(Room)}, null).Invoke(new object[] {placedObject, room });
            }

            public override PlacedObject.Data MakeEmptyData(PlacedObject pObj)
            {
                return new ManagedData(pObj, managedFields);
            }

            public override PlacedObjectRepresentation MakeRepresentation(PlacedObject pObj, ObjectsPage objPage)
            {
                //return new PlacedObjectRepresentation(objPage.owner, placedType.ToString() + "_Rep", objPage, pObj, placedType.ToString());
                return new ManagedRepresentation(GetObjectType(), objPage, pObj);
            }
        }


        public abstract class ManagedObjectType
        {
            public abstract PlacedObject.Type GetObjectType();

            public abstract UpdatableAndDeletable MakeObject(PlacedObject placedObject, Room room);

            public virtual PlacedObjectRepresentation MakeRepresentation(PlacedObject pObj, ObjectsPage objPage)
            {
                throw new NotImplementedException();
            }

            public virtual PlacedObject.Data MakeEmptyData(PlacedObject pObj)
            {
                throw new NotImplementedException();
            }
        }



        public abstract class ManagedField
        {
            public string key;
            public string displayName;
            public object defaultValue;

            public ManagedField(string key, object defaultValue, string displayName = null)
            {
                this.key = key;
                this.displayName = displayName ?? key;
                this.defaultValue = defaultValue;
            }

            public virtual string ToString(object value) => value.ToString();
            public abstract object FromString(string str);


            public virtual bool NeedsControlPanel { get => false; }
            public virtual Vector2 PanelUiSize { get => Vector2.zero; }
            
            public virtual object ReadControlPanelNode(DevUINode node)
            {
                throw new NotImplementedException();
            }

            public virtual PositionedDevUINode MakeControlPanelNode(ManagedData managedData, ManagedRepresentation managedRepresentation, ManagedControlPanel panel)
            {
                throw new NotImplementedException();
            }

            public virtual PositionedDevUINode MakeAditionalNodes(ManagedData managedData, ManagedRepresentation managedRepresentation)
            {
                return null;
                //throw new NotImplementedException();
            }
        }

        public class FloatField : ManagedField
        {
            public float min;
            public float max;
            //public float floatValue { get => (float)value; }
            public override bool NeedsControlPanel => true;
            public override Vector2 PanelUiSize => new Vector2(200f, 20f);
            public FloatField(string name, float min, float max, float defaultValue, string displayName=null) : base(name, defaultValue, displayName)
            {
                this.min = min;
                this.max = max;
            }

            public override object FromString(string str)
            {
                return float.Parse(str);
            }

            public override PositionedDevUINode MakeControlPanelNode(ManagedData managedData, ManagedRepresentation managedRepresentation, ManagedControlPanel panel)
            {
                return new ManagedSlider(this, managedData, panel);
            }

        }

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

            private readonly ManagedField[] fields;
            private readonly Dictionary<string, ManagedField> fieldsByKey;
            private readonly Dictionary<string, object> valuesByKey;
            public readonly bool needsControlPanel;
            private Vector2 panelPos;

            public T GetValue<T>(string fieldName)
            {
                return (T)valuesByKey[fieldName];
            }

            internal void SetValue<T>(string fieldName, T value)
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
                    Vector2 uiSize = new Vector2(2f, 2f);
                    Vector2 uiPos = new Vector2(2f, 2f);
                    for (int i = fields.Length - 1; i >= 0; i--) // down up
                    {
                        ManagedField field = fields[i];
                        if (field.NeedsControlPanel)
                        {
                            PositionedDevUINode node = field.MakeControlPanelNode(this, managedRepresentation, panel);
                            panel.managedNodes[field.key] = node;
                            panel.subNodes.Add(node);
                            node.pos = uiPos;
                            uiSize.x = Mathf.Max(uiSize.x, field.PanelUiSize.x);
                            uiSize.y += field.PanelUiSize.y;
                            uiPos.y += field.PanelUiSize.y;
                        }
                    }
                    panel.size = uiSize;
                }

                for (int i = 0; i < fields.Length; i++)
                {
                    ManagedField field = fields[i];
                    PositionedDevUINode node = field.MakeAditionalNodes(this, managedRepresentation);
                    if (node != null)
                    {
                        managedRepresentation.subNodes.Add(node);
                        managedRepresentation.managedNodes[field.key] = node;
                    }
                }
            }
        }


        internal class ManagedRepresentation : PlacedObjectRepresentation
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

        internal class ManagedControlPanel : Panel
        {
            public Dictionary<string, DevUINode> managedNodes;

            public ManagedControlPanel(DevUI owner, string IDstring, ManagedRepresentation parentNode, Vector2 pos, Vector2 size, string title) : base(owner, IDstring, parentNode, pos, size, title)
            {
                managedRepresentation = parentNode;
                managedNodes = new Dictionary<string, DevUINode>();
            }

            public ManagedRepresentation managedRepresentation { get; }
        }

        public class ManagedSlider : Slider
        {
            public FloatField floatField { get; }
            public ManagedData data { get; }
            public ManagedControlPanel managedControlPanel { get; }

            public ManagedSlider(FloatField floatField, ManagedData data, ManagedControlPanel panel) : base(panel.owner, floatField.key, panel, Vector2.zero, floatField.displayName, false, 110f)
            {
                this.floatField = floatField;
                this.data = data;
                managedControlPanel = panel;
            }

            public override void Refresh()
            {
                base.Refresh();
                float num = data.GetValue<float>(floatField.key);
                base.NumberText = (num).ToString();
                base.RefreshNubPos(Mathf.InverseLerp(floatField.min, floatField.max, num));
            }

            public override void NubDragged(float nubPos)
            {
                data.SetValue<float>(floatField.key, Mathf.Lerp(floatField.min, floatField.max, nubPos));
                this.managedControlPanel.managedRepresentation.Refresh();
                this.Refresh();
            }
        }
    }
}