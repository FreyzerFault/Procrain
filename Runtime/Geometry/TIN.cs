using System;
using System.Collections.Generic;
using System.Linq;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using Unity.Collections;
using UnityEngine;

namespace Procrain.Geometry
{
	public class Tin
	{
		private readonly Bounds2D _bounds;

		private readonly float errorTolerance = 0.1f;

		/// Mapa de Alturas. Clave => Punto 2D, Valor => Altura del Punto (2.5D)
		/// Como Ventaja al Array 2D usado antes, al utilizar la creacion de TIN Incremental,
		/// podemos borrar los puntos ya añadidos para mejorar la busqueda del Punto de Máximo Error
		/// Lista de puntos
		private readonly List<Vector3> heightMap = new();

		private readonly float heightScale = 100;
		public List<Edge> edges = new();

		public List<Vector3> lastVertexAdded;
		public List<float> lastVertexError;
		public int size = 1;

		public List<Triangle> triangles = new();
		public List<Vector3> vertices = new();


		public Tin()
		{
			lastVertexAdded = new List<Vector3>();
			lastVertexError = new List<float>();
		}

		public Tin(List<Triangle> triangles, List<Edge> edges, List<Vector3> vertices)
		{
			this.triangles = triangles;
			this.edges = edges;
			this.vertices = vertices;
		}

		private Tin(float errorTolerance = 1, float heightScale = 100, int maxIterations = -1) : this()
		{
			this.errorTolerance = errorTolerance;
			this.heightScale = heightScale;
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
			IEnumerable<Vector3> points, float errorTolerance = 1, float heightScale = 100,
			int maxIterations = -1, Bounds2D? bounds = null
		)
			: this(errorTolerance, heightScale, maxIterations)
		{
			_bounds = bounds ?? new Bounds2D(Vector2.zero, Vector2.one * size);
			foreach (Vector3 point in points) heightMap.Add(new Vector3(point.x, point.y * this.heightScale, point.z));
		}

		/// <summary>
		///     Creacion del TIN a partir de un Mapa de Alturas
		/// </summary>
		/// <param name="heightMap">Mapa de Alturas</param>
		/// <param name="size"></param>
		/// <param name="bounds">Bounding Box 2D</param>
		/// <param name="errorTolerance">Error Minimo tolerado => Condicion de Añadir un Punto</param>
		/// <param name="heightScale"></param>
		/// <param name="maxIterations">Iteraciones maximas permitidas (para una creacion progresiva y debugging)</param>
		public Tin(
			NativeArray<float> heightMap, int size, Bounds2D? bounds = null, float errorTolerance = 1,
			float heightScale = 100,
			int maxIterations = -1
		)
			: this(errorTolerance, heightScale, maxIterations)
		{
			this.size = size;

			_bounds = bounds ?? new Bounds2D(Vector2.zero, Vector2.one * size);

			// Guardamos el Mapa de Alturas como un conjunto de Vertices potenciales
			for (var x = 0; x < size; x++)
			for (var y = 0; y < size; y++)
				this.heightMap.Add(new Vector3(x, heightMap[x + y * size] * heightScale, y));
		}

		public Tin(
			float[] heightMap, int size, float errorTolerance = 1,
			float heightScale = 100,
			int maxIterations = -1, Bounds2D? bounds = null
		) : this(errorTolerance, heightScale, maxIterations)
		{
			this.size = size;

			_bounds = bounds ?? new Bounds2D(Vector2.zero, Vector2.one * size);

			// Guardamos el Mapa de Alturas como un conjunto de Vertices potenciales
			for (var x = 0; x < size; x++)
			for (var y = 0; y < size; y++)
				this.heightMap.Add(new Vector3(x, heightMap[x + y * size] * heightScale, y));
		}

		// Usa un Array 2D de Mapa de Alturas
		public Tin(
			float[,] heightMap, float errorTolerance = 1, float heightScale = 100,
			int maxIterations = -1
		) : this(errorTolerance, heightScale, maxIterations)
		{
			size = heightMap.GetLength(0);

			_bounds = new Bounds2D(Vector2.zero, Vector2.one * size);

			// Guardamos el Mapa de Alturas como un conjunto de Vertices potenciales
			for (var x = 0; x < size; x++)
			for (var y = 0; y < size; y++)
				this.heightMap.Add(new Vector3(x, heightMap[x, y] * heightScale, y));
		}

