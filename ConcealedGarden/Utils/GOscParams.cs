using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static ConcealedGarden.Utils.CGUtils;

namespace ConcealedGarden.Utils
{
    internal struct GOscParams
    {
        public readonly float amp;
        public readonly float frq;
        public readonly float phase;
        public Func<float, float> oscm { get => _oscm ?? UnityEngine.Mathf.Sin; set { _oscm = value ?? UnityEngine.Mathf.Sin; } }
        private Func<float, float> _oscm;
        public float GetRes(float t) => oscm((phase + t) * frq) * amp;
        public GOscParams(float amp, float frq, float phase, Func<float, float> oscm)
        {
            this.amp = amp;
            this.frq = frq;
            this.phase = phase;
            this._oscm = oscm;
        }
        public GOscParams Deviate(in GOscParams fluke)
        {
            return new GOscParams(ClampedFloatDeviation(this.amp, fluke.amp),
                ClampedFloatDeviation(this.frq, fluke.frq),
                ClampedFloatDeviation(phase, fluke.phase), (UnityEngine.Random.value < 0.5) ? this.oscm : fluke.oscm);
        }
    }
}
