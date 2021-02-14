using DevInterface;

namespace Climbables
{
    internal class BezierObjectRepresentation : PlacedObjectRepresentation
    {
        public BezierObjectRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
        {
        }
    }
}