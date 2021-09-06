using ManagedPlacedObjects;
using UnityEngine;
using System;
using System.Linq;
using System.IO;

namespace ConcealedGarden
{
    internal class CGSkyLine : CosmeticSprite, ShelterBehaviors.IReactToShelterEvents
    {
        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("CGSkyLine",
                typeof(CGSkyLine), typeof(CGSkyLineData), typeof(PlacedObjectsManager.ManagedRepresentation)));
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RainWorld.Start += RainWorld_Start;
        }

        private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);
            try
            {
                TryLoad();
            }
            catch (Exception)
            {
            }
        }
        private static void TryLoad()
        {
            if (CustomRegions.Mod.CustomWorldMod.activatedPacks.ContainsKey("Concealed Garden"))
                CustomAtlasLoader.ReadAndLoadCustomAtlas("cgbasketsprt", CustomRegions.Mod.CustomWorldMod.resourcePath + CustomRegions.Mod.CustomWorldMod.activatedPacks["Concealed Garden"] + Path.DirectorySeparatorChar + "Assets" + Path.DirectorySeparatorChar + "Futile" + Path.DirectorySeparatorChar + "Resources" + Path.DirectorySeparatorChar + "Atlases");
        }

        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);
            CGSkyLine csl = (CGSkyLine)self.room?.updateList?.Find(e => e is CGSkyLine);
            if(csl != null)
            {
                self.ReturnFContainer("Shortcuts").SetPosition(-csl.pos);
                self.ReturnFContainer("Background").SetPosition(-csl.pos);
            }
        }

        private CGSkyLineData data => pObj.data as CGSkyLineData;
        private readonly PlacedObject pObj;
        private readonly Vector2 origCamPos;
        private float inMovementFactor;
        private float speed;

        public CGSkyLine(Room room, PlacedObject pObj)
        {
            this.room = room;
            this.pObj = pObj;

            this.origCamPos = room.cameraPositions[0];
        }

        public void ShelterEvent(float newFactor, float closeSpeed)
        {
            this.inMovementFactor = newFactor;
            this.speed = closeSpeed;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            lastPos = pos;
            //inMovementFactor = data.test;//Mathf.Clamp01(inMovementFactor + speed);
            inMovementFactor = Mathf.Clamp01(inMovementFactor + speed);
            pos = data.movement * inMovementFactor;
            room.cameraPositions[0] = origCamPos - pos;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[]
            {
                new FSprite("cgbasketsprt", true){ 
                    shader = rCam.game.rainWorld.Shaders["ColoredSprite2"],
                    anchorX = 0f,
                    anchorY = 0f,
                }
            };

            ApplyPalette(sLeaser, rCam, rCam.currentPalette);
            AddToContainer(sLeaser, rCam, null);
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            
        }
        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
        }
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 displacement = Vector2.Lerp(lastPos, pos, timeStacker);
            sLeaser.sprites[0].SetPosition(rCam.levelGraphic.GetPosition() + displacement);
            //sLeaser.sprites[0].SetPosition(new Vector2(origCamPos.x + rCam.hDisplace + 8f, origCamPos.y + 18f));

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }


        private class CGSkyLineData : PlacedObjectsManager.ManagedData
        {
            [PlacedObjectsManager.ManagedData.BackedByField("mv")]
            public Vector2 movement;

            [PlacedObjectsManager.FloatField("ts",0,1,0)]
            public float test;
            
            public CGSkyLineData(PlacedObject owner) : base(owner, new PlacedObjectsManager.ManagedField[] { new PlacedObjectsManager.Vector2Field("mv", new Vector2(-100,0), label:"Mv")})
            {

            }
        }
    }
}