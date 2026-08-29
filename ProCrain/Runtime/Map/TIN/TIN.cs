using System;
using System.Collections.Generic;
using System.Linq;
using Procrain.Mesh;
using Unity.Collections;
using UnityEngine;

namespace Procrain
{
	public class Tin
	{
		private readonly AABB_2D _aabb;

		private readonly float _errorTolerance = 0.1f;

		/// Mapa de Alturas o puntos 3D de los que se parte como candidatos
		private readonly Vector3[] _heightMap;

		/// Lista Dinámica de Puntos que quedan por consultar para ir colocando como vértices si superan el Error Tolerado
		private List<Vector3> _samplePoints = new();

		public readonly int mapSize = 1;
		private readonly float _heightScale = 100;
		
		public MeshData_ThreadSafe meshData = new();

		public readonly List<Triangle> triangles = new();
		public readonly List<Vertex> vertices = new();
		public readonly List<Edge> edges = new();
		
		public List<Vector3> lastVertexAdded;
		public List<float> lastVertexError;

		
		#region CONTRUCTORES

		public Tin()
		{
			lastVertexAdded = new List<Vector3>();
			lastVertexError = new List<float>();
		}

		private Tin(float errorTolerance = 1, float heightScale = 100, int maxIterations = -1) : this()
		{
			_errorTolerance = errorTolerance;
			_heightScale = heightScale;
		}


		/// <summary>
		///     Creacion del TIN a partir de un Mapa de una Nube de Puntos
		/// </summary>
		/// <param name="points">Nube de puntos inicial</param>
		/// <param name="bounds">Bounding Box 2D</param>
		/// <param name="errorTolerance">Error Minimo tolerado => Condicion de Añadir un Punto</param>
		/// <param name="heightScale"></param>
		/// <param name="maxIterations">Iteraciones maximas permitidas (para una creacion progresiva y debugging)</param>
		public Tin(
			Vector3[] points, float errorTolerance = 1, float heightScale = 100,
			int maxIterations = -1, AABB_2D? bounds = null
		)
			: this(errorTolerance, heightScale, maxIterations)
		{
			_aabb = bounds ?? new AABB_2D(Vector2.zero, Vector2.one * mapSize);
			_heightMap = Mathf.Approximately(heightScale, 1)
				? points 
				: points.Select(p => new Vector3(p.x, p.y * heightScale, p.z)).ToArray();

			_samplePoints = _heightMap.ToList();
		}
		
		/// <summary>
		///     Creacion del TIN a partir de un Mapa de Alturas
		/// </summary>
		/// <param name="heightMap">Mapa de Alturas</param>
		/// <param name="mapSize"></param>
		/// <param name="bounds">Bounding Box 2D</param>
		/// <param name="errorTolerance">Error Minimo tolerado => Condicion de Añadir un Punto</param>
		/// <param name="heightScale"></param>
		/// <param name="maxIterations">Iteraciones maximas permitidas (para una creacion progresiva y debugging)</param>
		public Tin(
			float[] heightMap, int mapSize, AABB_2D? bounds = null, float errorTolerance = 1,
			float heightScale = 100, int maxIterations = -1
		) : this(errorTolerance, heightScale, maxIterations)
		{
			this.mapSize = mapSize;
			
			if (mapSize * mapSize != heightMap.Length)
				throw new Exception(
					"El Mapa de Alturas no tiene el tamaño correcto.\n" +
					$"El tamaño del mapa es {mapSize}x{mapSize} y el tamaño del array es {heightMap.Length}" +
					$" ({Mathf.Sqrt(heightMap.Length)} x {Mathf.Sqrt(heightMap.Length)}?)"
				);

			_aabb = bounds ?? new AABB_2D(Vector2.zero, Vector2.one * mapSize);
			
			// Guardamos el Mapa de Alturas como un conjunto de Vertices potenciales
			_heightMap = new Vector3[heightMap.Length];
			for (var x = 0; x < mapSize; x++)
			for (var y = 0; y < mapSize; y++)
				_heightMap[x + y * mapSize] = new Vector3(x, heightMap[x + y * mapSize] * heightScale, y);
			
			_samplePoints = _heightMap.ToList();
		}

		public Tin(
			NativeArray<float> heightMap, int mapSize, AABB_2D? bounds = null, float errorTolerance = 1,
			float heightScale = 100, int maxIterations = -1
		) : this(heightMap.ToArray(), mapSize, bounds, errorTolerance, heightScale, maxIterations)
		{ }
		
