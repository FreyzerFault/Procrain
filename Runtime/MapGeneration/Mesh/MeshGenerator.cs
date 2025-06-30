using Procrain.MapGeneration;
using Procrain.MapParameters;
using Procrain.Noise;
using Procrain.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Octaves = Procrain.Noise.PerlinNoise.PerlinOctaves;

namespace Procrain.Geometry.Mesh
{
	public static class MeshGenerator
	{
		public static IMeshData BuildMeshData(
			IHeightMap map,
			ITerrainParams terrainParams,
			Gradient gradient = null
		)
		{
			int lod = terrainParams.LOD;
			float heightScale = terrainParams.HeightScale;
			
			// La malla la creamos centrada en 0:
			float initCoord = (map.Size - 1) / -2f;

			// El LOD NECESITA ser múltiplo de la anchura para que sea simétrico
			while (lod != 0 && (map.Size - 1) % lod != 0)
				lod += 1;

			// Incremento entre vertices para asegurar el LOD
			int simplificationIncrement = lod == 0 ? 1 : lod * 2;
			int verticesPerLine = (map.Size - 1) / simplificationIncrement + 1;

			MeshDataStatic data = new(verticesPerLine, verticesPerLine, lod);

			var vertIndex = 0;
			for (var y = 0; y < map.Size; y += simplificationIncrement)
			for (var x = 0; x < map.Size; x += simplificationIncrement)
			{
				float heightValue = map.GetHeight(x, y);
				
				float3 pos = new(initCoord + x, heightValue * heightScale, initCoord + y);
				float2 uv = new((float)x / map.Size, (float)y / map.Size);
				Color color = gradient?.Evaluate(heightValue) ?? Color.white;
				
				data.AddVertex(pos, uv, color);
				
				// Ignorando la ultima fila y columna de vertices, añadimos los triangulos
				if (x < map.Size - 1 && y < map.Size - 1)
				{
					int indexBL = vertIndex;
					int indexTL = vertIndex + verticesPerLine;
					int indexTR = vertIndex + verticesPerLine + 1;
					int indexBR = vertIndex + 1;
					
					data.AddTriangle(indexBL, indexTL, indexTR);
					data.AddTriangle(indexBL, indexTR, indexBR);
				}

				vertIndex++;
			}

			return data;
		}

		/// <summary>
		/// Genera la Mesh Data de cero.
		/// Optimiza el proceso general, calculando la altura solo en los vertices,
		/// quitando la necesidad de pregenerar el Mapa de Alturas
		/// </summary>
		public static IMeshData BuildMeshData(
			IHeightMapParams noiseParams,
			ITerrainParams terrainParams,
			Gradient gradient = null
		)
		{
			if (noiseParams is not PerlinNoiseParams_ThreadSafe np_ts)
				np_ts = ((PerlinNoiseParams)noiseParams).ToThreadSafe();
			
			#region PERLIN NOISE BASIC PARAMS

			PerlinNoise_ThreadSafe.PerlinOctaves_ThreadSafe octaves = new(np_ts);
			
			// Scale no puede ser negativa
			float scale = np_ts.scale > 0 ? np_ts.scale : 0.0001f;

			int size = np_ts.SampleSize;
			float heightScale = terrainParams.HeightScale;

			float halfSize = size / 2f;
			float2 center = new float2(halfSize, halfSize) + np_ts.Offset;

			#endregion

			#region MESH INITIALIZATION

			int lod = terrainParams.LOD;
				
			// La malla la creamos centrada en 0:
			float initCoord = (size - 1) / -2f;

			// El LOD NECESITA ser múltiplo de la anchura para que sea simétrico
			while (lod != 0 && (size - 1) % lod != 0)
				lod += 1;

			// Incremento entre vertices para asegurar el LOD
			int simplificationIncrement = lod == 0 ? 1 : lod * 2;
			int verticesPerLine = (size - 1) / simplificationIncrement + 1;

			MeshDataStatic data = new(verticesPerLine, verticesPerLine, lod);

			#endregion
			
			var vertIndex = 0;
			for (var y = 0; y < size; y += simplificationIncrement)
			for (var x = 0; x < size; x += simplificationIncrement)
			{
				#region NOISE VALUE

				float2 coords = new float2(x, y) - center;
				
				float heightValue = PerlinNoise_ThreadSafe.GetNoiseHeight(coords, scale, octaves);
				
				if (!np_ts.heightCurve.IsEmpty) 
					heightValue = np_ts.heightCurve.Evaluate(heightValue);

				#endregion


				#region MESH DATA

				float3 pos = new(initCoord + x, heightValue * heightScale, initCoord + y);
				float2 uv = new((float)x / size, (float)y / size);
				Color color = gradient?.Evaluate(heightValue) ?? Color.white;

				data.AddVertex(pos, uv, color);
				
				// Ignorando la ultima fila y columna de vertices, añadimos los triangulos
				if (x < size - 1 && y < size - 1)
				{
					int indexBL = vertIndex;
					int indexTL = vertIndex + verticesPerLine;
					int indexTR = vertIndex + verticesPerLine + 1;
					int indexBR = vertIndex + 1;
					
					data.AddTriangle(indexBL, indexTL, indexTR);
					data.AddTriangle(indexBL, indexTR, indexBR);
				}
				
				vertIndex++;

				#endregion
			}

			return data;
		}
	}

