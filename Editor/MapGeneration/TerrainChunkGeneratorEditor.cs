using DavidUtils.DebugUtils;
using Procrain.MapDisplay.InfiniteTerrain;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.MapGeneration
{
    [CustomEditor(typeof(TerrainChunkGenerator))]
    public class TerrainChunkGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var terrainChunkGen = target as TerrainChunkGenerator;
            if (terrainChunkGen == null) return;

            if (DrawDefaultInspector() && terrainChunkGen.autoUpdate) terrainChunkGen.OnValuesUpdated();

            // Boton para generar el mapa
            if (GUILayout.Button("Regenerate Terrain")) DebugTimer.DebugTime(terrainChunkGen.RegenerateTerrain);

            if (GUILayout.Button("Reset Seed")) terrainChunkGen.ResetSeed();

            if (GUILayout.Button("Clear")) terrainChunkGen.Clear();
        }
    }
}