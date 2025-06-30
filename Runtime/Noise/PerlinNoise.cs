using System;
using System.IO;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Procrain.Geometry;
using Procrain.MapParameters;
using Procrain.Utils;
using Unity.Burst;
using Random = Unity.Mathematics.Random;

namespace Procrain.Noise
{
	public static class PerlinNoise
	{
		public static uint GenerateRandomSeed() => (uint)DateTime.Now.Millisecond;
		
		/// <summary>
		/// Build a HeightMap (float[]) from Perlin Noise
		/// </summary>
		/// <param name="np">PerlinNoiseParams</param>
		/// <param name="valuePostProcessor">Post Processing Function (is none -> NULL)</param>
		/// <returns>Flat 1D Map</returns>
		public static float[] BuildHeightMap(PerlinNoiseParams np, Func<float, float> valuePostProcessor = null)
		{
			PerlinOctaves octaves = new(np);
			
			// Scale no puede ser negativa
			if (np.Scale <= 0) np.Scale = 0.0001f;

			int size = np.SampleSize;

			var noiseMap = new float[size * size];

			float halfSize = size / 2f;
			float2 center = new float2(halfSize, halfSize) + np.Offset;

			// Recorremos el mapa en 2D
			for (var y = 0; y < size; y++)
			for (var x = 0; x < size; x++)
			{
				float2 coords = new float2(x, y) - center;
				noiseMap[x + y * size] = GetNoiseHeight(coords, np, octaves);
				
				// POSTPROCESSING
				if (valuePostProcessor != null)
					noiseMap[x + y * size] = valuePostProcessor(noiseMap[x + y * size]);
			}

			return noiseMap;
		}

		/// <summary>
		/// Build a Map of any Type you want using Perlin Noise
		/// </summary>
		/// <param name="np">PerlinNoiseParams</param>
		/// <param name="valuePostProcessor">Post Processing Function</param>
		/// <returns>Flat 1D Map</returns>
		public static T[] BuildMap<T>(PerlinNoiseParams np, Func<float, T> valuePostProcessor)
		{
			PerlinOctaves octaves = new(np);
			
			// Scale no puede ser negativa
			if (np.Scale <= 0) np.Scale = 0.0001f;

			int size = np.SampleSize;

			T[] map = new T[size * size];

			float halfSize = size / 2f;
			float2 center = new float2(halfSize, halfSize) + np.Offset;

			// Recorremos el mapa en 2D
			for (var y = 0; y < size; y++)
			for (var x = 0; x < size; x++)
			{
				float2 coords = new float2(x, y) - center;
				map[x + y * size] = valuePostProcessor(GetNoiseHeight(coords, np, octaves));
			}

			return map;
		}

		/// <summary>
		///     Calcula la Altura de un punto con Ruido de Perlin a partir de unos parametros y unos octavos
		/// </summary>
		/// <param name="point">Punto en el mapa</param>
		/// <param name="np">Parametros del Ruido</param>
		/// <param name="octaves">Parametros de cada octavo (frecuencia, amplitud, offset)</param>
		public static float GetNoiseHeight(float2 point, float scale, PerlinOctaves octaves)
		{
			float height = 0;
			for (var i = 0; i < octaves.Count; i++)
			{
				float2 offset = octaves.offsets[i];
				float frecuency = octaves.frecuencies[i];
				float amplitude = octaves.amplitudes[i];

				float2 coords = new(
					(point.x + offset.x) / scale * frecuency,
					(point.y + offset.y) / scale * frecuency
				);
				
				height += (Mathf.PerlinNoise(coords.x, coords.y) * 2 - 1) * amplitude;
			}

			// El Ruido resultante se interpola entre el Maximo y el Minimo
			return Mathf.InverseLerp(-octaves.maxNoiseValue, octaves.maxNoiseValue, height);
		}

		public static float GetNoiseHeight(float2 point, PerlinNoiseParams np, PerlinOctaves octaves) =>
			GetNoiseHeight(point, np.Scale, octaves);

		#region OCTAVES

		public struct PerlinOctaves
		{
			public int Count => offsets.Length;
			public float2[] offsets;
			public float[] frecuencies;
			public float[] amplitudes;
			public float maxNoiseValue;

