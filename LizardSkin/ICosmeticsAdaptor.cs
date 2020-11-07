using UnityEngine;
using System.Collections.Generic;


namespace LizardSkin
{
    public interface ICosmeticsAdaptor
    {
        // Tightly paired with GenericCosmeticsAdaptor... 
        // This is not ideal but lowers the amount of changes inside cosmetics code by a lot


        PhysicalObject owner { get; }
        // I hate this one in particular
        GraphicsModule graphics { get; }

        List<GenericCosmeticTemplate> cosmetics { get;}
        float BodyAndTailLength { get; }
        float bodyLength { get;}
        float tailLength { get;}
        Color effectColor { get;}
        RoomPalette palette { get;}
        int firstSprite { get;}

        float showDominance { get; }
        float depthRotation { get; }
        float headDepthRotation { get; }
        float lastDepthRotation { get; }
        float lastHeadDepthRotation { get; }

        BodyPart head { get; }
        BodyChunk mainBodyChunk { get; }
        BodyPart baseOfTail { get; }


        CosmeticsParams cosmeticsParams { get; }

        void AddCosmetic(GenericCosmeticTemplate cosmetic);
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