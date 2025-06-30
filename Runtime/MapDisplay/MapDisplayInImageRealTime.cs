using Procrain.Core;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.MapDisplay
{
    // Añade movimiento al MapDisplay para testear el rendimiento de la generación del Mapa en Tiempo Real
    public class MapDisplayInImageRealTime : MapDisplayInImage
    {
        [Space]
        public bool movement;

        [Range(0, 10)]
        public float speed = 1;

        [Range(0, 100)]
        public float movementRadius = 10;

        private float _angle;

        private void Update()
        {
            if (!Application.isPlaying || !movement)
                return;

            Move();
        }

        private void Move()
        {
            _angle += Time.deltaTime * speed;
            MapManager.NoiseParams.Offset = new float2(
                Mathf.Cos(_angle) * movementRadius,
                Mathf.Sin(_angle) * movementRadius
            );
        }
    }
}
