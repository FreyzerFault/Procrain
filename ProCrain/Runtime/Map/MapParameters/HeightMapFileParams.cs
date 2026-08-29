using UnityEngine;

namespace Procrain
{
    [CreateAssetMenu(fileName = "HeightMap File Params", menuName = "Procrain/HeightMap File Params")]
    public class HeightMapFileParams: ScriptableObject
    {
        public string filePath;
        
        [SerializeField] private AnimationCurve heightCurve;

        public AnimationCurve HeightCurve => heightCurve;
        public SampledAnimationCurve SampledHeightCurve => new(heightCurve);
    }
}