		/// <summary>
		///     Crea los 2 Primeros Triangulos a partir de una Nube de Puntos irregular. Busca el punto de mayor x y mayor z
		/// </summary>
		/// <exception cref="Exception">Deben existir los puntos (0,0), (width-1, 0), (0, height-1) y (width-1, height-1)</exception>
		public void InitGeometry()
		{
			// Extraemos las esquinas (0,0), (width-1,0), (0,height-1), (width-1, height-1)
			// Presupongo que SIZE es la anchura del mapa y que Width == Height
			if (heightMap.Count != size * size)
				throw new Exception(
					"Estoy buscando las esquinas del Mapa de Alturas\n" +
					"y resulta que 'size' no indica la anchura. En teoría debería ser 'size * size'.\n" +
					$"size: {size} - size * size: {size * size} heightMap.Count: {heightMap.Count}"
				);

			Vector3 vBotLeft = heightMap[0];
			Vector3 vBotRight = heightMap[size - 1];
			Vector3 vTopLeft = heightMap[size * (size - 1)];
			Vector3 vTopRight = heightMap[size * size - 1];

			// Al principio añadimos las 4 esquinas:
			vertices.Add(vBotLeft);
			vertices.Add(vBotRight);
			vertices.Add(vTopLeft);
			vertices.Add(vTopRight);

			// Las unimos con Aristas formando 2 Triangulos
			Edge e1 = AddEdge(vBotLeft, vBotRight);
			Edge e2 = AddEdge(vBotRight, vTopRight);
			Edge e3 = AddEdge(vTopRight, vBotLeft);
			Edge e4 = AddEdge(vTopRight, vTopLeft);
			Edge e5 = AddEdge(vTopLeft, vBotLeft);

			// Triangulos
			AddTri(e1, e2, e3);
			AddTri(e3, e4, e5);
		}

		/// <summary>
		///     Encapsula la adición inicial de Vértices, Aristas y Triángulos
		///     al principio del Algoritmo de Creacion
		/// </summary>
		public void InitGeometry(float[,] newHeightMap)
		{
			int newW = newHeightMap.GetLength(0);
			int newH = newHeightMap.GetLength(1);

			// Extraemos las esquinas (0,0), (width-1,0), (0,height-1), (width-1, height-1)
			var vBotLeft = new Vector3(0, newHeightMap[0, 0] * heightScale, 0);
			var vBotRight = new Vector3(newW - 1, newHeightMap[newW - 1, 0] * heightScale, 0);
			var vTopLeft = new Vector3(0, newHeightMap[0, newH - 1] * heightScale, newH - 1);
			var vTopRight = new Vector3(newW - 1, newHeightMap[newW - 1, newH - 1] * heightScale, newH - 1);

			InitCorners(vBotLeft, vBotRight, vTopLeft, vTopRight);
		}

		public void InitGeometry(float[] newHeightMap, int newSize)
		{
			int lastX = newSize - 1;
			int lastY = newSize * (newSize - 1);
			// Extraemos las esquinas (0,0), (width-1,0), (0,height-1), (width-1, height-1)
			var vBotLeft = new Vector3(0, newHeightMap[0] * heightScale, 0);
			var vBotRight = new Vector3(newSize - 1, newHeightMap[lastX] * heightScale, 0);
			var vTopLeft = new Vector3(0, newHeightMap[lastY] * heightScale, newSize - 1);
			var vTopRight = new Vector3(newSize - 1, newHeightMap[lastX + lastY] * heightScale, newSize - 1);

			InitCorners(vBotLeft, vBotRight, vTopLeft, vTopRight);
		}

		public void InitGeometry(NativeArray<float> newHeightMap, int newSize)
		{
			int lastX = newSize - 1;
			int lastY = newSize * (newSize - 1);
			// Extraemos las esquinas (0,0), (width-1,0), (0,height-1), (width-1, height-1)
			var vBotLeft = new Vector3(0, newHeightMap[0] * heightScale, 0);
			var vBotRight = new Vector3(newSize - 1, newHeightMap[lastX] * heightScale, 0);
			var vTopLeft = new Vector3(0, newHeightMap[lastY] * heightScale, newSize - 1);
			var vTopRight = new Vector3(newSize - 1, newHeightMap[lastX + lastY] * heightScale, newSize - 1);

			InitCorners(vBotLeft, vBotRight, vTopLeft, vTopRight);
		}

