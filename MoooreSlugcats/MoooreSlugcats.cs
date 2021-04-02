using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoooreSlugcats
{
    public class MoooreSlugcats : PartialityMod
    {
        public MoooreSlugcats()
        {
            this.ModID = "MoooreSlugcats (april fools)";
            this.Version = "0.69";
            this.author = "Henpemaz";

            instance = this;
        }

        public static MoooreSlugcats instance;

        public override void OnEnable()
        {
            base.OnEnable();

            On.Room.Update += Room_Update;

        }

        private void Room_Update(On.Room.orig_Update orig, Room self)
        {
            orig(self);

            int character = self.world.game.session.Players.Count > 0 ? (int)(self.world.game.session.Players[0].state as PlayerState)?.slugcatCharacter : 0;
            if (UnityEngine.Input.GetKey("l"))
            {
                if (self.game.IsStorySession && self.abstractRoom.connections.Length > 0)
                {
                    //AbstractRoom neightbour = self.world.GetAbstractRoom(self.abstractRoom.connections[0]);
                    AbstractCreature abstractCreature =
                        new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate("Slugcat"), null,
                        new WorldCoordinate(self.abstractRoom.connections[0], 0, 0, 0), new EntityID(-1, 1));
                    abstractCreature.state = new PlayerState(abstractCreature, 0, character, false);
                    self.world.GetAbstractRoom(abstractCreature.pos.room).AddEntity(abstractCreature);
                    abstractCreature.ChangeRooms(new WorldCoordinate(self.abstractRoom.index, 0, 0, 0));
                }
                else if (self.game.IsArenaSession && self.exitAndDenIndex.Length > 0)
                {
                    AbstractCreature abstractCreature =
                        new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate("Slugcat"), null,
                        new WorldCoordinate(self.abstractRoom.index, self.exitAndDenIndex[0].x, self.exitAndDenIndex[0].y, 0), new EntityID(-1, 1));
                    abstractCreature.state = new PlayerState(abstractCreature, 0, character, false);
                    self.abstractRoom.MoveEntityToDen(abstractCreature);
                    self.abstractRoom.MoveEntityOutOfDen(abstractCreature);

                }
            }
        }
    }
}
