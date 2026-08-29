using System;
using System.Collections;
using UnityEngine;

namespace Procrain
{
    /// <summary>
    /// Abstract base class for texture builders that generate textures from height maps
    /// </summary>
    public abstract class TextureBuilder: MapBuilder
    {
        public Texture2D Build(HeightMap heightMap, TextureParams textureParams) => Build(heightMap.ToThreadSafe(), textureParams);
        public Texture2D Build(PerlinNoiseParams heightMapParams, TextureParams textureParams) => Build(heightMapParams.ToThreadSafe(), textureParams);
        
        public abstract Texture2D Build(HeightMap_ThreadSafe heightMap, TextureParams textureParams);

        public abstract Texture2D Build(PerlinNoiseParams_ThreadSafe heightMapParams, TextureParams textureParams);

        public abstract IEnumerator BuildCoroutine(
            HeightMap_ThreadSafe heightMap, TextureParams textureParams, Action onStart = null, Action<Texture2D> onEnd = null);

        public abstract IEnumerator BuildCoroutine(
            PerlinNoiseParams_ThreadSafe heightMapParams, TextureParams textureParams, Action onStart = null, Action<Texture2D> onEnd = null);

        public IEnumerator BuildCoroutine(
            HeightMap heightMap, TextureParams textureParams, Action onStart = null, Action<Texture2D> onEnd = null) =>
            BuildCoroutine(heightMap.ToThreadSafe(), textureParams, onStart, onEnd);

        public IEnumerator BuildCoroutine(
            PerlinNoiseParams heightMapParams, TextureParams textureParams, Action onStart = null, Action<Texture2D> onEnd = null) =>
            BuildCoroutine(heightMapParams.ToThreadSafe(), textureParams, onStart, onEnd);
        
        protected abstract IEnumerator BuildTexture_ParallelizedCoroutine(
            HeightMap_ThreadSafe heightMap, ITextureParams @params,  
            Action onStart = null, Action<Texture2D> onEnd = null);

        protected abstract IEnumerator BuildTexture_ParallelizedCoroutine(
            PerlinNoiseParams_ThreadSafe heightMapParams, ITextureParams textureParams,  
            Action onStart = null, Action<Texture2D> onEnd = null);
    }
}
