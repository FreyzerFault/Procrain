using System;
using System.Collections;
using Procrain.Geometry;
using Procrain.Geometry.Mesh;
using Procrain.MapGeneration;
using Procrain.MapParameters;
using Procrain.Utils;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using TexGen = Procrain.MapGeneration.Texture.TextureGenerator;

namespace Procrain.MapBuilder
{
    /// <summary>
    /// Builds textures using height gradient mapping for terrain visualization
    /// </summary>
    public class TextureBuilder_HeightGradient: TextureBuilder
    {
        /// <summary>
        /// Builds texture directly from height map data
        /// </summary>
        public override Texture2D Build(IHeightMap heightMap, TextureParams textureParams) => 
            TexGen.BuildTexture2D(heightMap, textureParams);

        /// <summary>
        /// Builds texture from height map parameters, specifically for Perlin noise
        /// </summary>
        public override Texture2D Build(IHeightMapParams heightMapParams, TextureParams textureParams) => 
            TexGen.BuildTexture2D(heightMapParams, textureParams);

        /// <summary>
        /// Asynchronously builds texture from height map, with optional parallelization
        /// </summary>
        public override IEnumerator BuildCoroutine(IHeightMap heightMap, TextureParams textureParams, Action onStart = null, Action<Texture2D> onEnd = null)
        {
            yield return BuildCoroutineInternal(heightMap, textureParams,
                hm => ((HeightMap)hm).ToThreadSafe(),
                BuildHeightMap_ParallelizedCoroutine, TexGen.BuildTexture2D,
                onStart, onEnd);
        }

        /// <summary>
        /// Asynchronously builds texture from height map parameters, with optional parallelization
        /// </summary>
        public override IEnumerator BuildCoroutine(IHeightMapParams heightMapParams, TextureParams textureParams, Action onStart = null, Action<Texture2D> onEnd = null)
        {
            yield return BuildCoroutineInternal(heightMapParams, textureParams,
                np => ((PerlinNoiseParams)np).ToThreadSafe(),
                BuildHeightMap_ParallelizedCoroutine, TexGen.BuildTexture2D,
                onStart, onEnd);
        }
        
        /// Construye el Texture2D a partir de un HeightMap o HeightMapParams como tipo T.
        /// Usa TextureParams para configurar la Textura
        /// Permite que se ejecute en paralelo o no segun el valor de paralelized
        private IEnumerator BuildCoroutineInternal<T, TSafe>(
            T heightMapInput, ITextureParams textureParams, Func<T, TSafe> toThreadSafe,
            Func<TSafe, ITextureParams, Action, Action<Texture2D>, IEnumerator> buildParallelized,
            Func<T, ITextureParams, Texture2D> buildTexture,
            Action onStart = null, Action<Texture2D> onEnd = null)
        {
            if (paralelized)
            {
                // Convierte ambos parametros en sus versiones ThreadSafe
                TSafe threadSafeInput = heightMapInput is TSafe ts ? ts : toThreadSafe(heightMapInput);
                TextureParams_ThreadSafe tp_ts = textureParams is TextureParams_ThreadSafe tpts 
                    ? tpts 
                    : ((TextureParams)textureParams).ToThreadSafe();

                yield return buildParallelized(threadSafeInput, tp_ts, onStart, onEnd);
            }
            else
            {
                onStart?.Invoke();
                Texture2D texture = buildTexture(heightMapInput, textureParams);
                onEnd?.Invoke(texture);
            }
        }
        

        #region THREADING

        /// Handles parallel processing of texture generation using Unity Jobs system
        private JobHandle _textureDataJob;
        
        /// <summary>
        /// Generates texture from thread-safe height map using parallel processing
        /// to generate the Color Data
        /// </summary>
        protected override IEnumerator BuildHeightMap_ParallelizedCoroutine(
            HeightMap_ThreadSafe heightMap, ITextureParams textureParams,
            Action onStart = null, Action<Texture2D> onEnd = null)
        {
            if (textureParams is not TextureParams_ThreadSafe tp_ts)
                tp_ts = ((TextureParams)textureParams).ToThreadSafe();
            
            TexGen.GeneratePerlinNoiseTextureJob job = new(heightMap, tp_ts);
            
            yield return ExecuteBuildJobCoroutine(job, textureParams.Width, textureParams.Height, onStart, onEnd);
        }

        /// <summary>
        /// Generates texture from height map parameters using parallel processing
        /// to generate the Color Data
        /// </summary>
        protected override IEnumerator BuildHeightMap_ParallelizedCoroutine(
            PerlinNoiseParams_ThreadSafe heightMapParams, ITextureParams textureParams,
            Action onStart = null, Action<Texture2D> onEnd = null)
        {
            if (textureParams is not TextureParams_ThreadSafe tp_ts)
                tp_ts = ((TextureParams)textureParams).ToThreadSafe();

            TexGen.GeneratePerlinNoiseTextureJob job = new(heightMapParams, tp_ts);
            
            yield return ExecuteBuildJobCoroutine(job, textureParams.Width, textureParams.Height, onStart, onEnd);
        }
        
        /// Crea el MeshData y ejecuta el Job para construirlo
        private IEnumerator ExecuteBuildJobCoroutine(
            TexGen.GeneratePerlinNoiseTextureJob job,
            int width, int height, Action onStart = null, Action<Texture2D> onEnd = null)
        {
            Texture2D texture = new(width, height, TextureFormat.RGBA32, false);

            // Get TextureData reference from Texture2D to modify it
            job.textureData = texture.GetRawTextureData<Color32>();
    
            _textureDataJob = _textureDataJob.ScheduleJob(job);
    
            onStart?.Invoke();
    
            // TODO es necesario aplicar los datos de la textura?
            // texture.SetPixelData(_textureDataThreadSafe, 0);
            
            yield return _textureDataJob.WaitForJobToEnd(debug,
                $"Generación de Texture2D {width} x {height}");
    
            onEnd?.Invoke(texture);
        }

        #endregion
    }
}
