using Procrain.Runtime.MapGeneration.MeshGeneration;
using Procrain.Runtime.MapGeneration.TextureGeneration;
using UnityEngine;

namespace Procrain.Runtime.MapGeneration.TerrainGeneration
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