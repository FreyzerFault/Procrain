using System.Collections.Generic;
using Procrain.Core;
using UnityEngine;
using UnityEngine.Serialization;
using Procrain.MapGeneration;
using Procrain.MapParameters;
using Procrain.Noise;
using Procrain.Utils;

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
	public class TerrainChunkGenerator : Singleton<TerrainChunkGenerator>
	{
		// Generación Dinámica de cada Chunk según la distancia al Jugador
		// Distancia de Renderizado
		[FormerlySerializedAs("renderDist")]
		[Range(1, 12)]
		public int maxRenderDist = 4;

		// PLAYER
		public IPlayer Player => IPlayer.Player;

		[SerializeField]
		private Vector2Int playerChunkCoords;
		private Vector2 _lastPlayerChunkCoords;
		private bool PlayerChunkCoordsChanged => playerChunkCoords != _lastPlayerChunkCoords;

		public bool autoUpdate = true;

		[SerializeField]
		private TerrainChunk chunkPrefab;
		
		// Almacen de chunks generados indexados por su ChunkCoord [X,Y]
		private readonly Dictionary<Vector2, TerrainChunk> _chunkDictionary = new();
		
		// Cache de Chunks visibles en el ultimo update
		// Comprueba siempre si salieron del rango de vision para esconderlos
		private readonly List<TerrainChunk> _chunkLastVisibleList = new();

		private int ChunkSize => NoiseParams.Size;
		
		public TerrainParams TerrainParams => MapManager.TerrainSettings;
		public PerlinNoiseParams NoiseParams => MapManager.NoiseParams;
		public Gradient Gradient => MapManager.Instance?.heightGradient;

		private Vector2 PlayerPos2D => new(Player.Position.x, Player.Position.z);
		private TerrainChunk PlayerChunk => _chunkDictionary[playerChunkCoords];

		// Longitud del Borde de los chunks, que sera el tama�o de mi matriz de Chunks Renderizados
		private int VisibilityChunkBorderLength => maxRenderDist * 2 + 1;

		private void Start()
		{
			if (autoUpdate && TerrainParams != null) TerrainParams.OnValuesUpdated += OnValuesUpdated;
			if (Player != null) Player.OnPlayerMove += HandleOnPlayerMove;
		}
		
		private void OnDestroy()
		{
			if (TerrainParams != null) TerrainParams.OnValuesUpdated -= OnValuesUpdated;
			if (Player != null) Player.OnPlayerMove -= HandleOnPlayerMove;
		}


		#region PLAYER MOVEMENT

		/// Actualiza el Chunk actual del Jugador y comprueba si ha cambiado
		/// Actualiza los Chunks visibles alrededor del Jugador si se movió de Chunk
		private void HandleOnPlayerMove(Vector2 move)
		{
			playerChunkCoords = TerrainChunk.GetChunkCoord(PlayerPos2D, ChunkSize);
			
			if (PlayerChunkCoordsChanged) UpdateVisibleChunks();
			
			_lastPlayerChunkCoords = playerChunkCoords;
		}
		
		#endregion
		
		
		private void OnValidate()
		{
			if (TerrainParams == null) return;
			TerrainParams.OnValuesUpdated -= OnValuesUpdated;
			if (autoUpdate) TerrainParams.OnValuesUpdated += OnValuesUpdated;
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
			foreach (TerrainChunk chunk in _chunkLastVisibleList)
				chunk.Visible = false;

			// Recorremos toda la malla alrededor del jugador que entra dentro de la distancia de renderizado
			for (int yOffset = -maxRenderDist; yOffset <= maxRenderDist; yOffset++)
			for (int xOffset = -maxRenderDist; xOffset <= maxRenderDist; xOffset++)
			{
				// Se generan los chunks relativos a la distancia con el Viewer
				Vector2Int chunkCoords = new Vector2Int(xOffset, yOffset) + playerChunkCoords;

				// Si no existe el chunk se genera y se añade
				if (!_chunkDictionary.TryGetValue(chunkCoords, out TerrainChunk chunk))
					_chunkDictionary.Add(chunkCoords, chunk = InstantiateChunk(chunkCoords));

				// Actualizamos el chunk segun la posicion del Jugador
				chunk.UpdateVisibility(maxRenderDist);

				// Y si es visible recordarlo para hacerlo invisible cuando se escape del rango de renderizado
				if (chunk.Visible)
					_chunkLastVisibleList.Add(chunk);
			}
		}

		private TerrainChunk InstantiateChunk(Vector2Int coords)
		{
			TerrainChunk chunk = Instantiate(chunkPrefab, transform);
			chunk.ChunkCoord = coords;
			return chunk;
		}

		/// Resetea la Semilla de forma Aleatoria
		public void ResetSeed() => NoiseParams.ResetSeed();

		/// Borra todos los terrenos renderizados
		public void Clear()
		{
			foreach (TerrainChunk chunk in GetComponentsInChildren<TerrainChunk>(true))
				if (Application.isEditor)
					DestroyImmediate(chunk.gameObject);
				else
					Destroy(chunk.gameObject);

			_chunkDictionary.Clear();
			_chunkLastVisibleList.Clear();
		}
	}
}
