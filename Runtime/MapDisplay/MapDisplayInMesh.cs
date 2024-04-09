using System;
using Map;
using Procrain.MapGeneration.Mesh;
using UnityEngine;

namespace Procrain.MapDisplay
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class MapDisplayInMesh : MapDisplayBase
    {
        public enum TextureMode
        {
            SetTexture,
            UseShader
        }

        public TextureMode textureMode = TextureMode.UseShader;

        public bool useLocalLoD;

        [Range(0, 16)]
        protected int localLoD;
        protected int LoD
        {
            get => useLocalLoD ? localLoD : TerrainSettings.LOD;
            set
            {
                if (LoD == value)
                    return;

                if (useLocalLoD)
                {
                    OnLocalLoDUpdate(value);
                    localLoD = value;
                }
                else
                {
                    TerrainSettings.LOD = value;
                }
            }
        }

        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        protected virtual void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            var meshData = MapManager.Instance.GetMeshData(LoD);
            if (meshData != null)
                ApplyMeshData(LoD, meshData);

            var texture = MapManager.Instance.texture;
            if (texture != null)
                ApplyTexture(texture);

            MapManager.Instance.OnTextureUpdated += ApplyTexture;
            MapManager.Instance.OnMeshUpdated += ApplyMeshData;
        }

        protected virtual void OnDestroy()
        {
            MapManager.Instance.OnTextureUpdated -= ApplyTexture;
            MapManager.Instance.OnMeshUpdated -= ApplyMeshData;
        }

        protected void ApplyTexture(Texture2D texture)
        {
            if (textureMode != TextureMode.SetTexture)
                return;
            texture.Apply();
            _meshRenderer.sharedMaterial.mainTexture = texture;
        }

        protected void ApplyMeshData(int lod, IMeshData meshData)
        {
            if (useLocalLoD && localLoD != lod)
                return;

            var mesh = meshData.CreateMesh();

            _meshFilter.sharedMesh = mesh;
            _meshCollider.sharedMesh = mesh;
        }

        protected virtual void OnLocalLoDUpdate(int newLod)
        {
            var meshData = MapManager.Instance.GetMeshData(LoD);
            if (meshData != null)
                ApplyMeshData(LoD, meshData);
        }
    }
}
