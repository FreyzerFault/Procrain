using Procrain.Editor.Utils;
using Procrain.Runtime.Noise;
using Procrain.Runtime.Utils;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Procrain.Runtime.MapGeneration
{
    [ExecuteAlways]
    [CreateAssetMenu(menuName = "Terrain Params", fileName = "Terrain Params")]
    public class TerrainSettingsSo : AutoUpdatableSoWithBackup<TerrainSettingsSo>
    {
        [SerializeField] private PerlinNoiseParams noiseParams = PerlinNoiseParams.Default();
        [SerializeField] private AnimationCurve heightCurve = AnimationCurveUtils.DefaultCurve();
        [SerializeField] private float heightMultiplier = 100;

#if UNITY_EDITOR
        [SerializeField] [PowerOfTwo(0, 4, true)]
#endif
        private int lod;

        public PerlinNoiseParams NoiseParams
        {
            get => noiseParams;
            set
            {
                noiseParams = value;
                OnUpdateValues();
            }
        }

        public AnimationCurve HeightCurve
        {
            get => heightCurve;
            set
            {
                heightCurve = value;
                OnUpdateValues();
            }
        }

        public float HeightMultiplier
        {
            get => heightMultiplier;
            set
            {
                heightMultiplier = value;
                OnUpdateValues();
            }
        }

        public int LOD
        {
            get => lod;
            set
            {
                lod = value;
                OnUpdateValues();
            }
        }

        public float2 Offset
        {
            get => noiseParams.offset;
            set
            {
                noiseParams.offset = value;
                OnUpdateValues();
            }
        }

        public uint Seed
        {
            get => noiseParams.seed;
            set
            {
                noiseParams.seed = value;
                OnUpdateValues();
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