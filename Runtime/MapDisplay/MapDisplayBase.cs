using Map;
using Procrain.MapGeneration;
using UnityEngine;

namespace Procrain.MapDisplay
{
    public abstract class MapDisplayBase : MonoBehaviour
    {
        protected IHeightMap HeightMap => MapManager.Instance.HeightMap;
        protected TerrainSettingsSo TerrainSettings => MapManager.Instance.terrainSettings;
    }
}
