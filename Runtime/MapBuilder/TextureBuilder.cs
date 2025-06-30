using System;
using System.Collections;
using Procrain.Geometry;
using Procrain.MapGeneration;
using Procrain.MapParameters;
using UnityEngine;

namespace Procrain.MapBuilder
{
    /// <summary>
    /// Abstract base class for texture builders that generate textures from height maps
    /// </summary>
    public abstract class TextureBuilder: MapBuilder
    {
        /// <summary>
        /// Builds a texture synchronously from a height map
        /// </summary>
        public abstract Texture2D Build(IHeightMap heightMap, TextureParams textureParams);

        /// <summary>
        /// Builds a texture synchronously from height map parameters
        /// </summary>
        public abstract Texture2D Build(IHeightMapParams heightMapParams, TextureParams textureParams);

        /// <summary>
        /// Builds a texture asynchronously from a height map 
        /// </summary>
        public abstract IEnumerator BuildCoroutine(
            IHeightMap heightMap, TextureParams textureParams, Action onStart = null, Action<Texture2D> onEnd = null);

        /// <summary>
        /// Builds a texture asynchronously from height map parameters
        /// </summary>
        public abstract IEnumerator BuildCoroutine(
            IHeightMapParams heightMapParams, TextureParams textureParams, Action onStart = null, Action<Texture2D> onEnd = null);

        /// <summary>
        /// Builds a texture asynchronously using parallel processing from a thread-safe height map
        /// </summary>
        protected abstract IEnumerator BuildHeightMap_ParallelizedCoroutine(
            HeightMap_ThreadSafe heightMap, ITextureParams @params,  
            Action onStart = null, Action<Texture2D> onEnd = null);

        /// <summary>
        /// Builds a texture asynchronously using parallel processing from height map parameters
        /// </summary>
        protected abstract IEnumerator BuildHeightMap_ParallelizedCoroutine(
            PerlinNoiseParams_ThreadSafe heightMapParams, ITextureParams textureParams,  
            Action onStart = null, Action<Texture2D> onEnd = null);
    }
}
