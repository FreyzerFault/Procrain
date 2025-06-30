using System;
using System.Linq;
using Procrain.Utils;
using UnityEngine;

namespace Procrain.Geometry
{
    public struct Triangle: IEquatable<Triangle>
	{
		public enum PointTriPosition
		{
			IN,
			OUT,
			COLINEAR,
			VERTEX
		}
		
		public readonly int index;
		
		public readonly Edge e1;
		public readonly Edge e2;
		public readonly Edge e3;
		
		public Tuple<Edge, Edge, Edge> EdgesTuple => new (e1, e2, e3);
		public Edge[] EdgeArray => new Edge[] { e1, e2, e3 };

		public readonly Vertex v1;
		public readonly Vertex v2;
		public readonly Vertex v3;
		
		public Tuple<Vertex, Vertex, Vertex> VertexTuple => new (v1, v2, v3);
		public Vertex[] VertexArray => new Vertex[] { v1, v2, v3 };

		public Vector3 V1 => v1.xyz;
		public Vector3 V2 => v2.xyz;
		public Vector3 V3 => v3.xyz;

		public Vector2 V1_XZ => v1.xz;
		public Vector2 V2_XZ => v2.xz;
		public Vector2 V3_XZ => v3.xz;

		public Vector3[] Vertices => new[] { V1, V2, V3 };
		public Vector2[] Vertices_XZ => new[] { V1_XZ, V2_XZ, V3_XZ };

		public static Triangle InvalidTri => new Triangle(Vertex.InvalidVertex, Vertex.InvalidVertex, Vertex.InvalidVertex); 
		public bool IsInvalid => v1.IsInvalid || v2.IsInvalid || v3.IsInvalid;

		public Triangle(Tuple<Edge, Edge, Edge> edges, int index = -1) : this(edges.Item1, edges.Item2, edges.Item3, index) {}
		
		public Triangle(Edge e1, Edge e2, Edge e3, int index = -1)
		{
			this.index = index;

			this.e1 = e1;
			this.e2 = e2;
			this.e3 = e3;

			// Los vertices los extraemos de las aristas
			v1 = e1.begin;

			// No tienen por que ser todos el begin de las aristas
			// Si el begin de la 2 coincide con el v1, se elige el end
			v2 = e2.begin.Equals(v1) ? e2.end : e2.begin;
			v3 = e3.begin.Equals(v1) || e3.begin.Equals(v2) ? e3.end : e3.begin;
			
			if (v1.Equals(v2) || v2.Equals(v3) || v3.Equals(v1))
				throw new Exception($"Alguno de los vertices de el Triangulo esta mal: {{{V1}, {V2}, {V3}}}");

			// Hay que ordenarlos en orden ANTIHORARIO
			// (si alguno esta a la Derecha de la arista opuesta se hace un Swap de la opuesta):
			if (V3.IsRight(V1, V2)) 
				(v3, v2) = (v2, v3); // SWAP v2 <-> v3
		}
		
		public Triangle(Vertex v1, Vertex v2, Vertex v3, int index = -1)
		: this(new Edge(v1, v2), new Edge(v2, v3), new Edge(v3, v1), index) { }


		/// <summary>
		///     Busca el Eje que concuerda con los Vertices pasados como argumentos.
		///     No tiene por que tener la misma orientacion
		/// </summary>
		/// <param name="begin"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public Edge GetEdge(Vertex begin, Vertex end)
		{
			return EdgeArray.FirstOrDefault(e => 
				(e.begin.Equals(begin) && e.end.Equals(end))
				|| (e.begin.Equals(end) && e.end.Equals(begin))
				);
		}

		/// <summary>
		///     Busca el Eje OPUESTO del Vertice pasado como argumento.
		/// </summary>
		/// <param name="vertex">Vertice Opuesto</param>
		/// <returns></returns>
		public Edge GetOppositeEdge(Vertex vertex) => 
			EdgeArray.FirstOrDefault(e => !e.begin.Equals(vertex) && !e.end.Equals(vertex));

		/// <summary>
		///     Busca el Vertice que no pertenece a la arista que se pasa
		/// </summary>
		/// <param name="opposite">Vertice Opuesto de la Arista</param>
		/// <param name="edge">Arista opuesta</param>
		/// <returns>False si no lo encuentra o no tiene (es un borde)</returns>
		/// <exception cref="Exception">No encuentra el opuesto</exception>
		public bool GetOppositeVertex(out Vertex opposite, Edge edge)
		{
			opposite = Vertex.InvalidVertex;

			// Buscamos el vertice que no pertenece a la arista (no es ni Begin ni End)
			foreach (Vertex vertex in VertexArray)
			{
				if (vertex.Equals(edge.begin) || vertex.Equals(edge.end)) continue;
				opposite = vertex;
				return true;
			}

			return false;
		}

