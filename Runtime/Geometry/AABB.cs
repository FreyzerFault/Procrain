using UnityEngine;

namespace Geometry
{
    public class AABB
    {
        public Vector2 max;
        public Vector2 min;

        public AABB()
        {
            max = new Vector2(float.MinValue, float.MinValue);
            min = new Vector2(float.MaxValue, float.MaxValue);
        }

        public float Width => max.x - min.x;
        public float Height => max.y - min.y;

        /// <summary>
        ///     Comprueba si el Punto esta dentro del AABB
        /// </summary>
        /// <param name="p"></param>
        /// <returns>true si dentro</returns>
        public bool IsInside(Vector2 p) => p.x <= max.x && p.x >= min.x && p.y <= max.y && p.y >= min.y;
    }
}