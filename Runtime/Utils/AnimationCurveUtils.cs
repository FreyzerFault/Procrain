using UnityEngine;

namespace Procrain.Runtime.Utils
{
    public static class AnimationCurveUtils
    {
        public static AnimationCurve CopyCurve(AnimationCurve curve) => new(curve.keys);

        public static AnimationCurve DefaultCurve() => new(new Keyframe(0, 0), new Keyframe(1, 1));
    }
}