		// Usa un Array 2D de Mapa de Alturas
		public Tin(
			float[,] heightMap, AABB_2D? bounds = null, float errorTolerance = 1,
			float heightScale = 100, int maxIterations = -1
		) : this(heightMap.Flatten(), heightMap.GetLength(0), bounds, errorTolerance, heightScale, maxIterations)
		{ }
		
		private Tin(HeightMap heightMap, float errorTolerance, float heightScale)
		: this(heightMap.ToArray2D(), errorTolerance: errorTolerance, heightScale: heightScale) {}

		#endregion
		
		
		#region INITIALIZATION

		/// <summary>
		///     Crea los 2 Primeros Triangulos a partir de una Nube de Puntos irregular. Busca el punto de mayor x y mayor z
		/// </summary>
		/// <exception cref="Exception">Deben existir los puntos (0,0), (width-1, 0), (0, height-1) y (width-1, height-1)</exception>
		public void InitGeometry()
		{
			// Extraemos las esquinas (0,0), (width-1,0), (0,height-1), (width-1, height-1)
			// Presupongo que SIZE es la anchura del mapa y que Width == Height
			if (_samplePoints.Count != mapSize * mapSize)
				throw new Exception(
					"Estoy buscando las esquinas del Mapa de Alturas\n" +
					"y resulta que 'size' no indica la anchura. En teoría debería ser 'size * size'.\n" +
					$"size: {mapSize} - size * size: {mapSize * mapSize} heightMap.Count: {_samplePoints.Count}"
				);

			AddCorners(
				vBotLeft: _samplePoints[0],
				vBotRight: _samplePoints[mapSize - 1],
				vTopLeft: _samplePoints[mapSize * (mapSize - 1)],
				vTopRight: _samplePoints[mapSize * mapSize - 1]
			);

			_samplePoints.RemoveAt(mapSize * mapSize - 1);
			_samplePoints.RemoveAt(mapSize * (mapSize - 1));
			_samplePoints.RemoveAt(mapSize - 1);
			_samplePoints.RemoveAt(0);
		}
		
		private void AddCorners(Vector3 vBotLeft, Vector3 vBotRight, Vector3 vTopLeft, Vector3 vTopRight)
		{
			// Al principio añadimos las 4 esquinas:
			vertices.Add(new Vertex(vBotLeft));
			vertices.Add(new Vertex(vBotRight));
			vertices.Add(new Vertex(vTopLeft));
			vertices.Add(new Vertex(vTopRight));

			// Las unimos con Aristas formando 2 Triangulos
			Edge e1 = AddEdge(new Vertex(vBotLeft), new Vertex(vBotRight));
			Edge e2 = AddEdge(new Vertex(vBotRight), new Vertex(vTopRight));
			Edge e3 = AddEdge(new Vertex(vTopRight), new Vertex(vBotLeft));
			Edge e4 = AddEdge(new Vertex(vTopRight), new Vertex(vTopLeft));
			Edge e5 = AddEdge(new Vertex(vTopLeft), new Vertex(vBotLeft));

			// Triangulos
			AddTri(e1, e2, e3);
			AddTri(e3, e4, e5);
		}

		#endregion


		#region BUILD LOOP
		
		public struct PointCandidate
		{
			public Vector3 point;
			
			// Guardamos DONDE caen los puntos, que puede ser o un Tri o un Edge si cae entre 2 triangulos
			public int triIntersectedIndex;
			public Edge edgeIntersectedIndex;

			public PointCandidate(Vector3 point, int tri, Edge edge)
			{
				this.point = point;
				triIntersectedIndex = tri;
				edgeIntersectedIndex = edge;
			}
		}

		public List<PointCandidate> pointsToAdd = new();
		public HashSet<Triangle> deletedTriangles = new();
		public HashSet<Edge> deletedEdges = new();
		
		/// <summary>
		///     Bucle Incremental de Adición de nuevos Vertices que cumplen con la condicion de ser añadidos:
		///     Mayor error del tolerado
		/// </summary>
		public void AddPointLoop(int maxIterations = -1)
		{
			int iterations = 0;

			// Condicion de parada: ningun punto del Mapa de Alturas tiene un error mayor al tolerado
			while (iterations > maxIterations)
			{
				SearchPointsToAdd();
				if (pointsToAdd.Count == 0) break;
				
				AddVertexLoopIteration();

				iterations++;
			}
		}

