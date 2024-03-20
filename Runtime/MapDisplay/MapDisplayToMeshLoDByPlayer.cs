using UnityEngine;

namespace MapDisplay
{
    // Actualiza el LOD del Mapa en Tiempo Real dependiendo de la distancia del Jugador
    [RequireComponent(
        typeof(MeshFilter),
        typeof(MeshRenderer)
    )]
    public class MapDisplayToMeshLoDByPlayer : MapDisplayInMesh
    {
        private Transform player;

        private Vector2 PlayerPos2D => new(player.position.x, player.position.z);

        protected override void Awake()
        {
            base.Awake();

            // Busca al Jugador para calcular el LOD dependiendo de su distancia
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            LOD = GetLOD(PlayerPos2D);
        }

        public void Update()
        {
            if (player == null) return;

            UpdateLOD(GetLOD(PlayerPos2D));
        }

        private int GetLOD(Vector2 playerWorldPos2D)
        {
            var position = transform.position;
            var terrainWorldPos = new Vector2(position.x, position.z);
            return Mathf.FloorToInt((terrainWorldPos - playerWorldPos2D).magnitude / NoiseParams.size);
        }
    }
}