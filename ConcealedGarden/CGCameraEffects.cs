using System;

namespace ConcealedGarden
{
    internal class CGCameraEffects
    {
        internal static void Apply()
        {
            On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
        }

        public static class EnumExt_CGWaterFallEffect
        {
#pragma warning disable 0649
            public static RoomSettings.RoomEffect.Type CGWaterFallEffect;
            public static RoomSettings.RoomEffect.Type CGSteamEffect;
            public static RoomSettings.RoomEffect.Type CGHeatEffect;
#pragma warning restore 0649
        }

        private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera self)
        {
            orig(self);
            if (self.room != null && self.fullScreenEffect == null)
            {
                if (self.room.roomSettings.GetEffectAmount(EnumExt_CGWaterFallEffect.CGWaterFallEffect) > 0f)
                {
                    self.SetUpFullScreenEffect("Foreground");
                    self.fullScreenEffect.shader = self.game.rainWorld.Shaders["WaterFall"];
                    self.lightBloomAlphaEffect = EnumExt_CGWaterFallEffect.CGWaterFallEffect;
                    self.lightBloomAlpha = self.room.roomSettings.GetEffectAmount(EnumExt_CGWaterFallEffect.CGWaterFallEffect);
                    self.fullScreenEffect.color = new UnityEngine.Color(self.lightBloomAlpha, 1f - self.lightBloomAlpha, 1f - self.lightBloomAlpha);
                } 
                else if (self.room.roomSettings.GetEffectAmount(EnumExt_CGWaterFallEffect.CGSteamEffect) > 0f)
                {
                    self.SetUpFullScreenEffect("Water");
                    self.fullScreenEffect.shader = self.game.rainWorld.Shaders["Steam"];
                    self.lightBloomAlphaEffect = EnumExt_CGWaterFallEffect.CGSteamEffect;
                    self.lightBloomAlpha = self.room.roomSettings.GetEffectAmount(EnumExt_CGWaterFallEffect.CGSteamEffect);
                } 
                else if (self.room.roomSettings.GetEffectAmount(EnumExt_CGWaterFallEffect.CGHeatEffect) > 0f)
                {
                    self.SetUpFullScreenEffect("Bloom");
                    self.fullScreenEffect.shader = self.game.rainWorld.Shaders["HeatDistortion"];
                    self.lightBloomAlphaEffect = EnumExt_CGWaterFallEffect.CGHeatEffect;
                    self.lightBloomAlpha = self.room.roomSettings.GetEffectAmount(EnumExt_CGWaterFallEffect.CGHeatEffect);
                }
            } 
        }
    }
}