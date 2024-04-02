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
        public bool movement;

        [SerializeField] private Material terrainMaterial;

        [Space] private Terrain terrain;

        private TerrainCollider terrainCollider;

        private TerrainData TerrainData
        {
            get => terrain.terrainData;
            set => terrain.terrainData = value;
        }

        private void Awake()
        {
            terrain = GetComponent<Terrain>();
            terrainCollider = GetComponent<TerrainCollider>();
        }

        private void Update()
        {
            if (!movement) return;
            Offset += new float2(1, 0);
        }

        public override void DisplayMap()
        {
            if (TerrainData == null) TerrainData = new TerrainData();
            TerrainGenerator.ApplyToTerrainData(TerrainData, heightMap, HeightMultiplier);
            terrainCollider.terrainData = TerrainData;
            terrain.materialTemplate = terrainMaterial;
        }
    }
}