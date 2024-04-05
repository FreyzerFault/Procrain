using System.Collections;
using DavidUtils.DebugUtils;
using Procrain.MapGeneration.Mesh;
using Unity.Jobs;
using UnityEngine;

namespace Procrain.MapDisplay
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MapDisplayInMesh : MapDisplayInTexture
    {
        public enum TextureMode
        {
            SetTexture,
            UseShader
        }

        [Space] public TextureMode textureMode = TextureMode.UseShader;

        private MeshCollider meshCollider;
        protected IMeshData meshData;
        private MeshFilter meshFilter;

        private IMeshData MeshData => paralelized ? meshDataThreadSafe : meshData;

        protected virtual void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            textureRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            meshDataThreadSafe.Dispose();
        }

        public override void BuildMap()
        {
            if (terrainSettingsSo == null) return;
            if (paralelized)
            {
                StartCoroutine(BuildMapParallelizedCoroutine());
            }
            else
            {
                var size = NoiseParams.size;
                DebugTimer.DebugTime(BuildHeightMap, $"Time to build HeightMap {size} x {size}");
                if (textureMode == TextureMode.SetTexture)
                    DebugTimer.DebugTime(
                        BuildTextureData,
                        $"Time to build TextureData {size} x {size}"
                    );

                DebugTimer.DebugTime(
                    BuildMeshData,
                    $"Time to build MeshData {NoiseParams.SampleSize} x {NoiseParams.SampleSize}"
                );
                DebugTimer.DebugTime(DisplayMap, "Time to display map");
            }
        }

        protected virtual void BuildMeshData() =>
            meshData = MeshGenerator.BuildMeshData(Map, LOD, HeightMultiplier);

        #region LoD

        protected void UpdateLOD(int newLod)
        {
            if (newLod == LOD) return;
            BuildMeshData(); // Esto es paralelizable
            UpdateMesh(); // Esto no
        }

        #endregion

        #region DISPLAY

        public override void DisplayMap()
        {
            if (textureMode == TextureMode.SetTexture) SetTexture();

            UpdateMesh();
        }

        private void UpdateMesh()
        {
            if (meshFilter == null) Awake();

            var mesh = MeshData.CreateMesh();

            meshFilter.sharedMesh = mesh;

            if (meshCollider != null) meshCollider.sharedMesh = mesh;
        }

        #endregion

        #region PARALELIZATION

        private MeshDataThreadSafe meshDataThreadSafe;

        private void InitializeMeshDataThreadSafe()
        {
            // Si no cambia el tamaño de la malla, no hace falta crear una nueva
            if (meshDataThreadSafe.IsEmpty || meshDataThreadSafe.width != Map.Size)
                meshDataThreadSafe = new MeshDataThreadSafe(Map.Size, Map.Size, LOD);
            else
                meshDataThreadSafe.Reset();
        }

        protected override IEnumerator BuildMapParallelizedCoroutine()
        {
            yield return BuildHeightMapParallelizedCoroutine();

            if (textureMode == TextureMode.SetTexture) yield return BuildTextureParallelizedCoroutine();

            yield return BuildMeshParallelizedCoroutine();

            DisplayMap();
        }

        private IEnumerator BuildMeshParallelizedCoroutine()
        {
            InitializeMeshDataThreadSafe();

            var meshJob = new MeshGeneratorThreadSafe.BuildMeshDataJob
            {
                meshData = meshDataThreadSafe,
                heightMap = heightMapThreadSafe,
                lod = LOD,
                heightMultiplier = HeightMultiplier
            }.Schedule();

            yield return new WaitWhile(() => !meshJob.IsCompleted);

            meshJob.Complete();
        }

        #endregion
    }
}