using Map;
using Procrain.MapGeneration;
using UnityEngine;

namespace Procrain.MapDisplay
{
    public abstract class MapDisplayBase : MonoBehaviour
    {
        protected IHeightMap HeightMap => MapManager.Instance.HeightMap;

        protected virtual void OnEnable() => MapManager.Instance.OnMapUpdated += DisplayMap;
        private void OnDisable() => MapManager.Instance.OnMapUpdated -= DisplayMap;

        protected abstract void DisplayMap(IHeightMap heightMap);
    }
}