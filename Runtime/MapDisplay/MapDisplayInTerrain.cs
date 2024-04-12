using Procrain.MapGeneration;
using Procrain.MapGeneration.Terrain;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.MapDisplay
{
	// TODO: Texturas y Collider
	[RequireComponent(typeof(Terrain), typeof(TerrainCollider))]
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

		protected override void OnHeightMapUpdated(IHeightMap heightMap)
		{
			UpdateTerrainData(heightMap);
			UpdateMaterial();
		}

		public override void DisplayMap()
		{
			if (HeightMap == null) return;
			UpdateTerrainData(HeightMap);
			UpdateMaterial();
		}

		private void UpdateTerrainData(IHeightMap heightMap)
		{
			TerrainData terrainData = Terrain.terrainData;
			if (terrainData == null)
				terrainData = new TerrainData();

			terrainData.ApplyToHeightMap(
				HeightMap,
				TerrainSettings.HeightScale,
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
