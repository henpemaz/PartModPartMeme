using UnityEngine;
using System.Collections.Generic;


namespace LizardSkin
{
    public interface ICosmeticsAdaptor
    {
        // Tightly paired with GenericCosmeticsAdaptor... Helps me keep track of what needs new implementations

        PhysicalObject owner { get; }
        GraphicsModule graphics { get; }

        List<GenericCosmeticTemplate> cosmetics { get;}
        float BodyAndTailLength { get; }
        float bodyLength { get;}
        float tailLength { get;}
        Color effectColor { get;}
        RoomPalette palette { get;}
        int firstSprite { get;}

        /// <summary>
        /// float [0,1] Makes cosmetics shiver
        /// </summary>
        float showDominance { get; }
        /// <summary>
        /// float [-1,1] Body rotation
        /// </summary>
        float depthRotation { get; }
        float headDepthRotation { get; }
        float lastDepthRotation { get; }
        float lastHeadDepthRotation { get; }

        BodyPart head { get; }
        BodyPart baseOfTail { get; }
        // Hmmmm this one is not ideal but whatever
        BodyChunk mainBodyChunk { get; }


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