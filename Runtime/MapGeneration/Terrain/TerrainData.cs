using Procrain.Geometry;
using Procrain.Geometry.Mesh;
using UnityEngine;
using Procrain.MapGeneration.Texture;

namespace Procrain.MapGeneration.Terrain
{
    public struct TerrainMapData
    {
        public HeightMap heightMap;
        public Color[] textureData;
        public IMeshData meshData;

        public Texture2D BuildTexture() =>
            TextureGenerator.BuildTexture2D(textureData, heightMap.size, heightMap.size);

        public UnityEngine.Mesh BuildMesh() =>
            meshData.CreateMesh();
    }
}
