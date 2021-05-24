﻿using ManagedPlacedObjects;
using RWCustom;
using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal static class BunkerShelterParts
    {
        class BunkerShelterFlapData : PlacedObjectsManager.ManagedData
        {
            private static PlacedObjectsManager.ManagedField[] fields = new PlacedObjectsManager.ManagedField[]
                {
                    new PlacedObjectsManager.IntVector2Field("handle", new RWCustom.IntVector2(2,4), PlacedObjectsManager.IntVector2Field.IntVectorReprType.rect),
                };

            [PlacedObjectsManager.FloatField("dpt", 1, 30, 8, 1, displayName: "Depth")]
            public float depth;
            [PlacedObjectsManager.FloatField("ofx", -10, 10, 0, 1, displayName:"Offset X")]
            public float offsetX;
            [PlacedObjectsManager.FloatField("ofy", -10, 10, 0, 1, displayName: "Offset Y")]
            public float offsetY;
            [BackedByField("handle")]
            public IntVector2 handle;

            public BunkerShelterFlapData(PlacedObject owner) : base(owner, fields) { }
        }
        internal static void Register()
        {
            PlacedObjectsManager.RegisterManagedObject(new PlacedObjectsManager.ManagedObjectType("BunkerShelterFlap", typeof(BunkerShelterFlap),
                dataType: typeof(BunkerShelterFlapData), typeof(PlacedObjectsManager.ManagedRepresentation)));
        }

        private class BunkerShelterFlap : CosmeticSprite, ShelterBehaviors.IReactToShelterEvents
        {
            private float closedFactor;
            private float lastClosedFactor;
            private float closeSpeed;
            private PlacedObject pObj;
            IntRect rect;
            private int height;
            private int width;
            private int area;
            private float heightFloat;
            private float widthFloat;

            BunkerShelterFlapData data => pObj.data as BunkerShelterFlapData;

            public BunkerShelterFlap(Room room, PlacedObject pObj)
            {
                this.room = room;
                this.pObj = pObj;

                var origin = new IntVector2(Mathf.FloorToInt(pObj.pos.x / 20f), Mathf.FloorToInt(pObj.pos.y / 20f));
                rect = IntRect.MakeFromIntVector2(origin);
                rect.ExpandToInclude(origin + data.handle);
                rect.right++;
                rect.top++; // match visuals
                this.height = rect.Height;
                this.width= rect.Width;
                this.area = rect.Area;
                this.heightFloat = 20f * height;
                this.widthFloat = 20f * width;

                //Debug.LogError("Flaps created");
                // measure onc
            }

            public void ShelterEvent(float newFactor, float closeSpeed)
            {
                this.closedFactor = newFactor;
                this.lastClosedFactor = newFactor;
                this.closeSpeed = closeSpeed;
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                lastClosedFactor = closedFactor;
                closedFactor = Mathf.Clamp01(closedFactor + closeSpeed);
            }

            private int Cog(int i) => i == 0 ? 0 : 1;
            private int LidBL(int i) => 2 + i;
            private int LidBR(int i) => 2 + height + i;
            private int LidB(int i, int j) => 2 + 2 * height + i * width + j;

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                base.InitiateSprites(sLeaser, rCam);
                sLeaser.sprites = new FSprite[rect.Height * 2 + rect.Area + 2];
                FShader shader = this.room.game.rainWorld.Shaders["ColoredSprite2"];
                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[Cog(i)] = new FSprite("bkr_cog", true) { shader = shader };
                }
                for (int i = 0; i < height; i++)
                {
                    sLeaser.sprites[LidBL(i)] = new FSprite("bkr_lidB_left", true) { shader = shader };
                    sLeaser.sprites[LidBR(i)] = new FSprite("bkr_lidB_right", true) { shader = shader };
                    for (int j = 0; j < width; j++)
                    {
                        sLeaser.sprites[LidB(i, j)] = new FSprite("bkr_lidB_mid", true) { shader = shader };
                    }
                }

                this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
                //Debug.LogError("Flaps initiated, with " + sLeaser.sprites.Length + " sprites");
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                Vector2 start = new Vector2(rect.left * 20 - camPos.x + data.offsetX, rect.bottom * 20 - camPos.y + data.offsetY);

                //Debug.Log("rendering flaps at " + start);
                float factor = Mathf.Lerp(lastClosedFactor, closedFactor, timeStacker);
                float easedFactor = 
                      0.15f * Mathf.Pow(Mathf.InverseLerp(0, 0.2f, factor), 2f) 
                    + 0.7f * Mathf.InverseLerp(0.2f, 0.8f, factor) 
                    + 0.15f * Mathf.Pow(Mathf.InverseLerp(0.8f, 1, factor), 0.5f);
                float depth = data.GetValue<float>("dpt") / 30f;
                //Debug.Log("rendering flaps at " + depth);
                float depthStep = 1f / 30f;
                float heightStep = 20f - 1f / height;

                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[Cog(i)].x = start.x + (i == 0 ? -4f : widthFloat + 4f);
                    sLeaser.sprites[Cog(i)].y = start.y + 8;
                    sLeaser.sprites[Cog(i)].alpha = depth + depthStep;
                    sLeaser.sprites[Cog(i)].rotation = easedFactor * 1440f * (i == 0 ? -1f : 1f);
                }
                float animatedLidY = 7 * Mathf.InverseLerp(0, 0.75f, Mathf.Pow(easedFactor, 0.69f)) + 3 * Mathf.InverseLerp(0.75f, 1f, easedFactor);
                float animatedLidZ = 3 * depthStep * Mathf.InverseLerp(0.6f, 0.9f, easedFactor);
                for (int i = 0; i < height; i++)
                {
                    sLeaser.sprites[LidBL(i)].x = start.x -5f ;
                    sLeaser.sprites[LidBL(i)].y = start.y + 5f + i * heightStep + animatedLidY;
                    sLeaser.sprites[LidBL(i)].alpha = depth + animatedLidZ;
                    sLeaser.sprites[LidBR(i)].x = start.x + widthFloat + 5f;
                    sLeaser.sprites[LidBR(i)].y = start.y + 5f + i * heightStep + animatedLidY;
                    sLeaser.sprites[LidBR(i)].alpha = depth + animatedLidZ;
                    for (int j = 0; j < width; j++)
                    {
                        sLeaser.sprites[LidB(i, j)].x = start.x + 10f + j * 20f;
                        sLeaser.sprites[LidB(i, j)].y = start.y + 5f + i * heightStep + animatedLidY;
                        sLeaser.sprites[LidB(i, j)].alpha = depth + animatedLidZ;
                    }
                }
            }
        }
    }
}