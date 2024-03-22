using MapGeneration.InfiniteGeneration;
using UnityEditor;
using UnityEngine;
using Utils;

namespace Editor.MapGeneration
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