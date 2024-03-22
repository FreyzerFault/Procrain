using MapGeneration;
using Procrain.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Editor.MapGeneration
{
    [CustomEditor(typeof(TerrainSettingsSo), true)]
    public class TerrainParamsSoEditor : AutoUpdatableSoEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var terrainParamsSo = target as TerrainSettingsSo;
            if (terrainParamsSo == null) return;

            if (GUILayout.Button("Reset Seed")) terrainParamsSo.ResetSeed();

            if (!terrainParamsSo.dirty) return;

            if (GUILayout.Button("✔ Save Changes")) terrainParamsSo.SaveChanges();
            if (GUILayout.Button("✖ Undo Changes")) terrainParamsSo.UndoChanges();
        }
    }
}