	public static class MeshGeneratorThreadSafe
	{
		[BurstCompile]
		public struct GenerateMeshDataJob : IJob
		{
			// Puede generarse calculando el ruido SOLO en los puntos apropiados
			// o consultando valores de altura de un HeightMap pregenerado para optimizarlo
			// 
			[ReadOnly] public PerlinNoiseParams_ThreadSafe noiseParams;
			[ReadOnly] public HeightMap_ThreadSafe prebuiltHeightMap;
			
			[ReadOnly] public TerrainParams_ThreadSafe terrainParams;
			[ReadOnly] public Gradient_ThreadSafe gradient;
			
			[WriteOnly] public MeshData_ThreadSafe meshData;

			public void Execute()
			{
				PerlinNoise_ThreadSafe.PerlinOctaves_ThreadSafe octaves = new(noiseParams);
				
				int size = prebuiltHeightMap.IsEmpty ? noiseParams.size : prebuiltHeightMap.size;
				float heightScale = terrainParams.heightScale;
				int lod = terrainParams.lod;
			
				// La malla la creamos centrada en 0:
				var initCoord = (int)math.floor((size - 1) / -2f);

				// Incremento entre vertices para asegurar el LOD
				int simplificationIncrement = GetVertexSpace(size, lod);
				int verticesPerLine = (size - 1) / simplificationIncrement + 1;

				meshData = new MeshData_ThreadSafe(
					verticesPerLine * verticesPerLine,
					(verticesPerLine - 1) * 2,
					lod
				);
				
				// VERTICES
				for (var y = 0; y < size; y += simplificationIncrement)
				for (var x = 0; x < size; x += simplificationIncrement)
				{
					int2 coord = new(initCoord + x, initCoord + y);
					
					// Se samplea calculando con Perlin Noise o consultando el mapa pregenerado 
					float heightValue = prebuiltHeightMap.IsEmpty 
						? PerlinNoise_ThreadSafe.GetNoiseHeight(coord, noiseParams.scale, octaves)
						:prebuiltHeightMap.GetHeight(coord);
					
					// Se aplica la curva de altura y se upscalea con la HeightScale
					heightValue = noiseParams.heightCurve.Evaluate(heightValue) * heightScale;

					float3 vertexPos = new(coord.x, heightValue, coord.y);
					float2 uv = new((float)coord.x / size, (float)coord.y / size);
					
					meshData.AddVertex(vertexPos, uv, GetColor(heightValue));
				}
				
				AddTrianglesAsRectGrid(meshData, new int2(verticesPerLine, verticesPerLine));
			}

			private static readonly Color32 Black = new(0, 0, 0, 1);
			private static readonly Color32 White = new(1, 1, 1, 1);
			
			[BurstCompile]
			private Color32 GetColor(float value) =>
				gradient.IsEmpty ? Color32.Lerp(Black, White, value) : gradient.Evaluate(value);
		}

		/// Incremento entre vertices para asegurar el LOD (LOD 0, 1, 2, 3,...)
		[BurstCompile]
		private static int GetVertexSpace(int size, int lod)
		{
			// El LOD NECESITA ser múltiplo de la anchura para que sea simétrico
			while (lod != 0 && (size - 1) % lod != 0)
				lod += 1;

			return lod == 0 ? 1 : lod * 2;
		}
		
		[BurstCompile]
		public static void AddTrianglesAsRectGrid(MeshData_ThreadSafe meshData, int2 size)
		{
			for (var y = 0; y < size.y; y++)
			for (var x = 0; x < size.x; x++)
			{
				// Ignore right and top border
				if (x != size.x - 1 || y != size.y - 1) continue;
					
				// Bot-Left, Bot-Right, Top-Left, Top-Right
				int indexBL = x + y * size.x;
				int indexTL = indexBL + size.x;
				int indexTR = indexBL + size.x + 1;
				int indexBR = indexBL + 1;
					
				meshData.AddTriangle(indexBL, indexTL, indexTR);
				meshData.AddTriangle(indexBL, indexTR, indexBR);
			}
		}
	}
}
