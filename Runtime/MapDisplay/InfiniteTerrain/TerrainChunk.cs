using System.Collections.Generic;
using Procrain.Core;
using Procrain.MapGeneration;
using Procrain.MapGeneration.Mesh;
using Procrain.MapGeneration.Texture;
using Procrain.Noise;
using UnityEngine;

namespace Procrain.MapDisplay.InfiniteTerrain
{
	public class TerrainChunk : MapDisplayInMesh_LoDByPlayer
	{
		private IHeightMap localHeightMap;

		[SerializeField]
		private Vector2Int chunkCoord;

		[SerializeField]
		private PerlinNoiseParams localNoiseParams;

		private readonly Dictionary<int, IMeshData> _meshDataPerLOD = new();
		private Bounds _bounds;
		private int Size => localNoiseParams.Size;
		private Vector2Int PlayerChunk =>
			GetChunkCoord(
				_player?.transform.position ?? GameObject.FindWithTag("Player").transform.position
			);

		private float Extent => Size / 2f;

		private Vector3 CenterPos =>
			new(transform.position.x + Extent, 0, transform.position.z + Extent);

		public bool Visible
		{
			get => gameObject.activeSelf;
			set => gameObject.SetActive(value);
		}

		public Vector2Int ChunkCoord
		{
			get => chunkCoord;
			set => MoveToCoord(value);
		}

		public Gradient Gradient
		{
			get => MapManager.Instance.heightGradient;
			set
			{
				MapManager.Instance.heightGradient = value;
				ApplyGradient(value);
			}
		}

		// =================================================================================================== //
		// Parametros que dependen del Chunk:

		private int LodByDistToPlayer
		{
			get
			{
				// Si no es potencia de 2, redondea al siguiente
				int dist = Mathf.FloorToInt(Vector2Int.Distance(PlayerChunk, chunkCoord));
				return dist == 0 ? 0 : Mathf.ClosestPowerOfTwo(dist);
			}
		}

		// Posicion del Chunk en el Espacio de Mundo
		private Vector2Int WorldPosition2D => chunkCoord * Size;
		private Vector3Int WorldPosition3D => new(WorldPosition2D.x, 0, WorldPosition2D.y);

		protected override void Awake()
		{
			base.Awake();

			localNoiseParams = MapManager.Instance.NoiseParams;
		}

		private void BuildMeshData(int lod)
		{
			// Actualiza la Malla al LOD actual si ya fue generada
			if (_meshDataPerLOD.TryGetValue(lod, out IMeshData meshData))
				return;

			// Si no la genera y la guarda
			meshData = MeshGenerator.BuildMeshData(MapManager.Instance.HeightMap, lod, MapManager.Instance.TerrainSettings.HeightScale);
			_meshDataPerLOD.Add(lod, meshData);
		}

		// Cuando se posiciona en su coordenada se construye el Mapa
		public void MoveToCoord(Vector2Int coord)
		{
			chunkCoord = coord;
			transform.localPosition = WorldPosition3D;
			localNoiseParams.Offset = -new Vector2(WorldPosition2D.x, WorldPosition2D.y);
			BuildHeightMap();
		}

		private void BuildHeightMap()
		{
			localHeightMap = HeightMapGenerator.CreatePerlinNoiseHeightMap(
				localNoiseParams,
				MapManager.Instance.TerrainSettings.HeightCurve
			);

			// Al regenerar el Mapa de Alturas, quedan obsoletas todas las Mallas
			_meshDataPerLOD.Clear();
		}

		private void ApplyGradient(Gradient newGradient)
		{
			if (textureMode != TextureMode.SetTexture)
				return;

			Texture2D texture = BuildTextureData();
			ApplyTexture(texture);
		}

		private Texture2D BuildTextureData() =>
			TextureGenerator.BuildTexture2D(localHeightMap, MapManager.Instance.heightGradient);

        /// <summary>
        ///     Actualiza la Visibilidad del Chunk (si debe ser renderizado o no).
        ///     Y actualiza tambien el LOD
        /// </summary>
        /// <param name="maxRenderDist">Distancia Maxima de Renderizado de Chunks</param>
        public void UpdateVisibility(int maxRenderDist)
		{
			// La distancia del jugador al chunk
			int chunkDistance = Mathf.FloorToInt(Vector2Int.Distance(ChunkCoord, PlayerChunk));

			// Sera visible si la distancia al player viewer es menor a la permitida
			Visible = chunkDistance <= maxRenderDist;

			// Si no está visible no hace falta actualizar el LOD
			if (!Visible)
				return;

			LoD = LodByDistToPlayer;
		}

		protected override void OnLocalLoDUpdate(int newLod)
		{
			if (newLod == LoD)
				return;
			if (!_meshDataPerLOD.TryGetValue(newLod, out IMeshData meshData))
				BuildMeshData(newLod);

			ApplyMeshData(newLod, meshData);
		}

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
	}
}
