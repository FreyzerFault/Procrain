using Procrain.Mesh;
using Procrain.Texture;
using UnityEngine;

namespace Procrain
{
    public struct TerrainMapData
    {
        public HeightMap heightMap;
        public Color[] textureData;
        public IMeshData meshData;

        public Texture2D BuildTexture() =>
            TextureGenerator.BuildTexture2D(textureData, heightMap.size, heightMap.size);

        public UnityEngine.Mesh BuildMesh() => meshData.CreateMesh();
    }
}
