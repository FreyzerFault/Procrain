using System;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry;
using JetBrains.Annotations;
using UnityEngine;

namespace Procrain.Geometry
{
    public class Triangle
	{
		public enum PointTriPosition
		{
			IN,
			OUT,
			COLINEAR,
			VERTEX
		}

		public Edge[] edges;

		public Edge E1 => edges[0];
		public Edge E2 => edges[1];
		public Edge E3 => edges[2];

		public Vector3 v1;
		public Vector3 v2;
		public Vector3 v3;

		public Vector2 V1XZ => v1.ToV2xz();
		public Vector2 V2XZ => v2.ToV2xz();
		public Vector2 V3XZ => v3.ToV2xz();

		public Vector3[] Vertices => new[] { v1, v2, v3 };
		public Vector2[] Vertices2D => new[] { V1XZ, V2XZ, V3XZ };

		public int index;

		public Triangle(Edge[] edges, int index = -1)
		{
			this.index = index;

			this.edges = edges;

			// Los vertices los extraemos de las aristas
			v1 = E1.begin;

			// No tienen por que ser todos el begin de las aristas
			// Si el begin de la 2 coincide con el v1, se elige el end
			v2 = E2.begin;
			if (v2 == v1) v2 = E2.end;

			// Lo mismo para v3, si se repite, elegir el otro vertice
			v3 = E3.begin;
			if (v3 == v2 || v3 == v1) v3 = E3.end;

			if (v1 == v2 || v2 == v3 || v3 == v1)
				throw new Exception($"Alguno de los vertices de el Triangulo esta mal: {{{v1}, {v2}, {v3}}}");

			EnsureCounterClockwise();
		}

		public Triangle(Edge e1, Edge e2, Edge e3, int index = -1) : this(new[] { e1, e2, e3 }, index)
		{
		}

		public Triangle(Vector3 v1, Vector3 v2, Vector3 v3, Edge e1, Edge e2, Edge e3, int index = -1)
		{
			this.index = index;

			edges = new[] { e1, e2, e3 };

			this.v1 = v1;
			this.v2 = v2;
			this.v3 = v3;

			EnsureCounterClockwise();
		}

		public Triangle(Vector3 v1, Vector3 v2, Vector3 v3, int index = -1)
		{
			this.index = index;

			this.v1 = v1;
			this.v2 = v2;
			this.v3 = v3;
			edges = new[] { new Edge(v1, v2, this), new Edge(v2, v3, this), new Edge(v3, v1, this) };

			EnsureCounterClockwise();
		}

		private void EnsureCounterClockwise()
		{
			// Hay que ordenarlos en orden ANTIHORARIO
			// (si alguno esta a la Derecha de la arista opuesta se hace un Swap de la opuesta):
			if (GeometryUtils.IsRight(v1, v2, v3)) (v3, v2) = (v2, v3); // SWAP v2 <-> v3
		}

		/// <summary>
		///     Busca el Eje que concuerda con los Vertices pasados como argumentos.
		///     No tiene por que tener la misma orientacion
		/// </summary>
		/// <param name="begin"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public Edge GetEdge(Vector3 begin, Vector3 end)
		{
			Edge[] edges = { E1, E2, E3 };

			foreach (Edge edge in edges)
				if ((edge.begin == begin || edge.begin == end) &&
				    (edge.end == begin || edge.end == end))
					return edge;

			return null;
		}

		/// <summary>
		///     Busca el Eje OPUESTO del Vertice pasado como argumento.
		/// </summary>
		/// <param name="vertex">Vertice Opuesto</param>
		/// <returns></returns>
		public Edge GetOppositeEdge(Vector3 vertex)
		{
			if (vertex == v1) return GetEdge(v2, v3);
			if (vertex == v2) return GetEdge(v3, v1);
			if (vertex == v3) return GetEdge(v1, v2);

			return null;
		}

		/// <summary>
		///     Busca el Vertice que no pertenece a la arista que se pasa
		/// </summary>
		/// <param name="opposite">Vertice Opuesto de la Arista</param>
		/// <param name="edge">Arista opuesta</param>
		/// <returns>False si no lo encuentra o no tiene (es un borde)</returns>
		/// <exception cref="Exception">No encuentra el opuesto</exception>
		public bool GetOppositeVertex(out Vector3 opposite, Edge edge)
		{
			opposite = Vector3.zero;

			// Buscamos el vertice que no pertenece a la arista (no es ni Begin ni End)
			foreach (Vector3 vertex in Vertices)
			{
				if (vertex == edge.begin || vertex == edge.end) continue;
				opposite = vertex;
				return true;
			}

			return false;
		}

