using System;
using System.Linq;
using Procrain.Geometry;
using Procrain.MapParameters;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Procrain.Noise;
using Unity.Mathematics;
using static Procrain.Noise.PerlinNoise_ThreadSafe;
using Octaves = Procrain.Noise.PerlinNoise.PerlinOctaves;

namespace Procrain.MapGeneration.Texture
{
    public static class TextureGenerator
    {
        #region TEXTURE DATA

            #region TexSize Differ from MapSize

            public static Color32[] BuildTextureData32(IHeightMap map, TextureParams textureParams) =>
                BuildTextureData(map, textureParams).Select(ToColor32).ToArray();
            
            public static Color[] BuildTextureData(IHeightMap heightMap, ITextureParams textureParams
                , Func<Color, Color> postprocessing = null)
            {
                if (heightMap.Size == textureParams.Width && heightMap.Size == textureParams.Height) 
                    return BuildTextureData(heightMap, textureParams.Gradient);
                
                // INTERPOLACIÓN de valores de altura con los valores del mapas más cercanos a cada pixel.
                
                var textureData = new Color[textureParams.Width * textureParams.Height];

                // Factor de escala para mapear entre las resoluciones
                float scaleX = (float)(heightMap.Size - 1) / (textureParams.Width - 1);
                float scaleY = (float)(heightMap.Size - 1) / (textureParams.Height - 1);

                // Recorremos la Textura
                for (var y = 0; y < textureParams.Height; y++)
                for (var x = 0; x < textureParams.Width; x++)
                {
                    // Posición en el heightMap
                    Vector2 sourceCoords = new(x * scaleX, y * scaleY);

                    // Interpolar según el Filtro configurado
                    float interpolatedHeight = textureParams.Filter switch
                    {
                        ITextureParams.InterpolationFilter.Nearest => 
                            heightMap.GetHeight(Vector2Int.RoundToInt(sourceCoords)),
                        ITextureParams.InterpolationFilter.Linear =>
                            BilinearInterpolation(heightMap.Map, heightMap.Size2D, sourceCoords),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    // Evaluar en el gradiente y almacenar
                    textureData[y * textureParams.Width + x] = textureParams.Gradient.Evaluate(interpolatedHeight);

                    if (postprocessing != null)
                        textureData[y * textureParams.Width + x] = postprocessing(textureData[y * textureParams.Width + x]);
                }

                return textureData;
            }

            #endregion

            
            #region Using Map and Gradient (SAME SIZE as Map)

            // Build a Texture with the SAME Resolution as the HeightMap
            public static Color[] BuildTextureData(IHeightMap map, Gradient gradient) =>
                map.ToArray().Select(gradient.Evaluate).ToArray();

            public static Color32[] BuildTextureData32(IHeightMap map, Gradient gradient) =>
                map.ToArray().Select(gradient.Evaluate).Select(ToColor32).ToArray();
            
            #endregion
        
            
            #region Generate Perlin Noise on the way

            /// Construye los datos de Textura consultando los valores sobre la marcha,
            /// sin tener que generar el Mapa de Alturas
            public static Color[] BuildTextureData(
                IHeightMapParams noiseParams,
                ITextureParams texParams
            )
            {
                return noiseParams switch
                {
                    PerlinNoiseParams np when texParams is TextureParams tp => 
                        BuildTextureData(np, tp),
                    
                    PerlinNoiseParams_ThreadSafe np_ts when texParams is TextureParams_ThreadSafe tp_ts =>
                        BuildTextureData(np_ts, tp_ts),
                    
                    _ => throw new ArgumentException(
                        "NoiseParams and TextureParams must be of types " +
                        "[PerlinNoiseParams or PerlinNoiseParams_ThreadSafe] " +
                        "and [TextureParams or TextureParams_ThreadSafe]", nameof(noiseParams))
                };
            }
            
            public static Color[] BuildTextureData(
                PerlinNoiseParams noiseParams,
                TextureParams texParams
            )
            {
                Vector2Int res = new Vector2Int(texParams.Width, texParams.Height);
                Octaves octaves = new(noiseParams);
                var textureData = new Color[res.x * res.y];
            
                // Directamente sampleamos la textura en el ruido, pero escalado a Size para poder tomar distintos zooms
                Vector2Int noiseSize = res * noiseParams.Size;
            
                // La centramos para que al modificar el size haga un zoom central
                float2 center = noiseParams.Offset + new float2(res.x * 0.5f, res.y * 0.5f);
            
                for (var y = 0; y < res.y; y++)
                for (var x = 0; x < res.x; x++)
                {
                    float2 coords = new float2((float)x / res.x * noiseSize.x, (float)y / res.y * noiseSize.y) - center;
                    float value = PerlinNoise.GetNoiseHeight(coords, noiseParams, octaves);
                    textureData[x * res.x + y] = texParams.Gradient.Evaluate(value);
                }

                return textureData;
            }
        
            /// Genera el Mapa de Alturas convirtiendo los valores a Color sobre la marcha
            public static Color[] BuildTextureData(PerlinNoiseParams noiseParams, Gradient gradient) => 
                PerlinNoise.BuildMap(noiseParams, gradient.Evaluate);

            /// Genera el Mapa de Alturas convirtiendo los valores a Color32 sobre la marcha
            public static Color32[] BuildTextureData32(PerlinNoiseParams noiseParams, Gradient gradient) =>
                PerlinNoise.BuildMap(noiseParams, f => gradient.Evaluate(f).ToColor32());
        
            #endregion
            
        #endregion
        
        
        #region TEXTURE2D
        
            #region FROM ColorData

            public static Texture2D BuildTexture2D(Color[] textureData, int width, int height) =>
                ColorDataToTexture2D(textureData, width, height);

            public static Texture2D BuildTexture2D(Color32[] textureData, int width, int height) =>
                ColorDataToTexture2D(textureData, width, height);
                    
            #endregion
            
            
            #region FROM HeightMap

            public static Texture2D BuildTexture2D(IHeightMap map, ITextureParams texParams) =>
                ColorDataToTexture2D(BuildTextureData(map, texParams), texParams.Width, texParams.Height);
                
            public static Texture2D BuildTexture2D(IHeightMap map, Gradient gradient) =>
                ColorDataToTexture2D(BuildTextureData(map, gradient), map.Size, map.Size);

            #endregion

            
            #region From Perlin Noise generated on the way
                
            public static Texture2D BuildTexture2D(IHeightMapParams noiseParams, ITextureParams texParams) =>
                ColorDataToTexture2D(BuildTextureData(noiseParams, texParams), texParams.Width, texParams.Height);
            
            public static Texture2D BuildTexture2D(PerlinNoiseParams noiseParams, Gradient gradient) =>
                ColorDataToTexture2D(BuildTextureData(noiseParams, gradient), noiseParams.Size, noiseParams.Size);

            #endregion

        #endregion
        
        
        #region FOR THREADING

        /// Construye los datos de Textura consultando los valores sobre la marcha,
        /// sin tener que generar el Mapa de Alturas
        [BurstCompile]
        public static Color[] BuildTextureData(
            PerlinNoiseParams_ThreadSafe noiseParams,
            TextureParams_ThreadSafe texParams
        )
        {
            Vector2Int res = new Vector2Int(texParams.Width, texParams.Height);
            PerlinOctaves_ThreadSafe octaves = new(noiseParams);
            var textureData = new Color[res.x * res.y];
            
            // Directamente sampleamos la textura en el ruido, pero escalado a Size para poder tomar distintos zooms
            Vector2Int noiseSize = res * noiseParams.Size;
            
            // La centramos para que al modificar el size haga un zoom central
            float2 center = noiseParams.Offset + new float2(res.x * 0.5f, res.y * 0.5f);
            
            for (var y = 0; y < res.y; y++)
            for (var x = 0; x < res.x; x++)
            {
                float2 coords = new float2((float)x / res.x * noiseSize.x, (float)y / res.y * noiseSize.y) - center;
                float value = GetNoiseHeight(coords, noiseParams.scale, octaves);
                textureData[x * res.x + y] = texParams.Gradient.Evaluate(value);
            }

            return textureData;
        }
        
        [BurstCompile]
        public struct MapToTextureJob : IJob
        {
            public HeightMap_ThreadSafe heightMap;
            public TextureParams_ThreadSafe textureParams;
            public NativeArray<Color32> textureData;
            
            // public bool IsCompleted => heightMap.IsCreated && textureData.IsCreated;

            public void Execute()
            {
                int2 mapSize = heightMap.Size;
                int2 texSize = new int2(textureParams.width, textureParams.height);
                textureData = new NativeArray<Color32>(textureParams.width * textureParams.height, Allocator.Persistent);
                
                // Same Size: Map <=> Texture
                if (mapSize.x == texSize.x && mapSize.y == texSize.y)
                   textureParams.gradient.FromHeightMap(textureData, heightMap.map);
                
                // Different Size: Interpolate values of the map

                // Factor de escala para mapear entre las resoluciones
                float scaleX = (float)(heightMap.Size2D.x - 1) / (textureParams.width - 1);
                float scaleY = (float)(heightMap.Size2D.y - 1) / (textureParams.height - 1);

                // Recorremos la Textura
                for (var y = 0; y < textureParams.height; y++)
                for (var x = 0; x < textureParams.width; x++)
                {
                    // Posición en el heightMap
                    float2 sourceCoords = new(x * scaleX, y * scaleY);

                    // Interpolar según el Filtro configurado
                    float interpolatedHeight = textureParams.filter switch
                    {
                        ITextureParams.InterpolationFilter.Nearest => 
                            heightMap.GetHeight((int2)math.round(sourceCoords)),
                        ITextureParams.InterpolationFilter.Linear =>
                            BilinearInterpolation_ThreadSafe(heightMap.map, heightMap.Size, sourceCoords),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    // Evaluar en el gradiente y almacenar
                    textureData[y * texSize.x + x] = textureParams.gradient.Evaluate(interpolatedHeight);
                }
            }
        }

        [BurstCompile]
        public struct GeneratePerlinNoiseTextureJob : IJob
        {
            [ReadOnly] public HeightMap_ThreadSafe heightMap;
            [ReadOnly] public PerlinNoiseParams_ThreadSafe noiseParams;
            [ReadOnly] public TextureParams_ThreadSafe textureParams;
            
            [WriteOnly] public NativeArray<Color32> textureData;

            public GeneratePerlinNoiseTextureJob(IHeightMapParams noiseParams,
                TextureParams_ThreadSafe textureParams)
            {
                this.noiseParams = noiseParams switch
                {
                    PerlinNoiseParams np => np.ToThreadSafe(),
                    PerlinNoiseParams_ThreadSafe np_ts => np_ts,
                    _ => throw new ArgumentException(
                        "Los parámetros deben ser de tipo PerlinNoiseParams o PerlinNoiseParams_ThreadSafe",
                        nameof(noiseParams))
                };

                this.textureParams = textureParams;
                
                textureData = new NativeArray<Color32>(textureParams.width * textureParams.height,
                    Allocator.Persistent);

                heightMap = new HeightMap_ThreadSafe();
            }

            public GeneratePerlinNoiseTextureJob(IHeightMap heightMap,
                TextureParams_ThreadSafe textureParams)
            {
                this.heightMap = heightMap switch
                {
                    HeightMap hm => hm.ToThreadSafe(),
                    HeightMap_ThreadSafe hm_ts => hm_ts,
                    _ => throw new ArgumentException(
                        "Los parámetros deben ser de tipo HeightMap o HeightMap_ThreadSafe",
                        nameof(heightMap))
                };
                
                this.textureParams = textureParams;
                
                textureData = new NativeArray<Color32>(textureParams.width * textureParams.height,
                    Allocator.Persistent);

                noiseParams = new PerlinNoiseParams_ThreadSafe();
            }
            
            /// Construye los datos de Textura consultando los valores sobre la marcha,
            /// sin tener que generar el Mapa de Alturas
            public void Execute()
            {
                if (heightMap.IsEmpty)
                    GenerateByParams();
                else
                    GenerateByHeightMap();
            }

            [BurstCompile]
            private void GenerateByParams()
            {
                int2 texSize = new(textureParams.width, textureParams.height);
                PerlinOctaves_ThreadSafe octaves = new(noiseParams);
            
                // Directamente sampleamos la textura en el ruido, pero escalado a Size para poder tomar distintos zooms
                int2 noiseSize = texSize * noiseParams.size;
            
                // La centramos para que al modificar el size haga un zoom central
                float2 center = noiseParams.Offset + new float2(texSize.x * 0.5f, texSize.y * 0.5f);
            
                for (var y = 0; y < texSize.y; y++)
                for (var x = 0; x < texSize.x; x++)
                {
                    float2 coords = new float2((float)x / texSize.x * noiseSize.x, (float)y / texSize.y * noiseSize.y) - center;
                    float value = GetNoiseHeight(coords, noiseParams.scale, octaves);
                    textureData[x * texSize.x + y] = textureParams.gradient.Evaluate(value);
                }
            }
            
            [BurstCompile]
            private void GenerateByHeightMap()
            {
                int2 mapSize = heightMap.Size;
                int2 texSize = new(textureParams.width, textureParams.height);
                textureData = new NativeArray<Color32>(textureParams.width * textureParams.height, Allocator.Persistent);
                
                // Same Size: Map <=> Texture
                if (mapSize.x == texSize.x && mapSize.y == texSize.y)
                    textureParams.gradient.FromHeightMap(textureData, heightMap.map);
                
                // Different Size: Interpolate values of the map

                // Factor de escala para mapear entre las resoluciones
                float scaleX = (float)(heightMap.Size2D.x - 1) / (textureParams.width - 1);
                float scaleY = (float)(heightMap.Size2D.y - 1) / (textureParams.height - 1);

                // Recorremos la Textura
                for (var y = 0; y < textureParams.height; y++)
                for (var x = 0; x < textureParams.width; x++)
                {
                    // Posición en el heightMap
                    float2 sourceCoords = new(x * scaleX, y * scaleY);

                    // Interpolar según el Filtro configurado
                    float interpolatedHeight = textureParams.filter switch
                    {
                        ITextureParams.InterpolationFilter.Nearest => 
                            heightMap.GetHeight((int2)math.round(sourceCoords)),
                        ITextureParams.InterpolationFilter.Linear =>
                            BilinearInterpolation_ThreadSafe(heightMap.map, heightMap.Size, sourceCoords),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    // Evaluar en el gradiente y almacenar
                    textureData[y * texSize.x + x] = textureParams.gradient.Evaluate(interpolatedHeight);
                }
            }
        }

        #endregion


        #region UTILS

        private static Color32 ToColor32(this Color color) => new(
            Convert.ToByte(color.r),
            Convert.ToByte(color.g),
            Convert.ToByte(color.b),
            Convert.ToByte(color.a)
        );

        public static Texture2D ColorDataToTexture2D<T>(T[] colorData, int width, int height) where T : struct
        {
            Texture2D texture = new(width, height)
            { filterMode = FilterMode.Point };
            switch (colorData)
            {
                case Color32[]:
                    texture.SetPixels32(colorData.Cast<Color32>().ToArray());
                    break;
                case Color[]:
                    texture.SetPixels(colorData.Cast<Color>().ToArray());
                    break;
            }
            texture.Apply();
            return texture;
        }
        
        private static float BilinearInterpolation(float[] map, Vector2Int size, Vector2 floatingCoord)
        {
            // Obtener las coordenadas de los píxeles vecinos
            Vector2Int coord0 = Vector2Int.FloorToInt(floatingCoord);
            Vector2Int coord1 = Vector2Int.Min(coord0 + Vector2Int.one, size - Vector2Int.one);

            // Calcular pesos de interpolación
            float tx = floatingCoord.x - coord0.x;
            float ty = floatingCoord.y - coord0.y;

            // Obtener valores de los puntos vecinos
            float h00 = map[coord0.x * size.x + coord0.y];
            float h10 = map[coord1.x * size.x + coord0.y];
            float h01 = map[coord0.x * size.x + coord1.y];
            float h11 = map[coord1.x * size.x + coord1.y];

            // Interpolación bilineal
            return h00 * (1 - tx) * (1 - ty) +
                   h10 * tx * (1 - ty) +
                   h01 * (1 - tx) * ty +
                   h11 * tx * ty;
        }

        [BurstCompile]
        private static float BilinearInterpolation_ThreadSafe(NativeArray<float> map, int2 size, float2 floatingCoord)
        {
            int2 one = new(1, 1);
           
            // Obtener las coordenadas de los píxeles vecinos
            int2 coord0 = (int2)math.floor(floatingCoord);
            int2 coord1 = math.min(coord0 + one, size - one);

            // Calcular pesos de interpolación
            float tx = floatingCoord.x - coord0.x;
            float ty = floatingCoord.y - coord0.y;

            // Obtener valores de los puntos vecinos
            float h00 = map[coord0.x * size.x + coord0.y];
            float h10 = map[coord1.x * size.x + coord0.y];
            float h01 = map[coord0.x * size.x + coord1.y];
            float h11 = map[coord1.x * size.x + coord1.y];

            // Interpolación bilineal
            return h00 * (1 - tx) * (1 - ty) +
                   h10 * tx * (1 - ty) +
                   h01 * (1 - tx) * ty +
                   h11 * tx * ty;
        }
        
        #endregion
    }

    
}
