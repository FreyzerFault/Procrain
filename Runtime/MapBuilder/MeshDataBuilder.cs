using System;
using System.Collections;
using Procrain.Geometry;
using Procrain.Geometry.Mesh;
using Procrain.MapGeneration;
using Procrain.MapParameters;
using Unity.Jobs;
using UnityEngine;

namespace Procrain.MapBuilder
{
    /// <summary>
    /// Abstract base class for mesh data builders that generate mesh data from height maps
    /// </summary>
    public abstract class MeshDataBuilder: MapBuilder
    {
        /// Builds mesh data synchronously from a height map
        public abstract IMeshData Build(IHeightMap heightMap, ITerrainParams terrainParams);

        /// Builds mesh data synchronously from height map parameters
        public abstract IMeshData Build(IHeightMapParams heigthMapParams, ITerrainParams terrainParams);
        
        /// Builds mesh data asynchronously from a height map
        public abstract IEnumerator BuildCoroutine(IHeightMap heigthMap, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null);

        /// Builds mesh data asynchronously from height map parameters
        public abstract IEnumerator BuildCoroutine(IHeightMapParams heigthMapParams, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null);

        
        #region THREADING

        protected JobHandle meshJobHandle;
        
        /// <summary>
        /// Builds Mesh Data asynchronously using parallel processing from a thread-safe height map and Terrain parameters
        /// </summary>
        protected abstract IEnumerator Build_ParallelizedCoroutine(
            HeightMap_ThreadSafe heightMap, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null);

        /// <summary>
        /// Builds Mesh Data asynchronously using parallel processing from Height Map and Terrain parameters
        /// </summary>
        protected abstract IEnumerator Build_ParallelizedCoroutine(
            PerlinNoiseParams_ThreadSafe heightMapParams, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null);

        #endregion
    }
}
