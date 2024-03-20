using Noise;
using Procrain.Runtime.Noise;
using Procrain.Runtime.Utils.Threading;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;

namespace Procrain.Runtime.MapGeneration
{
    public static class HeightMapGenerator
    {
        public static HeightMap CreatePerlinNoiseHeightMap(PerlinNoiseParams np, AnimationCurve heightCurve = null)
        {
            var heigthMap = new HeightMap(PerlinNoise.BuildHeightMap(np), np.size + 1, np.seed);

            // Animation Curve
            if (heightCurve != null) heigthMap.ApplyHeightCurve(heightCurve);

            return heigthMap;
        }
    }


    public static class HeightMapGeneratorThreadSafe
    {
        #region PERLIN NOISE

        [BurstCompile]
        public struct PerlinNoiseMapBuilderJob : IJob
        {
            public PerlinNoiseParams noiseParams;
            public HeightMapThreadSafe heightMap;
            public SampledAnimationCurve heightCurve;

            public void Execute()
            {
                PerlinNoiseThreadSafe.BuildHeightMap(heightMap.map, noiseParams);

                if (!heightCurve.IsEmpty)
                    heightMap.ApplyHeightCurve(heightCurve);
            }
        }

        #endregion
    }
}