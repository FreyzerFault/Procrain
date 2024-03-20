using System.Collections.Generic;
using MapDisplay;
using MapGeneration.MeshGeneration;
using Noise;
using UnityEngine;

namespace MapGeneration.TerrainGeneration.InfiniteGeneration
{
    public class TerrainChunk : MapDisplayInMesh
    {
        [SerializeField] private Vector2Int chunkCoord;

        [SerializeField] private PerlinNoiseParams localNoiseParams = PerlinNoiseParams.Default();

        // LOD local del Chunk
        [SerializeField] private int lod;
        private readonly Dictionary<int, IMeshData> meshDataPerLOD = new();
        private Bounds bounds;

        private Transform playerTransform;

        public override int LOD
        {
            get => lod;
            set => lod = value;
        }

        public override PerlinNoiseParams NoiseParams
        {
            get => localNoiseParams;
            set
            {
                localNoiseParams = value;
                BuildHeightMap();
            }
        }

        private int Size => localNoiseParams.size;

        private Vector2Int PlayerChunk => GetChunkCoord(
            playerTransform?.position ?? GameObject.FindWithTag("Player").transform.position
        );

        private float Extent => Size / 2f;
        private Vector3 CenterPos => new(transform.position.x + Extent, 0, transform.position.z + Extent);

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
            get => gradient;
            set => ApplyGradient(value);
        }

        // =================================================================================================== //
        // Parametros que dependen del Chunk:

        private int LodByDistToPlayer
        {
            get
            {
                // Si no es potencia de 2, redondea al siguiente
                var dist = Mathf.FloorToInt(Vector2Int.Distance(PlayerChunk, chunkCoord));
                return dist == 0 ? 0 : Mathf.ClosestPowerOfTwo(dist);
            }
        }

        // Posicion del Chunk en el Espacio de Mundo
        private Vector2Int WorldPosition2D => chunkCoord * Size;
        private Vector3Int WorldPosition3D => new(WorldPosition2D.x, 0, WorldPosition2D.y);

        protected override void Awake()
        {
            base.Awake();
            playerTransform = GameObject.FindWithTag("Player")?.transform;
            localNoiseParams = terrainSettingsSo.NoiseParams;
        }

        // No construir el mapa al iniciar
        protected override void Start() => playerTransform = GameObject.FindWithTag("Player")?.transform;

        protected override void BuildMeshData()
        {
            // Actualiza la Malla al LOD actual si ya fue generada
            if (meshDataPerLOD.TryGetValue(LOD, out meshData)) return;

            // Si no la genera y la guarda
            meshData = MeshGenerator.BuildMeshData(heightMap, LOD, HeightMultiplier);
            meshDataPerLOD.Add(LOD, meshData);
        }

        // Cuando se posiciona en su coordenada se construye el Mapa
        public void MoveToCoord(Vector2Int coord)
        {
            chunkCoord = coord;
            transform.localPosition = WorldPosition3D;
            localNoiseParams.offset = -new Vector2(WorldPosition2D.x, WorldPosition2D.y);
            BuildHeightMap();
        }

        protected override void BuildHeightMap()
        {
            heightMap = HeightMapGenerator.CreatePerlinNoiseHeightMap(localNoiseParams, HeightCurve);
            meshDataPerLOD.Clear();
        }

        private void ApplyGradient(Gradient newGradient)
        {
            gradient = newGradient;
            if (textureMode != TextureMode.SetTexture) return;

            BuildTextureData();
            SetTexture();
        }


        /// <summary>
        ///     Actualiza la Visibilidad del Chunk (si debe ser renderizado o no).
        ///     Y actualiza tambien el LOD
        /// </summary>
        /// <param name="maxRenderDist">Distancia Maxima de Renderizado de Chunks</param>
        public void UpdateVisibility(int maxRenderDist)
        {
            // La distancia del jugador al chunk
            var chunkDistance = Mathf.FloorToInt(Vector2Int.Distance(ChunkCoord, PlayerChunk));

            // Sera visible si la distancia al player viewer es menor a la permitida
            Visible = chunkDistance <= maxRenderDist;

            // Si no está visible no hace falta actualizar el LOD
            if (!Visible) return;
            UpdateLOD();
        }

        public void UpdateLOD()
        {
            // LOD = Distancia en Chunks al Viewer
            var newLOD = LodByDistToPlayer;
            if (newLOD == LOD && meshData != null) return;

            // LOD changed => Reload Mesh
            LOD = newLOD;
            BuildMeshData();
            DisplayMap();
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