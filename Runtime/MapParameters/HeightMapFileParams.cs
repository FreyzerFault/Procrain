using Procrain.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.MapParameters
{
    [CreateAssetMenu(menuName = "Map Generation", fileName = "Procrain/HeightMap File Params")]
    public class HeightMapFileParams: ScriptableObject, IHeightMapParams
    {
        public string filePath;
        
        [SerializeField] private AnimationCurve heightCurve;

        public AnimationCurve HeightCurve => heightCurve;
        public SampledAnimationCurve SampledHeightCurve => new(heightCurve);

        public int Size => 1;
        public float Scale => 1;
        public float2 Offset => float2.zero;
    }
}
