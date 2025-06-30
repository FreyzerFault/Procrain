using System;
using Procrain.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.Geometry
{
    [Serializable]
    public struct Edge: IEquatable<Edge>
    {
        public enum PointEdgePosition { Right, Left, Colinear }

        public readonly int index;
        
        // Begin -> End
        public Vertex begin;
        public Vertex end;
        
        public float3 Begin_XYZ => begin.xyz;
        public float3 End_XYZ => end.xyz;
        
        public float2 Begin_XZ => begin.xz;
        public float2 End_XZ => end.xz;

        // [Left, Right] (CCW, CW)
        public Triangle leftTri;
        public Triangle rightTri;
        public Tuple<Triangle, Triangle> Tris => new(leftTri, rightTri);
        
        public static Edge InvalidEdge => new Edge(Vertex.InvalidVertex, Vertex.InvalidVertex); 
        public bool IsInvalid => begin.IsInvalid || end.IsInvalid;
        
        public Edge(Vertex begin, Vertex end, Triangle tIzq = null, Triangle tDer = null, int index = -1)
        {
            this.index = index;

            this.begin = begin;
            this.end = end;
            
            Tris = new Tuple<Triangle, Triangle>(tIzq, tDer);
        }

        /// <summary>
        ///     El Eje es Frontera siempre que le falte asignarle un Triangulo a la Izquierda o Derecha
        /// </summary>
        public bool IsFrontier => Tris.Item1 == null || Tris.Item2 == null;

        /// <summary>
        ///     Asigna un Triangulo segun su posicion como Izquierdo o Derecho
        /// </summary>
        public void AssignTriangle(Triangle tri)
        {
            if (!tri.GetOppositeVertex(out Vector3 opposite, this)) return;
            Tris = opposite.IsRight(begin, end)
                ? new Tuple<Triangle, Triangle>(Tris.Item1, tri)
                : new Tuple<Triangle, Triangle>(tri, Tris.Item2);
        }

        public readonly Triangle OppositeTri(Triangle tri) => tri.Equals(leftTri) ? rightTri : leftTri;

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
            float area = MathVectorExtensions.TriArea2(begin, end, p);

            // EPSILON Grande en este caso, porque las veces que cae un punto en un triangulo
            // puede estar muy cerca de una arista y el resultado puede ser un Triangulo muy estirado

            return area > 0.1f
                ? PointEdgePosition.Left
                : area < -0.1f
                    ? PointEdgePosition.Right
                    : PointEdgePosition.Colinear;
        }

        public static PointEdgePosition GetPointEdgePosition(Vector3 p, Vector3 begin, Vector3 end) =>
            GetPointEdgePosition(p.ToV2XZ(), begin.ToV2XZ(), end.ToV2XZ());

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
        public readonly bool GetIntersectionPoint(Vector2 a, Vector2 b, out Vector2? intersectionPoint)
        {
            intersectionPoint = null;
            PointEdgePosition posA = GetPointEdgePosition(a, begin.ToV2XZ(), end.ToV2XZ());
            PointEdgePosition posB = GetPointEdgePosition(b, begin.ToV2XZ(), end.ToV2XZ());

            // Solo hay interseccion si los dos puntos estan en lados opuestos de la arista
            if ((posA == PointEdgePosition.Right && posB == PointEdgePosition.Left) ||
                (posA == PointEdgePosition.Left && posB == PointEdgePosition.Right))
            {
                Vector2 c = begin.ToV2XZ();
                Vector2 d = end.ToV2XZ();

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

        public bool Equals(Edge other) => begin.Equals(other.begin) && begin.Equals(other.end);

        /// <summary>
        ///     No puede haber mas de un Eje con los mismos vertices
        /// </summary>
        public override int GetHashCode() => begin.GetHashCode() + end.GetHashCode();
    }
}
