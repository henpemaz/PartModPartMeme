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

		GameObject lineObject;
		LineRenderer lineRenderer;
		FGameObjectNode lineNode;
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

			lineObject = new GameObject();
			lineRenderer = lineObject.AddComponent("LineRenderer") as LineRenderer;
			lineRenderer.material = new Material(FShader.defaultShader.shader);

			UpdateLineSegments();

			lineNode = new FGameObjectNode(lineObject, false, false, false);
			owner.placedObjectsContainer.AddChild(lineNode);

			lineRenderer.SetColors(Color.white, Color.white);
		}

		protected void UpdateLineSegments()
        {
			float heuristicDistance = handleB.pos.magnitude;
			heuristicDistance += handleD.pos.magnitude;
			heuristicDistance += (handleB.pos - (handleC.pos + handleD.pos)).magnitude;

			int nsegments = Mathf.CeilToInt(heuristicDistance / 10f);

			Vector2 posA = handleA.absPos;
			Vector2 posB = handleB.absPos;
			Vector2 posC = handleC.absPos;
			Vector2 posD = handleD.absPos;


			lineRenderer.SetVertexCount(nsegments);
			float step = 1f / nsegments;
            for (int i = 0; i < nsegments; i++)
            {
				float t = step * i;
				float num = 1f - t;
				Vector2 pt =  num * num * num * posA + 3f * num * num * t * posB + 3f * num * t * t * posD + t * t * t * posC;
				lineRenderer.SetPosition(i, pt);
            }
		}


        public override void ClearSprites()
        {
			base.ClearSprites();
			lineObject = null;
			lineRenderer = null;
			lineNode.RemoveFromContainer();
			lineNode = null;
		}

		public override void SetColor(Color col)
        {
			base.SetColor(col);
			lineRenderer.SetColors(col, col);
        }

        public override void Refresh()
		{
			base.Refresh();
			(this.pObj.data as PlacedObject.QuadObjectData).handles[0] = handleB.pos;
			(this.pObj.data as PlacedObject.QuadObjectData).handles[1] = handleC.pos;
			(this.pObj.data as PlacedObject.QuadObjectData).handles[2] = handleD.pos + handleC.pos;
			base.MoveSprite(1, this.absPos);
			this.fSprites[1].scaleY = handleB.pos.magnitude;
			this.fSprites[1].rotation = RWCustom.Custom.VecToDeg(handleB.pos);
			base.MoveSprite(2, this.absPos + handleC.pos);
			this.fSprites[2].scaleY = handleD.pos.magnitude;
			this.fSprites[2].rotation = RWCustom.Custom.VecToDeg(handleD.pos);

			UpdateLineSegments();
		}

	}
}