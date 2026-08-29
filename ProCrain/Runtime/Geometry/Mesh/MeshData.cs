using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.Mesh
{
    [Serializable]
    public class MeshDataStatic : IMeshData
    {
        private readonly HalfEdgeVertex[] _vertices;
        private readonly HalfEdgeData[] _halfEdges;
        private readonly TriangleData[] _triangles;
        private readonly Vector2[] _uvs;
        private readonly Color[] _colors;
        
        private int _vertexIndex;
        private int _halfEdgeIndex;
        private int _triangleIndex;
        
        public readonly int lod;
        
        public int VertexCount => _vertices.Length;
        public int HalfEdgeCount => _halfEdges.Length;
        public int TriangleCount => _triangles.Length;
        public bool IsEmpty => VertexCount == 0;
        
        public float3[] Vertices => _vertices.Select(v => v.position).ToArray();
        public float2[] Vertices2D => _vertices.Select(v => v.position.xz).ToArray();
        public HalfEdgeData[] HalfEdges => _halfEdges;
        public TriangleData[] Triangles => _triangles;

        public MeshDataStatic(int maxVertices, int maxTriangles, int lod = 0)
        {
            // Inicializar arrays con capacidad máxima
            _vertices = new HalfEdgeVertex[maxVertices];
            // Cada triángulo necesita 3 half-edges
            _halfEdges = new HalfEdgeData[maxTriangles * 3];
            _triangles = new TriangleData[maxTriangles];
            _uvs = new Vector2[maxVertices];
            _colors = new Color[maxVertices];

            _vertexIndex = 0;
            _halfEdgeIndex = 0;
            _triangleIndex = 0;

            this.lod = lod;
        }

        private NativeArray<HalfEdgeData> GetHalfEdges(int triIndex)
        {
            TriangleData tri = _triangles[triIndex];
            var array = new NativeArray<HalfEdgeData>(3, Allocator.TempJob);
            array[0] = _halfEdges[tri.firstEdgeIndex];
            array[1] = _halfEdges[array[0].nextEdgeIndex];
            array[2] = _halfEdges[array[0].nextEdgeIndex];
            return array;
        }
        
        public void Reset()
        {
            for (var i = 0; i < VertexCount; i++)
            {
                _vertices[i] = HalfEdgeVertex.InvalidHalfVertex;
                _uvs[i] = Vector2.zero;
                _colors[i] = new Color();
            }

            for (var i = 0; i < TriangleCount; i++)
            {
                _triangles[i] = TriangleData.InvalidTriangle;
                _halfEdges[i * 3] = HalfEdgeData.InvalidHalfEdge;
                _halfEdges[i * 3 + 1] = HalfEdgeData.InvalidHalfEdge;
                _halfEdges[i * 3 + 2] = HalfEdgeData.InvalidHalfEdge;
            }
            
            _vertexIndex = 0;
            _halfEdgeIndex = 0;
            _triangleIndex = 0;
        }

        public int AddVertex(float3 pos, float2 uv, Color color = default) => 
            AddVertex((Vector3)pos, (Vector2)uv, color);
        
        public int AddVertex(float3 pos, float2 uv, Color32 color = default) => 
            AddVertex((Vector3)pos, (Vector2)uv, (Color)color);

        public int AddTriangle(float3 v1, float3 v2, float3 v3, float2 uv1, float2 uv2, float2 uv3, bool connectTwins = false) => 
            AddTriangle((Vector3)v1, (Vector3)v2, (Vector3)v3, (Vector2)uv1, (Vector2)uv2, (Vector2)uv3, connectTwins);


        #region BUILDING

        public int AddVertex(Vector3 pos, Vector2 uv, Color color = default)
        {
            _uvs[_vertexIndex] = uv;
            _colors[_vertexIndex] = color;
            _vertices[_vertexIndex] = new HalfEdgeVertex(_vertexIndex, pos);
            return _vertexIndex++;
        }

        public int AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Vector2 uv1, Vector2 uv2, Vector2 uv3, bool connectTwins = false)
        {
            int v1Index = AddVertex(v1, uv1);
            int v2Index = AddVertex(v2, uv2);
            int v3Index = AddVertex(v3, uv3);
            
            return AddTriangle(v1Index, v2Index, v3Index, connectTwins);
        }
        
        
        /// Search their Twins in ALL the Mesh. Computationaly expensive.
        /// Use AddTriangle(v1, v2, v3, tri1, tri2, tri3) to set the adyacente Triangles while adding it
        public int AddTriangle(int v1Index, int v2Index, int v3Index, bool connectTwins = false)
        {
            int triIndex = AddTriangle(v1Index, v2Index, v3Index, -1, -1, -1);
            
            int he1Index = _triangles[triIndex].firstEdgeIndex;
            int he2Index = _halfEdges[he1Index].nextEdgeIndex;
            int he3Index = _halfEdges[he1Index].prevEdgeIndex;
            
            // Conectar twins
            if (connectTwins)
            {
                SearchAndConnectTwin(he1Index);
                SearchAndConnectTwin(he2Index);
                SearchAndConnectTwin(he3Index);
            }

            return triIndex;
        }
        
        
        /// Método para añadir un triángulo.
        /// Busca los ejes compartidos con los triangulos adyacentes para conectar los HalfEdge Twins
        public int AddTriangle(int v1Index, int v2Index, int v3Index,
            int adyacentTri1Index, int adyacentTri2Index, int adyacentTri3Index,
            bool connectTwins = false)
        {
            int triIndex = _triangleIndex++;
            int he1Index = _halfEdgeIndex++;
            int he2Index = _halfEdgeIndex++;
            int he3Index = _halfEdgeIndex++;

            // Crear half-edges
            HalfEdgeData he1 = new (
                he1Index,
                beginVertexIndex: v1Index,
                nextEdgeIndex: he2Index,
                prevEdgeIndex: he3Index,
                faceIndex: triIndex);
            
            HalfEdgeData he2 = new (
                he2Index,
                beginVertexIndex: v2Index,
                nextEdgeIndex: he3Index,
                prevEdgeIndex: he1Index,
                faceIndex: triIndex);
            
            HalfEdgeData he3 = new (
                he3Index,
                beginVertexIndex: v3Index,
                nextEdgeIndex: he1Index,
                prevEdgeIndex: he2Index,
                faceIndex: triIndex);

            // Crear triángulo
            TriangleData tri = new(triIndex, he1Index);

            // Guardar en arrays
            _halfEdges[he1Index] = he1;
            _halfEdges[he2Index] = he2;
            _halfEdges[he3Index] = he3;
            _triangles[triIndex] = tri;

            if (connectTwins)
            {
                // Connecting Twins of each Half Edge
                // Testing each Adyacent Triangle
                NativeArray<int> adyTriIndeces = new NativeArray<int>(3, Allocator.TempJob);
                adyTriIndeces[0] = adyacentTri1Index;
                adyTriIndeces[1] = adyacentTri2Index;
                adyTriIndeces[2] = adyacentTri3Index;

                foreach (int adyacentTriIndex in adyTriIndeces)
                {
                    TryConnectTwin(he1Index, adyacentTriIndex);
                    TryConnectTwin(he2Index, adyacentTriIndex);
                    TryConnectTwin(he3Index, adyacentTriIndex);
                }
            }
            
            return triIndex;
        }

        #endregion
        
        
        #region HALF EDGE TWINS

        // Try to Connect a Twin in the Half Edge by searching it in an adyacent Triangle
        private void TryConnectTwin(int edgeIndex, int adyacentTriIndex)
        {
            HalfEdgeData edge = _halfEdges[edgeIndex];
            foreach (HalfEdgeData adyEdge in GetHalfEdges(adyacentTriIndex))
            {
                if (AreTwins(edgeIndex, adyEdge.index))
                {
                    // No se pueden modificar directamente, deben copiarse y actualizarse
                    HalfEdgeData updatedAdyEdge = _halfEdges[adyEdge.index];

                    edge.twinEdgeIndex = adyEdge.index;
                    updatedAdyEdge.twinEdgeIndex = edge.index;

                    _halfEdges[edge.index] = edge;
                    _halfEdges[adyEdge.index] = updatedAdyEdge;
                }
            }
        }
        
        /// Repite la búsqueda y asignación de twins para TODOS los HalfEdges
        private void SearchAndConnectAllTwins()
        {
            foreach (HalfEdgeData halfEdge in _halfEdges.Where(halfEdge => halfEdge.twinEdgeIndex == -1))
                SearchAndConnectTwin(halfEdge.index);
        }
        
        /// Busca en TODOS los ejes el eje Twin
        private void SearchAndConnectTwin(int edgeIndex)
        {
            HalfEdgeData edge = _halfEdges[edgeIndex];
        
            // Buscar twin
            for (var i = 0; i < _halfEdgeIndex; i++)
            {
                if (i == edgeIndex) continue;
                if (AreTwins(edgeIndex, i))
                {
                    HalfEdgeData twin = _halfEdges[i];
                    
                    // Encontramos un twin
                    edge.twinEdgeIndex = i;
                    twin.twinEdgeIndex = edgeIndex;
                
                    _halfEdges[edgeIndex] = edge;
                    _halfEdges[i] = twin;
                    break;
                }
            }
        }
        
        /// Si el siguiente del eje contrario tiene un vertice igual al del eje, y viceversa, es que son Twins
        private bool AreTwins(int edgeIndex1, int edgeIndex2)
        {
            HalfEdgeData e1 = _halfEdges[edgeIndex1];
            HalfEdgeData e2 = _halfEdges[edgeIndex2];
            HalfEdgeData e1NextEdge = _halfEdges[e1.nextEdgeIndex];
            HalfEdgeData e2NextEdge = _halfEdges[e2.nextEdgeIndex];
            return e1.beginVertexIndex == e2NextEdge.beginVertexIndex
                   && e2.beginVertexIndex == e1NextEdge.beginVertexIndex;
        }

        #endregion
        
        
        #region MESH CREATION

        /// Creacion del Objeto Mesh que necesita Unity (no Paralelizable)
        public UnityEngine.Mesh CreateMesh() => ApplyMesh(new UnityEngine.Mesh());

        public UnityEngine.Mesh ApplyMesh(UnityEngine.Mesh mesh)
        {
            if (!mesh)
                mesh = new UnityEngine.Mesh();
            
            var vertexArray = new Vector3[_vertexIndex];
            var triangleArray = new int[_triangleIndex * 3];

            // Copy vertices
            for (var i = 0; i < _vertexIndex; i++)
                vertexArray[i] = _vertices[i].position;

            // Copy triangles
            for (var i = 0; i < _triangleIndex; i++) 
            {
                TriangleData tri = _triangles[i];
                HalfEdgeData he1 = _halfEdges[tri.firstEdgeIndex];
                HalfEdgeData he2 = _halfEdges[he1.nextEdgeIndex];
                HalfEdgeData he3 = _halfEdges[he1.prevEdgeIndex];

                triangleArray[i * 3] = he1.beginVertexIndex;
                triangleArray[i * 3 + 1] = he2.beginVertexIndex; 
                triangleArray[i * 3 + 2] = he3.beginVertexIndex;
            }

            mesh.vertices = vertexArray;
            mesh.triangles = triangleArray;
            mesh.colors = _colors;
            mesh.uv = _uvs;

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.Optimize();

            return mesh;
        }

        #endregion
        
        
        public override string ToString() =>
            $"Triangles: {TriangleCount}. Vertices: {VertexCount} [LOD {lod}] " +
            $"Mesh Size: {IMeshData.GetMeshSize(_vertices.Length, _triangles.Length) / 1024} KB" +
            $"Mesh Data Size (HalfEdge): {IMeshData.GetMeshDataSize(_vertices.Length, _triangles.Length) / 1024} KB";
    }

    [Serializable]
    public class MeshDataDynamic : IMeshData
    {
        private readonly List<HalfEdgeVertex> _vertices;
        private readonly List<HalfEdgeData> _halfEdges;
        private readonly List<TriangleData> _triangles;
        private readonly List<Vector2> _uvs;
        private readonly List<Color> _colors;

        private int _vertexIndex;
        private int _halfEdgeIndex;
        private int _triangleIndex;
        
        public readonly int lod;
        
        public int VertexCount => _vertices.Count;
        public int HalfEdgeCount => _halfEdges.Count;
        public int TriangleCount => _triangles.Count;
        public bool IsEmpty => VertexCount == 0;

        public float3[] Vertices => _vertices.Select(v => v.position).ToArray();
        public float2[] Vertices2D => _vertices.Select(v => v.position.xz).ToArray();
        public TriangleData[] Triangles => _triangles.ToArray();
        public HalfEdgeData[] HalfEdges => _halfEdges.ToArray();

        public MeshDataDynamic(int maxVertices, int maxTriangles, int lod = 0)
        {
            // Inicializar arrays con capacidad máxima
            _vertices = new List<HalfEdgeVertex>(maxVertices);
            // Cada triángulo necesita 3 half-edges
            _halfEdges = new List<HalfEdgeData>(maxTriangles * 3);
            _triangles = new List<TriangleData>(maxTriangles);
            _uvs = new List<Vector2>(maxVertices);
            _colors = new List<Color>(maxVertices);
            
            _vertexIndex = 0;
            _halfEdgeIndex = 0;
            _triangleIndex = 0;

            this.lod = lod;
        }
        
        private NativeArray<HalfEdgeData> GetHalfEdges(int triIndex)
        {
            TriangleData tri = _triangles[triIndex];
            var array = new NativeArray<HalfEdgeData>(3, Allocator.TempJob);
            array[0] = _halfEdges[tri.firstEdgeIndex];
            array[1] = _halfEdges[array[0].nextEdgeIndex];
            array[2] = _halfEdges[array[0].nextEdgeIndex];
            return array;
        }
        
        public void Reset()
        {
            _vertices.Clear();
            _halfEdges.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _colors.Clear();
            
            _vertexIndex = 0;
            _halfEdgeIndex = 0;
            _triangleIndex = 0;
        }

        
        
        #region BUILDING

        public int AddVertex(float3 pos, float2 uv, Color32 color = default)
        {
            _uvs[_vertexIndex] = uv;
            _colors[_vertexIndex] = color;
            _vertices[_vertexIndex] = new HalfEdgeVertex(_vertexIndex, pos);
            return _vertexIndex++;
        }

        public int AddTriangle(float3 v1, float3 v2, float3 v3, float2 uv1, float2 uv2, float2 uv3, bool connectTwins = false)
        {
            int v1Index = AddVertex(v1, uv1);
            int v2Index = AddVertex(v2, uv2);
            int v3Index = AddVertex(v3, uv3);
            
            return AddTriangle(v1Index, v2Index, v3Index, connectTwins);
        }
        
        
        /// Search their Twins in ALL the Mesh. Computationaly expensive.
        /// Use AddTriangle(v1, v2, v3, tri1, tri2, tri3) to set the adyacente Triangles while adding it
        public int AddTriangle(int v1Index, int v2Index, int v3Index, bool connectTwins = false)
        {
            int triIndex = AddTriangle(v1Index, v2Index, v3Index, -1, -1, -1);
            
            int he1Index = _triangles[triIndex].firstEdgeIndex;
            int he2Index = _halfEdges[he1Index].nextEdgeIndex;
            int he3Index = _halfEdges[he1Index].prevEdgeIndex;
            
            // Conectar twins
            if (connectTwins)
            {
                SearchAndConnectTwin(he1Index);
                SearchAndConnectTwin(he2Index);
                SearchAndConnectTwin(he3Index);
            }

            return triIndex;
        }
        
        
        /// Método para añadir un triángulo.
        /// Busca los ejes compartidos con los triangulos adyacentes para conectar los HalfEdge Twins
        public int AddTriangle(int v1Index, int v2Index, int v3Index,
            int adyacentTri1Index, int adyacentTri2Index, int adyacentTri3Index,
            bool connectTwins = false)
        {
            int triIndex = _triangleIndex++;
            int he1Index = _halfEdgeIndex++;
            int he2Index = _halfEdgeIndex++;
            int he3Index = _halfEdgeIndex++;

            // Crear half-edges
            HalfEdgeData he1 = new (
                he1Index,
                beginVertexIndex: v1Index,
                nextEdgeIndex: he2Index,
                prevEdgeIndex: he3Index,
                faceIndex: triIndex);
            
            HalfEdgeData he2 = new (
                he2Index,
                beginVertexIndex: v2Index,
                nextEdgeIndex: he3Index,
                prevEdgeIndex: he1Index,
                faceIndex: triIndex);
            
            HalfEdgeData he3 = new (
                he3Index,
                beginVertexIndex: v3Index,
                nextEdgeIndex: he1Index,
                prevEdgeIndex: he2Index,
                faceIndex: triIndex);

            // Crear triángulo
            TriangleData tri = new(triIndex, he1Index);

            // Guardar en arrays
            _halfEdges[he1Index] = he1;
            _halfEdges[he2Index] = he2;
            _halfEdges[he3Index] = he3;
            _triangles[triIndex] = tri;

            if (connectTwins)
            {
                // Connecting Twins of each Half Edge
                // Testing each Adyacent Triangle
                NativeArray<int> adyTriIndeces = new NativeArray<int>(3, Allocator.TempJob);
                adyTriIndeces[0] = adyacentTri1Index;
                adyTriIndeces[1] = adyacentTri2Index;
                adyTriIndeces[2] = adyacentTri3Index;

                foreach (int adyacentTriIndex in adyTriIndeces)
                {
                    TryConnectTwin(he1Index, adyacentTriIndex);
                    TryConnectTwin(he2Index, adyacentTriIndex);
                    TryConnectTwin(he3Index, adyacentTriIndex);
                }
            }
            
            return triIndex;
        }

        #endregion
        
        
        #region HALF EDGE TWINS

        // Try to Connect a Twin in the Half Edge by searching it in an adyacent Triangle
        private void TryConnectTwin(int edgeIndex, int adyacentTriIndex)
        {
            HalfEdgeData edge = _halfEdges[edgeIndex];
            foreach (HalfEdgeData adyEdge in GetHalfEdges(adyacentTriIndex))
            {
                if (AreTwins(edgeIndex, adyEdge.index))
                {
                    // No se pueden modificar directamente, deben copiarse y actualizarse
                    HalfEdgeData updatedAdyEdge = _halfEdges[adyEdge.index];

                    edge.twinEdgeIndex = adyEdge.index;
                    updatedAdyEdge.twinEdgeIndex = edge.index;

                    _halfEdges[edge.index] = edge;
                    _halfEdges[adyEdge.index] = updatedAdyEdge;
                }
            }
        }
        
        /// Repite la búsqueda y asignación de twins para TODOS los HalfEdges
        private void SearchAndConnectAllTwins()
        {
            foreach (HalfEdgeData halfEdge in _halfEdges.Where(halfEdge => halfEdge.twinEdgeIndex == -1))
                SearchAndConnectTwin(halfEdge.index);
        }
        
        /// Busca en TODOS los ejes el eje Twin
        private void SearchAndConnectTwin(int edgeIndex)
        {
            HalfEdgeData edge = _halfEdges[edgeIndex];
        
            // Buscar twin
            for (var i = 0; i < _halfEdgeIndex; i++)
            {
                if (i == edgeIndex) continue;
                if (AreTwins(edgeIndex, i))
                {
                    HalfEdgeData twin = _halfEdges[i];
                    
                    // Encontramos un twin
                    edge.twinEdgeIndex = i;
                    twin.twinEdgeIndex = edgeIndex;
                
                    _halfEdges[edgeIndex] = edge;
                    _halfEdges[i] = twin;
                    break;
                }
            }
        }
        
        /// Si el siguiente del eje contrario tiene un vertice igual al del eje, y viceversa, es que son Twins
        private bool AreTwins(int edgeIndex1, int edgeIndex2)
        {
            HalfEdgeData e1 = _halfEdges[edgeIndex1];
            HalfEdgeData e2 = _halfEdges[edgeIndex2];
            HalfEdgeData e1NextEdge = _halfEdges[e1.nextEdgeIndex];
            HalfEdgeData e2NextEdge = _halfEdges[e2.nextEdgeIndex];
            return e1.beginVertexIndex == e2NextEdge.beginVertexIndex
                   && e2.beginVertexIndex == e1NextEdge.beginVertexIndex;
        }

        #endregion
        
        
        #region MESH CREATION

        /// Creacion del Objeto Mesh que necesita Unity (no Paralelizable)
        public UnityEngine.Mesh CreateMesh() => ApplyMesh(new UnityEngine.Mesh());

        public UnityEngine.Mesh ApplyMesh(UnityEngine.Mesh mesh)
        {
            if (!mesh)
                mesh = new UnityEngine.Mesh();

            var vertexArray = new Vector3[_vertexIndex];
            var triangleArray = new int[_triangleIndex * 3];

            // Copy vertices
            for (var i = 0; i < _vertexIndex; i++)
                vertexArray[i] = _vertices[i].position;

            // Copy triangles
            for (var i = 0; i < _triangleIndex; i++) 
            {
                TriangleData tri = _triangles[i];
                HalfEdgeData he1 = _halfEdges[tri.firstEdgeIndex];
                HalfEdgeData he2 = _halfEdges[he1.nextEdgeIndex];
                HalfEdgeData he3 = _halfEdges[he1.prevEdgeIndex];

                triangleArray[i * 3] = he1.beginVertexIndex;
                triangleArray[i * 3 + 1] = he2.beginVertexIndex; 
                triangleArray[i * 3 + 2] = he3.beginVertexIndex;
            }

            mesh.vertices = vertexArray;
            mesh.triangles = triangleArray;
            mesh.colors = _colors.ToArray();
            mesh.uv = _uvs.ToArray();

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.Optimize();

            return mesh;
        }

        #endregion
        
        
        public override string ToString() =>
            $"Triangles: {TriangleCount}. Vertices: {VertexCount} [LOD {lod}] " +
            $"Mesh Size: {IMeshData.GetMeshSize(_vertices.Count, _triangles.Count) / 1024} KB" +
            $"Mesh Data Size (HalfEdge): {IMeshData.GetMeshDataSize(_vertices.Count, _triangles.Count) / 1024} KB";
    }
}
