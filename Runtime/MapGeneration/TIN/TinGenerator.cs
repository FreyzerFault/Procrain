using System;
using DavidUtils.Geometry;
using Procrain.Geometry;
using Procrain.MapGeneration.Mesh;
using UnityEngine;

namespace Procrain.MapGeneration.TIN
{
	public static class TinGenerator
	{
		/// <summary>
		///     Genera un TIN a partir de un HeightMap
		/// </summary>
		/// <param name="heightMap">Mapa de Alturas</param>
		/// <param name="aabb2D">Bounding Box</param>
		/// <param name="heigthScale">[0,1] -> [0, heightScale]</param>
		/// <param name="errorTolerance">Error Maximo Tolerado por el Tin</param>
		/// <param name="maxIterations">Limite de iteraciones maximas del Tin</param>
		/// <returns>Datos de una Malla que va a usar Unity</returns>
		private static Tin BuildTin(
			HeightMap heightMap,
			float heigthScale = 1,
			float errorTolerance = 1,
			int maxIterations = 10,
			AABB2D aabb2D = null
		)
		{
			// Creacion del Tin (Estructura topologica interna)
			var tin = new Tin(
				heightMap.map,
				heightMap.Size,
				errorTolerance,
				heigthScale,
				maxIterations,
				aabb2D
			);
			tin.InitGeometry(heightMap.map, heightMap.Size);
			tin.AddPointLoop();

			// Creacion de la Malla
			return tin;
		}

		/// <summary>
		///     Genera un TIN a partir de unos puntos iniciales (las esquinas formadas por el AABB)
		/// </summary>
		/// <param name="aabb2D">Bounding Box</param>
		/// <param name="errorTolerance">Error Maximo Tolerado por el Tin</param>
		/// <param name="heightScale"></param>
		/// <param name="maxIterations">Limite de iteraciones maximas del Tin</param>
		/// <param name="points">Puntos extras pregenerados</param>
		/// <returns>Datos de una Malla que va a usar Unity</returns>
		private static Tin BuildTin(
			AABB2D aabb2D,
			float errorTolerance = 1,
			float heightScale = 100,
			int maxIterations = 10,
			Vector3[] points = null
		)
		{
			// Creacion del Tin (Estructura topologica interna)
			var tin = new Tin(
				points ?? Array.Empty<Vector3>(),
				errorTolerance,
				heightScale,
				maxIterations,
				aabb2D
			);
			tin.InitGeometry();
			tin.AddPointLoop();

			// Creacion de la Malla
			return tin;
		}

		/// <summary>
		///     Generacion de la Malla a partir de un Tin
		/// </summary>
		/// <param name="tin"></param>
		/// <returns>Datos de una Malla que va a usar en Unity</returns>
		public static MeshDataDynamic TinToMesh(Tin tin)
		{
			// Creacion de la malla (Datos basicos que necesita Unity)
			var data = new MeshDataDynamic();

			for (var i = 0; i < tin.triangles.Count; i++)
			{
				for (var v = 0; v < 3; v++)
				{
					Vector3 vertex = tin.triangles[i].Vertices[v];
					data.AddVertex(vertex);
					data.AddUV(vertex.x / tin.size, vertex.z / tin.size);
					data.AddTriIndex(i * 3 + v);
				}
			}
			return data;
		}

		public static IMeshData BuildTinMeshData(
			out Tin tin, HeightMap heightMap, float errorTolerance = 1,
			float heightScale = 100,
			int maxIterations = 10
		)
		{
			tin = BuildTin(heightMap, heightScale, errorTolerance, maxIterations);
			return TinToMesh(tin);
		}
	}
}
