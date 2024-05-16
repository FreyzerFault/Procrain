using System;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using UnityEngine;

namespace Procrain.Geometry
{
    public class Edge
    {
        public enum PointEdgePosition { RIGHT, LEFT, COLINEAR }

        // Begin -> End
        public Vector3 begin;
        public Vector3 end;
        private int index;

        // [Left, Right] (CCW, CW)
        public Tuple<Triangle, Triangle> tris;
        public Triangle LeftTri => tris.Item1;
        public Triangle RightTri => tris.Item2;

        public Edge(Vector3 begin, Vector3 end, Triangle tIzq = null, Triangle tDer = null, int index = -1)
        {
            this.index = index;

            this.begin = begin;
            this.end = end;
            tris = new Tuple<Triangle, Triangle>(tIzq, tDer);
        }

        /// <summary>
        ///     El Eje es Frontera siempre que le falte asignarle un Triangulo a la Izquierda o Derecha
        /// </summary>
        public bool IsFrontier => tris.Item1 == null || tris.Item2 == null;

        /// <summary>
        ///     Asigna un Triangulo segun su posicion como Izquierdo o Derecho
        /// </summary>
        public void AssignTriangle(Triangle tri)
        {
            if (!tri.GetOppositeVertex(out Vector3 opposite, this)) return;
            tris = GeometryUtils.IsRight(opposite, begin, end)
                ? new Tuple<Triangle, Triangle>(tris.Item1, tri)
                : new Tuple<Triangle, Triangle>(tri, tris.Item2);
        }

        public Triangle OppositeTri(Triangle tri) => tri == LeftTri ? RightTri : LeftTri;

        /// <summary>
        ///     NEGATIVA => DERECHA; POSITIVA => IZQUIERDA; ~0 => COLINEAR
        ///     (tiene un margen grande para no crear triangulos sin apenas grosor)
        /// </summary>
        /// <param name="p"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns>RIGHT / LEFT / COLINEAR</returns>
        public static PointEdgePosition GetPointEdgePosition(Vector2 p, Vector2 begin, Vector2 end)
        {
            float area = GeometryUtils.TriArea2(begin, end, p);

            // EPSILON Grande en este caso, porque las veces que cae un punto en un triangulo
            // puede estar muy cerca de una arista y el resultado puede ser un Triangulo muy estirado

            return area > 0.1f
                ? PointEdgePosition.LEFT
                : area < -0.1f
                    ? PointEdgePosition.RIGHT
                    : PointEdgePosition.COLINEAR;
        }

        public static PointEdgePosition GetPointEdgePosition(Vector3 p, Vector3 begin, Vector3 end) =>
            GetPointEdgePosition(p.ToV2xz(), begin.ToV2xz(), end.ToV2xz());

        /// <summary>
        ///     Interpolacion de la altura en un punto 2D en la Arista.
        ///     Inversamente proporcional a la distancia de cada vertice al punto 2D
        /// </summary>
        /// <param name="point">Punto 2D</param>
        /// <returns></returns>
        public float GetHeightInterpolation(Vector2 point)
        {
            // Interpolamos la altura entre begin y end
            float distBegin = (point - new Vector2(begin.x, begin.z)).magnitude;
            float distEnd = (point - new Vector2(end.x, end.z)).magnitude;

            float distanceInterpolation = 0;
            distanceInterpolation += begin.y / distBegin;
            distanceInterpolation += end.y / distEnd;
            return distanceInterpolation / (1 / distBegin + 1 / distEnd);
        }


        /// <summary>
        ///     Calcula el Punto de interseccion de un Segmento A -> B
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="intersectionPoint"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool GetIntersectionPoint(Vector2 a, Vector2 b, out Vector2? intersectionPoint)
        {
            intersectionPoint = null;
            PointEdgePosition posA = GetPointEdgePosition(a, begin.ToV2xz(), end.ToV2xz());
            PointEdgePosition posB = GetPointEdgePosition(b, begin.ToV2xz(), end.ToV2xz());

            // Solo hay interseccion si los dos puntos estan en lados opuestos de la arista
            if ((posA == PointEdgePosition.RIGHT && posB == PointEdgePosition.LEFT) ||
                (posA == PointEdgePosition.LEFT && posB == PointEdgePosition.RIGHT))
            {
                Vector2 c = begin.ToV2xz();
                Vector2 d = end.ToV2xz();

                Vector2 ab = b - a;
                Vector2 cd = d - c;
                Vector2 ac = c - a;

                float denominador = cd.x * ab.y - ab.x * cd.y;

                if (denominador == 0) throw new Exception("La interseccion es paralela");

                float s = (cd.x * ac.y - ac.x * cd.y) / denominador;
                float t = (ab.x * ac.y - ac.x * ab.y) / denominador;

                // Si s o t estan fuera de [0,1] => la interseccion esta fuera de los segmentos
                if (s < 0 || s > 1 || t < 0 || t > 1) return false;

                intersectionPoint = a + (b - a) * s;

                return true;
            }

            intersectionPoint = null;
            return false;
        }

        public override string ToString() => "e" + index + " {" + begin + " -> " + end + "}";

        /// <summary>
        ///     No puede haber mas de un Eje con los mismos vertices
        /// </summary>
        public override int GetHashCode() => begin.GetHashCode() + end.GetHashCode();
    }
}