		/// <summary>
		///		Busca puntos candidatos pa añadir en el mapa, y guarda tambien el triangulo o el eje donde cae ese punto
		/// </summary>
		public void SearchPointsToAdd(int maxPointsPerIteration = 5, float minDistanceBetweenPoints = 0)
		{
			// Busca el Punto de Maximo Error si supera la toleracia
			try
			{
				if (maxPointsPerIteration == 1)
				{
					if (FindMaxErrorPoint(out Vector3 point, out int tri, out Edge edge))
						pointsToAdd.Add(new PointCandidate(point, tri, edge));
				}
				else
				{
					Vector3[] points = FindMaxErrorPoint(
						out int[] trisIntersected, out Edge[] edgesIntersected,
						maxPointsPerIteration,
						minDistanceBetweenPoints
					);

					for (int i = 0; i < points.Length; i++) 
						pointsToAdd.Add(new PointCandidate(points[i], trisIntersected[i], edgesIntersected[i]));
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message + "\n" + e.StackTrace);
			}
		}

		/// <summary>
		///     Iteracion standalone del bucle principal para ejecutar progresivamente y añadir nuevos vertices.
		/// </summary>
		public void AddVertexLoopIteration()
		{
			// No encuentra un Punto => Se cumple la condicion de parada
			if (pointsToAdd.Count > 0) return;


			// Lo añade a la Malla actualizando la Topologia
			// y se le pasa la Informacion sobre la posicion del punto calculada en el Calculo del Error (Triangulo o Eje)
			for (int i = pointsToAdd.Count - 1; i >= 0; i++)
			{
				AddPoint(pointsToAdd[i]);
				_samplePoints.Remove(pointsToAdd[i].point);
			}
		}

		#endregion


		/// <summary>
		///     Añade un Punto como Vertice del TIN y actualiza la Topologia.
		///     <p>
		///         En caso de estar añadiendo varios seguidos en una misma iteracion, debemos comprobar que su triangulo
		///         (o eje) no haya sido modificado (eliminado y subdividido) => su error ha cambiado
		///     </p>
		/// </summary>
		/// <param name="point"></param>
		/// <param name="triIndex"></param>
		/// <param name="edge"></param>
		/// <param name="deletedTriangles">Triangulos que se van a eliminar al añadir el Punto</param>
		/// <param name="deletedEdges">Triangulos que se van a eliminar al añadir el Punto</param>
		/// <returns>Devuelve this para poder llamar otros metodos en cadena</returns>
		private Tin AddPoint(
			Vector3 point, int triIndex, Edge edge, HashSet<Triangle> deletedTriangles, HashSet<Edge> deletedEdges
		)
		{
			deletedTriangles ??= new HashSet<Triangle>();
			deletedEdges ??= new HashSet<Edge>();

			if (triIndex == -1 && edge == Edge.InvalidEdge)
				// Si no se ha precalculado lo calculamos
				if (!GetTriangleOrEdge(point.ToV2XZ(), out triIndex, out edge))
				{
					// Si aun no se consigue nada es que o esta fuera o ya se añadio
					_samplePoints.Remove(point);
					Debug.LogError(
						"Uno de los Puntos del Mapa de Alturas no aporta nada" +
						" (Esta fuera o ya estaba en los vertices del TIN"
					);
					return this;
				}

			// Siempre que se haya añadido un punto antes de este en la misma iteracion (no se ha recalculado su error)
			// El error de este nuevo punto no habra variado porque se calcula con el triangulo (o arista) al que pertenece
			// Por lo que si el punto anterior elimino ese triangulo (o arista) al que pertenece modifico su topologia,
			// y el error habra cambiado, por lo que no es seguro añadirlo, habria que recalcularlo
			// y comprobar si vale la pena añadirlo otra vez, por lo que lo descartamos:

			if ((triIndex >= 0 && deletedTriangles.Contains(triangles[triIndex])) || (edge != Edge.InvalidEdge && deletedEdges.Contains(edge)))
			{
				// El punto no se añadira
				int index = lastVertexAdded.IndexOf(point);
				lastVertexAdded.RemoveAt(index);
				lastVertexError.RemoveAt(index);
				return this;
			}

			// Añadimos el Punto, pero segun si pertenece a un Triangulo o a una Arista
			// usamos el metodo normal o el especial:
			if (triIndex >= 0 && !deletedTriangles.Contains(triangles[triIndex]))
				AddPointInTri(point, triangles[triIndex], deletedTriangles, deletedEdges);

			else if (edge != Edge.InvalidEdge && !deletedEdges.Contains(edge))
				AddPointInEdge(point, edge, deletedTriangles, deletedEdges);


			return this;
		}
		
		private void AddPoint(PointCandidate p) => AddPoint(p.point, p.triIntersectedIndex, p.edgeIntersectedIndex, deletedTriangles, deletedEdges);

