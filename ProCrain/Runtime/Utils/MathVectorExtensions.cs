using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain
{
    public static class MathVectorExtensions
    {
        public const float Epsilon = 0.00000001f;
        
        public static bool Equals(float a, float b) => Mathf.Abs(a - b) < Epsilon;
        public static bool Equals(float2 v1, float2 v2) => Mathf.Abs((v1 - v2).Magnitude()) < Epsilon;

        
        #region 3D to 2D

        public static float2 ToV2(this float3 v, bool isXZplane = false) => isXZplane ? v.ToV2XZ() : v.ToV2XY();
        public static float2 ToV2XZ(this float3 v) => new(v.x, v.z);
        public static float2 ToV2XY(this float3 v) => new(v.x, v.y);

        public static IEnumerable<float2> ToV2(this IEnumerable<float3> v, bool isXZplane = false) =>
            isXZplane ? v?.ToV2XZ() : v?.ToV2XY();

        public static IEnumerable<float2> ToV2XZ(this IEnumerable<float3> v) => v.Select(ToV2XZ);
        public static IEnumerable<float2> ToV2XY(this IEnumerable<float3> v) => v.Select(ToV2XY);

        #endregion

        
        #region 2D to 3D

        public static float3 ToV3(this float2 v, bool xZplane = false) => xZplane ? v.ToV3XZ() : v.ToV3XY();
        public static float3 ToV3XZ(this float2 v) => new(v.x, 0, v.y);
        public static float3 ToV3XY(this float2 v) => new(v.x, v.y, 0);

        public static IEnumerable<float3> ToV3(this IEnumerable<float2> v, bool xZplane = false) =>
            xZplane ? v?.ToV3XZ() : v?.ToV3XY();

        public static IEnumerable<float3> ToV3XZ(this IEnumerable<float2> v) => v.Select(ToV3XZ);
        public static IEnumerable<float3> ToV3XY(this IEnumerable<float2> v) => v.Select(ToV3XY);


        public static float3 WithX(this float3 v, float x) => new(x, v.y, v.z);
        public static float3 WithY(this float3 v, float y) => new(v.x, y, v.z);
        public static float3 WithZ(this float3 v, float z) => new(v.x, v.y, z);

		
        // [X, Y, X, Y, ...] => [(X,Y), (X,Y) ,...]
        public static float2[] ToVector2Array(this double[] xy) => 
            Enumerable.Range(0, xy.Length / 2)
                .Select(i => new float2((float)xy[i * 2], (float)xy[i * 2 + 1])).ToArray();
		
        public static float2[] ToVector2Array(this float[] xy) => 
            Enumerable.Range(0, xy.Length / 2)
                .Select(i => new float2(xy[i * 2], xy[i * 2 + 1])).ToArray();
		
        // [X, Y, Z, X, Y, Z, ...] => [(X,Y,Z), (X,Y,Z) ,...]
        public static float3[] ToVector3Array(this double[] xyz) => 
            Enumerable.Range(0, xyz.Length / 3)
                .Select(i => new float3((float)xyz[i * 3], (float)xyz[i * 3 + 1], (float)xyz[i * 3 + 2])).ToArray();
		
        public static float3[] ToVector3Array(this float[] xyz) => 
            Enumerable.Range(0, xyz.Length / 3)
                .Select(i => new float3(xyz[i * 3], xyz[i * 3 + 1], xyz[i * 3 + 2])).ToArray();
		
        #endregion
        

        #region POINT CLOUD ANALYSIS
        
        // MAX / MIN from a collection of points => Can build AABB
        public static float2 MinPosition(this IEnumerable<float2> points) =>
            points.Aggregate(new float2(math.INFINITY), math.min);

        public static float2 MaxPosition(this IEnumerable<float2> points) =>
            points.Aggregate(new float2(-math.INFINITY), math.max);

        public static float3 MinPosition(this IEnumerable<float3> points) =>
            points.Aggregate(new float3(math.INFINITY), math.min);

        public static float3 MaxPosition(this IEnumerable<float3> points) =>
            points.Aggregate(new float3(-math.INFINITY), math.max);

        #endregion
        
        
        #region POINT RELATIVE POSITION TEST

        /// <summary>
        ///     AreaTri (p,begin,end) == NEGATIVO => Esta a la Derecha de la Arista (begin -> end)
        /// </summary>
        public static bool IsRight(this float2 p, float2 begin, float2 end) => TriArea2(begin, end, p) < -Epsilon;
        public static bool IsRight(this float3 p, float3 begin, float3 end) => TriArea2(begin, end, p) < -Epsilon;

        public static bool IsLeft(this float2 p, float2 begin, float2 end) => TriArea2(begin, end, p) > Epsilon;
        public static bool IsLeft(this float3 p, float3 begin, float3 end) => TriArea2(begin, end, p) > Epsilon;
		
        public static bool CollinearPointInLine(float2 begin, float2 end, float2 p) => Equals(TriArea2(begin, end, p), 0);

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
        public static bool PointInCirle(float2 p, float2 a, float2 b, float2 c)
        {
            float2 centro = CircleCenter(a, b, c);

            // Si el radio es mayor que la distancia de P al Centro => DENTRO
            return (a - centro).Magnitude() > (p - centro).Magnitude();
        }

        /// <summary>
        ///     Comprueba si el Punto p esta en linea definida por los puntos A,B
        /// </summary>
        /// <returns></returns>
        public static bool PointOnLine(float2 a, float2 b, float2 p) => CollinearPointInLine(a, b, p);

        /// <summary>
        ///     Comprueba si el Punto P esta en el segmento A-B
        /// </summary>
        /// <returns></returns>
        public static bool PointOnSegment(float2 p, float2 a, float2 b) =>
            PointOnLine(a, b, p) && (p - a).Magnitude() + (p - b).Magnitude() <= (a - b).Magnitude() + Epsilon;


        #endregion

        
        #region INTERSECTIONS LINES

        /// <summary>
        ///     Calcula la interseccion de dos rectas definidas por los puntos (a,b) y (c,d)
        /// </summary>
        /// <returns>NULL si son paralelas</returns>
        public static bool IntersectionLineLine(float2 a, float2 b, float2 c, float2 d, out float2 intersection)
        {
            intersection = float2.zero;
			
            float2 ab = b - a;
            float2 cd = d - c;
            float2 ac = c - a;

            // t = (cd x ac) / (ab x cd)
            // s = (ab x ap) / (ab x cd)
            // (x: Cross Product)

            float denominador = cd.x * ab.y - ab.x * cd.y;

            // Colinear
            if (math.abs(denominador) < Epsilon) return false;

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
        public static float TriArea2(float2 p1, float2 p2, float2 p3)
            => Det3X3(p1.x, p1.y, 1, p2.x, p2.y, 1, p3.x, p3.y, 1);

        public static float TriArea2(float3 p1, float3 p2, float3 p3)
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
        public static Vector2 CircleCenter(float2 a, float2 b, float2 c)
        {
            
            float2 abMediatriz = math.normalize((b - a).Perpendicular());
            float2 bcMediatriz = math.normalize((b - c).Perpendicular());

            float2 abMedio = a + (b - a) / 2;
            float2 bcMedio = b + (c - b) / 2;

            // Circuncentro
            if (IntersectionLineLine(abMedio, abMedio + abMediatriz, bcMedio, bcMedio + bcMediatriz,
                    out float2 intersection))
                return intersection;
			
            // Punto Medio
            return  (a + b + c) / 3;
        }

        #endregion


        #region TRANSFORMATIONS

        public static float2 Perpendicular(this float2 vector) => new(-vector.y, vector.x);
        public static float Magnitude(this float2 vector) => math.length(vector);

        #endregion
    }
}
