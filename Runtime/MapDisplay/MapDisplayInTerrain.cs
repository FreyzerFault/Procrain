using System;
using Map;
using Procrain.MapGeneration;
using Procrain.MapGeneration.Terrain;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.MapDisplay
{
    // TODO: Texturas y Collider
    [RequireComponent(typeof(Terrain), typeof(TerrainCollider))]
    [ExecuteAlways]
    public class MapDisplayInTerrain : MapDisplayBase
    {
        [Range(1, 16)]
        public int resolutionAmplifier = 1;

        [SerializeField]
        private Material terrainMaterial;

        private Terrain _terrain;
        private TerrainCollider _terrainCollider;

        private Terrain Terrain => _terrain ? _terrain : GetComponent<Terrain>();
        private TerrainCollider TerrainCollider =>
            _terrainCollider ? _terrainCollider : GetComponent<TerrainCollider>();

        private void Awake()
        {
            _terrain = GetComponent<Terrain>();
            _terrainCollider = GetComponent<TerrainCollider>();
        }

        private void Start() => MapManager.Instance.OnMapUpdated += UpdateTerrain;

        private void OnDestroy() => MapManager.Instance.OnMapUpdated -= UpdateTerrain;

        private void UpdateTerrain(IHeightMap heightMap)
        {
            var terrainData = Terrain.terrainData;
            if (terrainData == null)
                terrainData = new TerrainData();

            terrainData.ApplyToHeightMap(
                heightMap,
                TerrainSettings.HeightMultiplier,
                resolutionAmplifier
            );
            TerrainCollider.terrainData = terrainData;
            UpdateMaterial();
        }

        private void UpdateMaterial() =>
            // TODO: Actualizar Uniforms del Shader
            Terrain.materialTemplate = terrainMaterial;

        #region MOVEMENT

        // MOVIMIENTO DEL HEIGHTMAP en una dirección para TESTING
        public bool movement;

        private void Update()
        {
            if (!movement)
                return;
            TerrainSettings.Offset += new float2(1, 0);
        }

        #endregion
    }
}
