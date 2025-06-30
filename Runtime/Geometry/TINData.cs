using System.Collections.Generic;
using Procrain.Geometry.Mesh;
using Unity.Collections;
using Unity.Mathematics;

namespace Procrain.Geometry
{
    public class TINData
    {
        public MeshDataDynamic meshData;
        
        private List<float3> samplePoints = new();

        public int iterations = 0;
        
        private List<float3> pointsToAdd = new();
        private List<TriangleData>  pointTriangles = new();
        private List<HalfEdgeData>  pointEdges = new();
        
        public float3[] Vertices => meshData.Vertices;
        public HalfEdgeData[] HalfEdges => meshData.HalfEdges;
        public TriangleData[] Triangles => meshData.Triangles;
        
        private bool PointFoundOverThreshold { get; }
    }

    public struct TINData_ThreadSafe
    {
        public MeshData_ThreadSafe meshData;

        private NativeArray<float3> samplePoints;
        
        public int iterations;
        
        private NativeArray<float3> pointsToAdd;
        private NativeArray<TriangleData>  pointTriangles;
        private NativeArray<HalfEdgeData>  pointEdges;
        
        public float3[] Vertices => meshData.Vertices;
        public HalfEdgeData[] HalfEdges => meshData.HalfEdges;
        public TriangleData[] Triangles => meshData.Triangles;
        
        private bool PointFoundOverThreshold { get; }
    }
}
