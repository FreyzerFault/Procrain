using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Procrain.Geometry.Mesh
{
    public struct MeshData_ThreadSafe: IMeshData, IDisposable
    {
        // Arrays nativos para mejor rendimiento con jobs
        private NativeArray<HalfEdgeVertex> _vertices;
        private NativeArray<HalfEdgeData> _halfEdges;
        private NativeArray<TriangleData> _triangles;
        private NativeArray<float2> _uvs;
        private NativeArray<Color32> _colors;
        
        // Contadores para gestión de índices
        private int _vertexIndex;
        private int _halfEdgeIndex;
        private int _triangleIndex;

        public readonly int lod;

        public int VertexCount => _vertices.Length;
        public int HalfEdgeCount => _halfEdges.Length;
        public int TriangleCount => _triangles.Length;
        public bool IsEmpty => _vertexIndex == 0;

        public float3[] Vertices => _vertices.Select(v => v.position).ToArray();
        public float2[] Vertices2D => _vertices.Select(v => v.position.xz).ToArray();
        public TriangleData[] Triangles => _triangles.ToArray();
        public HalfEdgeData[] HalfEdges => _halfEdges.ToArray();

        public MeshData_ThreadSafe(int maxVertices, int maxTriangles, int lod = 0)
        {
            // Inicializar arrays con capacidad máxima
            _vertices = new NativeArray<HalfEdgeVertex>(maxVertices, Allocator.Persistent);
            // Cada triángulo necesita 3 half-edges
            _halfEdges = new NativeArray<HalfEdgeData>(maxTriangles * 3, Allocator.Persistent);
            _triangles = new NativeArray<TriangleData>(maxTriangles, Allocator.Persistent);
            _uvs = new NativeArray<float2>(maxVertices, Allocator.Persistent);
            _colors = new NativeArray<Color32>(maxVertices, Allocator.Persistent);

            _vertexIndex = 0;
            _halfEdgeIndex = 0;
            _triangleIndex = 0;

            this.lod = lod;
        }

        public void Dispose()
        {
            _vertices.Dispose();
            _halfEdges.Dispose();
            _triangles.Dispose();
            _uvs.Dispose();
            _colors.Dispose();
        }

        public void Reset()
        {
            for (var i = 0; i < VertexCount; i++)
            {
                _vertices[i] = HalfEdgeVertex.InvalidHalfVertex;
                _uvs[i] = float2.zero;
                _colors[i] = new Color32();
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

        private NativeArray<HalfEdgeData> GetHalfEdges(int triIndex)
        {
            TriangleData tri = _triangles[triIndex];
            var array = new NativeArray<HalfEdgeData>(3, Allocator.TempJob);
            array[0] = _halfEdges[tri.firstEdgeIndex];
            array[1] = _halfEdges[array[0].nextEdgeIndex];
            array[2] = _halfEdges[array[0].nextEdgeIndex];
            return array;
        }
        
        
        #region BUILDING

        [BurstCompile]
        public int AddVertex(float3 pos, float2 uv, Color32 color = default)
        {
            _uvs[_vertexIndex] = uv;
            _colors[_vertexIndex] = color;
            _vertices[_vertexIndex] = new HalfEdgeVertex(_vertexIndex, pos);
            return _vertexIndex++;
        }

        [BurstCompile]
        public int AddTriangle(float3 v1, float3 v2, float3 v3, float2 uv1, float2 uv2, float2 uv3, bool connectTwins = false)
        {
            int v1Index = AddVertex(v1, uv1);
            int v2Index = AddVertex(v2, uv2);
            int v3Index = AddVertex(v3, uv3);
            
            return AddTriangle(v1Index, v2Index, v3Index, connectTwins);
        }
        
        /// Método para añadir un triángulo.
        /// Busca los ejes compartidos con los triangulos adyacentes para conectar los HalfEdge Twins
        [BurstCompile]
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
        
        /// Search their Twins in ALL the Mesh. Computationaly expensive.
        /// Use AddTriangle(v1, v2, v3, tri1, tri2, tri3) to set the adyacente Triangles while adding it
        [BurstCompile]
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

        #endregion
        
        
        #region HALF EDGE TWINS

        // Try to Connect a Twin in the Half Edge by searching it in an adyacent Triangle
        [BurstCompile]
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
        [BurstCompile]
        private void SearchAndConnectAllTwins()
        {
            foreach (HalfEdgeData halfEdge in _halfEdges.Where(halfEdge => halfEdge.twinEdgeIndex == -1))
                SearchAndConnectTwin(halfEdge.index);
        }
        
        /// Busca en TODOS los ejes el eje Twin
        [BurstCompile]
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
        [BurstCompile]
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
        

        #region UNITY MESH CONVERSION

        public UnityEngine.Mesh CreateMesh() => ApplyMesh(new UnityEngine.Mesh());

        public UnityEngine.Mesh ApplyMesh(UnityEngine.Mesh mesh)
        {
            if (!mesh)
                mesh = new UnityEngine.Mesh();

            mesh.Clear();
            
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

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.Optimize();

            return mesh;
        }


        public static MeshData_ThreadSafe CreateFromMesh(UnityEngine.Mesh mesh, bool connectTwins = true)
        {
            ValidateMesh(mesh);

            int vertexCount = mesh.vertexCount;
            int triCount = mesh.triangles.Length / 3;
            int edgeCount = mesh.triangles.Length; // 3 half-edges por triángulo

            var triangles = new NativeArray<TriangleData>(triCount, Allocator.Persistent);
            var halfEdges = new NativeArray<HalfEdgeData>(edgeCount, Allocator.Persistent);
            var vertices = new NativeArray<HalfEdgeVertex>(vertexCount, Allocator.Persistent);
            
            MeshData_ThreadSafe meshData = new(vertexCount, triCount)
            {
                _triangles = triangles,
                _halfEdges = halfEdges,
                _vertices = vertices,
            };
            
            try 
            {
                // Copiar vértices primero
                var meshVertices = mesh.vertices;
                for (var i = 0; i < vertexCount; i++) 
                    vertices[i] = new HalfEdgeVertex(i, meshVertices[i]);

                // Procesar triángulos y half-edges
                for (var i = 0; i < mesh.triangles.Length; i++)
                {
                    int triIndex = i / 3;
                    int edgeIndex = i;
                    int vertexIndex = mesh.triangles[i];

                    if (i % 3 == 0)
                        triangles[triIndex] = new TriangleData(triIndex, edgeIndex);

                    // Calcular índices prev/next asegurando que son cíclicos dentro del triángulo
                    int prevIndex = i % 3 == 0 ? edgeIndex + 2 : edgeIndex - 1;
                    int nextIndex = i % 3 == 2 ? edgeIndex - 2 : edgeIndex + 1;

                    // Crear half-edge
                    halfEdges[i] = new HalfEdgeData(
                        index: edgeIndex,
                        beginVertexIndex: vertexIndex,
                        nextEdgeIndex: nextIndex,
                        prevEdgeIndex: prevIndex,
                        faceIndex: triIndex
                    );
                    
                    // Actualizar firstEdgeIndex del vértice si no tiene uno asignado
                    if (vertices[vertexIndex].firstEdgeIndex == -1)
                    {
                        HalfEdgeVertex vertex = vertices[vertexIndex];
                        vertices[vertexIndex] = new HalfEdgeVertex(vertex.index, vertex.position, edgeIndex);
                    }
                }
                
                if (connectTwins)
                    meshData.SearchAndConnectAllTwins();
            }
            catch
            {
                // Limpieza en caso de error
                triangles.Dispose();
                halfEdges.Dispose();
                vertices.Dispose();
                throw;
            }

            return meshData;
        }
        
        
        private static void ValidateMesh(UnityEngine.Mesh mesh)
        {
            if (!mesh)
                throw new ArgumentNullException(nameof(mesh));
            
            if (mesh.vertexCount == 0)
                throw new ArgumentException("Mesh cannot be empty", nameof(mesh));
    
            if (mesh.triangles.Length % 3 != 0)
                throw new ArgumentException($"Invalid triangle count: {mesh.triangles.Length}", nameof(mesh));
        }

        #endregion
        
        
        public override string ToString() =>
            $"Triangles: {TriangleCount}. Vertices: {VertexCount} [LOD {lod}] " +
            $"Mesh Size: {IMeshData.GetMeshSize(_vertices.Length, _triangles.Length) / 1024} KB" +
            $"Mesh Data Size (HalfEdge): {IMeshData.GetMeshDataSize(_vertices.Length, _triangles.Length) / 1024} KB";
    }
}
