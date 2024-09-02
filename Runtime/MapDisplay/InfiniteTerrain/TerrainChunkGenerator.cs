using System.Collections.Generic;
using DavidUtils.Player;
using Procrain.MapGeneration;
using Procrain.Noise;
using UnityEngine;
using UnityEngine.Serialization;

// Generador de Terreno Adaptativo a la posición del Jugador
//
// Visualiza solo los Chunks que estan dentro de la distancia de renderizado al Jugador
// Guarda los terrenos en un Mapa según su Coordenada de Chunk (pos / chunkSize)
//
// La generación es lazy:
// no genera el Chunk hasta que el jugador se acerca a menos de la distancia de renderizado
namespace Procrain.MapDisplay.InfiniteTerrain
{
	[ExecuteAlways]
	public class TerrainChunkGenerator : MonoBehaviour
	{
		// TEXTURAS
		public Gradient gradient = new();

		// Generación Dinámica de cada Chunk según la distancia al Jugador
		// Distancia de Renderizado
		[FormerlySerializedAs("renderDist")]
		[Range(1, 12)]
		public int maxRenderDist = 4;

		// PLAYER
		public Player player => Player.Instance;

		[SerializeField]
		private Vector2Int playerChunkCoords;

		public bool autoUpdate = true;

		// Almacen de chunks generados indexados por su ChunkCoord [X,Y]
		[SerializeField]
		private TerrainChunk chunkPrefab;

		public TerrainSettingsSo terrainSettingsSo;
		public PerlinNoiseParams noiseParams;

		private readonly Dictionary<Vector2, TerrainChunk> chunkDictionary = new();

		// Cache de Chunks visibles en el ultimo update
		// Comprueba siempre si salieron del rango de vision para esconderlos
		private readonly List<TerrainChunk> chunkLastVisibleList = new();
		private Vector2 lastPlayerChunkCoords;

		private int ChunkSize => noiseParams.Size;

		private Vector2 PlayerPos2D => new(player.Position.x, player.Position.z);
		private TerrainChunk PlayerChunk => chunkDictionary[playerChunkCoords];

		// Longitud del Borde de los chunks, que sera el tama�o de mi matriz de Chunks Renderizados
		private int VisibilityChunkBorderLength => maxRenderDist * 2 + 1;

		private void Start()
		{
			if (autoUpdate)
				terrainSettingsSo.ValuesUpdated += OnValuesUpdated;
		}

		public void Update()
		{
			playerChunkCoords = TerrainChunk.GetChunkCoord(PlayerPos2D, ChunkSize);

			// Solo si el viewer cambia de chunk se actualizan los chunks
			if (lastPlayerChunkCoords != playerChunkCoords)
				UpdateVisibleChunks();

			// Ultimo chunk del jugador para comprobar si ha cambiado de chunk
			lastPlayerChunkCoords = playerChunkCoords;
		}

		private void OnDestroy() => terrainSettingsSo.ValuesUpdated -= OnValuesUpdated;

		private void OnValidate()
		{
			if (!autoUpdate)
				return;
			terrainSettingsSo.ValuesUpdated -= OnValuesUpdated;
			terrainSettingsSo.ValuesUpdated += OnValuesUpdated;
		}

		public void RegenerateTerrain()
		{
			Clear();
			playerChunkCoords = TerrainChunk.GetChunkCoord(PlayerPos2D, ChunkSize);
			UpdateVisibleChunks();
		}

		public void OnValuesUpdated() => RegenerateTerrain();

		public void UpdateVisibleChunks()
		{
			foreach (TerrainChunk chunk in chunkLastVisibleList)
				chunk.Visible = false;

			// Recorremos toda la malla alrededor del jugador que entra dentro de la distancia de renderizado
			for (int yOffset = -maxRenderDist; yOffset <= maxRenderDist; yOffset++)
			for (int xOffset = -maxRenderDist; xOffset <= maxRenderDist; xOffset++)
			{
				// Se generan los chunks relativos a la distancia con el Viewer
				Vector2Int chunkCoords = new Vector2Int(xOffset, yOffset) + playerChunkCoords;

				// Si no existe el chunk se genera y se añade
				if (!chunkDictionary.TryGetValue(chunkCoords, out TerrainChunk chunk))
					chunkDictionary.Add(chunkCoords, chunk = InstantiateChunk(chunkCoords));

				// Actualizamos el chunk segun la posicion del Jugador
				chunk.UpdateVisibility(maxRenderDist);

				// Y si es visible recordarlo para hacerlo invisible cuando se escape del rango de renderizado
				if (chunk.Visible)
					chunkLastVisibleList.Add(chunk);
			}
		}

		private TerrainChunk InstantiateChunk(Vector2Int coords)
		{
			TerrainChunk chunk = Instantiate(chunkPrefab, transform);
			chunk.ChunkCoord = coords;
			chunk.Gradient = gradient;
			return chunk;
		}

		// Resetea la Semilla de forma Aleatoria
		public void ResetSeed() => noiseParams.ResetSeed();

		// Borra todos los terrenos renderizados
		public void Clear()
		{
			foreach (TerrainChunk chunk in GetComponentsInChildren<TerrainChunk>(true))
				if (Application.isEditor)
					DestroyImmediate(chunk.gameObject);
				else
					Destroy(chunk.gameObject);

			chunkDictionary.Clear();
			chunkLastVisibleList.Clear();
		}
	}
}
