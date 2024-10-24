using System;
using System.IO;
using DavidUtils.ExtensionMethods;
using DavidUtils.Geometry.Bounding_Box;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Procrain.Noise
{
	public static class PerlinNoise
	{
		public static uint GenerateRandomSeed() => (uint)DateTime.Now.Millisecond;

		/// <summary>
		///     Genera un Mapa de Ruido con las caracteristicas dadas en NoiseParams
		/// </summary>
		/// <param name="np">Parametros del ruido</param>
		/// <returns>Array Bidimensional de alturas</returns>
		public static float[] BuildHeightMap(PerlinNoiseParams np) => BuildHeightMap(np, BuildOctaves(np));

		public static float[] BuildHeightMap(PerlinNoiseParams np, PerlinOctaves octaves)
		{
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
				// Almacenamos el Ruido resultante
				noiseMap[x + y * size] = GetNoiseHeight(
					coords,
					np,
					octaves
				);
			}

			return noiseMap;
		}

		/// <summary>
		///     Calcula la Altura de un punto con Ruido de Perlin a partir de unos parametros y unos octavos
		/// </summary>
		/// <param name="point">Punto en el mapa</param>
		/// <param name="np">Parametros del Ruido</param>
		/// <param name="octaves">Parametros de cada octavo (frecuencia, amplitud, offset)</param>
		public static float GetNoiseHeight(float2 point, PerlinNoiseParams np, PerlinOctaves octaves)
		{
			float height = 0;
			for (var i = 0; i < np.NumOctaves; i++)
			{
				float2 offset = octaves.offsets[i];
				float frecuency = octaves.frecuencies[i];
				float amplitude = octaves.amplitudes[i];

				var coords = new float2(
					(point.x + offset.x) / np.Scale * frecuency,
					(point.y + offset.y) / np.Scale * frecuency
				);

				height += (Mathf.PerlinNoise(coords.x, coords.y) * 2 - 1) * amplitude;
			}

			// El Ruido resultante se interpola entre el Maximo y el Minimo
			return Mathf.InverseLerp(-octaves.maxNoiseValue, octaves.maxNoiseValue, height);
		}

		#region OCTAVES

		public struct PerlinOctaves
		{
			private int Count => offsets.Length;
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
		}

		public static PerlinOctaves BuildOctaves(PerlinNoiseParams np)
		{
			var octaves = new PerlinOctaves(np.NumOctaves);

			var rand = new Random(np.Seed);
			var maxOffset = new float2(100000, 100000);

			for (var i = 0; i < np.NumOctaves; i++)
			{
				octaves.offsets[i] = new float2(rand.NextFloat2(-maxOffset, maxOffset));
				octaves.frecuencies[i] = Frequency(np.Lacunarity, i);
				octaves.amplitudes[i] = Amplitude(np.Persistance, i);

				octaves.maxNoiseValue += octaves.amplitudes[i];
			}

			return octaves;
		}

		public static float Frequency(float lacunarity, int octave) => Mathf.Pow(lacunarity, octave);
		public static float Amplitude(float persistance, int octave) => Mathf.Pow(persistance, octave);

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
		public static Vector3[] SampleNoiseInPointsFromFile(PerlinNoiseParams np, string filePath, out AABB_2D aabb)
		{
			// Scale no puede ser negativa
			if (np.Scale <= 0) np.Scale = 0.0001f;

			PerlinOctaves octaves = BuildOctaves(np);

			// Nube de puntos
			Vector3[] points = Array.Empty<Vector3>();
			var index = 0;

			// Leemos el archivo de texto
			string[] lines = File.ReadAllLines(filePath);

			// Puntos del AABB para crear puntos en las esquinas
			aabb = new AABB_2D(Vector2.positiveInfinity, Vector2.negativeInfinity);

			foreach (string line in lines)
			{
				// Header = Tamaño de la Nube de Puntos
				if (!line.Contains(' '))
				{
					// Le añadimos 4 mas por las esquinas
					points = new Vector3[int.Parse(line) + 4];
					continue;
				}

				// Extraemos el punto
				string[] sCoords = line.Split(' ');
				var mapCoords = new Vector2(float.Parse(sCoords[0]), float.Parse(sCoords[1]));

				// Lo añadimos a la Nube con su altura
				if (points.Length > index)
					points[index++] = new Vector3(
						mapCoords.x,
						GetNoiseHeight(
							mapCoords,
							np,
							octaves
						),
						mapCoords.y
					);

				// Pillamos el maximo y el minimo con cada punto para el AABB
				aabb.max.x = Mathf.Max(aabb.max.x, mapCoords.x);
				aabb.max.y = Mathf.Max(aabb.max.y, mapCoords.y);
				aabb.min.x = Mathf.Min(aabb.min.x, mapCoords.x);
				aabb.min.y = Mathf.Min(aabb.min.y, mapCoords.y);
			}

			// Añadimos las ESQUINAS
			Vector3[] corners = GetWorldCorners(aabb, np, octaves);
			foreach (Vector3 corner in corners) points[index++] = corner;

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
		private static Vector3[] GetWorldCorners(AABB_2D aabb, PerlinNoiseParams np, PerlinOctaves octaves) => new[]
		{
			aabb.BL.ToV3xz().WithY(GetNoiseHeight(aabb.BL, np, octaves)),
			aabb.BR.ToV3xz().WithY(GetNoiseHeight(aabb.BR, np, octaves)),
			aabb.TL.ToV3xz().WithY(GetNoiseHeight(aabb.TL, np, octaves)),
			aabb.TR.ToV3xz().WithY(GetNoiseHeight(aabb.TR, np, octaves))
		};

		#endregion
	}


	public static class PerlinNoise_ThreadSafe
	{
		public static void BuildHeightMap(NativeArray<float> map, PerlinNoiseParams_ThreadSafe np) =>
			BuildHeightMap(map, np, BuildOctaves(np));

		public static void BuildHeightMap(
			NativeArray<float> map, PerlinNoiseParams_ThreadSafe np, PerlinOctaves_ThreadSafe octaves
		)
		{
			// Scale no puede ser negativa
			if (np.scale <= 0) np.scale = 0.0001f;

			int size = np.SampleSize;

			float halfSize = size / 2f;
			float2 center = new float2(halfSize, halfSize) + np.offset;

			// Recorremos el mapa en 2D
			for (var y = 0; y < size; y++)
			for (var x = 0; x < size; x++)
			{
				var coords = new float2(x - center.x, y - center.y);
				// Almacenamos el Ruido resultante
				map[x + y * size] = GetNoiseHeight(coords, np, octaves);
			}
		}

		public static float GetNoiseHeight(
			float2 point, PerlinNoiseParams_ThreadSafe np, PerlinOctaves_ThreadSafe octaves
		)
		{
			float height = 0;
			for (var i = 0; i < np.numOctaves; i++)
			{
				float2 offset = octaves.offsets[i];
				float frecuency = octaves.frecuencies[i];
				float amplitude = octaves.amplitudes[i];

				var coords = new float2(
					(point.x + offset.x) / np.scale * frecuency,
					(point.y + offset.y) / np.scale * frecuency
				);

				height += (Mathf.PerlinNoise(coords.x, coords.y) * 2 - 1) * amplitude;
			}

			// El Ruido resultante se interpola entre el Maximo y el Minimo
			return Mathf.InverseLerp(-octaves.maxNoiseValue, octaves.maxNoiseValue, height);
		}

		public static PerlinOctaves_ThreadSafe BuildOctaves(PerlinNoiseParams_ThreadSafe np)
		{
			var octaves = new PerlinOctaves_ThreadSafe(np.numOctaves);

			var rand = new Random(np.seed);
			var maxOffset = new float2(100000, 100000);

			for (var i = 0; i < np.numOctaves; i++)
			{
				octaves.offsets[i] = new float2(rand.NextFloat2(-maxOffset, maxOffset));
				octaves.frecuencies[i] = PerlinNoise.Frequency(np.lacunarity, i);
				octaves.amplitudes[i] = PerlinNoise.Amplitude(np.persistance, i);

				octaves.maxNoiseValue += octaves.amplitudes[i];
			}

			return octaves;
		}

		public struct PerlinOctaves_ThreadSafe
		{
			private int Count => offsets.Length;
			public NativeArray<float2> offsets;
			public NativeArray<float> frecuencies;
			public NativeArray<float> amplitudes;
			public float maxNoiseValue;

			public PerlinOctaves_ThreadSafe(int numOctaves)
			{
				maxNoiseValue = 0;
				offsets = new NativeArray<float2>(numOctaves, Allocator.Temp);
				frecuencies = new NativeArray<float>(numOctaves, Allocator.Temp);
				amplitudes = new NativeArray<float>(numOctaves, Allocator.Temp);
			}
		}
	}
}
