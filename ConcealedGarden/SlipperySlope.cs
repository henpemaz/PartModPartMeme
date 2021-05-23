using ManagedPlacedObjects;
using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal class SlipperySlope : UpdatableAndDeletable
    {
        private PlacedObject pObj;
        private SlipperySlopeData data => pObj.data as SlipperySlopeData;
        public SlipperySlope(Room room, PlacedObject pObj)
        {
            this.room = room;
            this.pObj = pObj;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            Rect aarect = new Rect(pObj.pos.x, pObj.pos.y, data.xhandle.magnitude, data.yhandle.magnitude);
            Vector2 centerOfRect = aarect.center;
            float angleOfTheDangle = RWCustom.Custom.VecToDeg(data.yhandle);
            foreach (var upd in room.updateList)
            {
                if (upd is PhysicalObject phys)
                {
                    foreach (var chunk in phys.bodyChunks)
                    {
                        Vector2 rotatedPosition = RWCustom.Custom.RotateAroundVector(chunk.pos, pObj.pos, -angleOfTheDangle);
                        Vector2 centerBias = (centerOfRect - rotatedPosition).normalized * 0.01f; ;
                        Vector2 collisionCandidate = RWCustom.Custom.RotateAroundVector(aarect.GetClosestInteriorPoint(rotatedPosition), pObj.pos, angleOfTheDangle);
                        centerBias = RWCustom.Custom.RotateAroundOrigo(centerBias, angleOfTheDangle);
                        phys.PushOutOf(collisionCandidate + centerBias, 0f,-1);
                    }
                }
            }
        }

        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("SlipperySlope",
                typeof(SlipperySlope), typeof(SlipperySlope.SlipperySlopeData), typeof(PlacedObjectsManager.ManagedRepresentation)));

        }

        internal class SlipperySlopeData : PlacedObjectsManager.ManagedData
        {
            private static readonly PlacedObjectsManager.ManagedField[] customFields = new PlacedObjectsManager.ManagedField[]
            {
                new PlacedObjectsManager.Vector2Field("ev2", new Vector2(-100, -40), PlacedObjectsManager.Vector2Field.VectorReprType.none),
                new PlacedObjectsManager.DrivenVector2Field("ev3", "ev2", new Vector2(-100, -40), PlacedObjectsManager.DrivenVector2Field.DrivenControlType.rectangle),
            };
            [BackedByField("ev2")]
            public Vector2 xhandle;
            [BackedByField("ev3")]
            public Vector2 yhandle;
            public SlipperySlopeData(PlacedObject owner) : base(owner, customFields) { }
        }
    }
}