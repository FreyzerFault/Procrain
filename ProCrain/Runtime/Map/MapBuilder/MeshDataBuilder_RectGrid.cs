using System;
using System.Collections;
using Procrain.Mesh;
using UnityEngine;

namespace Procrain
{
    [CreateAssetMenu(fileName = "Rect Grid Mesh Builder", menuName = "Procrain/Builders/Rect Grid Mesh Data Builder")]
    public class MeshDataBuilder_RectGrid: MeshDataBuilder
    {
        public override IMeshData Build(HeightMap_ThreadSafe heightMap, ITerrainParams terrainParams) => 
            MeshGenerator.BuildMeshData(heightMap, terrainParams, null);

        public override IMeshData Build(PerlinNoiseParams_ThreadSafe heigthMapParams, ITerrainParams terrainParams) => 
            MeshGenerator.BuildMeshData(heigthMapParams, terrainParams, null);

        public override IEnumerator BuildCoroutine(HeightMap_ThreadSafe heigthMap, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            if (paralelized)
            {
                // Convierte ambos parametros en sus versiones ThreadSafe
                yield return Build_ParallelizedCoroutine(heigthMap, terrainParams, onStart, onEnd);
            }
            else
            {
                onStart?.Invoke();
                IMeshData meshData = Build(heigthMap, terrainParams);
                onEnd?.Invoke(meshData);
            }
        }
        
        public override IEnumerator BuildCoroutine(PerlinNoiseParams_ThreadSafe heigthMapParams, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            
            if (paralelized)
            {
                // Convierte ambos parametros en sus versiones ThreadSafe
                yield return Build_ParallelizedCoroutine(heigthMapParams, terrainParams, onStart, onEnd);
            }
            else
            {
                onStart?.Invoke();
                IMeshData meshData = Build(heigthMapParams, terrainParams);
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
