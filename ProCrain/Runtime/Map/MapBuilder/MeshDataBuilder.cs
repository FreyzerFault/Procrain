using System;
using System.Collections;
using Procrain.Mesh;
using Unity.Jobs;
using UnityEngine;

namespace Procrain
{
    /// <summary>
    /// Abstract base class for mesh data builders that generate mesh data from height maps
    /// </summary>
    public abstract class MeshDataBuilder: MapBuilder
    {
        public IMeshData Build(HeightMap heightMap, ITerrainParams terrainParams) => Build(heightMap.ToThreadSafe(), terrainParams);
        public IMeshData Build(PerlinNoiseParams heightMapParams, ITerrainParams terrainParams) => Build(heightMapParams.ToThreadSafe(), terrainParams);
        
        public abstract IMeshData Build(HeightMap_ThreadSafe heightMap, ITerrainParams terrainParams);

        public abstract IMeshData Build(PerlinNoiseParams_ThreadSafe heigthMapParams, ITerrainParams terrainParams);
        
        public abstract IEnumerator BuildCoroutine(HeightMap_ThreadSafe heigthMap, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null);
        
        public IEnumerator BuildCoroutine(HeightMap heigthMap, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null) => BuildCoroutine(heigthMap.ToThreadSafe(), terrainParams, onStart, onEnd);

        public abstract IEnumerator BuildCoroutine(PerlinNoiseParams_ThreadSafe heigthMapParams, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null);

        public IEnumerator BuildCoroutine(PerlinNoiseParams noiseParams, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null) => BuildCoroutine(noiseParams.ToThreadSafe(), terrainParams, onStart, onEnd);
        
        #region THREADING

        protected JobHandle meshJobHandle;

        protected abstract IEnumerator Build_ParallelizedCoroutine(
            HeightMap_ThreadSafe heightMap, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null);

        protected abstract IEnumerator Build_ParallelizedCoroutine(
            PerlinNoiseParams_ThreadSafe heightMapParams, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null);

        #endregion
    }
}
