using Unity.Mathematics;
using UnityEngine;

namespace Procrain.Runtime.MapDisplay
{
    public class MapDisplayInTextureRealTime : MapDisplayInTexture
    {
        [Space] public bool movement;

        [Range(0, 10)] public float speed = 1;
        [Range(0, 100)] public float movementRadius = 10;

        private float angle;

        private void Update()
        {
            if (!Application.isPlaying || !movement) return;

            Move();
        }

        private void Move()
        {
            angle += Time.deltaTime * speed;
            Offset = new float2(Mathf.Cos(angle) * movementRadius, Mathf.Sin(angle) * movementRadius);
        }
    }
}