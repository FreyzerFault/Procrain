using Procrain.Core;
using Procrain.MapGeneration;
using Procrain.MapGeneration.Mesh;
using UnityEngine;

namespace Procrain.MapDisplay
{
    public abstract class MapDisplayBase : MonoBehaviour
    {
        protected IHeightMap HeightMap => MapManager.Instance.HeightMap;
        protected Texture2D Texture => MapManager.Instance.texture;
        protected IMeshData MeshData => MapManager.Instance.GetMeshData();
        protected TerrainSettingsSo TerrainSettings => MapManager.Instance.terrainSettings;

        protected virtual void Start()
        {
            MapManager.Instance.OnMapUpdated += OnHeightMapUpdated;
            MapManager.Instance.OnTextureUpdated += OnTextureUpdated;
            MapManager.Instance.OnMeshUpdated += OnMeshDataUpdated;

            DisplayMap();
        }

        protected virtual void OnDestroy()
        {
            MapManager.Instance.OnMapUpdated -= OnHeightMapUpdated;
            MapManager.Instance.OnTextureUpdated -= OnTextureUpdated;
            MapManager.Instance.OnMeshUpdated -= OnMeshDataUpdated;
        }

        protected virtual void OnHeightMapUpdated(IHeightMap heightMap) { }

        protected virtual void OnTextureUpdated(Texture2D texture) { }

        protected virtual void OnMeshDataUpdated(int lod, IMeshData meshData) { }

        // This is for External Calls, and for Testing without a MapManager event
        public virtual void DisplayMap() { }
    }
}
