using DevInterface;

namespace ConcealedGarden
{
    internal class CosmeticLeaves : UpdatableAndDeletable
    {
        private PlacedObject placedObject;

        public CosmeticLeaves(PlacedObject placedObject, Room instance)
        {
            this.placedObject = placedObject;
            this.room = instance;
        }

        internal class CosmeticLeavesObjectRepresentation : PlacedObjectRepresentation
        {
            public CosmeticLeavesObjectRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
            {
            }
        }

        internal class CosmeticLeavesObjectData : PlacedObject.Data
        {
            public CosmeticLeavesObjectData(PlacedObject owner) : base(owner)
            {
            }
        }
    }
}