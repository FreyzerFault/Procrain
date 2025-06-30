using Procrain.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.MapParameters
{
    public interface IHeightMapParams
    {
        public AnimationCurve HeightCurve { get; }
        public SampledAnimationCurve SampledHeightCurve { get; }
        public int Size { get; }
        public float Scale { get; }
        public float2 Offset { get; }
    }
}
