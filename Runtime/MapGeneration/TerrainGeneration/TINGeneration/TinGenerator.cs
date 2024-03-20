using System;
using Procrain.Runtime.Geometry;
using Procrain.Runtime.MapGeneration.MeshGeneration;
using UnityEngine;

namespace Procrain.Runtime.MapGeneration.TerrainGeneration.TINGeneration
{
    public static class TinGenerator
    {
        /// <summary>
        ///     Genera un TIN a partir de un HeightMap
        /// </summary>
        /// <param name="heightMap">Mapa de Alturas</param>
        /// <param name="aabb">Bounding Box</param>
        /// <param name="heigthScale">[0,1] -> [0, heightScale]</param>
        /// <param name="errorTolerance">Error Maximo Tolerado por el Tin</param>
        /// <param name="maxIterations">Limite de iteraciones maximas del Tin</param>
        /// <returns>Datos de una Malla que va a usar Unity</returns>
        private static Tin BuildTin(
            HeightMap heightMap,
            float heigthScale = 1,
            float errorTolerance = 1,
            int maxIterations = 10,
            AABB aabb = null
        )
        {
            // Creacion del Tin (Estructura topologica interna)
            var tin = new Tin(
                heightMap.map,
                heightMap.Size,
                errorTolerance,
                heigthScale,
                maxIterations,
                aabb
            );
            tin.InitGeometry(heightMap.map, heightMap.Size);
            tin.AddPointLoop();

            // Creacion de la Malla
            return tin;
        }

        /// <summary>
        ///     Genera un TIN a partir de unos puntos iniciales (las esquinas formadas por el AABB)
        /// </summary>
        /// <param name="aabb">Bounding Box</param>
        /// <param name="errorTolerance">Error Maximo Tolerado por el Tin</param>
        /// <param name="heightMultiplier"></param>
        /// <param name="maxIterations">Limite de iteraciones maximas del Tin</param>
        /// <param name="points">Puntos extras pregenerados</param>
        /// <returns>Datos de una Malla que va a usar Unity</returns>
        private static Tin BuildTin(
            AABB aabb,
            float errorTolerance = 1,
            float heightMultiplier = 100,
            int maxIterations = 10,
            Vector3[] points = null
        )
        {
            // Creacion del Tin (Estructura topologica interna)
            var tin = new Tin(
                points ?? Array.Empty<Vector3>(),
                errorTolerance,
                heightMultiplier,
                maxIterations,
                aabb
            );
            tin.InitGeometry();
            tin.AddPointLoop();

            // Creacion de la Malla
            return tin;
        }

        /// <summary>
        ///     Generacion de la Malla a partir de un Tin
        /// </summary>
        /// <param name="tin"></param>
        /// <param name="heightCurve">Curva de Altura para ajustar la altura</param>
        /// <param name="gradient">Color segun la Altura (por si no se usa Textura, aplicado al vertice)</param>
        /// <returns>Datos de una Malla que va a usar en Unity</returns>
        public static MeshDataDynamic TinToMesh(Tin tin)
        {
            // var gradCopy = gradient != null ? gradient.GetGradientCopy() : GradientUtils.BuildGradientDefault();

            // Creacion de la malla (Datos basicos que necesita Unity)
            var data = new MeshDataDynamic();

            // Vertices
            foreach (var vertex in tin.vertices)
            {
                var point = vertex.v3D;
                data.AddVertex(point);
                data.AddUV(vertex.x / tin.size, vertex.z / tin.size);
                // data.AddColor(gradCopy.Evaluate(vertex.y));
            }

            // Indices
            foreach (var tri in tin.triangles)
                data.AddTriangle(tri.v1.index, tri.v3.index, tri.v2.index);

            return data;
        }

        public static IMeshData BuildTinMeshData(
            out Tin tin, HeightMap heightMap, float errorTolerance = 1,
            float heightMultiplier = 100,
            int maxIterations = 10
        )
        {
            tin = BuildTin(heightMap, heightMultiplier, errorTolerance, maxIterations);
            return TinToMesh(tin);
        }
    }
}