		public Triangle GetOppositeTriangle(Edge edge)
		{
			if (edge == E1) return E1.OppositeTri(this);
			if (edge == E2) return E2.OppositeTri(this);
			if (edge == E3) return E3.OppositeTri(this);

			return null;
		}


		/// <summary>
		///     Esta a la DERECHA de cualquier Eje => OUT;
		///     Esta a la Izquierda de TODOS los Ejes => IN;
		///     Es COLINEAR de algun Eje => COLINEAR + ¿A que eje es COLINEAR?;
		/// </summary>
		/// <param name="p"></param>
		/// <param name="colinearEdge">El Eje en caso de ser COLINEAR</param>
		/// <returns>OUT / IN / COLINEAR / VERTEX</returns>
		public PointTriPosition PointInTriangle(Vector2 p, out Edge colinearEdge)
		{
			colinearEdge = null;

			PointTriPosition pos = PointInTriangle(p);
			if (pos == PointTriPosition.COLINEAR)
			{
				// Comprobamos en que eje esta de los 3
				bool colinear1 = Edge.GetPointEdgePosition(p, V1XZ, V2XZ) == Edge.PointEdgePosition.COLINEAR;
				bool colinear2 = Edge.GetPointEdgePosition(p, V2XZ, V3XZ) == Edge.PointEdgePosition.COLINEAR;

				// Buscamos la Arista que concuerda con los vertices del Eje en el que esta
				colinearEdge = colinear1 ? GetEdge(v1, v2) : colinear2 ? GetEdge(v2, v3) : GetEdge(v3, v1);
				return PointTriPosition.COLINEAR;
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
		public PointTriPosition PointInTriangle(Vector2 p)
		{
			// Posicion Relativa del Punto a cada Arista (alineada en orden Antihorario)
			Edge.PointEdgePosition pos1 = Edge.GetPointEdgePosition(p, V1XZ, V2XZ);
			Edge.PointEdgePosition pos2 = Edge.GetPointEdgePosition(p, V2XZ, V3XZ);
			Edge.PointEdgePosition pos3 = Edge.GetPointEdgePosition(p, V3XZ, V1XZ);

			// En cuanto este a la derecha de cualquiera de las Aristas, esta FUERA
			if (pos1 == Edge.PointEdgePosition.RIGHT ||
			    pos2 == Edge.PointEdgePosition.RIGHT ||
			    pos3 == Edge.PointEdgePosition.RIGHT)
				return PointTriPosition.OUT;

			// Si esta a la IZQUIERDA de TODOS => esta DENTRO
			if (pos1 == Edge.PointEdgePosition.LEFT &&
			    pos2 == Edge.PointEdgePosition.LEFT &&
			    pos3 == Edge.PointEdgePosition.LEFT)
				return PointTriPosition.IN;

			// Si no, puede ser colinear con un eje, o estar en el vertice
			// Por si acaso comprobamos primero que no sea un vertice
			if (GeometryUtils.Equals(p, V1XZ) || GeometryUtils.Equals(p, V2XZ) || GeometryUtils.Equals(p, V3XZ))
				return PointTriPosition.VERTEX;

			return PointTriPosition.COLINEAR;
		}


		/// <summary>
		///     ¡¡¡¡¡¡ MENOS EFICIENTE !!!!!!!!
		///     Se basa en la Tecnica del Baricentro, que calcula P como la suma de vectores en la direccion
		///     de las Aristas del Triangulo (A->B y A->C) con una magnitud w1 y w2.
		///     <para>Se usa la función paramétrica del Plano: P = A + w1(C-A) + w2(B-A)</para>
		///     <para>Calculamos w1 y w2, y para que P esté dentro del Triángulo deben ser positivos y su suma menor a 1.</para>
		///     <para>https://www.youtube.com/watch?v=HYAgJN3x4GA</para>
		/// </summary>
		/// <param name="p"></param>
		/// <param name="colinearEdge">Eje al que seria Colinear</param>
		/// <returns></returns>
		public PointTriPosition PointInTriangleBarycentricTechnique(Vector2 p, out Edge colinearEdge)
		{
			colinearEdge = null;

			Vector2 a = V1XZ, b = V2XZ, c = V3XZ;

			float denom1 = (b.y - a.y) * (c.x - a.x) - (b.x - a.x) * (c.y - a.y);
			float denom2 = c.y - a.y;
			if (denom1 == 0 || denom2 == 0) return PointTriPosition.OUT;

			float w1 = (a.x * (c.y - a.y) + (p.y - a.y) * (c.x - a.x) - p.x * (c.y - a.y)) / denom1;

			float w2 = (p.y - a.y - w1 * (b.y - a.y)) / denom2;

			float suma = w1 + w2;

			//Debug.Log("W1: " + w1 + " W2: " + w2 + " Suma: " + suma);

			Vector2 expectedPoint = a + w1 * (b - a) + w2 * (c - a);
			if (!GeometryUtils.Equals(expectedPoint, p))
				throw new Exception("La ecuacion Baricentrica esta mal: " + expectedPoint + " != " + p);

			// w1 y w2 POSITIVOS y suma MENOR a 1 => DENTRO
			if (w1 > GeometryUtils.Epsilon && w2 > GeometryUtils.Epsilon && suma < 1 - GeometryUtils.Epsilon)
				//Debug.Log("POINT IN!!! W1 = " + w1 + " > 0; y W2 = " + w2 + " > 0;" + " y w1 + w2 = " + suma + " < 1");
				return PointTriPosition.IN;

			// w1 o w2 NEGATIVO o suma MAYOR a 1 => FUERA
			if (w1 < -GeometryUtils.Epsilon || w2 < -GeometryUtils.Epsilon || suma > 1 + GeometryUtils.Epsilon)
				return PointTriPosition.OUT;

			// w2 == 0
			if (w2 < GeometryUtils.Epsilon)
				// w1 == 1
				if (GeometryUtils.Equals(w1, 1))
					// VERTEX B
				{
					return PointTriPosition.VERTEX;
				}
				// w1 == 0
				else if (w1 < GeometryUtils.Epsilon)
					// VERTEX A
				{
					return PointTriPosition.VERTEX;
				}
				else
				{
					// COLINEAR A->B
					colinearEdge = GetEdge(v1, v2);
					return PointTriPosition.COLINEAR;
				}

			// w1 == 0
			if (w1 < GeometryUtils.Epsilon)
				// w2 == 1
				if (GeometryUtils.Equals(w2, 1))
					// VERTEX C
				{
					return PointTriPosition.VERTEX;
				}
				else
				{
					// COLINEAR A->C
					colinearEdge = GetEdge(v1, v3);
					return PointTriPosition.COLINEAR;
				}

			// SUMA == 1 => COLINEAR B->C
			if (GeometryUtils.Equals(suma, 1))
			{
				colinearEdge = GetEdge(v2, v3);
				return PointTriPosition.COLINEAR;
			}

			return PointTriPosition.IN;
		}

		/// <summary>
		///     Comrprueba si el Eje A->B intersecta al Triangulo. Solo si A o B estan FUERA
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public bool Intersect(Vector2 a, Vector2 b)
		{
			PointTriPosition posA = PointInTriangle(a);
			PointTriPosition posB = PointInTriangle(b);

			return posA == PointTriPosition.OUT || posB == PointTriPosition.OUT;
		}


		/// <summary>
		///     Calcula el punto de Interseccion de un eje A->B con el Triangulo.
		///     A se presupone que esta DENTRO del Triangulo (o colinear a una arista), por lo que solo tenemos que buscar una
		///     interseccion
		/// </summary>
		/// <param name="a">Punto Inicial</param>
		/// <param name="b">Punto Final</param>
		/// <param name="intersectionPoint">Punto de Interseccion</param>
		/// <param name="nextTriangle">El siguiente Triangulo (en la direccion A -> B)</param>
		/// <returns>false si no hay Interseccion</returns>
		public bool GetIntersectionPoint(
			Vector2 a, Vector2 b, out Vector2? intersectionPoint, out Triangle nextTriangle
		)
		{
			intersectionPoint = null;
			nextTriangle = null;

			if (!Intersect(a, b)) return false;

			// Primer Eje:
			if (E1.GetIntersectionPoint(a, b, out intersectionPoint))
			{
				// El siguiente Triangulo es el distinto a este
				nextTriangle = E1.OppositeTri(this);
				return true;
			}

			// Segundo Eje:
			if (E2.GetIntersectionPoint(a, b, out intersectionPoint))
			{
				nextTriangle = E2.OppositeTri(this);
				return true;
			}

			// Tercer Eje:
			if (E3.GetIntersectionPoint(a, b, out intersectionPoint))
			{
				nextTriangle = E3.OppositeTri(this);
				return true;
			}

			return false;
		}

		/// <summary>
		///     Calcula el punto de Interseccion de un eje A->B con cada arista
		///     Puede haber 1 Punto => A o B esta DENTRO o COLINEAR.
		///     Puede haber 2 Puntos => A y B estan FUERA.
		///     Ninguno si no hay puntos fuera.
		///     Tambien busca el triangulo siguiente en la direccion A -> B
		///     Las Intersecciones Impropias no se cuentan
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="intersectionPoint1"></param>
		/// <param name="intersectionPoint2"></param>
		/// <param name="nextTriangle">El siguiente Triangulo (en la direccion A -> B)</param>
		/// <param name="nextPoint">
		///     El siguiente Punto que sera el inicio de la nueva linea con la que calcular la interseccion
		///     siguiente
		/// </param>
		/// <returns>false si no hay Interseccion</returns>
		public bool GetIntersectionPoint(
			Vector2 a, Vector2 b, out Vector2? intersectionPoint1,
			out Vector2? intersectionPoint2, out Edge edgeIntersected1, out Edge edgeIntersected2,
			[CanBeNull] out Triangle nextTriangle, out Vector2? nextPoint
		)
		{
			intersectionPoint1 = intersectionPoint2 = null;
			edgeIntersected1 = edgeIntersected2 = null;
			nextTriangle = null;
			nextPoint = null;

			if (Intersect(a, b))
			{
				Vector2? intersection = null;
				Edge farEdge = null;

				// Primer Eje:
				if (E1.GetIntersectionPoint(a, b, out intersection))
				{
					farEdge = E1;
					edgeIntersected1 = E1;
					intersectionPoint1 = intersection;
					nextPoint = intersection;
				}

				// Segundo Eje:
				if (E2.GetIntersectionPoint(a, b, out intersection))
					// Si hubo una Interseccion con el Primero => lo asignamos como Segunda Interseccion
					if (intersectionPoint1 == null)
					{
						intersectionPoint1 = intersection;
						edgeIntersected1 = E2;
						nextPoint = intersection;
					}
					else
					{
						intersectionPoint2 = intersection;
						edgeIntersected2 = E2;

						// Si hay 2 Intersecciones => Comprobamos cual esta mas lejos de A
						if (intersectionPoint2 != null &&
						    (a - (Vector2)intersectionPoint2).magnitude > (a - (Vector2)intersectionPoint1).magnitude)
						{
							farEdge = E2;
							nextPoint = intersectionPoint2;
						}
						else
						{
							nextPoint = intersectionPoint1;
						}
					}

				// Si aun no hay una Segunda Interseccion, comprobamos la Tercera Arista
				if (intersectionPoint2 == null)
					if (E3.GetIntersectionPoint(a, b, out intersection))
						// Lo mismo, Si hubo una Interseccion con el Primero => lo asignamos como Segunda Interseccion
						if (intersectionPoint1 == null)
						{
							intersectionPoint1 = intersection;
							edgeIntersected1 = E3;
							nextPoint = intersection;
						}
						else
						{
							intersectionPoint2 = intersection;
							edgeIntersected2 = E3;

							// Si hay 2 Intersecciones => Comprobamos cual esta mas lejos de A
							if (intersectionPoint2 != null &&
							    (a - (Vector2)intersectionPoint2).magnitude >
							    (a - (Vector2)intersectionPoint1).magnitude)
							{
								farEdge = E2;
								nextPoint = intersectionPoint2;
							}
							else
							{
								nextPoint = intersectionPoint1;
							}
						}

				// No hubo interseccion
				if (intersectionPoint1 == null) return false;

				// Comprobamos cual es el Eje mas lejano, el cual tendra de vecino el SIGUIENTE TRIANGULO
				if (farEdge != null) nextTriangle = farEdge.OppositeTri(this);

				return true;
			}

			// Mientras que no haya alguno fuera, no hay interseccion
			// En caso de ser Colinear o ser un Vertice, las intersecciones Impropias no cuentan.
			return false;
		}

		private Vector3? GetVertex(Vector2 v)
		{
			if (v == V1XZ) return v1;
			if (v == V2XZ) return v2;
			if (v == V3XZ) return v3;
			return null;
		}

		/// <summary>
		///     Interpolacion de la altura en un punto 2D del Triangulo 3D.
		///     Inversamente proporcional a la distancia de cada vertice al punto 2D.
		///     https://codeplea.com/triangular-interpolation
		/// </summary>
		/// <param name="p">Punto 2D</param>
		/// <returns>Altura del punto en el triangulo</returns>
		public float GetHeightInterpolation(Vector2 p)
		{
			Vector2 a = V1XZ;
			Vector2 b = V2XZ;
			Vector2 c = V3XZ;

			// Usamos las coordenadas baricentricas como pesos:
			float denom = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
			float w1 = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / denom;
			float w2 = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / denom;
			float w3 = 1 - w1 - w2;

			return (v1.y * w1 + v2.y * w2 + v3.y * w3) / (w1 + w2 + w3);
		}

		public override string ToString() =>
			"t" + index + " {" + v1 + " -> " + v2 + " -> " + v3 + "} (" + E1 + ", " + E2 + ", " + E3 + ")";

		public override int GetHashCode() => v1.GetHashCode() + v2.GetHashCode() + v3.GetHashCode();
	}
}
