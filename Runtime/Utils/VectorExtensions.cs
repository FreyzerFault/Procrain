using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Procrain.Utils
{
    public static class VectorExtensions
    {
        public const float Epsilon = 0.00000001f;
        
        public static bool Equals(float a, float b) => Mathf.Abs(a - b) < Epsilon;
        public static bool Equals(Vector2 v1, Vector2 v2) => Mathf.Abs((v1 - v2).magnitude) < Epsilon;

        
        #region 3D to 2D

        public static Vector2 ToV2(this Vector3 v, bool isXZplane = false) => isXZplane ? v.ToV2XZ() : v.ToV2XY();
        public static Vector2 ToV2XZ(this Vector3 v) => new(v.x, v.z);
        public static Vector2 ToV2XY(this Vector3 v) => new(v.x, v.y);

        public static IEnumerable<Vector2> ToV2(this IEnumerable<Vector3> v, bool isXZplane = false) =>
            isXZplane ? v?.ToV2XZ() : v?.ToV2XY();

        public static IEnumerable<Vector2> ToV2XZ(this IEnumerable<Vector3> v) => v.Select(ToV2XZ);
        public static IEnumerable<Vector2> ToV2XY(this IEnumerable<Vector3> v) => v.Select(ToV2XY);

        #endregion

        
        #region 2D to 3D

        public static Vector3 ToV3(this Vector2 v, bool xZplane = false) => xZplane ? v.ToV3XZ() : v.ToV3XY();
        public static Vector3 ToV3XZ(this Vector2 v) => new(v.x, 0, v.y);
        public static Vector3 ToV3XY(this Vector2 v) => new(v.x, v.y, 0);

        public static IEnumerable<Vector3> ToV3(this IEnumerable<Vector2> v, bool xZplane = false) =>
            xZplane ? v?.ToV3XZ() : v?.ToV3XY();

        public static IEnumerable<Vector3> ToV3XZ(this IEnumerable<Vector2> v) => v.Select(ToV3XZ);
        public static IEnumerable<Vector3> ToV3XY(this IEnumerable<Vector2> v) => v.Select(ToV3XY);


        public static Vector3 WithX(this Vector3 v, float x) => new(x, v.y, v.z);
        public static Vector3 WithY(this Vector3 v, float y) => new(v.x, y, v.z);
        public static Vector3 WithZ(this Vector3 v, float z) => new(v.x, v.y, z);

		
        // [X, Y, X, Y, ...] => [(X,Y), (X,Y) ,...]
        public static Vector2[] ToVector2Array(this double[] xy) => 
            Enumerable.Range(0, xy.Length / 2)
                .Select(i => new Vector2((float)xy[i * 2], (float)xy[i * 2 + 1])).ToArray();
		
        public static Vector2[] ToVector2Array(this float[] xy) => 
            Enumerable.Range(0, xy.Length / 2)
                .Select(i => new Vector2(xy[i * 2], xy[i * 2 + 1])).ToArray();
		
        // [X, Y, Z, X, Y, Z, ...] => [(X,Y,Z), (X,Y,Z) ,...]
        public static Vector3[] ToVector3Array(this double[] xyz) => 
            Enumerable.Range(0, xyz.Length / 3)
                .Select(i => new Vector3((float)xyz[i * 3], (float)xyz[i * 3 + 1], (float)xyz[i * 3 + 2])).ToArray();
		
        public static Vector3[] ToVector3Array(this float[] xyz) => 
            Enumerable.Range(0, xyz.Length / 3)
                .Select(i => new Vector3(xyz[i * 3], xyz[i * 3 + 1], xyz[i * 3 + 2])).ToArray();
		
        #endregion
        

        #region POINT CLOUD ANALYSIS
        
        // MAX / MIN from a collection of points => Can build AABB
        public static Vector2 MinPosition(this IEnumerable<Vector2> points) =>
            points.Aggregate(Vector2.positiveInfinity, Vector2.Min);

        public static Vector2 MaxPosition(this IEnumerable<Vector2> points) =>
            points.Aggregate(Vector2.negativeInfinity, Vector2.Max);

        public static Vector3 MinPosition(this IEnumerable<Vector3> points) =>
            points.Aggregate(Vector3.positiveInfinity, Vector3.Min);

        public static Vector3 MaxPosition(this IEnumerable<Vector3> points) =>
            points.Aggregate(Vector3.negativeInfinity, Vector3.Max);

        #endregion
        
        
        #region POINT RELATIVE POSITION TEST

        /// <summary>
        ///     AreaTri (p,begin,end) == NEGATIVO => Esta a la Derecha de la Arista (begin -> end)
        /// </summary>
        public static bool IsRight(this Vector2 p, Vector2 begin, Vector2 end) => TriArea2(begin, end, p) < -Epsilon;
        public static bool IsRight(this Vector3 p, Vector3 begin, Vector3 end) => TriArea2(begin, end, p) < -Epsilon;

        public static bool IsLeft(this Vector2 p, Vector2 begin, Vector2 end) => TriArea2(begin, end, p) > Epsilon;
        public static bool IsLeft(this Vector3 p, Vector3 begin, Vector3 end) => TriArea2(begin, end, p) > Epsilon;
		
        public static bool CollinearPointInLine(Vector2 begin, Vector2 end, Vector2 p) => Equals(TriArea2(begin, end, p), 0);

        #endregion

        
        #region POINT INSIDE TEST

        
        /// <summary>
        ///     <para>Comprueba si el punto p esta dentro del Circulo formado por a,b,c</para>
        ///     <para>
        ///         Implicitamente lo que hace es comprobar si el Angulo(a,b,c) <= Angulo(p,b,c).
        ///         Siendo el Angulo del punto a y p
        ///     </para>
        ///     <para>Si p pertenece a la Circunferencia, se considera FUERA</para>
        /// </summary>
        /// <param name="p">Punto fuera o dentro</param>
        /// <returns>FALSE si esta fuera o es colinear con la Circunferencia</returns>
        public static bool PointInCirle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 centro = CircleCenter(a, b, c);

            // Si el radio es mayor que la distancia de P al Centro => DENTRO
            return (a - centro).magnitude > (p - centro).magnitude;
        }

        /// <summary>
        ///     Comprueba si el Punto p esta en linea definida por los puntos A,B
        /// </summary>
        /// <returns></returns>
        public static bool PointOnLine(Vector2 a, Vector2 b, Vector2 p) => CollinearPointInLine(a, b, p);

        /// <summary>
        ///     Comprueba si el Punto P esta en el segmento A-B
        /// </summary>
        /// <returns></returns>
        public static bool PointOnSegment(Vector2 p, Vector2 a, Vector2 b) =>
            PointOnLine(a, b, p) && (p - a).magnitude + (p - b).magnitude <= (a - b).magnitude + Epsilon;


        #endregion

        
        #region INTERSECTIONS LINES

        /// <summary>
        ///     Calcula la interseccion de dos rectas definidas por los puntos (a,b) y (c,d)
        /// </summary>
        /// <returns>NULL si son paralelas</returns>
        public static bool IntersectionLineLine(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 intersection)
        {
            intersection = Vector2.zero;
			
            Vector2 ab = b - a;
            Vector2 cd = d - c;
            Vector2 ac = c - a;

            // t = (cd x ac) / (ab x cd)
            // s = (ab x ap) / (ab x cd)
            // (x: Cross Product)

            float denominador = cd.x * ab.y - ab.x * cd.y;

            // Colinear
            if (Mathf.Abs(denominador) < Epsilon) return false;

            float t = (cd.x * ac.y - ac.x * cd.y) / denominador;

            intersection = a + ab * t;
            return true;
        }

        #endregion
        
        
        #region AREAS
        
        /// <summary>
        ///     Area del Triangulo al Cuadrado (para clasificar puntos a la derecha o izquierda de un segmento)
        ///     Area POSITIVA => IZQUIERDA
        ///     Area NEGATIVA => DERECHA
        /// </summary>
        public static float TriArea2(Vector2 p1, Vector2 p2, Vector2 p3)
            => Det3X3(p1.x, p1.y, 1, p2.x, p2.y, 1, p3.x, p3.y, 1);

        public static float TriArea2(Vector3 p1, Vector3 p2, Vector3 p3)
            => Det3X3(p1.x, p1.z, 1, p2.x, p2.z, 1, p3.x, p3.z, 1);

        /// <summary>
        ///     Determinante de una Matriz 3x3
        ///     ((a,b,c),(d,e,f),(g,h,i)
        /// </summary>
        private static float Det3X3(
            float a, float b, float c,
            float d, float e, float f,
            float g, float h, float i
        ) => a * e * i + g * b * f + c * d * h - c * e * g - i * d * b - a * h * f;

        #endregion
        
        
        #region CIRCLES

        /// <summary>
        ///     Calcula el Centro de un Circulo que pasa por 3 puntos (a,b,c)
        /// </summary>
        /// <returns>
        ///		Circuncentro
        ///		Si son colineares => Punto medio
        /// </returns>
        public static Vector2 CircleCenter(Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 abMediatriz = Vector2.Perpendicular(b - a).normalized;
            Vector2 bcMediatriz = Vector2.Perpendicular(b - c).normalized;

            Vector2 abMedio = a + (b - a) / 2;
            Vector2 bcMedio = b + (c - b) / 2;

            // Circuncentro
            if (IntersectionLineLine(abMedio, abMedio + abMediatriz, bcMedio, bcMedio + bcMediatriz,
                    out Vector2 intersection))
                return intersection;
			
            // Punto Medio
            return  (a + b + c) / 3;
        }

        #endregion
    }
}
