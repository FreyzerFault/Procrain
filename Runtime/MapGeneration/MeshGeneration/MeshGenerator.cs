using System;
using Noise;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MapGeneration.MeshGeneration
{
    public static class MeshGenerator
    {
        public static IMeshData BuildMeshData(HeightMap map, int lod = 0, float heightMultiplier = 100)
        {
            // La malla la creamos centrada en 0:
            var initCoord = (map.Size - 1) / -2f;

            // El LOD NECESITA ser múltiplo de la anchura para que sea simétrico
            while (lod != 0 && (map.Size - 1) % lod != 0)
                lod += 1;

            // Incremento entre vertices para asegurar el LOD
            var simplificationIncrement = lod == 0 ? 1 : lod * 2;
            var verticesPerLine = (map.Size - 1) / simplificationIncrement + 1;

            var data = new MeshDataStatic(verticesPerLine, verticesPerLine, lod);

            var vertIndex = 0;
            for (var y = 0; y < map.Size; y += simplificationIncrement)
            for (var x = 0; x < map.Size; x += simplificationIncrement)
            {
                data.AddVertex(
                    new Vector3(
                        initCoord + x,
                        map.GetHeight(x, y) * heightMultiplier,
                        initCoord + y
                    )
                );
                data.AddUV(new Vector2((float)x / map.Size, (float)y / map.Size));

                // TODO : Añadir color???
                // if (gradCopy != null)
                //     data.AddColor(gradCopy.Evaluate(heightMap.GetHeight(x, y)));

                // Ignorando la ultima fila y columna de vertices, añadimos los triangulos
                if (x < map.Size - 1 && y < map.Size - 1)
                {
                    data.AddTriangle(vertIndex, vertIndex + verticesPerLine, vertIndex + verticesPerLine + 1);
                    data.AddTriangle(vertIndex + verticesPerLine + 1, vertIndex + 1, vertIndex);
                }

                vertIndex++;
            }

            return data;
        }


        // TODO: Generar la Mesh de cero, sin tener de iterar por un mapa de alturas
        // TODO: Distinto lod segun los parametros de input
        public static IMeshData BuildMesh(
            PerlinNoiseParams noiseParams, AnimationCurve heightCurve = null, int lod = 0
        ) =>
            throw new NotImplementedException();
    }

    public static class MeshGeneratorThreadSafe
    {
        public static void BuildMeshData(
            MeshDataThreadSafe meshData, HeightMapThreadSafe map, int lod = 0, float heightMultiplier = 100
        )
        {
            // La malla la creamos centrada en 0:
            var initCoord = (map.Size - 1) / -2f;

            // El LOD NECESITA ser múltiplo de la anchura para que sea simétrico
            while (lod != 0 && (map.Size - 1) % lod != 0)
                lod += 1;

            // Incremento entre vertices para asegurar el LOD
            var simplificationIncrement = lod == 0 ? 1 : lod * 2;
            var verticesPerLine = (map.Size - 1) / simplificationIncrement + 1;

            if (meshData.IsEmpty)
                meshData = new MeshDataThreadSafe(verticesPerLine, verticesPerLine, lod);

            var vertIndex = 0;
            for (var y = 0; y < map.Size; y += simplificationIncrement)
            for (var x = 0; x < map.Size; x += simplificationIncrement)
            {
                meshData.AddVertex(
                    new float3(
                        initCoord + x,
                        map.GetHeight(x, y) * heightMultiplier,
                        initCoord + y
                    )
                );
                meshData.AddUV(new float2(x, y) / map.Size);

                // TODO : Añadir color???
                // if (gradCopy != null)
                //     data.AddColor(gradCopy.Evaluate(heightMap.GetHeight(x, y)));

                // Ignorando la ultima fila y columna de vertices, añadimos los triangulos
                if (x < map.Size - 1 && y < map.Size - 1)
                {
                    var tri1 = new int3(vertIndex, vertIndex + verticesPerLine, vertIndex + verticesPerLine + 1);
                    var tri2 = new int3(vertIndex + verticesPerLine + 1, vertIndex + 1, vertIndex);
                    meshData.AddTriangle(tri1);
                    meshData.AddTriangle(tri2);
                }

                vertIndex++;
            }
        }

        [BurstCompile]
        public struct BuildMeshDataJob : IJob
        {
            public HeightMapThreadSafe heightMap;
            public MeshDataThreadSafe meshData;
            public int lod;
            public float heightMultiplier;

            public void Execute() => BuildMeshData(meshData, heightMap, lod, heightMultiplier);
        }
    }
}