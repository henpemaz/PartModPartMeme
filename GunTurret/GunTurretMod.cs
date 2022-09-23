using BepInEx;
using UnityEngine;

namespace GunTurret
{
    [BepInPlugin("henpemaz.gunturretmod", "GunTurretMod", "0.1.0")]
    public class GunTurretMod : BaseUnityPlugin
    {
        public static class EnumExt_GunTurret
        {
#pragma warning disable 0649
            public static PlacedObject.Type GunTurret;
#pragma warning restore 0649
        }

        public void OnEnable()
        {
            On.Room.Loaded += Room_Loaded;
        }

        private void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig(self);
            if (self.game == null) return;

            foreach (var pobj in self.roomSettings.placedObjects)
            {
                if(pobj.active && pobj.type == EnumExt_GunTurret.GunTurret)
                {
                    self.AddObject(new GunTurret(pobj));
                    break;
                }
            }
        }

        class GunTurret : UpdatableAndDeletable, IDrawable
        {
            private PlacedObject pobj;

            Vector2 laseDir = new Vector2(-1f, 0f);
            Vector2 laseAnchor;
            float laserLen = 500f;
            private bool hitSomething;

            public GunTurret(PlacedObject pobj)
            {
                this.pobj = pobj;
                UnityEngine.Debug.Log("GunTurret created!!! =======================================================");
            }

            public override void Update(bool eu)
            {
                base.Update(eu);

                laseAnchor = this.pobj.pos + new Vector2(-25, 7);

                var hitPos = (laseAnchor + laseDir * laserLen);
                SharedPhysics.CollisionResult collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(null, this.room, laseAnchor, ref hitPos, 1f, 1, null, false);

                if (collisionResult.hitSomething) this.hitSomething = true;
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[3];

                sLeaser.sprites[0] = new FSprite("GunMount", true);
                sLeaser.sprites[0].anchorX = 0.5f;
                sLeaser.sprites[0].anchorY = 0.85f;
                sLeaser.sprites[0].shader = this.room.game.rainWorld.Shaders["ColoredSprite2"];
                sLeaser.sprites[0].alpha = 1f - 5f / 30f;
                
                sLeaser.sprites[1] = new FSprite("GunTurret", true);
                sLeaser.sprites[1].anchorX = 0.5f;
                sLeaser.sprites[1].anchorY = 0.15f;
                sLeaser.sprites[1].shader = this.room.game.rainWorld.Shaders["ColoredSprite2"];
                sLeaser.sprites[1].alpha = 1f - 4f/30f;

                sLeaser.sprites[2] = new CustomFSprite("Futile_White");
                sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders["HologramBehindTerrain"];

                this.AddToContainer(sLeaser, rCam, null);

            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                sLeaser.sprites[0].SetPosition(this.pobj.pos - camPos);
                sLeaser.sprites[1].SetPosition(this.pobj.pos - camPos);
                
                var laser = sLeaser.sprites[2] as CustomFSprite;
                Vector2 perp = RWCustom.Custom.PerpendicularVector(laseDir);
                laser.MoveVertice(0, laseAnchor + perp - camPos);
                laser.MoveVertice(1, laseAnchor - perp - camPos);
                laser.MoveVertice(2, laseAnchor + laserLen * laseDir - perp - camPos);
                laser.MoveVertice(3, laseAnchor + laserLen * laseDir + perp - camPos);

                if (hitSomething)
                {
                    //laser.color = new Color(1f, 0, 0);
                    for (int i = 0; i < laser.verticeColors.Length; i++)
                    {
                        laser.verticeColors[i] = new Color(1f, 0, 0);
                    }
                }
            }

            public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                if (newContatiner == null)
                {
                    newContatiner = rCam.ReturnFContainer("Items");
                }
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (i == 2)
                    {
                        rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[i]);
                    }
                    else
                    {
                        newContatiner.AddChild(sLeaser.sprites[i]);
                    }
                }
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {

            }
        }
    }
}
