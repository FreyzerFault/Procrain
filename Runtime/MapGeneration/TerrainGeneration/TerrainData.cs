using MapGeneration.MeshGeneration;
using MapGeneration.TextureGeneration;
using UnityEngine;

namespace MapGeneration.TerrainGeneration
{
    public struct TerrainMapData
    {
        public HeightMap heightMap;
        public Color[] textureData;
        public IMeshData meshData;

        public Texture2D BuildTexture() =>
            TextureGenerator.BuildTexture2D(textureData, heightMap.Size, heightMap.Size);

        public Mesh BuildMesh() =>
            meshData.CreateMesh();
    }
}