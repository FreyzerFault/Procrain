using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Procrain.Mesh;
using UnityEngine;

namespace Procrain.TIN
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
            RebuildMapData();
        }

        protected override void HandleTextureUpdated(Texture2D texture) => ApplyTexture(Texture);
        protected void HandleMeshDataUpdated(int lod, IMeshData meshData) { }
        
        /// Build the MeshData from the TIN
        public override IMeshData MeshData
        {
	        get
	        {
		        IMeshData meshData = TinGenerator.TinToMesh(_tin);
		        return meshData;
	        }
        }


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

			if (HeightMap.IsEmpty) RebuildMapData();

			bool finished = false;
			if (_tin == null || phase == 0)
			{
				timeConsumed = 0;
				_tin = new Tin(HeightMap.ToArray2D(), errorTolerance: errorTolerance);
				_tin.InitGeometry();
			}
			else
			{
				_tin.SearchPointsToAdd(maxIterations, minDistanceBetweenPointPerCycle);
				finished = _tin.pointsToAdd.Count == 0;
				
				if (!finished)
				{
					_tin.AddVertexLoopIteration();
					DrawAddedPointArrow();

					// Actualiza la distribucion de puntos consecutivos añadidos en una iteracion
					int numPuntos = LastAddedVertexCount;
					if (!_distribucionPuntosConsecutivos.TryAdd(numPuntos, 1))
						_distribucionPuntosConsecutivos[numPuntos]++;
				}
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
			MeshDataDynamic meshData = TinGenerator.TinToMesh(_tin);
			meshData.ApplyMesh(meshFilter.sharedMesh);
			meshCollider.sharedMesh = meshFilter.sharedMesh;
		}

		public void ResetTin()
		{
			StopAllCoroutines();
			isRunning = false;

			RebuildMapData();
			
			_tin = new Tin(HeightMap.ToArray2D(), errorTolerance: errorTolerance);
			phase = 0;

			// Points Added Arrows
			DrawAddedPointArrow();

			// MESH
			UpdateMesh();
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
			foreach (KeyValuePair<int, int> entry in _distribucionPuntosConsecutivos)
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

			if (_tin is not { lastVertexAdded: not null } || _tin.lastVertexAdded.Count == 0) return;

			foreach (Vector3 t in _tin.lastVertexAdded)
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
