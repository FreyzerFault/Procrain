using System;
using System.IO;
using Procrain.Geometry;
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
            if (np.scale <= 0) np.scale = 0.0001f;

            var size = np.SampleSize;

            var noiseMap = new float[size * size];

            var halfSize = size / 2f;
            var center = new float2(halfSize, halfSize) + np.offset;

            // Recorremos el mapa en 2D
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var coords = new float2(x, y) - center;
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
            for (var i = 0; i < np.numOctaves; i++)
            {
                var offset = octaves.offsets[i];
                var frecuency = octaves.frecuencies[i];
                var amplitude = octaves.amplitudes[i];

                var coords = new float2(
                    (point.x + offset.x) / np.scale * frecuency,
                    (point.y + offset.y) / np.scale * frecuency
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
            var octaves = new PerlinOctaves(np.numOctaves);

            var rand = new Random(np.seed);
            var maxOffset = new float2(100000, 100000);

            for (var i = 0; i < np.numOctaves; i++)
            {
                octaves.offsets[i] = new float2(rand.NextFloat2(-maxOffset, maxOffset));
                octaves.frecuencies[i] = Frequency(np.lacunarity, i);
                octaves.amplitudes[i] = Amplitude(np.persistance, i);

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
        public static Vector3[] SampleNoiseInPointsFromFile(PerlinNoiseParams np, string filePath, out AABB aabb)
        {
            // Scale no puede ser negativa
            if (np.scale <= 0) np.scale = 0.0001f;

            var octaves = BuildOctaves(np);

            // Nube de puntos
            var points = Array.Empty<Vector3>();
            var index = 0;

            // Leemos el archivo de texto
            var lines = File.ReadAllLines(filePath);

            // Puntos del AABB para crear puntos en las esquinas
            aabb = new AABB();

            foreach (var line in lines)
            {
                // Header = Tamaño de la Nube de Puntos
                if (!line.Contains(' '))
                {
                    // Le añadimos 4 mas por las esquinas
                    points = new Vector3[int.Parse(line) + 4];
                    continue;
                }

                // Extraemos el punto
                var sCoords = line.Split(' ');
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
            var corners = GetCorners(aabb, np, octaves);
            foreach (var corner in corners) points[index++] = corner;

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
        private static Vector3[] GetCorners(AABB aabb, PerlinNoiseParams np, PerlinOctaves octaves)
        {
            var corners = new Vector3[4];

            corners[0] = new Vector3(aabb.min.x, 0, aabb.min.y); // BOT LEFT
            corners[1] = new Vector3(aabb.max.x, 0, aabb.min.y); // BOT RIGHT
            corners[2] = new Vector3(aabb.min.x, 0, aabb.max.y); // TOP LEFT
            corners[3] = new Vector3(aabb.max.x, 0, aabb.max.y); // TOP RIGHT

            corners[0].y = GetNoiseHeight(new Vector2(corners[0].x, corners[0].z), np, octaves);
            corners[1].y = GetNoiseHeight(new Vector2(corners[1].x, corners[1].z), np, octaves);
            corners[2].y = GetNoiseHeight(new Vector2(corners[2].x, corners[2].z), np, octaves);
            corners[3].y = GetNoiseHeight(new Vector2(corners[3].x, corners[3].z), np, octaves);

            return corners;
        }

        #endregion
    }


    public static class PerlinNoiseThreadSafe
    {
        public static void BuildHeightMap(NativeArray<float> map, PerlinNoiseParams np) =>
            BuildHeightMap(map, np, BuildOctaves(np));

        public static void BuildHeightMap(
            NativeArray<float> map, PerlinNoiseParams np, PerlinOctavesThreadSafe octaves
        )
        {
            // Scale no puede ser negativa
            if (np.scale <= 0) np.scale = 0.0001f;

            var size = np.SampleSize;

            var halfSize = size / 2f;
            var center = new float2(halfSize, halfSize) + np.offset;

            // Recorremos el mapa en 2D
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var coords = new float2(x - center.x, y - center.y);
                // Almacenamos el Ruido resultante
                map[x + y * size] = GetNoiseHeight(
                    coords,
                    np,
                    octaves
                );
            }
        }

        public static float GetNoiseHeight(float2 point, PerlinNoiseParams np, PerlinOctavesThreadSafe octaves)
        {
            float height = 0;
            for (var i = 0; i < np.numOctaves; i++)
            {
                var offset = octaves.offsets[i];
                var frecuency = octaves.frecuencies[i];
                var amplitude = octaves.amplitudes[i];

                var coords = new float2(
                    (point.x + offset.x) / np.scale * frecuency,
                    (point.y + offset.y) / np.scale * frecuency
                );

                height += (Mathf.PerlinNoise(coords.x, coords.y) * 2 - 1) * amplitude;
            }

            // El Ruido resultante se interpola entre el Maximo y el Minimo
            return Mathf.InverseLerp(-octaves.maxNoiseValue, octaves.maxNoiseValue, height);
        }

        public static PerlinOctavesThreadSafe BuildOctaves(PerlinNoiseParams np)
        {
            var octaves = new PerlinOctavesThreadSafe(np.numOctaves);

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

        public struct PerlinOctavesThreadSafe
        {
            private int Count => offsets.Length;
            public NativeArray<float2> offsets;
            public NativeArray<float> frecuencies;
            public NativeArray<float> amplitudes;
            public float maxNoiseValue;

            public PerlinOctavesThreadSafe(int numOctaves)
            {
                maxNoiseValue = 0;
                offsets = new NativeArray<float2>(numOctaves, Allocator.Temp);
                frecuencies = new NativeArray<float>(numOctaves, Allocator.Temp);
                amplitudes = new NativeArray<float>(numOctaves, Allocator.Temp);
            }
        }
    }
}