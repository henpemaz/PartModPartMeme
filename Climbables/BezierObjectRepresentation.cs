using DevInterface;
using UnityEngine;

namespace Climbables
{
    internal class BezierObjectRepresentation : PlacedObjectRepresentation
    {

		Handle handleA => this;
		Handle handleB;
		Handle handleC;
		Handle handleD;
		public BezierObjectRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name) : base(owner, IDstring, parentNode, pObj, name)
        {
			handleB = new Handle(owner, "Rect_Handle", this, new Vector2(0f, 40f));
			this.subNodes.Add(handleB);
			handleB.pos = (pObj.data as PlacedObject.QuadObjectData).handles[0];
			handleC = new Handle(owner, "Rect_Handle", this, new Vector2(40f, 40f));
			this.subNodes.Add(handleC);
			handleC.pos = (pObj.data as PlacedObject.QuadObjectData).handles[1];
			handleD = new Handle(owner, "Rect_Handle", handleC, new Vector2(40f, 0f));
			handleC.subNodes.Add(handleD);
			handleD.pos = (pObj.data as PlacedObject.QuadObjectData).handles[2] - handleC.pos;
			for (int i = 0; i < 2; i++)
			{
				this.fSprites.Add(new FSprite("pixel", true));
				owner.placedObjectsContainer.AddChild(this.fSprites[1 + i]);
				this.fSprites[1 + i].anchorY = 0f;
			}
		}

		public override void Refresh()
		{
			base.Refresh();
			base.MoveSprite(1, this.absPos);
			for (int i = 0; i < 2; i++)
			{
				(this.pObj.data as PlacedObject.QuadObjectData).handles[i] = (this.subNodes[i] as Handle).pos;
			}
			(this.pObj.data as PlacedObject.QuadObjectData).handles[0] = handleB.pos;
			(this.pObj.data as PlacedObject.QuadObjectData).handles[1] = handleC.pos;
			(this.pObj.data as PlacedObject.QuadObjectData).handles[2] = handleD.pos + handleC.pos;
			base.MoveSprite(1, this.absPos);
			this.fSprites[1].scaleY = handleB.pos.magnitude;
			this.fSprites[1].rotation = RWCustom.Custom.VecToDeg(handleB.pos);
			base.MoveSprite(2, this.absPos + handleC.pos);
			this.fSprites[2].scaleY = handleD.pos.magnitude;
			this.fSprites[2].rotation = RWCustom.Custom.VecToDeg(handleD.pos);
		}

	}
}