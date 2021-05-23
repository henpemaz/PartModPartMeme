using ManagedPlacedObjects;
using RWCustom;
using System;
using UnityEngine;

namespace ConcealedGarden
{
    internal static class BunkerShelterParts
    {
        internal static void Register()
        {
            PlacedObjectsManager.RegisterFullyManagedObjectType( new PlacedObjectsManager.ManagedField[] {
                new PlacedObjectsManager.IntVector2Field("handle", new RWCustom.IntVector2(2,4), PlacedObjectsManager.IntVector2Field.IntVectorReprType.rect),
                new PlacedObjectsManager.FloatField("dpt", 1, 30, 8, 1, displayName:"Depth"),
                },typeof(BunkerShelterFlap), "BunkerShelterFlap");

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

            PlacedObjectsManager.ManagedData data => pObj.data as PlacedObjectsManager.ManagedData;

            public BunkerShelterFlap(Room room, PlacedObject pObj)
            {
                this.room = room;
                this.pObj = pObj;

                var origin = new IntVector2(Mathf.FloorToInt(pObj.pos.x / 20f), Mathf.FloorToInt(pObj.pos.y / 20f));
                rect = IntRect.MakeFromIntVector2(origin);
                rect.ExpandToInclude(origin + data.GetValue<IntVector2>("handle"));
                rect.right++;
                rect.top++; // match visuals
                this.height = rect.Height;
                this.width= rect.Width;
                this.area = rect.Area;
                this.heightFloat = 20f * height;
                this.widthFloat = 20f * width;

                Debug.LogError("Flaps created");
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

            private int Engine(int i) => i == 0 ? 0 : 1;
            private int Cog(int i) => i == 0 ? 2 : 3;
            private int LidBL(int i) => 4 + i;
            private int LidBR(int i) => 4 + height + i;
            private int LidB(int i, int j) => 4 + 2 * height + i * width + j;
            private int LidAL(int i) => 4 + 2 * height + area + i;
            private int LidAR(int i) => 4 + 3 * height + area + i;
            private int LidA(int i, int j) => 4 + 4 * height + area + i * width + j;
            private int BeamT(int i) => 4 + 4 * height + 2 * area + i;
            private int BeamB(int i) => 6 + 4 * height + 2 * area + i;
            private int Beam(int i, int j) => 8 + 4 * height + 2 * area + i * height + j;


            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                base.InitiateSprites(sLeaser, rCam);
                sLeaser.sprites = new FSprite[rect.Height * 6 + rect.Area * 2 + 8];
                FShader shader = this.room.game.rainWorld.Shaders["ColoredSprite2"];
                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[Engine(i)] = new FSprite("bkr_sidebeam", true) { shader = shader };
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
                for (int i = 0; i < height; i++)
                {
                    sLeaser.sprites[LidAL(i)] = new FSprite("bkr_lidA_left", true) { shader = shader };
                    sLeaser.sprites[LidAR(i)] = new FSprite("bkr_lidA_right", true) { shader = shader };
                    for (int j = 0; j < width; j++)
                    {
                        sLeaser.sprites[LidA(i, j)] = new FSprite("bkr_lidA_mid", true) { shader = shader };
                    }
                }
                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[BeamT(i)] = new FSprite("bkr_beam_top", true) { shader = shader };
                    sLeaser.sprites[BeamB(i)] = new FSprite("bkr_beam_bot", true) { shader = shader };
                    for (int j = 0; j < height; j++)
                    {
                        sLeaser.sprites[Beam(i, j)] = new FSprite("bkr_beam_mid", true) { shader = shader };
                    }
                }

                this.AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Foreground"));
                Debug.LogError("Flaps initiated, with " + sLeaser.sprites.Length + " sprites");
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                Vector2 start = new Vector2(rect.left * 20 - camPos.x, rect.bottom * 20 - camPos.y);
                //Debug.Log("rendering flaps at " + start);
                float factor = Mathf.Lerp(lastClosedFactor, closedFactor, timeStacker);
                float easedFactor = 
                      0.15f * Mathf.Pow(Mathf.InverseLerp(0, 0.2f, factor), 2f) 
                    + 0.7f * Mathf.InverseLerp(0.2f, 0.8f, factor) 
                    + 0.15f * Mathf.Pow(Mathf.InverseLerp(0.8f, 1, factor), 0.5f);
                float depth = data.GetValue<float>("dpt") / 30f;
                //Debug.Log("rendering flaps at " + depth);
                float depthStep = 1f / 30f;

                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[Engine(i)].x = start.x + (i == 0 ? -4f : widthFloat + 4f);
                    sLeaser.sprites[Engine(i)].y = start.y + 8;
                    sLeaser.sprites[Engine(i)].alpha = depth;
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
                    sLeaser.sprites[LidBL(i)].y = start.y + 5f + i * 20f + animatedLidY;
                    sLeaser.sprites[LidBL(i)].alpha = depth + animatedLidZ;
                    sLeaser.sprites[LidBR(i)].x = start.x + widthFloat + 5f;
                    sLeaser.sprites[LidBR(i)].y = start.y + 5f + i * 20f + animatedLidY;
                    sLeaser.sprites[LidBR(i)].alpha = depth + animatedLidZ;
                    for (int j = 0; j < width; j++)
                    {
                        sLeaser.sprites[LidB(i, j)].x = start.x + 10f + j * 20f;
                        sLeaser.sprites[LidB(i, j)].y = start.y + 5f + i * 20f + animatedLidY;
                        sLeaser.sprites[LidB(i, j)].alpha = depth + animatedLidZ;
                    }
                }
                depth += 2f * depthStep;
                for (int i = 0; i < height; i++)
                {
                    sLeaser.sprites[LidAL(i)].x = start.x - 5f;
                    sLeaser.sprites[LidAL(i)].y = start.y + 5f + i * 20f;
                    sLeaser.sprites[LidAL(i)].alpha = depth;
                    sLeaser.sprites[LidAR(i)].x = start.x + widthFloat + 5f;
                    sLeaser.sprites[LidAR(i)].y = start.y + 5f + i * 20f;
                    sLeaser.sprites[LidAR(i)].alpha = depth;
                    for (int j = 0; j < width; j++)
                    {
                        sLeaser.sprites[LidA(i, j)].x = start.x + 10f + j * 20f;
                        sLeaser.sprites[LidA(i, j)].y = start.y + 5f + i * 20f;
                        sLeaser.sprites[LidA(i, j)].alpha = depth;
                    }
                }
                depth += depthStep;
                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[BeamT(i)].x = start.x + (i == 0 ? -2f : widthFloat + 2f);
                    sLeaser.sprites[BeamT(i)].y = start.y + heightFloat + 5f;
                    sLeaser.sprites[BeamT(i)].alpha = depth;
                    sLeaser.sprites[BeamB(i)].x = start.x + (i == 0 ? -2f : widthFloat + 2f);
                    sLeaser.sprites[BeamB(i)].y = start.y - 5f;
                    sLeaser.sprites[BeamB(i)].alpha = depth;
                    for (int j = 0; j < height; j++)
                    {
                        sLeaser.sprites[Beam(i, j)].x = start.x + (i == 0 ? -2f : widthFloat + 2f);
                        sLeaser.sprites[Beam(i, j)].y = start.y + 10f + j * 20f;
                        sLeaser.sprites[Beam(i, j)].alpha = depth;
                    }
                }
            }
        }
    }
}