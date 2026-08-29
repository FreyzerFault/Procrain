using System;
using System.Collections;

namespace Procrain
{
    /// <summary>
    /// Delegate Pattern to construct some Map Data (HeightMap, Mesh, Texture...)
    /// Base abstract class for building height maps using different generation algorithms
    /// </summary>
    public abstract class HeightMapBuilder: MapBuilder 
    {
        /// <summary>
        /// Builds a height map synchronously using the provided parameters and optional height curve
        /// </summary>
        /// <param name="perlinNoiseParams">Parameters for height map generation</param>
        /// <param name="heightCurve">Optional animation curve to modify height values</param>
        /// <returns>Generated height map</returns>
        public abstract HeightMap Build(PerlinNoiseParams perlinNoiseParams);

        /// <summary>
        /// Builds a height map asynchronously using provided parameters 
        /// </summary>
        /// <param name="heightParams">Parameters for height map generation</param>
        /// <param name="heightCurve">Optional animation curve to modify height values</param>
        /// <param name="onStart">Callback invoked when generation starts</param>
        /// <param name="onEnd">Callback invoked with result when generation completes</param>
        public abstract IEnumerator BuildCoroutine(
            PerlinNoiseParams heightParams, Action onStart = null, Action<IHeightMap> onEnd = null);

        /// <summary>
        /// Internal coroutine for parallelized height map generation
        /// </summary>
        /// <param name="parameters">Parameters for height map generation</param>
        /// <param name="heightCurve">Thread-safe sampled animation curve</param>
        /// <param name="onStart">Callback invoked when generation starts</param>
        /// <param name="onEnd">Callback invoked with result when generation completes</param>
        protected abstract IEnumerator BuildHeightMap_ParallelizedCoroutine(
            PerlinNoiseParams_ThreadSafe parameters, Action onStart = null, Action<IHeightMap> onEnd = null);
    }
}