		public Triangle GetOppositeTriangle(Edge edge)
		{
			if (edge.Equals(e1)) return e1.OppositeTri(this);
			if (edge.Equals(e2)) return e2.OppositeTri(this);
			if (edge.Equals(e3)) return e3.OppositeTri(this);

			return InvalidTri;
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
			colinearEdge = Edge.InvalidEdge;

			PointTriPosition pos = PointInTriangle(p);
			if (pos == PointTriPosition.COLINEAR)
			{
				// Comprobamos en que eje esta de los 3
				bool colinear1 = Edge.GetPointEdgePosition(p, V1_XZ, V2_XZ) == Edge.PointEdgePosition.Colinear;
				bool colinear2 = Edge.GetPointEdgePosition(p, V2_XZ, V3_XZ) == Edge.PointEdgePosition.Colinear;

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
			Edge.PointEdgePosition pos1 = Edge.GetPointEdgePosition(p, V1_XZ, V2_XZ);
			Edge.PointEdgePosition pos2 = Edge.GetPointEdgePosition(p, V2_XZ, V3_XZ);
			Edge.PointEdgePosition pos3 = Edge.GetPointEdgePosition(p, V3_XZ, V1_XZ);

			// En cuanto este a la derecha de cualquiera de las Aristas, esta FUERA
			if (pos1 == Edge.PointEdgePosition.Right ||
			    pos2 == Edge.PointEdgePosition.Right ||
			    pos3 == Edge.PointEdgePosition.Right)
				return PointTriPosition.OUT;

			// Si esta a la IZQUIERDA de TODOS => esta DENTRO
			if (pos1 == Edge.PointEdgePosition.Left &&
			    pos2 == Edge.PointEdgePosition.Left &&
			    pos3 == Edge.PointEdgePosition.Left)
				return PointTriPosition.IN;

			// Si no, puede ser colinear con un eje, o estar en el vertice
			// Por si acaso comprobamos primero que no sea un vertice
			if (MathVectorExtensions.Equals(p, V1_XZ) || MathVectorExtensions.Equals(p, V2_XZ) || MathVectorExtensions.Equals(p, V3_XZ))
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
			colinearEdge = Edge.InvalidEdge;

			Vector2 a = V1_XZ, b = V2_XZ, c = V3_XZ;

			float denom1 = (b.y - a.y) * (c.x - a.x) - (b.x - a.x) * (c.y - a.y);
			float denom2 = c.y - a.y;
			if (denom1 == 0 || denom2 == 0) return PointTriPosition.OUT;

			float w1 = (a.x * (c.y - a.y) + (p.y - a.y) * (c.x - a.x) - p.x * (c.y - a.y)) / denom1;

			float w2 = (p.y - a.y - w1 * (b.y - a.y)) / denom2;

			float suma = w1 + w2;

			//Debug.Log("W1: " + w1 + " W2: " + w2 + " Suma: " + suma);

			Vector2 expectedPoint = a + w1 * (b - a) + w2 * (c - a);
			if (!MathVectorExtensions.Equals(expectedPoint, p))
				throw new Exception("La ecuacion Baricentrica esta mal: " + expectedPoint + " != " + p);

			// w1 y w2 POSITIVOS y suma MENOR a 1 => DENTRO
			if (w1 > MathVectorExtensions.Epsilon && w2 > MathVectorExtensions.Epsilon && suma < 1 - MathVectorExtensions.Epsilon)
				//Debug.Log("POINT IN!!! W1 = " + w1 + " > 0; y W2 = " + w2 + " > 0;" + " y w1 + w2 = " + suma + " < 1");
				return PointTriPosition.IN;

			// w1 o w2 NEGATIVO o suma MAYOR a 1 => FUERA
			if (w1 < -MathVectorExtensions.Epsilon || w2 < -MathVectorExtensions.Epsilon || suma > 1 + MathVectorExtensions.Epsilon)
				return PointTriPosition.OUT;

			// w2 == 0
			if (w2 < MathVectorExtensions.Epsilon)
				// w1 == 1
				if (MathVectorExtensions.Equals(w1, 1))
					// VERTEX B
				{
					return PointTriPosition.VERTEX;
				}
				// w1 == 0
				else if (w1 < MathVectorExtensions.Epsilon)
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
			if (w1 < MathVectorExtensions.Epsilon)
				// w2 == 1
				if (MathVectorExtensions.Equals(w2, 1))
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
			if (MathVectorExtensions.Equals(suma, 1))
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
			nextTriangle = InvalidTri;

			if (!Intersect(a, b)) return false;

			// Primer Eje:
			if (e1.GetIntersectionPoint(a, b, out intersectionPoint))
			{
				// El siguiente Triangulo es el distinto a este
				nextTriangle = e1.OppositeTri(this);
				return true;
			}

			// Segundo Eje:
			if (e2.GetIntersectionPoint(a, b, out intersectionPoint))
			{
				nextTriangle = e2.OppositeTri(this);
				return true;
			}

			// Tercer Eje:
			if (e3.GetIntersectionPoint(a, b, out intersectionPoint))
			{
				nextTriangle = e3.OppositeTri(this);
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
			out Triangle nextTriangle, out Vector2? nextPoint
		)
		{
			intersectionPoint1 = intersectionPoint2 = null;
			edgeIntersected1 = edgeIntersected2 = Edge.InvalidEdge;
			nextTriangle = InvalidTri;
			nextPoint = null;

			if (Intersect(a, b))
			{
				Edge farEdge = Edge.InvalidEdge;

				// Primer Eje:
				if (e1.GetIntersectionPoint(a, b, out Vector2? intersection))
				{
					farEdge = e1;
					edgeIntersected1 = e1;
					intersectionPoint1 = intersection;
					nextPoint = intersection;
				}

				// Segundo Eje:
				if (e2.GetIntersectionPoint(a, b, out intersection))
					// Si hubo una Interseccion con el Primero => lo asignamos como Segunda Interseccion
					if (intersectionPoint1 == null)
					{
						intersectionPoint1 = intersection;
						edgeIntersected1 = e2;
						nextPoint = intersection;
					}
					else
					{
						intersectionPoint2 = intersection;
						edgeIntersected2 = e2;

						// Si hay 2 Intersecciones => Comprobamos cual esta mas lejos de A
						if (intersectionPoint2 != null &&
						    (a - (Vector2)intersectionPoint2).magnitude > (a - (Vector2)intersectionPoint1).magnitude)
						{
							farEdge = e2;
							nextPoint = intersectionPoint2;
						}
						else
						{
							nextPoint = intersectionPoint1;
						}
					}

				// Si aun no hay una Segunda Interseccion, comprobamos la Tercera Arista
				if (intersectionPoint2 == null)
					if (e3.GetIntersectionPoint(a, b, out intersection))
						// Lo mismo, Si hubo una Interseccion con el Primero => lo asignamos como Segunda Interseccion
						if (intersectionPoint1 == null)
						{
							intersectionPoint1 = intersection;
							edgeIntersected1 = e3;
							nextPoint = intersection;
						}
						else
						{
							intersectionPoint2 = intersection;
							edgeIntersected2 = e3;

							// Si hay 2 Intersecciones => Comprobamos cual esta mas lejos de A
							if (intersectionPoint2 != null &&
							    (a - (Vector2)intersectionPoint2).magnitude >
							    (a - (Vector2)intersectionPoint1).magnitude)
							{
								farEdge = e2;
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
				if (farEdge.IsInvalid) nextTriangle = farEdge.OppositeTri(this);

				return true;
			}

			// Mientras que no haya alguno fuera, no hay interseccion
			// En caso de ser Colinear o ser un Vertice, las intersecciones Impropias no cuentan.
			return false;
		}

		private Vector3? GetVertex(Vector2 v)
		{
			if (v == V1_XZ) return V1;
			if (v == V2_XZ) return V2;
			if (v == V3_XZ) return V3;
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
			Vector2 a = V1_XZ;
			Vector2 b = V2_XZ;
			Vector2 c = V3_XZ;

			// Usamos las coordenadas baricentricas como pesos:
			float denom = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
			float w1 = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) / denom;
			float w2 = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) / denom;
			float w3 = 1 - w1 - w2;

			return (V1.y * w1 + V2.y * w2 + V3.y * w3) / (w1 + w2 + w3);
		}

		public override string ToString() =>
			"t" + index + " {" + V1 + " -> " + V2 + " -> " + V3 + "} (" + e1 + ", " + e2 + ", " + e3 + ")";

		public bool Equals(Triangle other) => 
			v1.Equals(other.v1) && v2.Equals(other.v2) && v3.Equals(other.v3);

		public override int GetHashCode() =>
			v1.GetHashCode() + v2.GetHashCode() + v3.GetHashCode();
	}
}