		private void InitCorners(Vector3 vBotLeft, Vector3 vBotRight, Vector3 vTopLeft, Vector3 vTopRight)
		{
			// Al principio añadimos las 4 esquinas:
			vertices.Add(vBotLeft);
			vertices.Add(vBotRight);
			vertices.Add(vTopLeft);
			vertices.Add(vTopRight);

			// Las unimos con Aristas formando 2 Triangulos
			Edge e1 = AddEdge(vBotLeft, vBotRight);
			Edge e2 = AddEdge(vBotRight, vTopRight);
			Edge e3 = AddEdge(vTopRight, vBotLeft);
			Edge e4 = AddEdge(vTopRight, vTopLeft);
			Edge e5 = AddEdge(vTopLeft, vBotLeft);

			// Triangulos
			AddTri(e1, e2, e3);
			AddTri(e3, e4, e5);
		}


		/// <summary>
		///     Bucle Incremental de Adición de nuevos Vertices que cumplen con la condicion de ser añadidos:
		///     Mayor error del tolerado
		/// </summary>
		public void AddPointLoop(int maxIterations = -1)
		{
			var iterations = 0;

			// Condicion de parada: ningun punto del Mapa de Alturas tiene un error mayor al tolerado
			while (true)
			{
				if (!AddPointLoopIteration() && iterations >= maxIterations) break;

				iterations++;
			}
		}

		/// <summary>
		///     Iteracion standalone del bucle principal para ejecutar progresivamente.
		/// </summary>
		/// <returns>
		///     Devuelve false en caso de haber acabado de añadir puntos por encima del error tolerado.
		///     O cuando ocurra algun error
		/// </returns>
		public bool AddPointLoopIteration(int maxPointsPerIteration = 5, float minDistanceBetweenPoints = 0)
		{
			var pointsToAdd = new List<Vector3>();
			var pointTriangles = new List<Triangle>();
			var pointEdges = new List<Edge>();

			Vector3 point = Vector3.one;
			Triangle tri = null;
			Edge edge = null;

			// Busca el Punto de Maximo Error si supera la toleracia
			try
			{
				if (maxPointsPerIteration == 1)
				{
					if (FindMaxErrorPoint(out point, out tri, out edge))
					{
						pointsToAdd.Add(point);
						pointTriangles.Add(tri);
						pointEdges.Add(edge);
					}
				}
				else
				{
					pointsToAdd = FindMaxErrorPoint(
						out pointTriangles,
						out pointEdges,
						maxPointsPerIteration,
						minDistanceBetweenPoints
					);
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message + "\n" + e.StackTrace);
				return false;
			}

			// No encuentra un Punto => Se cumple la condicion de parada
			if (pointsToAdd.Count > 0) return false;

			var deletedTriangles = new HashSet<Triangle>();
			var deletedEdges = new HashSet<Edge>();

			// Lo añade a la Malla actualizando la Topologia
			// y se le pasa la Informacion sobre la posicion del punto calculada en el Calculo del Error (Triangulo o Eje)
			for (var i = 0; i < pointsToAdd.Count; i++)
			{
				AddPoint(pointsToAdd[i], pointTriangles[i], pointEdges[i], deletedTriangles, deletedEdges);
				heightMap.Remove(pointsToAdd[i]);
			}

			return true;
		}

