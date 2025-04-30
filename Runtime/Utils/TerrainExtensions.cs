using System;
using System.Linq;
using UnityEngine;

namespace Procrain.Utils
{
    public static class TerrainExtensions
    {
        
		#region TEXTURE

		public static Texture2D ToTexture(
			this Terrain terrain,
			int texWidth,
			int texHeight,
			Gradient heightGradient
		)
		{
			// Heightmap to Texture
			Texture2D texture = new Texture2D(texWidth, texHeight);

			terrain.GetMinMaxHeight(out float minHeight, out float maxHeight);

			for (var y = 0; y < texHeight; y++)
			for (var x = 0; x < texWidth; x++)
			{
				float height = terrain.terrainData.GetInterpolatedHeight(
					x / (float)texWidth,
					y / (float)texHeight
				);
				float heightNormalized = Mathf.InverseLerp(minHeight, maxHeight, height);

				texture.SetPixel(x, y, heightGradient.Evaluate(heightNormalized));
			}

			texture.Apply();

			return texture;
		}

		#endregion

		#region BOUNDS

		public static Bounds GetBounds(this Terrain terrain)
		{
			Vector3 terrainPos = terrain.GetPosition();
			Vector3 size = terrain.terrainData.size;

			return new Bounds(terrainPos + size / 2, size);
		}

		public static bool OutOfBounds(this Terrain terrain, Vector2 pos) => !terrain.GetBounds().Contains(pos);

		#endregion

		#region SAMPLE_VERTICES

		// Vertice más cercano del terreno a la posición dada (0 altura)
		public static Vector2 GetNearestVertex(this Terrain terrain, Vector2 normalizedPos)
		{
			Vector2Int cornerIndex = Vector2Int.FloorToInt(
				terrain.terrainData.heightmapResolution * normalizedPos
			);
			float cellSize = terrain.terrainData.heightmapScale.x;

			return cellSize * new Vector2(cornerIndex.x, cornerIndex.y);
		}

		public static Vector3 GetNearestVertexByWorldPos(this Terrain terrain, Vector3 worldPos) =>
			terrain.GetNearestVertex(terrain.GetNormalizedPosition(worldPos));

		#endregion

		#region COORDS_TRANSFORMATIONS

		// [0,1] => [0, TerrainWidth] & [0, TerrainHeight]
		public static Vector3 GetWorldPosition(this Terrain terrain, Vector2 normalizedPos) =>
			new(
				normalizedPos.x * terrain.terrainData.size.x,
				terrain.GetInterpolatedHeight(normalizedPos),
				normalizedPos.y * terrain.terrainData.size.z
			);

		public static Vector2 GetNormalizedPosition(this Terrain terrain, Vector3 worldPos)
		{
			TerrainData terrainData = terrain.terrainData;
			return new Vector2(worldPos.x / terrainData.size.x, worldPos.z / terrainData.size.z);
		}

		#endregion

		#region HEIGHT

		public static float GetInterpolatedHeight(this Terrain terrain, Vector2 normalizedPos) =>
			terrain.terrainData.GetInterpolatedHeight(normalizedPos.x, normalizedPos.y);

		public static float GetInterpolatedHeight(this Terrain terrain, Vector3 worldPos) =>
			terrain.GetInterpolatedHeight(terrain.GetNormalizedPosition(worldPos));

		// Max & Min Height in HeightMap float[,]
		public static void GetMinMaxHeight(
			this Terrain terrain,
			out float minHeight,
			out float maxHeight
		)
		{
			TerrainData terrainData;
			int terrainRes = terrain.terrainData.heightmapResolution;
			float[,] heightMap = (terrainData = terrain.terrainData).GetHeights(
				0,
				0,
				terrainRes,
				terrainRes
			);

			minHeight = terrainData.heightmapScale.y;
			maxHeight = 0;

			foreach (float height in heightMap)
			{
				minHeight = Mathf.Min(height * terrain.terrainData.heightmapScale.y, minHeight);
				maxHeight = Mathf.Max(height * terrain.terrainData.heightmapScale.y, maxHeight);
			}
		}

		#endregion

		#region NORMAL

		public static Vector3 GetNormal(this Terrain terrain, Vector2 normPoint) =>
			terrain.terrainData.GetInterpolatedNormal(normPoint.x, normPoint.y);

		public static Vector3 GetNormal(this Terrain terrain, Vector3 worldPoint) =>
			terrain.GetNormal(terrain.GetNormalizedPosition(worldPoint));

		public static float GetSlopeAngle(this Terrain terrain, Vector3 worldPos)
		{
			Vector2 normalizedPos = terrain.GetNormalizedPosition(worldPos);
			return terrain.GetSlopeAngle(normalizedPos);
		}

