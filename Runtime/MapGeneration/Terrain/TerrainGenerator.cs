using UnityEngine;

namespace Procrain.MapGeneration.Terrain
{
    public static class TerrainGenerator
    {
        #region UNITY TERRAIN

        // Modifica el terreno para adaptarse a unas dimensiones
        // Y genera alturas en cada pixel
        public static void ApplyToHeightMap(
            this TerrainData terrainData,
            IHeightMap heightMap,
            float heightMultiplier,
            int resolutionAmplifier = 1
        )
        {
            if (terrainData == null)
                terrainData = new TerrainData();

            var heightMapSize = heightMap.Size;
            var terrainSize = heightMapSize / resolutionAmplifier;
            terrainData.heightmapResolution = heightMapSize + 1;
            terrainData.size = new Vector3(terrainSize, heightMultiplier, terrainSize);
            terrainData.SetHeights(0, 0, HeightMap.FlipCoordsXY(heightMap.ToArray2D()));
        }
        // public static TerrainData ApplyToTerrainData(
        //     TerrainData terrainData,
        //     IHeightMap heightMap,
        //     float heightMultiplier,
        //     int resolutionAmplifier = 1
        // )
        // {
        //     if (terrainData == null)
        //         terrainData = new TerrainData();
        //
        //     var heightMapSize = heightMap.Size;
        //     var terrainSize = heightMapSize / resolutionAmplifier;
        //     terrainData.heightmapResolution = heightMapSize + 1;
        //     terrainData.size = new Vector3(terrainSize, heightMultiplier, terrainSize);
        //     terrainData.SetHeights(0, 0, HeightMap.FlipCoordsXY(heightMap.ToArray2D()));
        //
        //     return terrainData;
        // }

        #endregion
    }
}
