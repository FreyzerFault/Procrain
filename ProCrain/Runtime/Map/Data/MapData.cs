using System;
using System.Collections;
using System.Linq;
using Procrain.Mesh;
using UnityEngine;
using UnityEngine.Serialization;

namespace Procrain
{
    /// Guarda los Parámetros, Builders y Datos para construir un Mapa (HeightMap, Textura y Mesh)
    /// Permite compartir el mapa generado entre varios elementos de visualización
    [CreateAssetMenu(fileName = "Map Data", menuName = "Procrain/Create Data", order = 0)]
    public class MapData: AutoUpdatableSo
    {
        [Header("Generation Parameters")]
        public PerlinNoiseParams heightMapParams;
        public TextureParams texParams;
        public TerrainParams terrainParams;
        
        [Header("Builders")]
        public HeightMapBuilder heightMapBuilder;
        public TextureBuilder textureBuilder;
        public MeshDataBuilder meshDataBuilder;
        
        [Header("Data")]
        public HeightMap heightMap;
        public Texture2D texture;
        private IMeshData[] _meshData = new IMeshData[5];
        public IMeshData[] MeshData_AllLods => _meshData;
        
        public IMeshData MeshData
        {
            get => _meshData[terrainParams.LODIndex];
            set => _meshData[terrainParams.LODIndex] = value;
        }

        public IMeshData MeshDataByLOD(int lodIndex) => _meshData[lodIndex];
        public IMeshData MeshDataByLOD_Pow2(int lod) => _meshData[lod];

        public event Action<IHeightMap> OnHeightMapUpdated;
        public event Action<Texture2D> OnTextureUpdated;
        public event Action<IMeshData> OnMeshDataUpdated;

        public UnityEngine.Mesh Mesh => MeshData.CreateMesh();
        public UnityEngine.Mesh ApplyToMesh(UnityEngine.Mesh mesh) => MeshData.ApplyMesh(mesh);
        
        public UnityEngine.Mesh MeshByLOD(int lodIndex) => MeshDataByLOD(lodIndex).CreateMesh();
        public UnityEngine.Mesh ApplyToMeshByLOD(UnityEngine.Mesh mesh, int lodIndex) => MeshDataByLOD(lodIndex).ApplyMesh(mesh);
        public UnityEngine.Mesh ApplyToMeshByLOD_Pow2(UnityEngine.Mesh mesh, int lod) => MeshDataByLOD_Pow2(lod).ApplyMesh(mesh);

        
        #region EVENTS

        public bool autoUpdateOnParameterChanges = true;
        public bool isSubscribed;

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (!autoUpdateOnParameterChanges && !isSubscribed) return;
            heightMapParams.OnValuesUpdated += BuildHeightMap; 
            texParams.OnValuesUpdated += BuildTexture_SampleCachedHeightMap;
            terrainParams.OnValuesUpdated += BuildMeshData_SampleCachedHeightMap;
            isSubscribed = true;
        }
        
        private void Unsubscribe()
        {
            if (!autoUpdateOnParameterChanges && isSubscribed) return;
            heightMapParams.OnValuesUpdated -= BuildHeightMap;
            texParams.OnValuesUpdated -= BuildTexture_SampleCachedHeightMap;
            terrainParams.OnValuesUpdated -= BuildMeshData_SampleCachedHeightMap;
            isSubscribed = false;
        }

        private void OnValidate()
        {
            bool shouldBeSubscribed = autoUpdateOnParameterChanges;
             
            // Check if auto-update subscription state needs to change using XOR operator:
            // shouldBeSubscribed ^ isSubscribed is true when states are different
            if (shouldBeSubscribed ^ isSubscribed)
            {
                if (shouldBeSubscribed)
                    Subscribe();
                else
                    Unsubscribe();
            }
        }

        #endregion


        #region BUILDING

        public void BuildHeightMap()
        {
            if (!heightMapBuilder || !heightMapParams) return;
            heightMap = heightMapBuilder.Build(heightMapParams);
            OnHeightMapUpdated?.Invoke(heightMap);
        }

        public void BuildTexture()
        {
            if (!textureBuilder || !heightMapParams || !texParams) return;
            texture = textureBuilder.Build(heightMapParams, texParams);
            OnTextureUpdated?.Invoke(texture);
        }
        
        public void BuildMeshData(int lod = -1)
        {
            if (!meshDataBuilder || !heightMapParams || !terrainParams) return;
            MeshData = meshDataBuilder.Build(heightMapParams, terrainParams);
            OnMeshDataUpdated?.Invoke(MeshData);
        }

        public void BuildMeshData(int[] lods)
        {
            if (lods is {Length: 0} || !meshDataBuilder || !heightMapParams || !terrainParams) return;
            _meshData = lods.Select(lod => meshDataBuilder?.Build(heightMapParams, terrainParams)).ToArray();
            OnMeshDataUpdated?.Invoke(MeshData);
        }


        /// Using the HeightMap built to sample it and process to Texture
        public void BuildTexture_SampleCachedHeightMap()
        {
            if (!textureBuilder || !texParams) return;
            
            if (heightMap == null)
                BuildHeightMap();
            
            texture = textureBuilder.Build(heightMap, texParams);
            OnTextureUpdated?.Invoke(texture);
        }
        
        /// Using the HeightMap built to sample it and process to Mesh
        public void BuildMeshData_SampleCachedHeightMap()
        {
            if (!meshDataBuilder || !terrainParams) return;
            
            if (heightMap == null)
                BuildHeightMap();
            
            MeshData = meshDataBuilder.Build(heightMap, terrainParams);
            OnMeshDataUpdated?.Invoke(MeshData);
        }

        public void BuildMeshData_SampleCachedHeightMap(int[] lods)
        {
            if (lods is {Length: 0} || !meshDataBuilder || !terrainParams) return;
            
            if (heightMap == null)
                BuildHeightMap();
            
            _meshData = lods.Select(lod => meshDataBuilder.Build(heightMap, terrainParams)).ToArray();
            OnMeshDataUpdated?.Invoke(MeshData);
        }


        public void BuildAllData()
        {
            BuildHeightMap();
            BuildTexture_SampleCachedHeightMap();
            BuildMeshData_SampleCachedHeightMap();
        }

        public IEnumerator BuildAllDataCoroutine()
        {
            if (!heightMapBuilder || !heightMapParams) yield break;
            
            yield return heightMapBuilder?.BuildCoroutine(heightMapParams, onEnd: hm => heightMap = hm as HeightMap);
            OnHeightMapUpdated?.Invoke(heightMap);
            yield return meshDataBuilder?.BuildCoroutine(heightMap, terrainParams, onEnd: md => MeshData = md);
            OnMeshDataUpdated?.Invoke(MeshData);
            yield return textureBuilder?.BuildCoroutine(heightMap, texParams, onEnd: t => texture = t);
            OnTextureUpdated?.Invoke(texture);
        }

        #endregion
    }
}
