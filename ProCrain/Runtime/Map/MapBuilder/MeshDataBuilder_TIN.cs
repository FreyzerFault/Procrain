using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Procrain.Mesh;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain
{
    [CreateAssetMenu(fileName = "TIN Mesh Builder", menuName = "Procrain/Builders/TIN Mesh Data Builder")]
    public class MeshDataBuilder_TIN: MeshDataBuilder
    {
        public enum PointTriPosition { In, Out, Colinear, Vertex }
        public enum PointEdgePosition { Right, Left, Colinear }
        
        public override IMeshData Build(HeightMap_ThreadSafe heightMap, ITerrainParams terrainParams)
        {
            if (terrainParams is not TINParams tinParams)
                throw new ArgumentException("TerrainParams must be of type TINParams");
            
            MeshDataDynamic meshData = new(64, 64);

            int mapSize = heightMap.Size;

            List<float3> samplePoints = heightMap.ToPoints().ToList();
            
            // Extraemos las esquinas (0, 0), (width-1, 0), (0, height - 1), (width - 1, height - 1)
            Vector2Int vBotLeft2D = new(0,0);
            Vector2Int vBotRight2D = new(mapSize - 1, 0);
            Vector2Int vTopLeft2D = new(0, mapSize - 1);
            Vector2Int vTopRight2D = new(mapSize - 1, mapSize - 1);
            
            Vector3 vBotLeft = new(vBotLeft2D.x, heightMap.GetHeight(vBotLeft2D), vBotLeft2D.y);
            Vector3 vBotRight = new(vBotRight2D.x, heightMap.GetHeight(vBotRight2D), vBotRight2D.y);
            Vector3 vTopLeft = new(vTopLeft2D.x, heightMap.GetHeight(vTopLeft2D), vTopLeft2D.y);
            Vector3 vTopRight = new(vTopRight2D.x, heightMap.GetHeight(vTopRight2D), vTopRight2D.y);
            
            // Al principio añadimos las 4 esquinas:
            meshData.AddVertex(vBotLeft, new Vector2(0, 0));
            meshData.AddVertex(vBotRight, new Vector2(1, 0));
            meshData.AddVertex(vTopLeft, new Vector2(0, 1));
            meshData.AddVertex(vTopRight, new Vector2(1, 1));
            
            meshData.AddTriangle(0, 3, 2);
            meshData.AddTriangle(0, 1, 3);
            
            // Remove the added points
            samplePoints.RemoveAt(mapSize * mapSize - 1);
            samplePoints.RemoveAt(mapSize * (mapSize - 1));
            samplePoints.RemoveAt(mapSize - 1);
            samplePoints.RemoveAt(0);
            
            // Start the TIN Generation
            var iterations = 0;
            var pointFoundOverErrorThreshold = true;
            
            while (pointFoundOverErrorThreshold && iterations < tinParams.maxIterations)
            {
                pointFoundOverErrorThreshold = FindMaxErrorPoint(meshData, samplePoints.ToArray(), tinParams.errorTolerance, 
                    out float3 point, out TriangleData tri, out HalfEdgeData edge);
                
                // TODO Pasarle los samplePoints modificables para eliminar o eliminar cuando devuelva los puntos aqui
                pointFoundOverErrorThreshold = AddPointLoopIteration(meshData, samplePoints.ToArray(), mapSize, tinParams.errorTolerance);

                
                
                iterations++;
            }
            
            return meshData;
        }


        public override IMeshData Build(PerlinNoiseParams_ThreadSafe heigthMapParams, ITerrainParams terrainParams)
        {
            throw new NotImplementedException();
        }

        public override IEnumerator BuildCoroutine(HeightMap_ThreadSafe heightMap, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            if (paralelized)
            {
                // Convierte ambos parametros en sus versiones ThreadSafe
                yield return Build_ParallelizedCoroutine(heightMap, terrainParams, onStart, onEnd);
            }
            else
            {
                onStart?.Invoke();
                IMeshData meshData = Build(heightMap, terrainParams);
                onEnd?.Invoke(meshData);
            }
        }
        
        public override IEnumerator BuildCoroutine(PerlinNoiseParams_ThreadSafe heigthMapParams, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            if (paralelized)
            {
                // Convierte ambos parametros en sus versiones ThreadSafe
                yield return Build_ParallelizedCoroutine(heigthMapParams, terrainParams, onStart, onEnd);
            }
            else
            {
                onStart?.Invoke();
                IMeshData meshData = Build(heigthMapParams, terrainParams);
                onEnd?.Invoke(meshData);
            }
        }
        

        #region THREADING

        protected override IEnumerator Build_ParallelizedCoroutine(
            HeightMap_ThreadSafe heightMap, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerator Build_ParallelizedCoroutine(
            PerlinNoiseParams_ThreadSafe heightMapParams, ITerrainParams terrainParams,
            Action onStart = null, Action<IMeshData> onEnd = null)
        {
            throw new NotImplementedException();
        }
        

        #endregion
        
        
        #region TIN BUILDING

        /// <summary>
        ///     Iteracion standalone del bucle principal para ejecutar progresivamente.
        /// </summary>
        /// <returns>
        ///     Devuelve false en caso de haber acabado de añadir puntos por encima del error tolerado.
        ///     O cuando ocurra algun error
        /// </returns>
        public bool AddPointLoopIteration(MeshDataDynamic meshData, float3[] samplePoints, float mapSize, float errorTolerance,
            int maxConcurrentPoints = 5, float minDistBetweenConcurrentPoints = 0)
        {
            var pointsToAdd = new List<float3>();
            var pointTriangles = new List<TriangleData>();
            var pointEdges = new List<HalfEdgeData>();

            // Busca el Punto de Maximo Error si supera la toleracia
            try
            {
                if (maxConcurrentPoints == 1)
                {
                    if (FindMaxErrorPoint(meshData, samplePoints, errorTolerance,
                            out float3 point, out TriangleData tri, out HalfEdgeData edge))
                    {
                        pointsToAdd.Add(point);
                        pointTriangles.Add(tri);
                        pointEdges.Add(edge);
                    }
                }
                else
                {
                    // TODO Concurrencia
                    // pointsToAdd = FindMaxErrorPoint(
                    //     out pointTriangles,
                    //     out pointEdges,
                    //     maxConcurrentPoints,
                    //     minDistBetweenConcurrentPoints
                    // );
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message + "\n" + e.StackTrace);
                return false;
            }

            // No encuentra un Punto => Se cumple la condicion de parada
            if (pointsToAdd.Count > 0)
                return false;

            var deletedTriangles = new HashSet<Triangle>();
            var deletedEdges = new HashSet<Edge>();

            // Lo añade a la Malla actualizando la Topologia
            // y se le pasa la Informacion sobre la posicion del punto calculada en el Calculo del Error (Triangulo o Eje)
            for (var i = 0; i < pointsToAdd.Count; i++)
            {
                float2 uv = pointsToAdd[i].xz / mapSize;
                meshData.AddVertex(pointsToAdd[i], uv);
                
                // TODO Hay que hacer el tema de Delaunay al añadir un vertice, eliminando el Triangulo y creando nuevos
                // Linea 330 de TIN.cs
                // AddPoint(pointsToAdd[i], pointTriangles[i], pointEdges[i], deletedTriangles, deletedEdges);
                
                // TODO Fuera hay qye eliminar los pointsToAdd de SamplePoints
                // _samplePoints.Remove(pointsToAdd[i]);
            }

            return true;
        }
        
        
        /// <summary>
        ///     Busca el Punto de mayor Error
        /// </summary>
        /// <param name="maxErrorPoint">Punto encontrado de mayor Error</param>
        /// <param name="pointTriangle">Triangulo al que pertenece el punto elegido</param>
        /// <param name="pointEdge">Eje al que pertenece el punto elegido</param>
        /// <returns>
        ///     Devuelve true si lo encuentra. Si no lo ha encontrado significa que o no quedan por añadir
        ///     o ninguno de los restantes supera el error minimo de tolerancia.
        /// </returns>
        private bool FindMaxErrorPoint(MeshDataDynamic meshData, float3[] points, float errorTolerance, 
            out float3 maxErrorPoint, out TriangleData pointTriangle, out HalfEdgeData pointEdge)
        {
            float maxError = 0;
            maxErrorPoint = float3.zero;
            pointTriangle = TriangleData.InvalidTriangle;
            pointEdge = HalfEdgeData.InvalidHalfEdge;

            // Recorremos TODOS los puntos para buscar el de maximo error
            foreach (float3 point in points)
            {
                float error = GetError(point, meshData, out TriangleData tri, out HalfEdgeData edge);
                
                if (error == -1) 
                {
                    // TODO
                }

                if (!(error > maxError) || !(error > errorTolerance))
                    continue;

                pointTriangle = tri;
                pointEdge = edge;
                maxError = error;
                maxErrorPoint = point;
            }

            // Si no ha encontrado ningun punto que supere el error minimo, no estara inicializado ni el triangulo ni el eje
            if (!pointTriangle.IsValid) return false;

            // Guardamos el historial del punto y su error
            // TODO Cachear todo esto
            // lastVertexAdded.Clear();
            // lastVertexError.Clear();
            // lastVertexAdded.Add(maxErrorPoint);
            // lastVertexError.Add(maxError);

            return true;
        }
        
        
        /// <summary>
        ///     La heuristica del Error es la diferencia de altura entre el punto del triangulo con el que coincide en 2D
        ///     y el mismo punto 2D de la muestra
        ///     Para ello podemos interpolar las alturas de cada vertice
        ///     Una interpolacion lineal es lo ideal para los triangulos ya que son superficies planas
        /// </summary>
        /// <param name="point">Punto con un error</param>
        /// <param name="triangle">Triangulo al que pertenece</param>
        /// <param name="edge">Eje al que pertenece en caso contrario</param>
        /// <returns>Error del Punto. -1 si no está dentro de un triángulo</returns>
        private float GetError(float3 point, MeshDataDynamic meshData, out TriangleData triangle, out HalfEdgeData edge)
        {
            // Buscamos el Triangulo al que pertenece o el Eje al que es Colinear
            // Si devuelve false es que no esta en ninguno
            if (!GetTriangle(point.ToV2XZ(), meshData, out triangle, out edge))
            {
                // TODO Eliminarlo devuelve -1
                return -1;
            }

            // 2 casos:
            // Pertenece a un Triangulo
            if (triangle.IsValid)
            {
                HalfEdgeData he1 = meshData.HalfEdges[triangle.firstEdgeIndex];
                HalfEdgeData he2 = meshData.HalfEdges[he1.nextEdgeIndex];
                HalfEdgeData he3 = meshData.HalfEdges[he1.prevEdgeIndex];
                
                float3 v1 = meshData.Vertices[he1.beginVertexIndex];
                float3 v2 = meshData.Vertices[he2.beginVertexIndex];
                float3 v3 = meshData.Vertices[he3.beginVertexIndex];
                
                return math.abs(GetHeightInterpolation(point.xz, v1, v2, v3) - point.y);
            }

            // O pertenece a un Eje
            if (edge.IsValid)
            {
                float3 begin = meshData.Vertices[edge.beginVertexIndex];
                float3 end = meshData.Vertices[meshData.HalfEdges[edge.nextEdgeIndex].beginVertexIndex];
                return math.abs(GetHeightInterpolation(point.xz, begin, end) - point.y);
            }

            return 0;
        }
        
        
        /// <summary>
        ///     Busca el Triangulo al que pertenece un punto usando el Test Point-Triangle
        /// </summary>
        /// <param name="point">Punto 2D (la altura no es necesaria)</param>
        /// <param name="tri">Triangulo al que pertenece</param>
        /// <param name="collinearEdge">Eje colinear al punto</param>
        /// <returns>False si no pertenece a nada</returns>
        public bool GetTriangle(float2 point, MeshDataDynamic meshData,
            out TriangleData tri, out HalfEdgeData collinearEdge)
        {
            tri = TriangleData.InvalidTriangle;
            collinearEdge = HalfEdgeData.InvalidHalfEdge;

            // Buscamos en todos los Triangulos
            foreach (TriangleData triangle in meshData.Triangles)
            {
                // Test Punto-Triangulo
                PointTriPosition test = PointInTriangle(point, triangle, meshData, out collinearEdge);

                switch (test)
                {
                    // Si esta fuera descarta el Triangulo
                    case PointTriPosition.Out: continue;

                    // Si esta DENTRO devuelve el Triangulo
                    case PointTriPosition.In:
                        tri = triangle;
                        return true;

                    // Si esta en una Arista devuelve la Arista
                    case PointTriPosition.Colinear: return true;

                    // Si es su vertice, descartamos el punto por completo y no devolvemos NADA
                    case PointTriPosition.Vertex: return false;
                    default: throw new ArgumentOutOfRangeException();
                }
            }

            return false;
        }
        
        
        /// <summary>
        ///     Esta a la DERECHA de cualquier Eje => OUT;
        ///     Esta a la Izquierda de TODOS los Ejes => IN;
        ///     Es COLINEAR de algun Eje => COLINEAR + ¿A que eje es COLINEAR?;
        /// </summary>
        /// <param name="p"></param>
        /// <param name="colinearEdge">El Eje en caso de ser COLINEAR</param>
        /// <returns>OUT / IN / COLINEAR / VERTEX</returns>
        public PointTriPosition PointInTriangle(float2 p, TriangleData tri, MeshDataDynamic meshData, out HalfEdgeData colinearEdge)
        {
            colinearEdge = HalfEdgeData.InvalidHalfEdge;

            HalfEdgeData he1 = meshData.HalfEdges[tri.firstEdgeIndex];
            HalfEdgeData he2 = meshData.HalfEdges[he1.nextEdgeIndex];
            HalfEdgeData he3 = meshData.HalfEdges[he1.prevEdgeIndex];
            
            float2 v1 = meshData.Vertices[he1.beginVertexIndex].xz;
            float2 v2 = meshData.Vertices[he2.beginVertexIndex].xz;
            float2 v3 = meshData.Vertices[he3.beginVertexIndex].xz;
            
            PointTriPosition pos = PointInTriangle(p, v1, v2, v3);
            
            if (pos == PointTriPosition.Colinear)
            {
                // Comprobamos en que eje esta de los 3
                bool colinear1 = GetPointEdgePosition(p, v1, v2) == PointEdgePosition.Colinear;
                bool colinear2 = GetPointEdgePosition(p, v2, v3) == PointEdgePosition.Colinear;

                // Buscamos la Arista que concuerda con los vertices del Eje en el que esta
                colinearEdge = colinear1 ? he1 : colinear2 ? he2 : he3;
                return PointTriPosition.Colinear;
            }

            return pos;
        }

        
        /// <summary>
        ///     Esta a la DERECHA de cualquier Eje => OUT;
        ///     Es COLINEAR de algun Eje => COLINEAR;
        ///     Esta a la Izquierda de TODOS los Ejes => IN;
        /// </summary>
        /// <param name="p"></param>
        /// <returns>OUT / IN / COLINEAR / VERTEX</returns>
        public PointTriPosition PointInTriangle(float2 p, float2 v1, float2 v2, float2 v3)
        {
            // Posicion Relativa del Punto a cada Arista (alineada en orden Antihorario)
            PointEdgePosition pos1 = GetPointEdgePosition(p, v1, v2);
            PointEdgePosition pos2 = GetPointEdgePosition(p, v2, v3);
            PointEdgePosition pos3 = GetPointEdgePosition(p, v3, v1);

            // En cuanto este a la derecha de cualquiera de las Aristas, esta FUERA
            if (pos1 == PointEdgePosition.Right ||
                pos2 == PointEdgePosition.Right ||
                pos3 == PointEdgePosition.Right)
                return PointTriPosition.Out;

            // Si esta a la IZQUIERDA de TODOS => esta DENTRO
            if (pos1 == PointEdgePosition.Left &&
                pos2 == PointEdgePosition.Left &&
                pos3 == PointEdgePosition.Left)
                return PointTriPosition.In;

            // Si no, puede ser colinear con un eje, o estar en el vertice
            // Por si acaso comprobamos primero que no sea un vertice
            if (MathVectorExtensions.Equals(p, v1) || MathVectorExtensions.Equals(p, v2) || MathVectorExtensions.Equals(p, v3))
                return PointTriPosition.Vertex;

            return PointTriPosition.Colinear;
        }
        
        
        /// <summary>
        ///     NEGATIVA => DERECHA; POSITIVA => IZQUIERDA; ~0 => COLINEAR
        ///     (tiene un margen grande para no crear triangulos sin apenas grosor)
        /// </summary>
        /// <param name="p"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns>RIGHT / LEFT / COLINEAR</returns>
        public static PointEdgePosition GetPointEdgePosition(float2 p, float2 begin, float2 end)
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
        
        
        
        /// <summary>
        ///     Interpolacion de la altura en un punto 2D del Triangulo 3D.
        ///     Inversamente proporcional a la distancia de cada vertice al punto 2D.
        ///     https://codeplea.com/triangular-interpolation
        /// </summary>
        /// <param name="p">Punto 2D</param>
        /// <returns>Altura del punto en el triangulo</returns>
        public float GetHeightInterpolation(float2 p, float3 v1, float3 v2, float3 v3)
        {
            float2 a = v1.xz;
            float2 b = v2.xz;
            float2 c = v3.xz;

            // Usamos las coordenadas baricentricas como pesos:
            float denom = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            float w1 = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / denom;
            float w2 = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / denom;
            float w3 = 1 - w1 - w2;

            return (v1.y * w1 + v2.y * w2 + v3.y * w3) / (w1 + w2 + w3);
        }
        
        
        /// <summary>
        ///     Interpolacion de la altura en un punto 2D en la Arista.
        ///     Inversamente proporcional a la distancia de cada vertice al punto 2D
        /// </summary>
        /// <param name="point">Punto 2D</param>
        /// <returns></returns>
        public float GetHeightInterpolation(float2 point, float3 begin, float3 end)
        {
            // Interpolamos la altura entre begin y end
            float distBegin = (point - new float2(begin.x, begin.z)).Magnitude();
            float distEnd = (point - new float2(end.x, end.z)).Magnitude();

            float distanceInterpolation = 0;
            distanceInterpolation += begin.y / distBegin;
            distanceInterpolation += end.y / distEnd;
            return distanceInterpolation / (1 / distBegin + 1 / distEnd);
        }
        
        

        #endregion

    }
}
