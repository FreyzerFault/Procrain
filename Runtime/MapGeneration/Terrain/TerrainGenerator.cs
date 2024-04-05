using UnityEngine;

namespace Procrain.MapGeneration.Terrain
{
    public static class TerrainGenerator
    {
        #region UNITY TERRAIN

        // Modifica el terreno para adaptarse a unas dimensiones
        // Y genera alturas en cada pixel
        public static TerrainData ApplyToTerrainData(
            TerrainData terrainData,
            HeightMap heightMap,
            float heightMultiplier
        )
        {
            if (terrainData == null) terrainData = new TerrainData();

            var size = heightMap.Size;
            terrainData.heightmapResolution = size + 1;
            terrainData.size = new Vector3(size, heightMultiplier, size);
            terrainData.SetHeights(0, 0, HeightMap.FlipCoordsXY(heightMap.ToArray2D()));

            return terrainData;
        }

        #endregion
    }
}