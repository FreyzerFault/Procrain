using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Procrain.Geometry;
using Procrain.Geometry.Mesh;
using Procrain.MapGeneration;
using Procrain.MapGeneration.Texture;
using Procrain.MapGeneration.TIN;
using Procrain.MapParameters;
using Procrain.Noise;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace Procrain.MapDisplay.TIN
{
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	[ExecuteAlways]
	public class TinVisualizer : MonoBehaviour
	{
		public bool autoUpdate = true;

		public event Action OnIterationEnd;
		public event Action OnGenerationEnd;

		#region UNITY

		private void Awake()
		{
			// MESH
			meshFilter = GetComponent<MeshFilter>();
			meshRenderer = GetComponent<MeshRenderer>();
			meshCollider = GetComponent<MeshCollider>();

			// UI
			lineRenderer = GetComponent<LineRenderer>();

			Time.timeScale = 1;
		}

		private void Start()
		{
			if (enablePath) minimap.onMapClick += AddPathPoint;

			BuildHeightMap();
			fase = 0;
		}

		#endregion

		#region PATH DRAWING

		public bool enablePath = true;
		private static Vector2 _startPoint2D;
		private static Vector2 _endPoint2D;
		public Vector3 startPoint = Vector3.zero;
		public Vector3 endPoint = Vector3.zero;
		public Vector3[] intersections;

		private LineRenderer lineRenderer;

		private GameObject startPointSprite;
		private GameObject endPointSprite;

		private void AddPathPoint(Vector3 mousePosition, PointerEventData.InputButton button)
		{
			if (!enablePath) return;

			UpdateLineExtremes(GetMousePoint2D(mousePosition), button);
			UpdateLineRenderer3D(_startPoint2D, _endPoint2D);
			UpdateLineRenderer2D();
		}

		private void ResetPath() => lineRenderer.positionCount = 0;

		private void UpdateLineRenderer2D()
		{
			if (lineRenderer.positionCount > 0)
			{
				var points = new Vector3[lineRenderer.positionCount];
				lineRenderer.GetPositions(points);
				uiController.UpdateLine(points);
			}
			else
			{
				uiController.UpdateLine(Array.Empty<Vector3>());
			}
		}

		/// <summary>
		///     Actualiza la Linea que representa el trazado de la VUELTA CICLISTA
		/// </summary>
		/// <param name="start">Punto Inicial 2D</param>
		/// <param name="end">Punto Final 2D</param>
		/// <exception cref="Exception"></exception>
		private void UpdateLineRenderer3D(Vector2 start, Vector2 end)
		{
			// Calculamos el trazado en 2D
			Vector2[] intersections2D = tin.GetIntersections(start, end);

			// Inicio -> Intersecciones -> Fin
			lineRenderer.positionCount = intersections2D.Length + 2;
			lineRenderer.SetPositions(
				intersections2D.Select(
						intersection =>
							tin.GetHeightInterpolated(intersection, out float height)
								? new Vector3(intersection.x, height, intersection.y)
								: Vector3.zero
					)
					.Prepend(startPoint)
					.Append(endPoint)
					.ToArray()
			);
		}

		#endregion

		
		#region TERRAIN GENERATOR

		[FormerlySerializedAs("terrainSettingsSo")] public TerrainParams terrainParams;
		public PerlinNoiseParams noiseParams;
		public Gradient gradient;

		private AnimationCurve HeightCurve => terrainParams.HeightCurve;
		private float HeightScale => terrainParams.HeightScale;

		private HeightMap heightMap;
		private IMeshData meshData;
		public Vector3[] pointCloud;

		public bool withTexture = true;
		public bool drawNormals;


		// Actualiza el Mapa de Ruido y la Textura asociada
		public void BuildHeightMap()
		{
			heightMap = HeightMapGenerator.CreatePerlinNoiseHeightMap(noiseParams, HeightCurve);
			if (withTexture) BuildTexture();
		}

		public void BuildTexture()
		{
			meshRenderer.sharedMaterial.mainTexture = TextureGenerator.BuildTexture2D(heightMap, gradient);
			meshRenderer.enabled = withTexture;
		}

		public void ResetRandomSeed() => noiseParams.ResetSeed();

		
		#region TIN

		public Tin tin;
		public float errorTolerance = 0.1f;
		public int maxIterations = 100;

		public int VertexCount => tin.vertices.Count;
		public int TriCount => tin.triangles.Count;
		public int LastAddedVertexCount => tin.lastVertexAdded.Count;
		public float LastVertexError => tin.lastVertexError[^1];

		// Genera el TIN en un frame
		private void BuildTinMesh()
		{
			meshData = TinGenerator.BuildTinMeshData(
				out tin,
				heightMap,
				errorTolerance,
				HeightScale,
				maxIterations
			);
			UpdateMesh();
		}

		#endregion


		#region PROGRESSIVE GENERATION

		public bool progressiveBuild = true;

		/// Contador de Distribución Puntos por iteración {[1 punto, 5 veces], [2 puntos, 20 veces] ...}
		private readonly Dictionary<int, int> distribucionPuntosConsecutivos = new();

		[Range(1, 30)] public int maxPointsPerCycle = 15;
		[Range(0, 20)] public int minDistanceBetweenPointPerCycle = 5;

		public float currentMaxPointError = 100;

		public int fase;
		public float timeConsumed;
		private float maxPercentageReached;

		public bool isRunning;

		// Arrow Objects
		public GameObject arrowPrefab;
		private readonly List<GameObject> lastArrows = new();

		// Genera el TIN de forma progresiva
		public bool RunIteration()
		{
			if (tin == null) fase = 0;

			if (heightMap.IsEmpty) BuildHeightMap();

			var finished = false;
			if (tin == null || fase == 0)
			{
				timeConsumed = 0;
				tin = CreateInitializedTIN(heightMap, errorTolerance, HeightScale);
			}
			else
			{
				finished = !tin.AddPointLoopIteration(maxPointsPerCycle, minDistanceBetweenPointPerCycle);
				DrawAddedPointArrow();

				// Actualiza la distribucion de puntos consecutivos añadidos en una iteracion
				int numPuntos = LastAddedVertexCount;
				if (!distribucionPuntosConsecutivos.TryAdd(numPuntos, 1))
					distribucionPuntosConsecutivos[numPuntos]++;
			}

			timeConsumed += Time.deltaTime;

			currentMaxPointError = tin.lastVertexError[^1];
			
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

			fase++;
			return false;
		}

		public void ResetTin()
		{
			StopAllCoroutines();
			isRunning = false;

			BuildHeightMap();
			
			tin = CreateInitializedTIN(heightMap, errorTolerance, HeightScale);
			fase = 0;

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
			foreach (GameObject lastArrow in lastArrows)
				if (Application.isEditor)
					DestroyImmediate(lastArrow);
				else
					Destroy(lastArrow, 0.1f);

			lastArrows.Clear();

			if (tin is not { lastVertexAdded: not null } || tin.lastVertexAdded.Count == 0) return;

			foreach (Vector3 t in tin.lastVertexAdded)
				lastArrows.Add(
					Instantiate(
						arrowPrefab,
						t + Vector3.up * 10,
						Quaternion.identity
					)
				);
		}


		#endregion

		#endregion

		#endregion
		
		
		#region MESH

		private MeshFilter meshFilter;
		private MeshRenderer meshRenderer;
		private MeshCollider meshCollider;
		
		/// Actualiza la Malla con el TIN generado
		private void UpdateMesh()
		{
			if (tin == null) return;
			meshData = TinGenerator.TinToMesh(tin);
			meshData.ApplyMesh(meshFilter.sharedMesh);
			meshCollider.sharedMesh = meshFilter.sharedMesh;
		}

		
		#endregion
		
		
		
		#region DEBUG

		private void OnDrawGizmos()
		{
			tin?.OnDrawGizmos();
			OnDrawGizmosIteration();

			OnDrawGizmosNormals();
		}
		
		private void OnDrawGizmosIteration()
		{
			if (tin?.lastVertexAdded == null || tin.lastVertexAdded.Count == 0) return;

			Gizmos.color = Color.red;
			foreach (Vector3 t in tin.lastVertexAdded) Gizmos.DrawSphere(t, 1);
		}
		
		private void OnDrawGizmosNormals()
		{
			if (!meshFilter || !drawNormals) return;

			Mesh mesh = meshFilter.sharedMesh;

			if (mesh == null) return;

			for (var i = 0; i < mesh.vertices.Length; i++)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawLine(mesh.vertices[i], mesh.vertices[i] + mesh.normals[i] * 10);
			}
		}

		#endregion
	}
}