		/// <summary>
		///     Añade un Punto dentro de un Triangulo (caso normal).
		///     Crea 3 nuevas Aristas, 3 nuevos Triangulos y elimina el Triangulo antiguo
		/// </summary>
		/// <param name="point"></param>
		/// <param name="tri"></param>
		/// <param name="deletedTriangles">Triangulos que se van a eliminar al añadir el Punto</param>
		/// <param name="deletedEdges">Triangulos que se van a eliminar al añadir el Punto</param>
		private void AddPointInTri(Vector3 point, Triangle tri, HashSet<Triangle> deletedTriangles, HashSet<Edge> deletedEdges)
		{
			deletedTriangles ??= new HashSet<Triangle>();
			deletedEdges ??= new HashSet<Edge>();

			// Añade el nuevo Vertice
			Vertex v = new(point);
			vertices.Add(v);

			// Creamos las nuevas Aristas uniendo el Punto nuevo con los Vertices del Triangulo
			Edge e1 = AddEdge(v, tri.v1);
			Edge e2 = AddEdge(v, tri.v2);
			Edge e3 = AddEdge(v, tri.v3);

			// Añadimos los triangulos con los ejes nuevos + la arista antigua
			// (aquella cuyo begin y end sean el end de la nueva arista)
			// Y ademas asigna a esos ejes el propio triangulo nuevo, ya sea a la Izquierda o a la Derecha
			Triangle tri1 = AddTri(e1, e2, tri);
			Triangle tri2 = AddTri(e2, e3, tri);
			Triangle tri3 = AddTri(e3, e1, tri);

			// Elimina el Triangulo Viejo
			triangles.Remove(tri);
			deletedTriangles.Add(tri);

			LegalizeEdge(tri1.GetEdge(e1.end, e2.end), tri1, v, deletedTriangles, deletedEdges);
			LegalizeEdge(tri2.GetEdge(e2.end, e3.end), tri2, v, deletedTriangles, deletedEdges);
			LegalizeEdge(tri3.GetEdge(e3.end, e1.end), tri3, v, deletedTriangles, deletedEdges);
		}

		/// <summary>
		///     Añade un Punto dentro de un Eje (caso excepcional).
		///     Crea 4 nuevas Aristas, 4 nuevos Triangulos y elimina el Eje antiguo y los 2 Triangulos vecinos
		/// </summary>
		/// <param name="point"></param>
		/// <param name="edge"></param>
		/// <param name="deletedTriangles">Triangulos que se van a eliminar al añadir el Punto</param>
		/// <param name="deletedEdges">Triangulos que se van a eliminar al añadir el Punto</param>
		private void AddPointInEdge(
			Vector3 point, Edge edge, HashSet<Triangle> deletedTriangles, HashSet<Edge> deletedEdges
		)
		{
			deletedTriangles ??= new HashSet<Triangle>();
			deletedEdges ??= new HashSet<Edge>();

			// Añade el nuevo Vertice
			Vertex v = new (point);
			vertices.Add(v);

			// Creamos 2 nuevas Aristas uniendo el Punto nuevo con los Vertices opuestos de los Triangulos Vecinos

			// Hay que tener en cuenta que podria ser frontera el eje:
			Edge e1 = Edge.InvalidEdge;
			Edge e2 = Edge.InvalidEdge;

			// Hay que tener en cuenta que puede ser Eje Frontera
			if (edge.lTriIndex >= 0)
			{
				triangles[edge.lTriIndex].GetOppositeVertex(out Vertex opposite, edge);
				e1 = AddEdge(v, opposite);
			}

			if (edge.rTriIndex >= 0)
			{
				triangles[edge.rTriIndex].GetOppositeVertex(out Vertex opposite, edge);
				e2 = AddEdge(v, opposite);
			}

			if (e1 == Edge.InvalidEdge && e2 == Edge.InvalidEdge)
				throw new Exception(
					"Al añadir un Punto en una Arista " +
					"no se han encontrado ningun triangulo vecino"
				);

			// Y 2 mas subdividiendo la arista del punto en 2 segmentos
			Edge e3 = AddEdge(v, edge.begin);
			Edge e4 = AddEdge(v, edge.end);

			// Añadimos los triangulos
			// Y ademas asigna a los ejes el propio triangulo nuevo, ya sea a la Izquierda o a la Derecha
			// El e1 esta en el tIzq y el e2 en el tDer, e3 y e4 forman el eje compartido por ambos
			Triangle tri1 = Triangle.InvalidTri;
			Triangle tri2 = Triangle.InvalidTri;
			Triangle tri3 = Triangle.InvalidTri;
			Triangle tri4 = Triangle.InvalidTri;
			if (e1 != Edge.InvalidEdge)
			{
				tri1 = AddTri(e1, e3, triangles[edge.lTriIndex]);
				tri2 = AddTri(e1, e4, triangles[edge.lTriIndex]);
			}

			if (e2 != Edge.InvalidEdge)
			{
				tri3 = AddTri(e2, e3, triangles[edge.rTriIndex]);
				tri4 = AddTri(e2, e4, triangles[edge.rTriIndex]);
			}

			// Elimina los Triangulos Antiguos y el Eje
			if (edge.lTriIndex >= 0)
			{
				Triangle tri = triangles[edge.lTriIndex];
				deletedTriangles.Add(tri);
				triangles.Remove(tri);
			}

			if (edge.rTriIndex >= 0)
			{
				Triangle tri = triangles[edge.rTriIndex];
				deletedTriangles.Add(tri);
				triangles.Remove(tri);
			}

			deletedEdges.Add(edge);
			edges.Remove(edge);

			// Legalizamos los ejes opuestos de cada tri al vertice nuevo
			if (tri1 != Triangle.InvalidTri && tri2 != Triangle.InvalidTri)
			{
				LegalizeEdge(tri1.GetEdge(e1.end, e3.end), tri1, v, deletedTriangles, deletedEdges);
				LegalizeEdge(tri2.GetEdge(e1.end, e4.end), tri2, v, deletedTriangles, deletedEdges);
			}

			if (tri3 != Triangle.InvalidTri && tri4 != Triangle.InvalidTri)
			{
				LegalizeEdge(tri3.GetEdge(e2.end, e3.end), tri3, v, deletedTriangles, deletedEdges);
				LegalizeEdge(tri4.GetEdge(e2.end, e4.end), tri4, v, deletedTriangles, deletedEdges);
			}
		}

