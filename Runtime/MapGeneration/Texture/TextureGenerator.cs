using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Procrain.Noise;
using GradientThreadSafe = Procrain.Utils.GradientThreadSafe;

namespace Procrain.MapGeneration.Texture
{
    public static class TextureGenerator
    {
        public static Color[] BuildTextureData(IHeightMap map, Gradient gradient) =>
            map.ToArray().Select(gradient.Evaluate).ToArray();

        public static Color32[] BuildTextureData32(IHeightMap map, Gradient gradient) =>
            map.ToArray().Select(gradient.Evaluate).Select(color => (Color32) color).ToArray();

        public static Texture2D BuildTexture2D(IHeightMap map, Gradient gradient) =>
            ColorDataToTexture2D(BuildTextureData(map, gradient), map.Size, map.Size);

        public static Texture2D BuildTexture2D(Color[] textureData, int width, int height) =>
            ColorDataToTexture2D(textureData, width, height);

        public static Texture2D BuildTexture2D(Color32[] textureData, int width, int height) =>
            ColorDataToTexture2D(textureData, width, height);

        // TODO: Generar la Textura de cero, sin tener de iterar por un mapa de alturas
        // TODO: Aplicar una resolucion, tal que pueda generar una textura 254x254 a partir de un mapa 128x128
        public static Color32[] BuildTextureData(
            PerlinNoiseParams noiseParams,
            Gradient gradient,
            Vector2Int resolutionSize
        ) => throw new NotImplementedException();

        public static Texture2D BuildTexture2D(
            PerlinNoiseParams noiseParams,
            Gradient gradient,
            Vector2Int resolutionSize
        ) =>
            BuildTexture2D(
                BuildTextureData(noiseParams, gradient, resolutionSize),
                resolutionSize.x,
                resolutionSize.y
            );
        
        
        public static Texture2D ColorDataToTexture2D<T>(IEnumerable<T> colorData, int width, int height) where T : struct
        {
            Texture2D texture = new(width, height);
            switch (colorData)
            {
                case IEnumerable<Color32>:
                    texture.SetPixels32(colorData.Cast<Color32>().ToArray());
                    break;
                case IEnumerable<Color>:
                    texture.SetPixels(colorData.Cast<Color>().ToArray());
                    break;
            }
            texture.Apply();
            return texture;
        }
    }

    #region FOR THREADING

    public static class TextureGeneratorThreadSafe
    {
        [BurstCompile]
        public struct MapToTextureJob : IJob
        {
            public NativeArray<float> heightMap;
            public NativeArray<Color32> textureData;
            public GradientThreadSafe gradient;

            public void Execute() => gradient.FromHeightMap(textureData, heightMap);
        }
    }

    #endregion
}
