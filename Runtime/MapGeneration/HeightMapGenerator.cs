using DavidUtils.Threading;
using Procrain.Noise;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

namespace Procrain.MapGeneration
{
    public static class HeightMapGenerator
    {
        public static HeightMap CreatePerlinNoiseHeightMap(
            PerlinNoiseParams np,
            AnimationCurve heightCurve = null
        )
        {
            var heigthMap = new HeightMap(PerlinNoise.BuildHeightMap(np), np.Size + 1, np.Seed);

            // Animation Curve
            if (heightCurve != null) heigthMap.ApplyHeightCurve(heightCurve);

            return heigthMap;
        }
    }

    public static class HeightMapGenerator_ThreadSafe
    {
        #region PERLIN NOISE

        [BurstCompile]
        public struct PerlinNoiseMapBuilderJob : IJob
        {
            public PerlinNoiseParams_ThreadSafe noiseParams;
            public HeightMap_ThreadSafe heightMap;
            public SampledAnimationCurve heightCurve;

            public void Execute()
            {
                PerlinNoise_ThreadSafe.BuildHeightMap(heightMap.map, noiseParams);

                if (!heightCurve.IsEmpty) heightMap.ApplyHeightCurve(heightCurve);
            }
        }

        #endregion
    }
}
