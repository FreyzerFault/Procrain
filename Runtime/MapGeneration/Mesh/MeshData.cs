using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.MapGeneration.Mesh
{
    public interface IMeshData
    {
        UnityEngine.Mesh CreateMesh();
        UnityEngine.Mesh ApplyMesh(UnityEngine.Mesh mesh);
    }

    public class MeshDataStatic : IMeshData
    {
        // Colores por vertice en caso de no usar textura
        private readonly Color[] colors;
        private readonly int[] triangles;
        private readonly Vector2[] uvs;
        private readonly Vector3[] vertices;
        private int colorIndex;

        public int lod;
        private int triIndex;
        private int uvIndex;

        private int vertIndex;

        public MeshDataStatic(int width, int height, int lod = 0)
        {
            this.lod = lod;
            vertices = new Vector3[width * height];
            uvs = new Vector2[width * height];
            colors = new Color[width * height];
            triangles = new int[(width - 1) * (height - 1) * 6];
        }

        /// Creacion del Objeto Mesh que necesita Unity (no Paralelizable)
        public UnityEngine.Mesh CreateMesh() => ApplyMesh(new UnityEngine.Mesh());

        public UnityEngine.Mesh ApplyMesh(UnityEngine.Mesh mesh)
        {
            if (mesh == null)
                mesh = new UnityEngine.Mesh();

            mesh.Clear();

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors = colors;

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.Optimize();

            return mesh;
        }

        public void AddVertex(Vector3 vertex) => vertices[vertIndex++] = vertex;

        public void AddUV(Vector2 uv) => uvs[uvIndex++] = uv;

        public void AddColor(Color color) => colors[colorIndex++] = color;

        public void AddTriangle(int a, int b, int c)
        {
            if (a >= vertices.Length || b >= vertices.Length || c >= vertices.Length)
            {
                Debug.LogError(
                    "Triangle out of Bounds!!! "
                        + vertices.Length
                        + " Vertices. Triangle("
                        + a
                        + ", "
                        + b
                        + ", "
                        + c
                        + ")"
                );
                return;
            }

            triangles[triIndex++] = a;
            triangles[triIndex++] = b;
            triangles[triIndex++] = c;
        }

        public void Reset()
        {
            vertIndex = 0;
            triIndex = 0;
            uvIndex = 0;
            colorIndex = 0;
        }
    }

    public class MeshDataDynamic : IMeshData
    {
        private readonly List<Color> colors = new();
        private readonly List<int> triangles = new();
        private readonly List<Vector2> uvs = new();
        private readonly List<Vector3> vertices = new();

        // Creacion del Objeto Mesh que necesita Unity (no Paralelizable)
        public UnityEngine.Mesh CreateMesh() => ApplyMesh(new UnityEngine.Mesh());

        public UnityEngine.Mesh ApplyMesh(UnityEngine.Mesh mesh)
        {
            if (mesh == null)
                mesh = new UnityEngine.Mesh();

            mesh.Clear();

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.colors = colors.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.Optimize();

            return mesh;
        }

        public void AddVertex(Vector3 vertex) => vertices.Add(vertex);

        public void AddVertex(Vector2 vertex, float height) =>
            vertices.Add(new Vector3(vertex.x, height, vertex.y));

        public void AddVertex(float x, float y, float height) =>
            vertices.Add(new Vector3(x, height, y));

        public void AddUV(Vector2 uv) => uvs.Add(uv);

        public void AddUV(float u, float v) => uvs.Add(new Vector2(u, v));

        public void AddColor(Color color) => colors.Add(color);

        public void AddColor(float r, float g, float b) => colors.Add(new Color(r, g, b));

        public void AddTriangle(int a, int b, int c)
        {
            if (a >= vertices.Count || b >= vertices.Count || c >= vertices.Count)
                throw new Exception(
                    "Triangle out of Bounds!!! "
                        + vertices.Count
                        + " Vertices. Triangle("
                        + a
                        + ", "
                        + b
                        + ", "
                        + c
                        + ")"
                );

            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }
    }

    public struct MeshData_ThreadSafe : IMeshData, IDisposable
    {
        private NativeArray<float3> vertices;
        private NativeArray<int> triangles;
        private NativeArray<float2> uvs;
        private NativeArray<Color> colors;

        private int vertIndex;
        private int triIndex;
        private int uvIndex;
        private int colorIndex;

        public readonly int width;
        public readonly int height;
        public int lod;

        public bool IsEmpty => !vertices.IsCreated || vertices.Length == 0;

        public MeshData_ThreadSafe(int lod = 0)
            : this() => this.lod = lod;

        public MeshData_ThreadSafe(int width, int height, int lod = 0)
            : this(lod)
        {
            this.width = width;
            this.height = height;
            vertices = new NativeArray<float3>(width * height, Allocator.Persistent);
            triangles = new NativeArray<int>((width - 1) * (height - 1) * 6, Allocator.Persistent);
            uvs = new NativeArray<float2>(width * height, Allocator.Persistent);
            colors = new NativeArray<Color>(width * height, Allocator.Persistent);
        }

        public MeshData_ThreadSafe(
            NativeArray<float3> vertices,
            NativeArray<int> triangles,
            NativeArray<float2> uvs,
            NativeArray<Color> colors,
            int lod = 0
        )
            : this(lod)
        {
            this.vertices = vertices;
            this.triangles = triangles;
            this.uvs = uvs;
            this.colors = colors;
        }

        // NO Thread Safe
        public UnityEngine.Mesh CreateMesh() => ApplyMesh(new UnityEngine.Mesh());

        public UnityEngine.Mesh ApplyMesh(UnityEngine.Mesh mesh)
        {
            if (mesh == null)
                mesh = new UnityEngine.Mesh();

            mesh.Clear();

            mesh.vertices = vertices.Select(vertex => (Vector3)vertex).ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.Select(uv => (Vector2)uv).ToArray();
            mesh.colors = colors.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.Optimize();

            return mesh;
        }

        public void AddVertex(float3 vertex) => vertices[vertIndex++] = vertex;

        public void AddUV(float2 uv) => uvs[uvIndex++] = uv;

        public void AddColor(Color color) => colors[colorIndex++] = color;

        public void AddTriangle(int3 indices)
        {
            if (
                indices.x >= vertices.Length
                || indices.y >= vertices.Length
                || indices.z >= vertices.Length
            )
            {
                var array = vertices;
                LogTriOutOfBoundsError(array, indices);
                return;
            }

            triangles[triIndex++] = indices.x;
            triangles[triIndex++] = indices.y;
            triangles[triIndex++] = indices.z;
        }

        public void Reset()
        {
            vertIndex = 0;
            triIndex = 0;
            uvIndex = 0;
            colorIndex = 0;
        }

        public void Dispose()
        {
            vertices.Dispose();
            triangles.Dispose();
            uvs.Dispose();
            colors.Dispose();
        }

        [BurstDiscard]
        public static void LogTriOutOfBoundsError(NativeArray<float3> vertices, int3 triIndices) =>
            Debug.LogError(
                "Triangle out of Bounds!!! "
                    + vertices.Length
                    + " Vertices. Triangle("
                    + triIndices.x
                    + ", "
                    + triIndices.y
                    + ", "
                    + triIndices.z
                    + ")"
            );
    }
}
