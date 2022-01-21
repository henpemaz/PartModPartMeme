using System;
using UnityEngine;

namespace ZandrasCharacterPackPort
{
	public class CustomVultMask : VultureMask
	{
		public CustomVultMask(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject, world) { }

		public override void Update(bool eu)
		{
			if (base.slatedForDeletetion)
			{
				return;
			}
			base.Forbid();
			if (this.grabbedBy.Count == 0)
			{
				base.Update(eu); // added
				this.waitCount--;
				if (this.waitCount <= 0)
				{
					Debug.Log("CustomVultMask destroyed");
					this.Destroy();
					return;
				}
			}
			else
			{
				waitCount = 5;
				//base.bodyChunks[0].pos = this.grabbedBy[0].grabber.mainBodyChunk.pos;
				//this.lastRotationA = this.rotationA;
				//this.lastRotationB = this.rotationB;
				//this.rotationA = this.grabbedBy[0].grabber.mainBodyChunk.Rotation;
				//this.donned = 1f;
				//this.lastDonned = 1f;
				if (!(this.grabbedBy[0].grabber is Player p))
                {
					Debug.Log("CustomVultMask destroyed");
					this.Destroy();
					return;
				}
				// swap hands
				this.grabbedBy[0].graspUsed = 1;
				var oldg1 = p.grasps[1];
				var oldhandpos = (p.graphicsModule as PlayerGraphics).hands[1].pos;
				(p.graphicsModule as PlayerGraphics).hands[1].pos = p.mainBodyChunk.pos;
				this.grabbedBy[0].grabber.grasps[1] = this.grabbedBy[0].grabber.grasps[2];
				this.grabbedBy[0].grabber.grasps[2] = oldg1;
				base.Update(eu); // added
				// patchup
				float to2 = 0f;
				if ((this.grabbedBy[0].grabber as Player).input[0].x != 0 && Mathf.Abs(this.grabbedBy[0].grabber.bodyChunks[1].lastPos.x - this.grabbedBy[0].grabber.bodyChunks[1].pos.x) > 2f)
				{
					to2 = (float)(this.grabbedBy[0].grabber as Player).input[0].x;
				}

				(p.graphicsModule as PlayerGraphics).hands[1].pos = oldhandpos;
				this.grabbedBy[0].grabber.grasps[2] = this.grabbedBy[0].grabber.grasps[1];
				this.grabbedBy[0].grabber.grasps[1] = oldg1;
				this.grabbedBy[0].graspUsed = 2;

				// funny values on these 2 = does not render
				this.donned = 1f;
				this.viewFromSide = RWCustom.Custom.LerpAndTick(this.viewFromSide, to2, 0.11f, 0.033333335f);
			}
		}

		private int waitCount = 5;
	}
}
