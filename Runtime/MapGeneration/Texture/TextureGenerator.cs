using System;
using System.Linq;
using DavidUtils;
using DavidUtils.ThreadingUtils;
using Procrain.Noise;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Procrain.MapGeneration.Texture
{
    public static class TextureGenerator
    {
        public static Color[] BuildTextureData(HeightMap map, Gradient gradient) =>
            map.map.Select(gradient.Evaluate).ToArray();

        public static Color32[] BuildTextureData32(HeightMap map, Gradient gradient) =>
            map.map.Select(gradient.Evaluate).Select(color => color.ToColor32()).ToArray();

        public static Texture2D BuildTexture2D(HeightMap map, Gradient gradient) =>
            TextureUtils.ColorDataToTexture2D(BuildTextureData(map, gradient), map.Size, map.Size);

        public static Texture2D BuildTexture2D(Color[] textureData, int width, int height) =>
            TextureUtils.ColorDataToTexture2D(textureData, width, height);

        // TODO: Generar la Textura de cero, sin tener de iterar por un mapa de alturas
        // TODO: Añadir resolucion
        public static Color[] BuildTextureData(PerlinNoiseParams noiseParams, Gradient gradient) =>
            throw new NotImplementedException();
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