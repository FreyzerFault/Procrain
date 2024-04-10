using DavidUtils;
using DavidUtils.ScriptableObjectsUtils;
using Procrain.Noise;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using DavidUtils.Editor;
#endif

namespace Procrain.MapGeneration
{
    [ExecuteAlways]
    [CreateAssetMenu(menuName = "Terrain Params", fileName = "Terrain Params")]
    public class TerrainSettingsSo : AutoUpdatableSoWithBackup<TerrainSettingsSo>
    {
        [SerializeField]
        private AnimationCurve heightCurve = AnimationCurveUtils.DefaultCurve();

        [SerializeField]
        private float heightMultiplier = 100;

        [SerializeField]
#if UNITY_EDITOR
        [PowerOfTwo(0, 4, true)]
#endif
        private int lod;

        [SerializeField]
        private PerlinNoiseParams noiseParams = PerlinNoiseParams.Default();

        public PerlinNoiseParams NoiseParams
        {
            get => noiseParams;
            set
            {
                noiseParams = value;
                NotifyUpdate();
            }
        }

        public AnimationCurve HeightCurve
        {
            get => heightCurve;
            set
            {
                heightCurve = value;
                NotifyUpdate();
            }
        }

        public float HeightMultiplier
        {
            get => heightMultiplier;
            set
            {
                heightMultiplier = value;
                NotifyUpdate();
            }
        }

        public int LOD
        {
            get => lod;
            set
            {
                lod = value;
                NotifyUpdate();
            }
        }

        public float2 Offset
        {
            get => noiseParams.offset;
            set
            {
                noiseParams.offset = value;
                NotifyUpdate();
            }
        }

        public uint Seed
        {
            get => noiseParams.seed;
            set
            {
                noiseParams.seed = value;
                NotifyUpdate();
            }
        }

        protected override void CopyValues(TerrainSettingsSo from, TerrainSettingsSo to)
        {
            to.noiseParams = from.noiseParams;
            to.heightCurve = from.heightCurve;
            to.heightMultiplier = from.heightMultiplier;
            to.lod = from.lod;
        }

        public void ResetSeed() => Seed = (uint)Random.Range(0, int.MaxValue);
    }
}
