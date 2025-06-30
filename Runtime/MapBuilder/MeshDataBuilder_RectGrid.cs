using System;
using System.Collections;
using JetBrains.Annotations;
using Procrain.Geometry;
using Procrain.Geometry.Mesh;
using Procrain.MapGeneration;
using Procrain.MapParameters;
using Procrain.Utils;
using UnityEngine;

namespace Procrain.MapBuilder
{
    public class MeshDataBuilder_RectGrid: MeshDataBuilder
    {
        /// Construye el MeshData a partir de un HeightMap preconstruido
        public override IMeshData Build(IHeightMap heightMap, ITerrainParams terrainParams) => 
            MeshGenerator.BuildMeshData(heightMap, terrainParams, null);

        /// Construye el MeshData calculando el PerlinNoise sobre la marcha
        public override IMeshData Build(IHeightMapParams heigthMapParams, ITerrainParams terrainParams) => 
            MeshGenerator.BuildMeshData(heigthMapParams, terrainParams, null);


        /// Construye el MeshData a partir de un HeightMap preconstruido
        public override IEnumerator BuildCoroutine(IHeightMap heigthMap, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            yield return BuildCoroutineInternal(heigthMap, terrainParams,
                np => ((HeightMap)np).ToThreadSafe(),
                Build_ParallelizedCoroutine, MeshGenerator.BuildMeshData,
                onStart, onEnd);
        }
        
        /// Construye el MeshData calculando el PerlinNoise sobre la marcha
        public override IEnumerator BuildCoroutine(IHeightMapParams heigthMapParams, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            yield return BuildCoroutineInternal(heigthMapParams, terrainParams,
                np => ((PerlinNoiseParams)np).ToThreadSafe(),
                Build_ParallelizedCoroutine, MeshGenerator.BuildMeshData,
                onStart, onEnd);
        }
        
        /// Construye el MeshData a partir de un HeightMap o HeightMapParams como tipo T.
        /// Usa TerrainParams para el escalado y el LOD
        /// Permite que se ejecute en paralelo o no segun el valor de paralelized
        private IEnumerator BuildCoroutineInternal<T, TSafe>(
            T heightMapInput, ITerrainParams terrainParams, Func<T, TSafe> toThreadSafe,
            Func<TSafe, ITerrainParams, Action, Action<IMeshData>, IEnumerator> buildParallelized,
            Func<T, ITerrainParams, Gradient, IMeshData> buildMeshData,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            if (paralelized)
            {
                // Convierte ambos parametros en sus versiones ThreadSafe
                TSafe threadSafeInput = heightMapInput is TSafe ts ? ts : toThreadSafe(heightMapInput);
                TerrainParams_ThreadSafe tp_ts = terrainParams is TerrainParams_ThreadSafe tpts 
                    ? tpts 
                    : ((TerrainParams)terrainParams).ToThreadSafe();

                yield return buildParallelized(threadSafeInput, tp_ts, onStart, onEnd);
            }
            else
            {
                onStart?.Invoke();
                IMeshData meshData = buildMeshData(heightMapInput, terrainParams, null);
                onEnd?.Invoke(meshData);
            }
        }


        
        #region THREADING

        /// Construcción del MeshData a partir de un HeightMap preconstruido
        protected override IEnumerator Build_ParallelizedCoroutine(
            HeightMap_ThreadSafe heightMap, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            MeshGeneratorThreadSafe.GenerateMeshDataJob job = new()
            {
                prebuiltHeightMap = heightMap,
                terrainParams = terrainParams is TerrainParams_ThreadSafe tpts ? tpts : ((TerrainParams)terrainParams).ToThreadSafe(),
                // TODO Pasarle un gradiente para los VertexColor
            };

            yield return ExecuteBuildJobCoroutine(job, heightMap.size, terrainParams.LOD, onStart, onEnd);
        }

        /// Construcción del MeshData calculando el PerlinNoise sobre la marcha
        protected override IEnumerator Build_ParallelizedCoroutine(
            PerlinNoiseParams_ThreadSafe heightMapParams, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            MeshGeneratorThreadSafe.GenerateMeshDataJob job = new()
            {
                noiseParams = heightMapParams,
                terrainParams = terrainParams is TerrainParams_ThreadSafe tpts ? tpts : ((TerrainParams)terrainParams).ToThreadSafe(),
                // TODO Pasarle un gradiente para los VertexColor
            };
            
            yield return ExecuteBuildJobCoroutine(job, heightMapParams.size, terrainParams.LOD, onStart, onEnd);
        }
        
        /// Crea el MeshData y ejecuta el Job para construirlo
        private IEnumerator ExecuteBuildJobCoroutine(
            MeshGeneratorThreadSafe.GenerateMeshDataJob job,
            int size, int lod, Action onStart = null, Action<IMeshData> onEnd = null)
        {
            MeshData_ThreadSafe meshData = new();
            job.meshData = meshData;
    
            meshJobHandle = meshJobHandle.ScheduleJob(job);
    
            onStart?.Invoke();
    
            yield return meshJobHandle.WaitForJobToEnd(debug, 
                $"Generación de Malla {size} x {size}, LoD {lod}");
    
            onEnd?.Invoke(meshData);
        }

        
        #endregion
    }
}
