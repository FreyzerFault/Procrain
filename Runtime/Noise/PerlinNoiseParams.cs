using System;
using Procrain.Editor.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.Runtime.Noise
{
    // Clase en la que se almacenan los parametros usados en el ruido
    [Serializable]
    public struct PerlinNoiseParams
    {
        // Tamaño del terreno (width = height)
        [PowerOfTwo(5, 12, label2d: true)] public int size;

        // Resolución del sampleo del ruido
        // - => mayor suavidad
        // + => mayor detalle
        public float scale;

        // Octavas del ruido
        // Capas de ruido con distinta frecuencia que se suman para dar mayor complejidad
        // Persistencia = Influencia de cada octava
        // Lacunarity = Frecuencia de cada octava (+ lacunarity -> + caótico)
        [Range(1, 10)] public int numOctaves;
        [Range(0, 2)] public float persistance;
        [Range(1, 5)] public float lacunarity;

        // Desplazamiento (x,y) del ruido
        // Permite obtener distintos resultados con el mismo seed
        // O desplazar el mapa manteniendo la coherencia de forma natural
        public float2 offset;

        public uint seed;

        public PerlinNoiseParams(
            int size = 241, float scale = 100, int numOctaves = 4, float persistance = 0.5f,
            float lacunarity = 2f, float2 offset = default, uint seed = 1
        )
        {
            this.size = size;
            this.scale = scale;
            this.numOctaves = numOctaves;
            this.persistance = persistance;
            this.lacunarity = lacunarity;
            this.offset = offset;
            this.seed = seed;
        }

        // Numero de puntos por lado del terreno
        public int SampleSize => size + 1;

        public static PerlinNoiseParams Default() =>
            new(128, 100, 5);
    }
}