			public PerlinOctaves(int numOctaves)
			{
				maxNoiseValue = 0;
				offsets = new float2[numOctaves];
				frecuencies = new float[numOctaves];
				amplitudes = new float[numOctaves];
			}
			
			public PerlinOctaves(PerlinNoiseParams np) : this(np.NumOctaves)
			{
				Random rand = new(np.Seed);
				float2 maxOffset = new(100000, 100000);

				for (var i = 0; i < np.NumOctaves; i++)
				{
					offsets[i] = new float2(rand.NextFloat2(-maxOffset, maxOffset));
					frecuencies[i] = Frequency(np.Lacunarity, i);
					amplitudes[i] = Amplitude(np.Persistance, i);

					maxNoiseValue += amplitudes[i];
				}
			}
		}

		public static float Frequency(float lacunarity, int octave) => math.pow(lacunarity, octave);
		public static float Amplitude(float persistance, int octave) => math.pow(persistance, octave);

		#endregion

		#region POINT CLOUDS

		/// <summary>
		///     Muestrea una Nube de Puntos de un Archivo
		///     Le añade una altura basada en la funcion de Ruido de Perlin
		///     Con los parametros que le pasemos
		/// </summary>
		/// <param name="np">Parametros del Ruido</param>
		/// <param name="filePath">Nombre del Archivo con la Nube de Puntos</param>
		/// <param name="aabb"></param>
		/// <returns>Nube de Puntos con Alturas segun el Ruido de Perlin</returns>
		public static float3[] SampleNoiseInPointsFromFile(PerlinNoiseParams np, string filePath, out AABB_2D aabb)
		{
			// Scale no puede ser negativa
			if (np.Scale <= 0) np.Scale = 0.0001f;

			PerlinOctaves octaves = new(np);

			// Nube de puntos
			float3[] points = Array.Empty<float3>();
			var index = 0;

			// Leemos el archivo de texto
			string[] lines = File.ReadAllLines(filePath);

			// Puntos del AABB para crear puntos en las esquinas
			aabb = new AABB_2D(new float2(math.INFINITY), -new float2(math.INFINITY));

			foreach (string line in lines)
			{
				// Header = Tamaño de la Nube de Puntos
				if (!line.Contains(' '))
				{
					// Le añadimos 4 mas por las esquinas
					points = new float3[int.Parse(line) + 4];
					continue;
				}

				// Extraemos el punto
				string[] sCoords = line.Split(' ');
				float2 mapCoords = new(float.Parse(sCoords[0]), float.Parse(sCoords[1]));

				// Lo añadimos a la Nube con su altura
				if (points.Length > index)
					points[index++] = new float3(
						mapCoords.x,
						GetNoiseHeight(mapCoords, np, octaves),
						mapCoords.y
					);

				// Pillamos el maximo y el minimo con cada punto para el AABB
				aabb.max.x = Mathf.Max(aabb.max.x, mapCoords.x);
				aabb.max.y = Mathf.Max(aabb.max.y, mapCoords.y);
				aabb.min.x = Mathf.Min(aabb.min.x, mapCoords.x);
				aabb.min.y = Mathf.Min(aabb.min.y, mapCoords.y);
			}

			// Añadimos las ESQUINAS
			float3[] corners = GetWorldCorners(aabb, np, octaves);
			foreach (float3 corner in corners) points[index++] = corner;

			return points;
		}


		/// <summary>
		///     Genera las esquinas de un espacio bidimensional definido por un AABB (maxpoint, minpoint)
		///     con la altura correspondiente en el Ruido de Perlin
		/// </summary>
		/// <param name="aabb"></param>
		/// <param name="np">Parametros del Ruido</param>
		/// <param name="octaveOffsets">Offsets de cada octavo</param>
		/// <param name="maxNoiseValue">Valor maximo de ruido posible</param>
		/// <returns>Array con las Esquinas {BOT LEFT, BOT RIGHT, TOP LEFT, TOP RIGHT}</returns>
		private static float3[] GetWorldCorners(AABB_2D aabb, PerlinNoiseParams np, PerlinOctaves octaves) => new[]
		{
			aabb.BL.ToV3XZ().WithY(GetNoiseHeight(aabb.BL, np, octaves)),
			aabb.BR.ToV3XZ().WithY(GetNoiseHeight(aabb.BR, np, octaves)),
			aabb.TL.ToV3XZ().WithY(GetNoiseHeight(aabb.TL, np, octaves)),
			aabb.TR.ToV3XZ().WithY(GetNoiseHeight(aabb.TR, np, octaves))
		};

