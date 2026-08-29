using Unity.Mathematics;
using UnityEngine;

namespace Procrain.Mesh
{
    public interface IMeshData
    {
        UnityEngine.Mesh CreateMesh();
        UnityEngine.Mesh ApplyMesh(UnityEngine.Mesh mesh);
        
        public int VertexCount { get; }
        public int TriangleCount { get; }
        
        float3[] Vertices { get; }
        float2[] Vertices2D { get; }
        TriangleData[] Triangles { get; }
        HalfEdgeData[] HalfEdges { get; }

        public void Reset();
        
        
        #region BUILDING

        public int AddVertex(float3 pos, float2 uv, Color32 color = default);

        public int AddTriangle(float3 v1, float3 v2, float3 v3, float2 uv1, float2 uv2, float2 uv3,
            bool connectTwins = false);
        
        public int AddTriangle(int v1Index, int v2Index, int v3Index, bool connectTwins = false);
        
        public int AddTriangle(int v1Index, int v2Index, int v3Index,
            int adyacentTri1Index, int adyacentTri2Index, int adyacentTri3Index,
            bool connectTwins = false);

        #endregion
        
        
        public static int GetMeshSize(int vertexCount, int triangleCount) {
            int vertexSize = vertexCount * sizeof(float) * 3;
            int triangleSize = triangleCount * sizeof(int) * 3;
            int uvSize = vertexCount * sizeof(float) * 2;
            int colorSize = vertexCount * sizeof(float) * 4;
            int normalSize = vertexCount * sizeof(float) * 3; // Las normales se calculan despues al construir la Mesh
			
            return vertexSize + triangleSize + uvSize + normalSize + colorSize;
        }
        
        
        public static int GetMeshDataSize(int vertexCount, int triangleCount)
        {
            int vertexSize = vertexCount * HalfEdgeVertex.SizeOf();
            int halfEdgesSize = triangleCount * 3 * HalfEdgeData.SizeOf();
            int triangleSize = triangleCount * TriangleData.SizeOf();
            int uvSize = vertexCount * sizeof(float) * 2;
            int colorSize = vertexCount * sizeof(float) * 4;
			
            return vertexSize + halfEdgesSize + triangleSize + uvSize + colorSize;
        }
    }
}