		public static float GetSlopeAngle(this Terrain terrain, Vector2 normalizedPos) =>
			// Terrain Normal => Slope Angle
			Vector3.Angle(
				Vector3.up,
				terrain.terrainData.GetInterpolatedNormal(normalizedPos.x, normalizedPos.y)
			);

		#endregion

		#region MESH

		// PROJECT MESH in Terrain
		public static Mesh ProjectMeshInTerrain(
			this Terrain terrain,
			Mesh mesh,
			Transform meshTransform,
			float offset
		)
		{
			Vector3[] vertices = mesh.vertices;

			for (var i = 0; i < vertices.Length; i++)
			{
				Vector3 localPos = vertices[i];
				Vector3 worldPos = meshTransform.TransformPoint(localPos);
				worldPos.y = terrain.SampleHeight(worldPos) + offset;
				vertices[i] = meshTransform.InverseTransformPoint(worldPos);
			}

			mesh.vertices = vertices;

			mesh.RecalculateBounds();
			mesh.RecalculateNormals();

			return mesh;
		}

		// Convierte el Terreno a una Mesh con la mayor resolucion
		public static void GetMesh(
			this Terrain terrain,
			out Vector3[] vertices,
			out int[] triangles,
			float heightOffset
		)
		{
			TerrainData terrainData = terrain.terrainData;
			float[,] heightMap = terrain.terrainData.GetHeights(
				0,
				0,
				terrainData.heightmapResolution,
				terrainData.heightmapResolution
			);

			float cellSize = terrainData.heightmapScale.x;
			int sideCellCount = heightMap.GetLength(0) - 1;
			int sideVerticesCount = heightMap.GetLength(0);

			vertices = new Vector3[sideVerticesCount * sideVerticesCount];
			triangles = new int[sideCellCount * sideCellCount * 6];

			for (var y = 0; y < sideVerticesCount; y++)
			for (var x = 0; x < sideVerticesCount; x++)
			{
				Vector3 vertex =
					new(
						x * cellSize,
						heightMap[y, x] * terrain.terrainData.heightmapScale.y + heightOffset,
						y * cellSize
					);

				int vertexIndex = x + y * sideVerticesCount;

				vertices[vertexIndex] = vertex;

				if (x >= sideCellCount || y >= sideCellCount) continue;

				// Triangles
				int triangleIndex = (x + y * sideCellCount) * 6;
				triangles[triangleIndex + 0] = vertexIndex + 0;
				triangles[triangleIndex + 1] = vertexIndex + sideVerticesCount + 0;
				triangles[triangleIndex + 2] = vertexIndex + sideVerticesCount + 1;

				triangles[triangleIndex + 3] = vertexIndex + 0;
				triangles[triangleIndex + 4] = vertexIndex + sideVerticesCount + 1;
				triangles[triangleIndex + 5] = vertexIndex + 1;
			}
		}

		// Crea el Patch en la posicion central dada
		public static void GetMeshPatch(
			this Terrain terrain,
			out Vector3[] vertices,
			out int[] triangles,
			float heightOffset,
			Vector2 center,
			float size
		)
		{
			// Vertice más cercano al centro para ajustar la malla al terreno
			Vector2 normalizedCenter = terrain.GetNormalizedPosition(
				new Vector3(center.x, 0, center.y)
			);
			Vector2 nearestTerrainVertex = terrain.GetNearestVertex(normalizedCenter);

			// Vector [centro -> vertice más cercano]
			Vector2 displacementToNearestVertex = nearestTerrainVertex - center;

			// Bounding Box del parche del terreno
			Vector2 minBound = center - Vector2.one * size / 2,
				maxBound = center + Vector2.one * size / 2;

			Vector2 normalizedMinBound = terrain.GetNormalizedPosition(
					new Vector3(minBound.x, 0, minBound.y)
				),
				normalizedMaxBound = terrain.GetNormalizedPosition(
					new Vector3(maxBound.x, 0, maxBound.y)
				);

			normalizedMinBound.x = normalizedMinBound.x < 0.001f ? 0.001f : normalizedMinBound.x;
			normalizedMinBound.y = normalizedMinBound.y < 0.001f ? 0.001f : normalizedMinBound.y;
			normalizedMaxBound.x = normalizedMaxBound.x > 0.999f ? 0.999f : normalizedMaxBound.x;
			normalizedMaxBound.y = normalizedMaxBound.y > 0.999f ? 0.999f : normalizedMaxBound.y;

			// Se genera a partir de la Bounding Box
			terrain.GetMeshPatch(
				out vertices,
				out triangles,
				heightOffset,
				normalizedMinBound,
				normalizedMaxBound,
				displacementToNearestVertex
			);
		}