		#endregion
	}


	public static class PerlinNoise_ThreadSafe
	{
		[BurstCompile]
		public static void BuildHeightMap(NativeArray<float> map, PerlinNoiseParams_ThreadSafe np) =>
			BuildHeightMap(map, np, new PerlinOctaves_ThreadSafe(np));

		[BurstCompile]
		public static void BuildHeightMap(
			NativeArray<float> map, PerlinNoiseParams_ThreadSafe np, PerlinOctaves_ThreadSafe octaves
		)
		{
			// Scale no puede ser negativa
			float scale = np.scale > 0 ? np.scale : 0.0001f;

			int size = np.SampleSize;

			float halfSize = size / 2f;
			float2 center = new float2(halfSize, halfSize) + np.offset;

			// Recorremos el mapa en 2D
			for (var y = 0; y < size; y++)
			for (var x = 0; x < size; x++)
			{
				float2 coords = new(x - center.x, y - center.y);
				
				float noiseHeight = GetNoiseHeight(coords, scale, octaves);
				
				if (np.heightCurve.IsEmpty)
					map[x + y * size] = noiseHeight;
				else
					map[x + y * size] = np.heightCurve.Evaluate(noiseHeight);
			}
		}

		[BurstCompile]
		public static float GetNoiseHeight(
			float2 point, float scale, PerlinOctaves_ThreadSafe octaves
		)
		{
			float height = 0;
			for (var i = 0; i < octaves.Count; i++)
			{
				float2 offset = octaves.offsets[i];
				float frecuency = octaves.frecuencies[i];
				float amplitude = octaves.amplitudes[i];

				float2 coords = new(
					(point.x + offset.x) / scale * frecuency,
					(point.y + offset.y) / scale * frecuency
				);

				height += (noise.cnoise(coords) * 2 - 1) * amplitude;
			}

			// El Ruido resultante se interpola entre el Maximo y el Minimo
			return math.unlerp(-octaves.maxNoiseValue, octaves.maxNoiseValue, height);
		}

		#region OCTAVES
		
		public readonly struct PerlinOctaves_ThreadSafe
		{
			public readonly NativeArray<float2> offsets;
			public readonly NativeArray<float> frecuencies;
			public readonly NativeArray<float> amplitudes;
			public readonly float maxNoiseValue;
			
			public int Count => offsets.Length;

			public PerlinOctaves_ThreadSafe(int numOctaves)
			{
				maxNoiseValue = 0;
				offsets = new NativeArray<float2>(numOctaves, Allocator.Temp);
				frecuencies = new NativeArray<float>(numOctaves, Allocator.Temp);
				amplitudes = new NativeArray<float>(numOctaves, Allocator.Temp);
			}
			
			public PerlinOctaves_ThreadSafe(PerlinNoiseParams_ThreadSafe np) : this(np.numOctaves)
			{
				Random rand = new(np.seed);
				float2 maxOffset = new(100000, 100000);

				for (var i = 0; i < np.numOctaves; i++)
				{
					offsets[i] = new float2(rand.NextFloat2(-maxOffset, maxOffset));
					frecuencies[i] = Frequency(np.lacunarity, i);
					amplitudes[i] = Amplitude(np.persistance, i);

					maxNoiseValue += amplitudes[i];
				}
			}

			public PerlinOctaves_ThreadSafe(PerlinNoise.PerlinOctaves octaves)
				: this(octaves.offsets.Length)
			{
				offsets.CopyFrom(octaves.offsets);
				frecuencies.CopyFrom(octaves.frecuencies);
				amplitudes.CopyFrom(octaves.amplitudes);
				maxNoiseValue = octaves.maxNoiseValue;
			}
			
			[BurstCompile]
			public static float Frequency(float lacunarity, int octave) => math.pow(lacunarity, octave);
		
			[BurstCompile]
			public static float Amplitude(float persistance, int octave) => math.pow(persistance, octave);

		}

		#endregion
	}
}
