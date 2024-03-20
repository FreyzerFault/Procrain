using Procrain.Runtime.MapGeneration.TerrainGeneration;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.Runtime.MapDisplay
{
    // TODO: Texturas y Collider
    [RequireComponent(typeof(Terrain), typeof(TerrainCollider))]
    [ExecuteAlways]
    public class MapDisplayInTerrain : MapDisplay
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
            TerrainGenerator.ApplyToTerrainData(TerrainData, heightMap, HeightMultiplier);
            terrainCollider.terrainData = TerrainData;
            terrain.materialTemplate = terrainMaterial;
        }
    }
}