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

        [Space] private Terrain _terrain;

        private TerrainCollider _terrainCollider;

        public Terrain Terrain => _terrain ? _terrain : GetComponent<Terrain>();

        public TerrainCollider TerrainCollider =>
            _terrainCollider ? _terrainCollider : GetComponent<TerrainCollider>();

        private TerrainData TerrainData
        {
            get => Terrain.terrainData;
            set => Terrain.terrainData = value;
        }

        private void Awake()
        {
            _terrain = GetComponent<Terrain>();
            _terrainCollider = GetComponent<TerrainCollider>();
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
            TerrainCollider.terrainData = TerrainData;
            Terrain.materialTemplate = terrainMaterial;
        }
    }
}