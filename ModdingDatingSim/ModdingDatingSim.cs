using ManagedPlacedObjects;
using Partiality.Modloader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: System.Runtime.CompilerServices.SuppressIldasm()]
namespace ModdingDatingSim
{
    public class ModdingDatingSim : PartialityMod
    {
        public static ModdingDatingSim instance;
        public ModdingDatingSim()
        {
            this.ModID = "Modding Dating Sim";
            this.Version = "1.0";
            this.author = "Henpemaz";

            instance = this;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            TheGar.Register();

            //DialogueBox.Register();
        }

        public class TheGar : UpdatableAndDeletable, INotifyWhenRoomIsReady
        {
            private readonly PlacedObject pobj;

            public static void Register()
            {
                PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("TheGar", typeof(TheGar), null, null));
            }

            public TheGar(Room room, PlacedObject pobj)
            {
                this.room = room;
                this.pobj = pobj;
            }


            public void AIMapReady()
            {
                var p = room.GetTilePosition(pobj.pos);
                WorldCoordinate wc = new WorldCoordinate(room.abstractRoom.index, p.x, p.y, -1);
                AbstractCreature slugcat = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, wc, room.game.GetNewID());
                slugcat.state = new PlayerState(slugcat, 0, this.room.game.GetStorySession.saveState.saveStateNumber, false);
                slugcat.RealizeInRoom();
                Player player = (slugcat.realizedCreature as Player);
                player.standing = true;
                player.controller = new ArenaGameSession.PlayerStopController();
                player.forceSleepCounter = 120;

                this.Destroy();
            }

            public void ShortcutsReady()
            {
            }
        }
    }
}

