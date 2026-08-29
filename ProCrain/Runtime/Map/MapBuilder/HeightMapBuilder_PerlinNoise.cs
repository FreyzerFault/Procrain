using System;
using System.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Procrain
{
    /// <summary>
    /// Builds height maps using Perlin noise
    /// </summary>
    [CreateAssetMenu(fileName = "HM Perlin Noise Builder", menuName = "Procrain/Builders/HeightMap Perlin Noise Builder")]
    public class HeightMapBuilder_PerlinNoise : HeightMapBuilder
    {
        /// <summary>
        /// Builds a Perlin Noise Map synchronously using the given Perlin Noise Parameters and a Height Curve
        /// </summary>
        /// <param name="perlinNoiseParams">The Perlin noise generation parameters</param>
        /// <param name="heightCurve">Optional height curve to modify the noise values</param>
        /// <returns>The generated Perlin Noise Map</returns>
        /// <exception cref="ArgumentException">Thrown if parameters are not PerlinNoiseParams</exception>
        public override HeightMap Build(PerlinNoiseParams perlinNoiseParams)
        {
            float[] map = PerlinNoise.BuildHeightMap(perlinNoiseParams, perlinNoiseParams.HeightCurve != null ? perlinNoiseParams.HeightCurve.Evaluate : null);
            return new HeightMap(map, perlinNoiseParams.Size + 1, perlinNoiseParams.Seed);
        }

        /// <summary>
        /// Builds a Perlin Noise Map asynchronously using coroutines 
        /// </summary>
        /// <param name="heightParams">The Perlin Noise generation parameters</param>
        /// <param name="heightCurve">Optional height curve to modify values</param>
        /// <param name="onStart">Callback when generation starts</param>
        /// <param name="onEnd">Callback when generation completes with the result</param>
        /// <exception cref="ArgumentException">Thrown if parameters are not PerlinNoiseParams</exception>
        public override IEnumerator BuildCoroutine(
            PerlinNoiseParams heightParams, Action onStart = null, Action<IHeightMap> onEnd = null)
        {
            if (heightParams is not PerlinNoiseParams np)
                throw new ArgumentException("Los parámetros deben ser de tipo PerlinNoiseParams", nameof(heightParams));

            if (paralelized)
                yield return BuildHeightMap_ParallelizedCoroutine(heightParams.ToThreadSafe(), onStart, onEnd);
            else
            {
                onStart?.Invoke();
                float[] map = PerlinNoise.BuildHeightMap(np, np.HeightCurve != null ? np.HeightCurve.Evaluate : null);
                onEnd?.Invoke(new HeightMap(map, np.Size + 1, np.Seed));
            }
        }

        /// Handle for the running job
        private JobHandle _buildheightMapJobHandle;
        private JobHandle _applyHeightCurveJobHandle;
        
        /// <summary>
        /// Internal method that handles parallelized height map generation using Jobs
        /// </summary>
        protected override IEnumerator BuildHeightMap_ParallelizedCoroutine(
            PerlinNoiseParams_ThreadSafe parameters, Action onStart = null, Action<IHeightMap> onEnd = null)
        {
            float time = Time.time;

            int sampleSize = parameters.SampleSize;
            uint seed = parameters.seed;

            // Initialize HeightMapThreadSafe
            HeightMap_ThreadSafe heightMap = new(sampleSize, seed);
            
            // If last Job didn't end, wait for it
            if (!_buildheightMapJobHandle.IsCompleted)
                yield return new WaitUntil(() => 
                    _buildheightMapJobHandle.IsCompleted && _applyHeightCurveJobHandle.IsCompleted);

            // Wait for JobHandle to END
            JobHandle fullHandle = _buildheightMapJobHandle =
                new HeightMap_ThreadSafe.PerlinNoiseMapBuilderJob
                {
                    noiseParams = parameters,
                    heightMap = heightMap,
                }.Schedule();
            
            // Add the Apply Animation Curve Job to the FullJobHandle pipeline
            if (!parameters.heightCurve.IsEmpty)
            {
                fullHandle = _applyHeightCurveJobHandle =
                    new HeightMap_ThreadSafe.ApplyAnimationCurveJob
                    {
                        heightCurve = parameters.heightCurve,
                        heightMap = heightMap
                    }.Schedule(_buildheightMapJobHandle);
            }
            
            onStart?.Invoke();
            
            yield return new WaitUntil(() => fullHandle.IsCompleted);

            // MAP GENERATED!!!
            fullHandle.Complete();
            onEnd?.Invoke(heightMap);

            if (debug) Debug.Log($"{(Time.time - time) * 1000:F1} ms para generar el mapa");
        }
    }
}
