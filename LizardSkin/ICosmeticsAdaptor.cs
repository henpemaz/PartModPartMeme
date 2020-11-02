using UnityEngine;
using System.Collections.Generic;


namespace LizardSkin
{
    public interface ICosmeticsAdaptor
    {
        GraphicsModule graphics { get; }
        List<GenericCosmeticsTemplate> cosmetics { get;}
        float BodyAndTailLength { get; }
        float bodyLength { get;}
        float tailLength { get;}
        Color effectColor { get;}
        RoomPalette palette { get;}
        float showDominance { get;}

        int AddCosmetic(int spriteIndex, GenericCosmeticsTemplate cosmetic);
        void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette);
        void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam);
        void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos);
        void Reset();
        void Update();
        Color BodyColor(float y);
        Color HeadColor(float v);
        float HeadRotation(float timeStacker);
        LizardGraphics.LizardSpineData SpinePosition(float spineFactor, float timeStacker);
    }
}