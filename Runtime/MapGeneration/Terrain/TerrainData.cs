using Procrain.MapGeneration.Mesh;
using Procrain.MapGeneration.Texture;
using UnityEngine;

namespace Procrain.MapGeneration.Terrain
{
    public struct TerrainMapData
    {
        public HeightMap heightMap;
        public Color[] textureData;
        public IMeshData meshData;

        public Texture2D BuildTexture() =>
            TextureGenerator.BuildTexture2D(textureData, heightMap.Size, heightMap.Size);

        public UnityEngine.Mesh BuildMesh() =>
            meshData.CreateMesh();
    }
}