using System;
using System.Collections;
using Unity.Jobs;
using UnityEngine;
using TexGen = Procrain.Texture.TextureGenerator;

namespace Procrain
{
    /// <summary>
    /// Builds textures using height gradient mapping for terrain visualization
    /// </summary>
    [CreateAssetMenu(fileName = "Texture Gradient Builder", menuName = "Procrain/Builders/Texture Height Gradient Builder")]
    public class TextureBuilder_HeightGradient: TextureBuilder
    {
        /// <summary>
        /// Builds texture directly from height map data
        /// </summary>
        public override Texture2D Build(HeightMap_ThreadSafe heightMap, TextureParams textureParams) => 
            TexGen.BuildTexture2D(heightMap, textureParams);

        /// <summary>
        /// Builds texture from height map parameters, specifically for Perlin noise
        /// </summary>
        public override Texture2D Build(PerlinNoiseParams_ThreadSafe heightMapParams, TextureParams textureParams) => 
            TexGen.BuildTexture2D(heightMapParams, textureParams);

        /// <summary>
        /// Asynchronously builds texture from height map, with optional parallelization
        /// </summary>
        public override IEnumerator BuildCoroutine(HeightMap_ThreadSafe heightMap, TextureParams textureParams, Action onStart = null, Action<Texture2D> onEnd = null)
        {
            if (paralelized)
            {
                yield return BuildTexture_ParallelizedCoroutine(heightMap, textureParams, onStart, onEnd);
            }
            else
            {
                onStart?.Invoke();
                Texture2D texture = Build(heightMap, textureParams);
                onEnd?.Invoke(texture);
            }
        }

        /// <summary>
        /// Asynchronously builds texture from height map parameters, with optional parallelization
        /// </summary>
        public override IEnumerator BuildCoroutine(PerlinNoiseParams_ThreadSafe heightMapParams, TextureParams textureParams, Action onStart = null, Action<Texture2D> onEnd = null)
        {
            if (paralelized)
            {
                yield return BuildTexture_ParallelizedCoroutine(heightMapParams, textureParams, onStart, onEnd);
            }
            else
            {
                onStart?.Invoke();
                Texture2D texture = Build(heightMapParams, textureParams);
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
        protected override IEnumerator BuildTexture_ParallelizedCoroutine(
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
        protected override IEnumerator BuildTexture_ParallelizedCoroutine(
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