		// Crea el Patch con la Bounding Box dada
		public static void GetMeshPatch(
			this Terrain terrain,
			out Vector3[] vertices,
			out int[] triangles,
			float heightOffset,
			Vector2 minBound,
			Vector2 maxBound,
			Vector2 displacement
		)
		{
			TerrainData terrainData;

			// Calculamos los indices del mapa de altura dentro de la Bounding Box
			int heightMapRes = terrain.terrainData.heightmapResolution;

			Vector2 baseIndex = heightMapRes * minBound;
			Vector2 count = heightMapRes * (maxBound - minBound);

			int xBase = Mathf.FloorToInt(baseIndex.x),
				yBase = Mathf.FloorToInt(baseIndex.y),
				xSize = Mathf.FloorToInt(count.x),
				ySize = Mathf.FloorToInt(count.y);

			// Mapa de Altura dentro de la Bounding Box
			float[,] heightMap = (terrainData = terrain.terrainData).GetHeights(
				xBase,
				yBase,
				xSize,
				ySize
			);

			// Dimensiones de la malla
			float cellSize = terrainData.heightmapScale.x;
			Vector2Int sideCellCount = new Vector2Int(
				heightMap.GetLength(1) - 1,
				heightMap.GetLength(0) - 1
			);
			Vector2Int sideVerticesCount = sideCellCount + Vector2Int.one;
			Vector2 sideSize = cellSize * new Vector2(sideCellCount.x, sideCellCount.y);

			vertices = new Vector3[sideVerticesCount.x * sideVerticesCount.y];
			triangles = new int[sideCellCount.x * sideCellCount.y * 6];

			for (var y = 0; y < sideVerticesCount.y; y++)
			for (var x = 0; x < sideVerticesCount.x; x++)
			{
				Vector3 vertex =
					new(
						x * cellSize,
						heightMap[y, x] * terrain.terrainData.heightmapScale.y + heightOffset,
						y * cellSize
					);

				// Center mesh
				// TODO No se centra bien en algunas posiciones
				vertex +=
					new Vector3(displacement.x, 0, displacement.y)
					- new Vector3(sideSize.x, 0, sideSize.y) / 2
					- new Vector3(cellSize / 2, 0, cellSize / 2);

				int vertexIndex = x + y * sideVerticesCount.x;

				vertices[vertexIndex] = vertex;

				if (x >= sideCellCount.x || y >= sideCellCount.y) continue;

				// Triangles
				int triangleIndex = (x + y * sideCellCount.x) * 6;
				triangles[triangleIndex + 0] = vertexIndex + 0;
				triangles[triangleIndex + 1] = vertexIndex + sideVerticesCount.x + 0;
				triangles[triangleIndex + 2] = vertexIndex + sideVerticesCount.x + 1;

				triangles[triangleIndex + 3] = vertexIndex + 0;
				triangles[triangleIndex + 4] = vertexIndex + sideVerticesCount.x + 1;
				triangles[triangleIndex + 5] = vertexIndex + 1;
			}
		}

		#endregion

		#region PROJECTION

		public static Vector3 Project(this Terrain terrain, Vector3 point, float offset = .1f) =>
			new(point.x, terrain.SampleHeight(point) + offset, point.z);

		public static Vector3[] ProjectPathToTerrain(
			this Terrain terrain, Vector3[] pathCheckpoints, bool loop = false, float offset = 0
		)
		{
			if (pathCheckpoints == null || pathCheckpoints.Length == 0) return Array.Empty<Vector3>();

			// Segment -> Path: each point sampled to Terrain Heights by Terrain Resolution
			return pathCheckpoints.SkipLast(1)
				.Select((p, i) =>
					terrain.ProjectSegmentToTerrain(p, pathCheckpoints[i + 1], -1, offset).SkipLast(1))
				.Append(loop // Add Last segment to close it or last point if no loop
					? terrain.ProjectSegmentToTerrain(pathCheckpoints[^1], pathCheckpoints[0], -1, offset) 
					: new []{pathCheckpoints[^1]})
				.SelectMany(p => p) // Flatten paths to 1 Path
				.ToArray();
		}

		public static Vector3[] ProjectSegmentToTerrain(
			this Terrain terrain,
			Vector3 a,
			Vector3 b,
			float resolution = -1,
			float offset = 0
		)
		{
			float distance = Vector3.Distance(a, b);

			// Resolucion == -1 => Resolucion no especificada => Usa Resolucion del terreno
			if (resolution < 0) resolution = terrain.terrainData.heightmapScale.x;

			// Si el segmento es más corto, no hace falta samplearlo
			if (resolution > distance) return new[] { a, b };

			// Se samplea a la resolucion del terreno
			int numSamples = Mathf.FloorToInt(distance / resolution);

			
			return new Vector3[numSamples] // Fill array Sampling a Lerped point between A and B
				.Select((_, i) => terrain.Project(Vector3.Lerp(a, b, (float)i / numSamples), offset))
				.ToArray();
		}

		#endregion
    }
}
