using System;
using System.Collections.Generic;
using System.Text;
using Procrain.Geometry;
using Procrain.Geometry.Mesh;
using Procrain.MapGeneration;
using Procrain.MapGeneration.TIN;
using UnityEngine;
using UnityEngine.Serialization;

namespace Procrain.MapDisplay.TIN
{
    public class TinDisplayInMesh : MapDisplayInMesh
    {
	    private Tin _tin;
	    
	    public event Action OnIterationEnd;
	    public event Action OnGenerationEnd;
	    
        public float errorTolerance = 1;
        public int maxIterations = 10;
        
        public int VertexCount => _tin.vertices.Count;
        public int TriCount => _tin.triangles.Count;
        public int LastAddedVertexCount => _tin.lastVertexAdded.Count;
        public float LastVertexError => _tin.lastVertexError[^1];

        protected override void Start()
        {
	        base.Start();
            RebuildHeightMap();
        }

        protected override void HandleTextureUpdated(Texture2D texture) => ApplyTexture(Texture);
        protected override void HandleMeshDataUpdated(int lod, IMeshData meshData) { }
        
        /// Build the MeshData from the TIN
        protected override IMeshData MeshData
        {
	        get
	        {
		        IMeshData meshData = TinGenerator.TinToMesh(_tin);
		        return meshData;
	        }
        }

        protected override void RebuildMeshData() => standaloneMeshData = MeshData;


        #region PROGRESSIVE GENERATION

        [Header("Progressive Generation")]
        public bool progressiveBuild = true;

        /// Contador de Distribución Puntos por iteración {[1 punto, 5 veces], [2 puntos, 20 veces] ...}
        private readonly Dictionary<int, int> _distribucionPuntosConsecutivos = new();

        [Range(1, 30)] public int maxPointsPerCycle = 15;
        [Range(0, 20)] public int minDistanceBetweenPointPerCycle = 5;

        public float currentMaxPointError = 100;

        public int phase;
        public float timeConsumed;
        private float _maxPercentageReached;

        public bool isRunning;
        
		// Arrow Objects
		public GameObject arrowPrefab;
		private readonly List<GameObject> _lastArrows = new();

		// Genera el TIN de forma progresiva
		public bool RunIteration()
		{
			if (_tin == null) phase = 0;

			if (HeightMap.IsEmpty) RebuildHeightMap();

			var finished = false;
			if (_tin == null || phase == 0)
			{
				timeConsumed = 0;
				_tin = CreateInitializedTIN(HeightMap as HeightMap, errorTolerance, TerrainParams.HeightScale);
			}
			else
			{
				finished = !_tin.AddPointLoopIteration(maxPointsPerCycle, minDistanceBetweenPointPerCycle);
				DrawAddedPointArrow();

				// Actualiza la distribucion de puntos consecutivos añadidos en una iteracion
				int numPuntos = LastAddedVertexCount;
				if (!_distribucionPuntosConsecutivos.TryAdd(numPuntos, 1))
					_distribucionPuntosConsecutivos[numPuntos]++;
			}

			timeConsumed += Time.deltaTime;

			currentMaxPointError = LastVertexError;
			
			UpdateMesh();
			
			OnIterationEnd?.Invoke();
			
			if (finished)
			{
				OnGenerationEnd?.Invoke();
				LogResults();

				isRunning = false;
				StopAllCoroutines();
				return true;
			}

			phase++;
			return false;
		}
		
		/// Actualiza la Malla con el TIN generado
		private void UpdateMesh()
		{
			if (_tin == null) return;
			meshData = TinGenerator.TinToMesh(tin);
			meshData.ApplyMesh(meshFilter.sharedMesh);
			meshCollider.sharedMesh = meshFilter.sharedMesh;
		}

		public void ResetTin()
		{
			StopAllCoroutines();
			isRunning = false;

			BuildHeightMap();
			
			tin = CreateInitializedTIN(heightMap, errorTolerance, HeightScale);
			phase = 0;

			// Points Added Arrows
			DrawAddedPointArrow();

			// MESH
			UpdateMesh();

			ResetPath();
		}


		private static Tin CreateInitializedTIN(HeightMap heightMap, float errorTolerance, float heightScale)
		{
			Tin tin = new(heightMap.map, heightMap.size, errorTolerance: errorTolerance, heightScale: heightScale);
			tin.InitGeometry();
			return tin;
		}

		// Inicia o para la Animacion de Construccion del TIN
		public void PlayPauseProgressiveGeneration()
		{
			if (!isRunning)
				StartCoroutine(ProgressiveGenerationCoroutine());
			else
				StopAllCoroutines();

			isRunning = !isRunning;
		}

		// Corutina que ejecuta una iteracion de la generacion de un TIN
		private IEnumerator ProgressiveGenerationCoroutine()
		{
			while (true)
			{
				// Espacio => PARA la generación
				if (Input.GetKeyDown(KeyCode.Space) || RunIteration()) break;

				yield return null;
			}
		}

		private void LogResults()
		{
			StringBuilder sb = new();
			foreach (KeyValuePair<int, int> entry in distribucionPuntosConsecutivos)
				sb.AppendLine($"Iteraciones con {entry.Key} puntos de golpe: {entry.Value}");

			Debug.Log($"Distribucion de puntos consecutivos: \n{sb}");
			Debug.Log($"Tiempo consumido: {timeConsumed}");
		}
		

		#region ITERATION VISUALIZATION

		private void DrawAddedPointArrow()
		{
			// Eliminamos las anteriores
			foreach (GameObject lastArrow in _lastArrows)
				if (Application.isEditor)
					DestroyImmediate(lastArrow);
				else
					Destroy(lastArrow, 0.1f);

			_lastArrows.Clear();

			if (tin is not { lastVertexAdded: not null } || tin.lastVertexAdded.Count == 0) return;

			foreach (Vector3 t in tin.lastVertexAdded)
				_lastArrows.Add(
					Instantiate(
						arrowPrefab,
						t + Vector3.up * 10,
						Quaternion.identity
					)
				);
		}


		#endregion

        #endregion

        
        #region DEBUG

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            _tin?.OnDrawGizmos();
            OnDrawGizmosIteration();
        }
        
        private void OnDrawGizmosIteration()
        {
            if (_tin?.lastVertexAdded == null || _tin.lastVertexAdded.Count == 0) return;

            Gizmos.color = Color.red;
            foreach (Vector3 t in _tin.lastVertexAdded) Gizmos.DrawSphere(t, 1);
        }

        #endregion
    }
}
