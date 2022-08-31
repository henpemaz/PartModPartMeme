using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroG
{
	[BepInEx.BepInPlugin("henpemaz.zerog", "ZeroG", "1.0")]
	public class ZeroG : BepInEx.BaseUnityPlugin
	{
		public void OnEnable()
		{
            On.Room.Update += Room_Update;
            On.AntiGravity.Update += AntiGravity_Update;

            On.PhysicalObject.Update += PhysicalObject_Update;
            IL.Player.MovementUpdate += Player_MovementUpdate;
		}

        private void Player_MovementUpdate(MonoMod.Cil.ILContext il)
        {
			// line 5467
			var c = new ILCursor(il);
			ILLabel label = null;
			if (c.TryGotoNext(MoveType.AfterLabel,
				i => i.MatchBeq(out label),
				i => i.MatchLdarg(0),
				i => i.MatchLdcI4(8), // zerogswim
				i => i.MatchStfld<Player>("bodyMode")
				))
			{
				c.Index++;
				c.MoveAfterLabels();
				c.Emit(OpCodes.Ldarg_0);
				c.Emit<Player>(OpCodes.Ldfld, "bodyMode");
				c.Emit(OpCodes.Ldc_I4, 7); // swimming
				c.Emit(OpCodes.Beq, label); // if swimming dont zerogswim
			}
			else Logger.LogError(new Exception("Couldn't IL-hook Player_MovementUpdate from ZeroG")); // deffendisve progrmanig
		}

        private void PhysicalObject_Update(On.PhysicalObject.orig_Update orig, PhysicalObject self, bool eu)
        {
            orig(self, eu);
            self.buoyancy = 0f;
        }

        private void AntiGravity_Update(On.AntiGravity.orig_Update orig, AntiGravity self, bool eu)
        {
            orig(self, eu);
            if(self.room != null) self.room.gravity = 0f;
        }

        private void Room_Update(On.Room.orig_Update orig, Room self)
        {
            orig(self);
            self.gravity = 0f;
        }
    }
}