		/// <summary>
		///     Añade una Arista
		/// </summary>
		/// <param name="v1">Begin</param>
		/// <param name="v2">End</param>
		/// <returns>Devuelve el eje</returns>
		private Edge AddEdge(Vertex v1, Vertex v2)
		{
			Edge edge = new Edge(v1, v2, -1, -1, edges.Count);
			edges.Add(edge);

			return edge;
		}

		/// <summary>
		///     Añade un Triangulo a partir de 3 Aristas.
		///     Y además asigna a estas aristas el propio triángulo que se va a añadir.
		/// </summary>
		/// <param name="e1">Arista 1</param>
		/// <param name="e2">Arista 2</param>
		/// <param name="e3">Arista 3</param>
		/// <returns>Triangulo creado</returns>
		private Triangle AddTri(Edge e1, Edge e2, Edge e3)
		{
			// Se crea el Triangulo a base de las Aristas,
			// los Vertices se añaden de forma que siempre estan ordenados de forma Antihoraria
			Triangle tri = new(e1, e2, e3, triangles.Count);
			triangles.Add(tri);

			// Asignamos a cada Arista el nuevo Triangulo,
			// que implicitamente ya se encarga de ponerlo como Izq o Der segun la posicion del Vertice opuesto
			e1.AssignTriangle(tri);
			e2.AssignTriangle(tri);
			e3.AssignTriangle(tri);

			return tri;
		}

		/// <summary>
		///     Añade un Triangulo dentro de otro.
		///     <para>Comprueba cual de las aristas del Triangulo contenedor es la que permite crear el triangulo con e1 y e2</para>
		///     <para>
		///         Para ello suponemos que e1 y e2 acaban en la Arista del Tri antiguo y
		///         con GetEdge() busca la que coincida con e1.end -> e2.end o al contrario e2.end -> e3.end
		///     </para>
		///     <para>Ademas tambien legalizamos la Arista antigua conforme al nuevo Triangulo</para>
		/// </summary>
		/// <param name="e1">Nuevo Eje 1</param>
		/// <param name="e2">Nuevo Eje 2</param>
		/// <param name="oldTri">Triangulo contenedor del nuevo</param>
		/// <returns></returns>
		private Triangle AddTri(Edge e1, Edge e2, Triangle oldTri)
		{
			Edge oldEdge = oldTri.GetEdge(e1.end, e2.end);

			if (oldEdge != Edge.InvalidEdge)
			{
				// Creamos el Nuevo Triangulo
				Triangle newTri = AddTri(e1, e2, oldEdge);

				return newTri;
			}

			return Triangle.InvalidTri;
		}

