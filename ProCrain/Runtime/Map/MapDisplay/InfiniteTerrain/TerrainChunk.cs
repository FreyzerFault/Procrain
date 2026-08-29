using System.Collections.Generic;
using Procrain.Mesh;
using Procrain.Texture;
using UnityEngine;

namespace Procrain.InfiniteTerrain
{
	[ExecuteAlways]
	public class TerrainChunk : MapDisplayInMesh_LoDGroup
	{
		private HeightMap _localHeightMap;
		[SerializeField] private bool prebuildHeightMap = true;

		[SerializeField] private Vector2Int chunkCoord;
		[SerializeField] private PerlinNoiseParams localNoiseParams;

		private int Size => localNoiseParams.Size;
		private Vector3 CenterPos => new(transform.position.x + Extent, 0, transform.position.z + Extent);
		private float Extent => Size / 2f;

		private Vector2Int PlayerChunk => GameManager.Instance.Player != null
			? GetChunkCoord(GameManager.Instance.Player.Position)
			: Vector2Int.zero;

		public bool Visible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public Vector2Int ChunkCoord
		{
			get => chunkCoord;
			set 
			{
				chunkCoord = value;
				transform.localPosition = WorldPosition3D;
				localNoiseParams.Offset = -new Vector2(WorldPosition2D.x, WorldPosition2D.y);
				lodGroup.RecalculateBounds();
				
				if (prebuildHeightMap)
					BuildLocalHeightMap();
			}
		}

		// Posicion del Chunk en el Espacio de Mundo
		private Vector2Int WorldPosition2D => chunkCoord * Size;
		private Vector3Int WorldPosition3D => new(WorldPosition2D.x, 0, WorldPosition2D.y);

		protected override void Awake()
		{
			base.Awake();

			localNoiseParams = MapManager.NoiseParams;
		}

		
		#region COORDS

		// Transformaciones de Espacio de Mundo al Espacio del Chunk:
		public Vector2Int GetChunkCoord(Vector2 pos) => GetChunkCoord(pos, Size);

		public Vector2Int GetChunkCoord(Vector3 pos) => GetChunkCoord(pos, Size);

		// Posicion relativa al centro del Chunk
		public Vector2 GetLocalPos(Vector2 pos) => pos - WorldPosition2D;

		public Vector2 GetLocalPos(Vector3 pos) => pos - WorldPosition3D;

		public static Vector2Int GetChunkCoord(Vector2 pos, int chunkSize) =>
			Vector2Int.RoundToInt(pos / chunkSize);

		public static Vector2Int GetChunkCoord(Vector3 pos, int chunkSize) =>
			GetChunkCoord(new Vector2(pos.x, pos.z), chunkSize);

		#endregion


		#region VISIBILITY

		/// <summary>
		///     Actualiza la Visibilidad del Chunk segun la distancia al Player.
		/// </summary>
		/// <param name="maxRenderDist">Distancia Maxima al Player a la que deja de ser Visible</param>
		public void UpdateVisibility(int maxRenderDist)
		{
			// La distancia del jugador al chunk
			int playerChunkDistance = Mathf.FloorToInt(Vector2Int.Distance(ChunkCoord, PlayerChunk));

			// Sera visible si la distancia al player viewer es menor a la permitida
			Visible = playerChunkDistance <= maxRenderDist;

			// Si no está visible no hace falta actualizar el LOD
			if (!Visible) return;
		}
		
		#endregion
		
		
		private void BuildLocalHeightMap()
		{
			_localHeightMap = Procrain.HeightMap.CreatePerlinNoiseHeightMap(localNoiseParams);

			// Al regenerar el Mapa de Alturas, quedan obsoletas todas las Mallas
			_meshDataPerLOD.Clear();
		}

		public void RebuildMap()
		{
			if (prebuildHeightMap)
				RebuildMapData();
			
			RebuildTexture();
			
			RebuildMeshData();
			
			DisplayMap();
		}

		// public override void DisplayMap()
		// {
		// 	if (textureMode == TextureMode.SetTexture)
		// 		ApplyTexture(_localTexture);
		// 	
		// 	ApplyAllMeshes();
		// }


		#region MESH

		private readonly Dictionary<int, IMeshData> _meshDataPerLOD = new();

		protected override IMeshData GetMeshData(int lod) => _meshDataPerLOD[lod];
		
		/// Reconstruye la MeshData de todos los LODs usados
		protected void RebuildMeshData()
		{
			for (var i = 0; i < lodGroup.lodCount; i++)
			{
				int lodPow2 = i == 0 ? 0 : (int)Mathf.Pow(2, i - 1);
				
				IMeshData meshData = prebuildHeightMap && _localHeightMap != null
					? MeshGenerator.BuildMeshData(_localHeightMap, mapData.terrainParams, mapData.texParams.gradient)
					: MeshGenerator.BuildMeshData(mapData.heightMapParams, mapData.terrainParams, mapData.texParams.gradient);
				
				if (!_meshDataPerLOD.TryAdd(lodPow2, meshData))
					_meshDataPerLOD[lodPow2] = meshData; 
			}
		}
		
		// Ignore MapManager MeshData
		protected void HandleMeshDataUpdated(int lod, IMeshData meshData) { }

		#endregion


		#region TEXTURE

		private Texture2D _localTexture;
		protected Texture2D Texture => _localTexture ??= BuildLocalTexture();

		protected override void HandleTextureUpdated(Texture2D texture) {}

		protected void RebuildTexture() => ApplyTexture(_localTexture = BuildLocalTexture());

		/// Construye la textura del Chunk a partir de un Gradiente
		private Texture2D BuildLocalTexture()
		{
			if (prebuildHeightMap && _localHeightMap != null)
				return TextureGenerator.BuildTexture2D(_localHeightMap, mapData.texParams.gradient);
			
			// Sample exact Noise Values for each pixel 
			return TextureGenerator.BuildTexture2D(mapData.heightMapParams, mapData.texParams.gradient);
		}

		#endregion
	}
}
