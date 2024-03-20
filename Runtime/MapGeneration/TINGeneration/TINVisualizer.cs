using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Geometry;
using MapGeneration.MeshGeneration;
using MapGeneration.TextureGeneration;
using Noise;
using UI.Minimap;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace MapGeneration.TerrainGeneration.TINGeneration
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    public class TinVisualizer : MonoBehaviour
    {
        public bool autoUpdate = true;

        #region UNITY

        private void Awake()
        {
            // MESH
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();

            // UI
            lineRenderer = GetComponent<LineRenderer>();
            uiController = GetComponent<TinControlUI>();
            minimap = GameObject.FindWithTag("Minimap")?.GetComponent<Minimap>();

            Time.timeScale = 1;
        }

        private void Start()
        {
            if (enablePath)
                minimap.onMapClick += AddPathPoint;

            BuildHeightMap();
            fase = 0;
        }

        private void OnDrawGizmos()
        {
            tin?.OnDrawGizmos();
            OnDrawGizmosIteration();

            OnDrawGizmosNormals();

            if (enablePath)
                OnDrawGizmosPath();
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
        ///     Actualiza los extremos de la Linea (startPoint y endPoint)
        /// </summary>
        private void UpdateLineExtremes(Vector2 point, PointerEventData.InputButton button)
        {
            var leftClick = button == PointerEventData.InputButton.Left;
            var rightClick = button == PointerEventData.InputButton.Right;

            if (!leftClick && !rightClick) return;

            var sprite = minimap.DrawPointInMousePosition(leftClick ? Color.red : Color.green);

            if (leftClick)
            {
                Destroy(startPointSprite);
                startPointSprite = sprite;
                _startPoint2D = point;
                if (tin.GetHeightInterpolated(point, out var height))
                    startPoint = new Vector3(point.x, height, point.y);
            }
            else
            {
                Destroy(endPointSprite);
                endPointSprite = sprite;
                _endPoint2D = point;
                if (tin.GetHeightInterpolated(point, out var height))
                    endPoint = new Vector3(point.x, height, point.y);
            }
        }

        /// <summary>
        ///     Punto del mundo en 2D (X,Z) al que apunta el mouse
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Necesita que haya un Camera Manager y una Camara con Indice 1 (Cenital)</exception>
        private Vector2 GetMousePoint2D(Vector3 mousePoint)
        {
            // Coordenada del Raton relativa a la camara del minimapa
            var screenPoint = minimap.GetScreenSpaceMousePoint(mousePoint);

            // La Z sera la distancia desde la camara al terreno
            var screenPoint3D = new Vector3(
                screenPoint.x,
                screenPoint.y,
                minimap.renderCamera.WorldToScreenPoint(transform.position).z
            );

            // Lo cambiamos a Coordenadas del mundo y en 2D
            var worldPoint = minimap.renderCamera.ScreenToWorldPoint(screenPoint3D);
            var worldPoint2D = new Vector2(worldPoint.x, worldPoint.z);
            return worldPoint2D;
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
            var intersections2D = tin.GetIntersections(start, end);

            // Inicio -> Intersecciones -> Fin
            lineRenderer.positionCount = intersections2D.Length + 2;
            lineRenderer.SetPositions(
                intersections2D.Select(
                        intersection =>
                            tin.GetHeightInterpolated(intersection, out var height)
                                ? new Vector3(intersection.x, height, intersection.y)
                                : Vector3.zero
                    )
                    .Prepend(startPoint)
                    .Append(endPoint)
                    .ToArray()
            );
        }

        private void OnDrawGizmosPath()
        {
            // Extremos de la linea
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(startPoint, 1);
            Gizmos.DrawSphere(endPoint, 1);

            // Punto de control de la linea
            Gizmos.color = Color.blue;
            foreach (var intersection in intersections)
                Gizmos.DrawSphere(intersection, 1);
        }

        #endregion

        #region TERRAIN GENERATOR

        public TerrainSettingsSo terrainSettingsSo;
        public Gradient gradient;

        private PerlinNoiseParams NoiseParams => terrainSettingsSo.NoiseParams;
        private AnimationCurve HeightCurve => terrainSettingsSo.HeightCurve;
        private float HeightMultiplier => terrainSettingsSo.HeightMultiplier;

        private HeightMap heightMap;
        private IMeshData meshData;
        public Vector3[] pointCloud;

        public bool withTexture = true;
        public bool drawNormals;


        // Actualiza el Mapa de Ruido y la Textura asociada
        public void BuildHeightMap()
        {
            heightMap = HeightMapGenerator.CreatePerlinNoiseHeightMap(NoiseParams, HeightCurve);
            if (withTexture)
                BuildTexture();
        }

        public void BuildTexture()
        {
            meshRenderer.sharedMaterial.mainTexture = TextureGenerator.BuildTexture2D(heightMap, gradient);
            meshRenderer.enabled = withTexture;
        }

        public void ResetRandomSeed() => terrainSettingsSo.ResetSeed();

        #region TIN

        public Tin tin;
        public float errorTolerance = 0.1f;
        public int maxIterations = 100;

        // Genera el TIN en un frame
        private void BuildTinMesh()
        {
            meshData = TinGenerator.BuildTinMeshData(
                out tin,
                heightMap,
                errorTolerance,
                HeightMultiplier,
                maxIterations
            );
            UpdateMesh();
        }

        #endregion


        #region PROGRESSIVE GENERATION

        public bool progressiveBuild = true;

        // Puntos por iteración
        private readonly Dictionary<int, int> distribucionPuntosConsecutivos = new();

        [Range(1, 30)] public int maxPointsPerCycle = 15;
        [Range(0, 20)] public int minDistanceBetweenPointPerCycle = 5;

        private float firstPointError = 100;

        public int fase;
        private float timeConsumed;
        private float maxPercentageReached;

        [FormerlySerializedAs("progressiveGenerationRunning")] [FormerlySerializedAs("animationRunning")]
        public bool isRunning;

        // Arrow Objects
        public GameObject arrowPrefab;
        private readonly List<GameObject> lastArrows = new();

        // Genera el TIN de forma progresiva
        public bool RunIteration()
        {
            if (tin == null)
                fase = 0;

            if (heightMap.IsEmpty)
                BuildHeightMap();

            var finished = false;
            if (tin == null || fase == 0)
            {
                timeConsumed = 0;
                InitializeTin();
            }
            else
            {
                finished = !tin.AddPointLoopIteration(maxPointsPerCycle, minDistanceBetweenPointPerCycle);
                DrawAddedPointArrow();

                if (finished)
                {
                    OnFinished();
                }
                else
                {
                    // Actualiza la distribucion de puntos consecutivos añadidos en una iteracion
                    var numPuntos = tin.lastVertexAdded.Count;
                    if (!distribucionPuntosConsecutivos.TryAdd(numPuntos, 1))
                        distribucionPuntosConsecutivos[numPuntos]++;
                }
            }

            timeConsumed += Time.deltaTime;
            OnIterationEnd();
            fase++;
            return finished;
        }

        public void ResetTin()
        {
            StopAllCoroutines();
            isRunning = false;

            BuildHeightMap();
            InitializeTin();
            fase = 0;

            // Points Added Arrows
            DrawAddedPointArrow();

            // MESH
            UpdateMesh();

            ResetPath();
        }


        private void InitializeTin()
        {
            var size = heightMap.Size;
            tin = new Tin(heightMap.map, heightMap.Size, errorTolerance, HeightMultiplier);
            tin.InitGeometry(heightMap.map, size);
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
                if (Input.GetKeyDown(KeyCode.Space) || RunIteration())
                    break;

                yield return null;
            }
        }

        private void OnIterationEnd()
        {
            UpdateMesh();
            UpdateUIStats();
            UpdateProgressionBar();
        }

        private void OnFinished()
        {
            uiController.UpdateProgressBar(1);

            // Muestra los Resultados
            Debug.Log("TERMINADO");

            var sb = new StringBuilder();
            foreach (var entry in distribucionPuntosConsecutivos)
                sb.AppendLine($"Iteraciones con {entry.Key} puntos de golpe: {entry.Value}");

            Debug.Log($"Distribucion de puntos consecutivos: \n{sb}");
            Debug.Log($"Tiempo consumido: {timeConsumed}");

            isRunning = false;
            StopAllCoroutines();
        }

        #region ITERATION VISUALIZATION

        private void DrawAddedPointArrow()
        {
            // Eliminamos las anteriores
            foreach (var lastArrow in lastArrows)
                if (Application.isEditor)
                    DestroyImmediate(lastArrow);
                else
                    Destroy(lastArrow, 0.1f);

            lastArrows.Clear();

            if (tin is not { lastVertexAdded: not null } || tin.lastVertexAdded.Count == 0) return;

            foreach (var t in tin.lastVertexAdded)
                lastArrows.Add(
                    Instantiate(
                        arrowPrefab,
                        t.v3D + Vector3.up * 10,
                        Quaternion.identity
                    )
                );
        }

        private void OnDrawGizmosIteration()
        {
            if (tin?.lastVertexAdded == null || tin.lastVertexAdded.Count == 0) return;

            Gizmos.color = Color.red;
            foreach (var t in tin.lastVertexAdded)
                Gizmos.DrawSphere(t.v3D, 1);
        }

        #endregion

        #endregion

        #endregion


        #region UI

        private TinControlUI uiController;
        private Minimap minimap;

        private void UpdateProgressionBar()
        {
            switch (fase)
            {
                case 0:
                    uiController.UpdateProgressBar(0);
                    maxPercentageReached = 0;
                    return;
                // Guarda el primer punto de mayor error como el maximo error
                case 1 when tin.lastVertexAdded.Count > 0:
                    firstPointError = tin.lastVertexError[^1];
                    break;
            }

            // Actualiza la Barra de Progreso con un valor entre 0% y 100%
            // 0% -> Error del Primer punto añadido
            // 100% -> Error maximo Tolerado
            if (tin.lastVertexAdded.Count <= 0) return;

            var error = tin.lastVertexError[0];
            var percentage = (error - errorTolerance) / (firstPointError - errorTolerance);
            var inversePercentage = 1f - percentage;
            var progressValue =
                Mathf.Clamp(Mathf.Max(Mathf.Pow(inversePercentage, 6), maxPercentageReached), 0, 1);
            maxPercentageReached = Mathf.Max(progressValue, maxPercentageReached);
            uiController.UpdateProgressBar(maxPercentageReached);
        }

        private void UpdateUIStats()
        {
            uiController.time.text = Mathf.FloorToInt(timeConsumed / 60) + ":" + Mathf.FloorToInt(timeConsumed % 60);
            uiController.iterations.text = fase + " iterations";
            uiController.vertices.text = tin.vertices.Count + " vertices";
            uiController.triangles.text = tin.triangles.Count + " triangles";
        }

        #endregion

        #region MESH

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        private void OnDrawGizmosNormals()
        {
            if (!meshFilter || !drawNormals) return;

            var mesh = meshFilter.sharedMesh;

            if (mesh == null) return;

            for (var i = 0; i < mesh.vertices.Length; i++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(mesh.vertices[i], mesh.vertices[i] + mesh.normals[i] * 10);
            }
        }

        // Actualiza la Malla con el TIN generado
        private void UpdateMesh()
        {
            if (tin == null) return;
            meshData = TinGenerator.TinToMesh(tin);
            meshFilter.sharedMesh = meshData.CreateMesh();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        #endregion
    }
}