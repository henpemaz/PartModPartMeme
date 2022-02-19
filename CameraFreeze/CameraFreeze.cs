using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;

namespace CameraFreeze
{
    [BepInPlugin("author.my_mod_id", "SomeModName", "0.1.0")]
    public class CameraFreeze : BaseUnityPlugin
    {
        public void OnEnable()
        {
            /* This is called when the mod is loaded. */

            On.Player.Update += Player_Update;

        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            var camera = self.room.game.cameras[0];
            if (self.input[0].mp && !self.input[1].mp)
            {
                if(camera.followAbstractCreature != null)
                {
                    camera.followAbstractCreature = null;
                }
                else
                {
                    camera.followAbstractCreature = self.abstractCreature;

                    if(self.room != camera.room && self.abstractCreature.pos.NodeDefined)
                    {
                        camera.MoveCamera(self.room, self.room.CameraViewingNode(self.abstractCreature.pos.abstractNode));
                    }
                }
            }
        }
    }
}