		/// <summary>
		///     Legaliza una Arista con el metodo de Delaunay (si esta dentro del circulo el vertice opuesto => FLIP)
		/// </summary>
		/// <param name="edge">Arista a Legalizar</param>
		/// <param name="tri">Triangulo que contiene la Arista y el Vertice nuevo</param>
		/// <param name="newVertex">Vertice Nuevo (opuesto a la arista)</param>
		/// <param name="deletedTriangles">Lista de Triangulos que se han eliminado al hacer FLIP</param>
		/// <param name="deletedEdges">Lista de Ejes que se han eliminado al hacer FLIP</param>
		/// <returns>True si se ha tenido que Legalizar</returns>
		private bool LegalizeEdge(
			Edge edge, Triangle tri, Vertex newVertex, HashSet<Triangle> deletedTriangles,
			HashSet<Edge> deletedEdges
		)
		{
			deletedEdges ??= new HashSet<Edge>();
			deletedTriangles ??= new HashSet<Triangle>();

			int triIndex = triangles.IndexOf(tri);

			// Si es frontera no hay que legalizarlo
			if (edge.IsFrontier) return false;

			// Buscamos el vecino del eje contrario a Tri
			int neighbourIndex = edge.lTriIndex == triIndex ? edge.rTriIndex : edge.lTriIndex;

			// Si no tiene es que el Eje es FRONTERA, no hace falta hacer FLIP
			if (neighbourIndex == -1) return false;
			
			Triangle neighbourTri = triangles[neighbourIndex];

			neighbourTri.GetOppositeVertex(out Vertex oppositeVertex, edge);

			// Comprobamos si vertice de el vertice del Vecino opuesto al Eje
			// esta dentro del Circulo formado por el vertice de Tri opuesto al Eje (el nuevo) y los demas vertices del Eje
			Vector2 p = oppositeVertex.xz;
			Vector2 a = newVertex.xz;
			Vector2 b = edge.begin.xz;
			Vector2 c = edge.end.xz;
			if (!MathVectorExtensions.PointInCirle(p, a, b, c)) return false;

			// FLIP:

			// Creamos el nuevo Eje
			Edge newEdge = AddEdge(newVertex, oppositeVertex);

			// Y cogemos los ejes externos de cada triangulo
			Edge triE1 = tri.GetEdge(newVertex, edge.begin);
			Edge triE2 = tri.GetEdge(newVertex, edge.end);
			Edge neighE1 = neighbourTri.GetEdge(oppositeVertex, edge.begin);
			Edge neighE2 = neighbourTri.GetEdge(oppositeVertex, edge.end);

			// Creo los Triangulos nuevos a partir de los ejes antiguos y el nuevo
			// Los Ejes antiguos seran (nuevo -> oldEdge.begin) y (opposite -> oldEdge.begin)
			// Y lo mismo para el oldEdge.end
			Triangle tri1 = AddTri(newEdge, triE1, neighE1);
			Triangle tri2 = AddTri(newEdge, triE2, neighE2);


			// Eliminamos los Triangulos y la Arista antiguos, pero antes los guardamos
			deletedTriangles.Add(tri);
			deletedTriangles.Add(neighbourTri);
			// En orden descendente pa no liarla
			triangles.Remove(neighbourTri);
			triangles.Remove(tri);

			deletedEdges.Add(edge);
			edges.Remove(edge);

			// Como cambia la topologia, tenemos que volverlo a comprobar para los ejes nuevos, de forma recursiva
			// Estos vertices son los del triangulo vecino que aun se mantienen, con cada triangulo nuevo 
			LegalizeEdge(neighE1, tri1, newVertex, deletedTriangles, deletedEdges);
			LegalizeEdge(neighE2, tri2, newVertex, deletedTriangles, deletedEdges);

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
		private bool FindMaxErrorPoint(out Vector3 maxErrorPoint, out int pointTriangle, out Edge pointEdge)
		{
			float maxError = 0;
			maxErrorPoint = Vector3.zero;
			pointTriangle = -1;
			pointEdge = Edge.InvalidEdge;

			// Recorremos TODOS los puntos para buscar el de maximo error
			foreach (Vector3 point in _samplePoints)
			{
				float error = GetError(point, out int tri, out Edge edge);

				if (!(error > maxError) || !(error > _errorTolerance)) continue;

				pointTriangle = tri;
				pointEdge = edge;
				maxError = error;
				maxErrorPoint = point;
			}

			// Si no ha encontrado ningun punto que supere el error minimo, no estara inicializado ni el triangulo ni el eje
			if (pointTriangle == -1) return false;

			// Guardamos el historial del punto y su error
			lastVertexAdded.Clear();
			lastVertexError.Clear();
			lastVertexAdded.Add(maxErrorPoint);
			lastVertexError.Add(maxError);

			return true;
		}

		/// <summary>
		///     Busca los N Puntos de mayor Error que no sean cercanos.
		///     <para>
		///         Utiliza colas para un orden FIFO, en el que el primero siempre sera el de menor error
		///         y el nuevo siempre tendra un error mayor que los que ya hay dentro, por lo que, conforme
		///         se va completando ya esta ordenada
		///     </para>
		/// </summary>
		/// <param name="pointTriangleIndices">Triangulos al que pertenecen los puntos elegidos</param>
		/// <param name="pointEdges">Ejes al que pertenecen los puntos elegidos</param>
		/// <param name="maxPoints">N</param>
		/// <param name="minDistanceBetweenPoints"></param>
		/// <returns>Devuelve una lista con los N Puntos de mayor Error</returns>
		private Vector3[] FindMaxErrorPoint(
			out int[] pointTriangleIndices, out Edge[] pointEdges,
			int maxPoints = 5, float minDistanceBetweenPoints = 5
		)
		{
			var maxErrorQueue = new Queue<float>();

			var triangleIndQueue = new Queue<int>();
			var edgeQueue = new Queue<Edge>();

			var pointQueue = new Queue<Vector3>();

			// Recorremos TODOS los puntos para buscar el de maximo error 
			foreach (Vector3 point in _samplePoints)
			{
				float error = GetError(point, out int pointTriIndex, out Edge pointEdge);

				// Si es mayor al tolerado y mayor al maximo de la cola (el ultimo) lo añadimos
				if (!(error > _errorTolerance) ||
				    (maxErrorQueue.Count != 0 && !(error > maxErrorQueue.Last())))
					continue;

				// Con la condicion de estar mas alejado de la minDistanceBetweenPoints de los otros puntos ya añadidos
				Vector3 point1 = point;
				bool atSafeDistance = pointQueue
					.Select(vertex => Vector2.Distance(vertex.ToV2XZ(), point1.ToV2XZ()))
					.All(distance => distance >= minDistanceBetweenPoints);

				if (!atSafeDistance) continue;

				pointQueue.Enqueue(point);
				triangleIndQueue.Enqueue(pointTriIndex);
				edgeQueue.Enqueue(pointEdge);
				maxErrorQueue.Enqueue(error);

				// Si hemos rellenado la cola con el numero maximo de puntos,
				// sacamos el primero, que es el de menor error
				if (pointQueue.Count <= maxPoints) continue;

				pointQueue.Dequeue();
				triangleIndQueue.Dequeue();
				edgeQueue.Dequeue();
				maxErrorQueue.Dequeue();
			}

			// Devolvemos la lista de Puntos candidatos,
			// pero al formarse en una cola, estan ordenados de menor a mayor error
			// Hay que invertir el orden
			Vector3[] pointsSorted = pointQueue.ToArray();
			pointsSorted = pointsSorted.Reverse().ToArray();

			pointTriangleIndices = triangleIndQueue.ToArray();
			pointTriangleIndices = pointTriangleIndices.Reverse().ToArray();
			
			pointEdges = edgeQueue.ToArray();
			pointEdges = pointEdges.Reverse().ToArray();

			// Guardamos los puntos y sus errores para debugear
			lastVertexAdded.Clear();
			lastVertexError.Clear();
			lastVertexAdded = pointQueue.ToList();
			lastVertexError = maxErrorQueue.ToList();

			return pointsSorted;
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
		/// <returns>Error del Punto</returns>
		private float GetError(Vector3 point, out int triangle, out Edge edge)
		{
			// Buscamos el Triangulo al que pertenece o el Eje al que es Colinear
			// Si devuelve false es que no esta en ninguno
			if (!GetTriangleOrEdge(point.ToV2XZ(), out triangle, out edge))
			{
				_samplePoints.Remove(point);
				return 0;
			}

			// 2 casos:
			// Pertenece a un Triangulo
			GetHeightInterpolated(point, out float height);
			if (triangle != -1) return Mathf.Abs(height - point.y);

			// Pertenece a un Eje
			if (edge != Edge.InvalidEdge) return Mathf.Abs(edge.GetHeightInterpolation(point.ToV2XZ()) - point.y);

			return 0;
		}


		/// <summary>
		///     Busca el Triangulo o el Edge al que pertenece un punto usando el Test Point-Triangle
		/// </summary>
		/// <param name="point">Punto 2D (la altura no es necesaria)</param>
		/// <param name="triIndex">Triangulo al que pertenece</param>
		/// <param name="collinearEdge">Eje colinear al punto</param>
		/// <returns>False si no pertenece a nada</returns>
		public bool GetTriangleOrEdge(Vector2 point, out int triIndex, out Edge collinearEdge)
		{
			triIndex = -1;
			collinearEdge = Edge.InvalidEdge;

			// Buscamos en todos los Triangulos
			for (int i = 0; i < triangles.Count; i++)
			{
				Triangle triangle = triangles[i];
				
				// Test Punto-Triangulo
				Triangle.PointTriPosition test = triangle.PointInTriangle(point, out collinearEdge);

				switch (test)
				{
					// Si esta fuera descarta el Triangulo
					case Triangle.PointTriPosition.OUT: continue;

					// Si esta DENTRO devuelve el Triangulo
					case Triangle.PointTriPosition.IN:
					case Triangle.PointTriPosition.COLINEAR:
						triIndex = i;
						return true;
					
					// Si es su vertice, descartamos el punto por completo y no devolvemos NADA
					case Triangle.PointTriPosition.VERTEX: return false;
					default: throw new ArgumentOutOfRangeException();
				}
			}

			return false;
		}

		public bool GetHeightInterpolated(Vector2 point, out float height)
		{
			height = 0;

			if (!GetTriangleOrEdge(point, out int tri, out Edge edge)) return false;

			height = edge == Edge.InvalidEdge 
				? triangles[tri].GetHeightInterpolation(point) 
				: edge.GetHeightInterpolation(point);
			
			return true;
		}


		/// <summary>
		///     Busca los Triangulos que compartan el punto como vertice
		/// </summary>
		/// <param name="point">Vertice del Triangulo en 2D</param>
		/// <returns>Array de Triangulos que comparten el vertice</returns>
		public Triangle[] GetTrianglesByVertex(Vector2 point) =>
			triangles.Where(tri => tri.Vertices_XZ.Any(vertex => vertex == point)).ToArray();


		/// <summary>
		///     Calcula los puntos de interseccion en 2D de una linea A -> B con cada Triangulo
		///     Los puntos deben estar dentro del AABB del TIN
		/// </summary>
		/// <param name="a">Inicio 2D</param>
		/// <param name="b">Final 2D</param>
		/// <returns>Array de Puntos 2D</returns>
		public Vector2[] GetIntersections(Vector2 a, Vector2 b)
		{
			// Deben estar dentro del AABB, porque si no habria que calcular 2 intersecciones en el primer y ultimo Triangulo
			if (!_aabb.Contains(a) || !_aabb.Contains(b))
				throw new Exception(
					"Calcular los puntos de interseccion de una linea con extremos fuera del AABB del TIN no esta implementado"
				);

			// Buscamos el primer triangulo con el que intersectar
			if (GetTriangleOrEdge(a, out int nextTriangleIndex, out Edge collinearEdge))
				// Si esta en una arista buscamos el primer triangulo intersectado
				if (nextTriangleIndex == -1 && collinearEdge != Edge.InvalidEdge)
				{
					// Segun la posicion de B relativa al eje colinear de A podemos saber el primer triangulo 
					Edge.PointEdgePosition pos =
						Edge.GetPointEdgePosition(
							b,
							collinearEdge.begin.xz,
							collinearEdge.end.xz
						);
					switch (pos)
					{
						case Edge.PointEdgePosition.Left:
							nextTriangleIndex = collinearEdge.lTriIndex;
							break;
						case Edge.PointEdgePosition.Right:
							nextTriangleIndex = collinearEdge.rTriIndex;
							break;
						// Es colinear a la misma arista que a => NO HAY INTERSECCIONES
						default: return Array.Empty<Vector2>();
					}
				}

			if (nextTriangleIndex == -1) return Array.Empty<Vector2>();

			// Si tenemos un Triangulo inicial hacemos un bucle hasta conseguir todas las intersecciones
			var intersections = new List<Vector2>();
			while (triangles[nextTriangleIndex].GetIntersectionPoint(a, b, out Vector2? intersectionPoint, out nextTriangleIndex))
				if (intersectionPoint != null)
				{
					intersections.Add((Vector2)intersectionPoint);
					a = (Vector2)intersectionPoint;
				}

			return intersections.ToArray();
		}


		public void OnDrawGizmos()
		{
			// Dibuja las aristas
			Gizmos.color = Color.magenta;
			
			foreach (Edge e in edges) 
				Gizmos.DrawLine(e.begin.xyz, e.end.xyz);
		}
	}
}