		/// <summary>
		///     Añade un Punto como Vertice del TIN y actualiza la Topologia.
		///     <p>
		///         En caso de estar añadiendo varios seguidos en una misma iteracion, debemos comprobar que su triangulo
		///         (o eje) no haya sido modificado (eliminado y subdividido) => su error ha cambiado
		///     </p>
		/// </summary>
		/// <param name="point"></param>
		/// <param name="tri"></param>
		/// <param name="edge"></param>
		/// <param name="deletedTriangles">Triangulos que se van a eliminar al añadir el Punto</param>
		/// <param name="deletedEdges">Triangulos que se van a eliminar al añadir el Punto</param>
		/// <returns>Devuelve this para poder llamar otros metodos en cadena</returns>
		private Tin AddPoint(
			Vector3 point, Triangle tri, Edge edge, HashSet<Triangle> deletedTriangles, HashSet<Edge> deletedEdges
		)
		{
			deletedTriangles ??= new HashSet<Triangle>();
			deletedEdges ??= new HashSet<Edge>();

			if (tri == null && edge == null)
				// Si no se ha precalculado lo calculamos
				if (!GetTriangle(point.ToVector2xz(), out tri, out edge))
				{
					// Si aun no se consigue nada es que o esta fuera o ya se añadio
					heightMap.Remove(point);
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

			if ((tri != null && deletedTriangles.Contains(tri)) || (edge != null && deletedEdges.Contains(edge)))
			{
				// El punto no se añadira
				int index = lastVertexAdded.IndexOf(point);
				lastVertexAdded.RemoveAt(index);
				lastVertexError.RemoveAt(index);
				return this;
			}

			// Añadimos el Punto, pero segun si pertenece a un Triangulo o a una Arista
			// usamos el metodo normal o el especial:
			if (tri != null && !deletedTriangles.Contains(tri))
				AddPointInTri(point, tri, deletedTriangles, deletedEdges);

			else if (edge != null && !deletedEdges.Contains(edge))
				AddPointInEdge(point, edge, deletedTriangles, deletedEdges);


			return this;
		}

		/// <summary>
		///     Añade un Punto dentro de un Triangulo (caso normal).
		///     Crea 3 nuevas Aristas, 3 nuevos Triangulos y elimina el Triangulo antiguo
		/// </summary>
		/// <param name="point"></param>
		/// <param name="tri"></param>
		/// <param name="deletedTriangles">Triangulos que se van a eliminar al añadir el Punto</param>
		/// <param name="deletedEdges">Triangulos que se van a eliminar al añadir el Punto</param>
		private void AddPointInTri(
			Vector3 point, Triangle tri, HashSet<Triangle> deletedTriangles,
			HashSet<Edge> deletedEdges
		)
		{
			deletedTriangles ??= new HashSet<Triangle>();
			deletedEdges ??= new HashSet<Edge>();

			// Añade el nuevo Vertice
			vertices.Add(point);

			// Creamos las nuevas Aristas uniendo el Punto nuevo con los Vertices del Triangulo
			Edge e1 = AddEdge(point, tri.v1);
			Edge e2 = AddEdge(point, tri.v2);
			Edge e3 = AddEdge(point, tri.v3);

			// Añadimos los triangulos con los ejes nuevos + la arista antigua
			// (aquella cuyo begin y end sean el end de la nueva arista)
			// Y ademas asigna a esos ejes el propio triangulo nuevo, ya sea a la Izquierda o a la Derecha
			Triangle tri1 = AddTri(e1, e2, tri);
			Triangle tri2 = AddTri(e2, e3, tri);
			Triangle tri3 = AddTri(e3, e1, tri);

			// Elimina el Triangulo Viejo
			triangles.Remove(tri);
			deletedTriangles.Add(tri);

			LegalizeEdge(tri1.GetEdge(e1.end, e2.end), tri1, point, deletedTriangles, deletedEdges);
			LegalizeEdge(tri2.GetEdge(e2.end, e3.end), tri2, point, deletedTriangles, deletedEdges);
			LegalizeEdge(tri3.GetEdge(e3.end, e1.end), tri3, point, deletedTriangles, deletedEdges);
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
			vertices.Add(point);

			// Creamos 2 nuevas Aristas uniendo el Punto nuevo con los Vertices opuestos de los Triangulos Vecinos

			// Hay que tener en cuenta que podria ser frontera el eje:
			Edge e1 = null;
			Edge e2 = null;

			// Hay que tener en cuenta que puede ser Eje Frontera
			if (edge.LeftTri != null)
			{
				edge.LeftTri.GetOppositeVertex(out Vector3 opposite, edge);
				e1 = AddEdge(point, opposite);
			}

			if (edge.RightTri != null)
			{
				edge.RightTri.GetOppositeVertex(out Vector3 opposite, edge);
				e2 = AddEdge(point, opposite);
			}

			if (e1 == null && e2 == null)
				throw new Exception(
					"Al añadir un Punto en una Arista " +
					"no se han encontrado ningun triangulo vecino"
				);

			// Y 2 mas subdividiendo la arista del punto en 2 segmentos
			Edge e3 = AddEdge(point, edge.begin);
			Edge e4 = AddEdge(point, edge.end);

			// Añadimos los triangulos
			// Y ademas asigna a los ejes el propio triangulo nuevo, ya sea a la Izquierda o a la Derecha
			// El e1 esta en el tIzq y el e2 en el tDer, e3 y e4 forman el eje compartido por ambos
			Triangle tri1 = null;
			Triangle tri2 = null;
			Triangle tri3 = null;
			Triangle tri4 = null;
			if (e1 != null)
			{
				tri1 = AddTri(e1, e3, edge.LeftTri);
				tri2 = AddTri(e1, e4, edge.LeftTri);
			}

			if (e2 != null)
			{
				tri3 = AddTri(e2, e3, edge.RightTri);
				tri4 = AddTri(e2, e4, edge.RightTri);
			}

			// Elimina los Triangulos Antiguos y el Eje
			if (edge.LeftTri != null)
			{
				deletedTriangles.Add(edge.LeftTri);
				triangles.Remove(edge.LeftTri);
			}

			if (edge.RightTri != null)
			{
				deletedTriangles.Add(edge.RightTri);
				triangles.Remove(edge.RightTri);
			}

			deletedEdges.Add(edge);
			edges.Remove(edge);

			// Legalizamos los ejes opuestos de cada tri al vertice nuevo
			if (tri1 != null && tri2 != null)
			{
				LegalizeEdge(tri1.GetEdge(e1.end, e3.end), tri1, point, deletedTriangles, deletedEdges);
				LegalizeEdge(tri2.GetEdge(e1.end, e4.end), tri2, point, deletedTriangles, deletedEdges);
			}

			if (tri3 != null && tri4 != null)
			{
				LegalizeEdge(tri3.GetEdge(e2.end, e3.end), tri3, point, deletedTriangles, deletedEdges);
				LegalizeEdge(tri4.GetEdge(e2.end, e4.end), tri4, point, deletedTriangles, deletedEdges);
			}
		}

		/// <summary>
		///     Añade una Arista
		/// </summary>
		/// <param name="v1">Begin</param>
		/// <param name="v2">End</param>
		/// <returns>Devuelve el eje</returns>
		private Edge AddEdge(Vector3 v1, Vector3 v2)
		{
			var edge = new Edge(v1, v2, null, null, edges.Count);
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
			var tri = new Triangle(e1, e2, e3, triangles.Count);
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

			if (oldEdge != null)
			{
				// Creamos el Nuevo Triangulo
				Triangle newTri = AddTri(e1, e2, oldEdge);

				return newTri;
			}

			return null;
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
			Edge edge, Triangle tri, Vector3 newVertex, HashSet<Triangle> deletedTriangles,
			HashSet<Edge> deletedEdges
		)
		{
			deletedEdges ??= new HashSet<Edge>();
			deletedTriangles ??= new HashSet<Triangle>();

			// Si es frontera no hay que legalizarlo
			if (edge.IsFrontier) return false;

			// Buscamos el vecino del eje contrario a Tri
			Triangle neighbour = edge.LeftTri == tri ? edge.RightTri : edge.LeftTri;

			// Si no tiene es que el Eje es FRONTERA, no hace falta hacer FLIP
			if (neighbour == null) return false;

			neighbour.GetOppositeVertex(out Vector3 oppositeVertex, edge);

			// Comprobamos si vertice de el vertice del Vecino opuesto al Eje
			// esta dentro del Circulo formado por el vertice de Tri opuesto al Eje (el nuevo) y los demas vertices del Eje
			Vector2 p = oppositeVertex.ToVector2xz();
			Vector2 a = newVertex.ToVector2xz();
			Vector2 b = edge.begin.ToVector2xz();
			Vector2 c = edge.end.ToVector2xz();
			if (!GeometryUtils.PointInCirle(p, a, b, c)) return false;

			// FLIP:

			// Creamos el nuevo Eje
			Edge newEdge = AddEdge(newVertex, oppositeVertex);

			// Y cogemos los ejes externos de cada triangulo
			Edge triE1 = tri.GetEdge(newVertex, edge.begin);
			Edge triE2 = tri.GetEdge(newVertex, edge.end);
			Edge neighE1 = neighbour.GetEdge(oppositeVertex, edge.begin);
			Edge neighE2 = neighbour.GetEdge(oppositeVertex, edge.end);

			// Creo los Triangulos nuevos a partir de los ejes antiguos y el nuevo
			// Los Ejes antiguos seran (nuevo -> oldEdge.begin) y (opposite -> oldEdge.begin)
			// Y lo mismo para el oldEdge.end
			Triangle tri1 = AddTri(newEdge, triE1, neighE1);
			Triangle tri2 = AddTri(newEdge, triE2, neighE2);


			// Eliminamos los Triangulos y la Arista antiguos, pero antes los guardamos
			deletedTriangles.Add(tri);
			deletedTriangles.Add(neighbour);
			triangles.Remove(tri);
			triangles.Remove(neighbour);

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
		private bool FindMaxErrorPoint(out Vector3 maxErrorPoint, out Triangle pointTriangle, out Edge pointEdge)
		{
			float maxError = 0;
			maxErrorPoint = Vector3.zero;
			pointTriangle = null;
			pointEdge = null;

			// Recorremos TODOS los puntos para buscar el de maximo error
			foreach (Vector3 point in heightMap)
			{
				float error = GetError(point, out Triangle tri, out Edge edge);

				if (!(error > maxError) || !(error > errorTolerance)) continue;

				pointTriangle = tri;
				pointEdge = edge;
				maxError = error;
				maxErrorPoint = point;
			}

			// Si no ha encontrado ningun punto que supere el error minimo, no estara inicializado ni el triangulo ni el eje
			if (pointTriangle == null) return false;

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
		/// <param name="pointTriangles">Triangulos al que pertenecen los puntos elegidos</param>
		/// <param name="pointEdges">Ejes al que pertenecen los puntos elegidos</param>
		/// <param name="maxPoints">N</param>
		/// <param name="minDistanceBetweenPoints"></param>
		/// <returns>Devuelve una lista con los N Puntos de mayor Error</returns>
		private List<Vector3> FindMaxErrorPoint(
			out List<Triangle> pointTriangles, out List<Edge> pointEdges,
			int maxPoints = 5, float minDistanceBetweenPoints = 5
		)
		{
			var maxErrorQueue = new Queue<float>();

			var triangleQueue = new Queue<Triangle>();
			var edgeQueue = new Queue<Edge>();

			var pointQueue = new Queue<Vector3>();

			// Recorremos TODOS los puntos para buscar el de maximo error 
			foreach (Vector3 point in heightMap)
			{
				float error = GetError(point, out Triangle pointTri, out Edge pointEdge);

				// Si es mayor al tolerado y mayor al maximo de la cola (el ultimo) lo añadimos
				if (!(error > errorTolerance) ||
				    (maxErrorQueue.Count != 0 && !(error > maxErrorQueue.Last())))
					continue;

				// Con la condicion de estar mas alejado de la minDistanceBetweenPoints de los otros puntos ya añadidos
				Vector3 point1 = point;
				bool atSafeDistance = pointQueue
					.Select(vertex => Vector2.Distance(vertex.ToVector2xz(), point1.ToVector2xz()))
					.All(distance => distance >= minDistanceBetweenPoints);

				if (!atSafeDistance) continue;

				pointQueue.Enqueue(point);
				triangleQueue.Enqueue(pointTri);
				edgeQueue.Enqueue(pointEdge);
				maxErrorQueue.Enqueue(error);

				// Si hemos rellenado la cola con el numero maximo de puntos,
				// sacamos el primero, que es el de menor error
				if (pointQueue.Count <= maxPoints) continue;

				pointQueue.Dequeue();
				triangleQueue.Dequeue();
				edgeQueue.Dequeue();
				maxErrorQueue.Dequeue();
			}

			// Devolvemos la lista de Puntos candidatos,
			// pero al formarse en una cola, estan ordenados de menor a mayor error
			// Hay que invertir el orden
			List<Vector3> pointsSorted = pointQueue.ToList();
			pointsSorted.Reverse();

			pointTriangles = triangleQueue.ToList();
			pointEdges = edgeQueue.ToList();
			pointTriangles.Reverse();
			pointEdges.Reverse();

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
		private float GetError(Vector3 point, out Triangle triangle, out Edge edge)
		{
			// Buscamos el Triangulo al que pertenece o el Eje al que es Colinear
			// Si devuelve false es que no esta en ninguno
			if (!GetTriangle(point.ToVector2xz(), out triangle, out edge))
			{
				heightMap.Remove(point);
				return 0;
			}

			// 2 casos:
			// Pertenece a un Triangulo
			if (triangle != null) return Mathf.Abs(triangle.GetHeightInterpolation(point.ToVector2xz()) - point.y);

			// Pertenece a un Eje
			if (edge != null) return Mathf.Abs(edge.GetHeightInterpolation(point.ToVector2xz()) - point.y);

			return 0;
		}


		/// <summary>
		///     Busca el Triangulo al que pertenece un punto usando el Test Point-Triangle
		/// </summary>
		/// <param name="point">Punto 2D (la altura no es necesaria)</param>
		/// <param name="tri">Triangulo al que pertenece</param>
		/// <param name="collinearEdge">Eje colinear al punto</param>
		/// <returns>False si no pertenece a nada</returns>
		public bool GetTriangle(Vector2 point, out Triangle tri, out Edge collinearEdge)
		{
			tri = null;
			collinearEdge = null;

			// Buscamos en todos los Triangulos
			foreach (Triangle triangle in triangles)
			{
				// Test Punto-Triangulo
				Triangle.PointTriPosition test = triangle.PointInTriangle(point, out collinearEdge);

				switch (test)
				{
					// Si esta fuera descarta el Triangulo
					case Triangle.PointTriPosition.OUT: continue;

					// Si esta DENTRO devuelve el Triangulo
					case Triangle.PointTriPosition.IN:
						tri = triangle;
						return true;

					// Si esta en una Arista devuelve la Arista
					case Triangle.PointTriPosition.COLINEAR: return true;

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

			if (!GetTriangle(point, out Triangle tri, out Edge edge)) return false;

			height = tri?.GetHeightInterpolation(point) ?? edge.GetHeightInterpolation(point);
			return true;
		}


		/// <summary>
		///     Busca los Triangulos que compartan el punto como vertice
		/// </summary>
		/// <param name="point">Vertice del Triangulo en 2D</param>
		/// <returns>Array de Triangulos que comparten el vertice</returns>
		public Triangle[] GetTrianglesByVertex(Vector2 point) =>
			triangles.Where(tri => tri.Vertices2D.Any(vertex => vertex == point)).ToArray();


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
			if (!_bounds.Contains(a) || !_bounds.Contains(b))
				throw new Exception(
					"Calcular los puntos de interseccion de una linea con extremos fuera del AABB del TIN no esta implementado"
				);

			// Buscamos el primer triangulo con el que intersectar
			if (GetTriangle(a, out Triangle nextTriangle, out Edge collinearEdge))
				// Si esta en una arista buscamos el primer triangulo intersectado
				if (nextTriangle == null && collinearEdge != null)
				{
					// Segun la posicion de B relativa al eje colinear de A podemos saber el primer triangulo 
					Edge.PointEdgePosition pos =
						Edge.GetPointEdgePosition(
							b,
							collinearEdge.begin.ToVector2xz(),
							collinearEdge.end.ToVector2xz()
						);
					switch (pos)
					{
						case Edge.PointEdgePosition.LEFT:
							nextTriangle = collinearEdge.LeftTri;
							break;
						case Edge.PointEdgePosition.RIGHT:
							nextTriangle = collinearEdge.RightTri;
							break;
						// Es colinear a la misma arista que a => NO HAY INTERSECCIONES
						default: return Array.Empty<Vector2>();
					}
				}

			if (nextTriangle == null) return new Vector2[] { };

			// Si tenemos un Triangulo inicial hacemos un bucle hasta conseguir todas las intersecciones
			var intersections = new List<Vector2>();
			while (nextTriangle != null &&
			       nextTriangle.GetIntersectionPoint(a, b, out Vector2? intersectionPoint, out nextTriangle))
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
			foreach (Edge e in edges) Gizmos.DrawLine(e.begin, e.end);
		}
	}
}
