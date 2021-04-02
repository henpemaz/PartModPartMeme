using Menu;
using System.Collections.Generic;
using UnityEngine;

namespace LizardSkin
{
    public class MenuCosmeticsAdaptor : OptionalUI.OpContainer, ICosmeticsAdaptor
    {
        public RainWorld rainWorld => menu.manager.rainWorld;

        public MenuCosmeticsAdaptor(Vector2 pos) : base(pos)
        {

            if (_init)
            {
                this.cosmetics = new List<GenericCosmeticTemplate>();

                UnityEngine.Random.seed = 1337;
                this.AddCosmetic(new GenericTailTuft(this));
                this.AddCosmetic(new GenericTailTuft(this));
                this.AddCosmetic(new GenericLongHeadScales(this));
                this.AddCosmetic(new GenericAntennae(this));

                this.leaserAdaptor = new LeaserAdaptor(this.firstSprite + totalSprites);

                this.cameraAdaptor = new CameraAdaptor(this.myContainer);
                this.paletteAdaptor = new PaletteAdaptor();
                this.palette = paletteAdaptor;

                for (int j = 0; j < this.cosmetics.Count; j++)
                {
                    this.cosmetics[j].InitiateSprites(leaserAdaptor, cameraAdaptor);
                    this.cosmetics[j].ApplyPalette(leaserAdaptor, cameraAdaptor, paletteAdaptor);
                    this.cosmetics[j].AddToContainer(leaserAdaptor, cameraAdaptor, cameraAdaptor.ReturnFContainer(null));
                }

                this.Show();
            }
        }

        public override void Update(float dt)
        {
            base.Update(dt);
            
            for (int l = 0; l < this.cosmetics.Count; l++)
            {
                this.cosmetics[l].Update();
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            for (int j = 0; j < this.cosmetics.Count; j++)
            {
                this.cosmetics[j].DrawSprites(leaserAdaptor, cameraAdaptor, 1f, Vector2.zero);
            }
        }

        public List<GenericCosmeticTemplate> cosmetics { get; protected set; }

        public float BodyAndTailLength => 80f;

        public float bodyLength => 40f;

        public float tailLength => 40f;

        public Color effectColor => new Color(0.05f, 0.87f, 0.92f);

        public PaletteAdaptor palette { get; protected set; }

        public int firstSprite { get; protected set; }

        public Vector2 headPos => Vector2.zero;

        public Vector2 headLastPos => Vector2.zero;

        public Vector2 baseOfTailPos => new Vector2(0f, -40f);

        public Vector2 baseOfTailLastPos => new Vector2(0f, -40f);

        internal Vector2 tipOfTail => new Vector2(0f, -80f);

        public Vector2 mainBodyChunkPos => headPos;

        public Vector2 mainBodyChunkLastPos => headPos;

        public Vector2 mainBodyChunkVel => Vector2.zero;

        public float showDominance => 0f;

        public float depthRotation => 0f;

        public float headDepthRotation => 0f;

        public float lastDepthRotation => 0f;

        public float lastHeadDepthRotation => 0f;


        private int totalSprites = 0;
        private LeaserAdaptor leaserAdaptor;
        private CameraAdaptor cameraAdaptor;
        private PaletteAdaptor paletteAdaptor;

        public void AddCosmetic(GenericCosmeticTemplate cosmetic)
        {
            this.cosmetics.Add(cosmetic);
            cosmetic.startSprite = this.totalSprites;
            this.totalSprites += cosmetic.numberOfSprites;
        }

        public Color BodyColor(float y)
        {
            return Color.white;
        }

        public Color HeadColor(float v)
        {
            return Color.white;
        }

        public float HeadRotation(float timeStacker)
        {
            return 0f;
        }

        public bool PointSubmerged(Vector2 pos)
        {
            return false;
        }

        public SpineData SpinePosition(float spineFactor, float timeStacker)
        {
            Vector2 pos = Vector2.Lerp(headPos, tipOfTail, spineFactor);
            float rad = RWCustom.Custom.LerpMap(spineFactor, 0.5f, 1f, 10f, 1f);
            Vector2 normalized = new Vector2(0f, 1f);
            Vector2 perp = new Vector2(1f, 0f);

            return new SpineData(spineFactor, pos, pos + perp*rad, normalized, perp, 0f, rad);
        }
    }
}