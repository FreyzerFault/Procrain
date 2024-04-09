using Map;
using Procrain.MapDisplay;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.MapDisplay
{
    [CustomEditor(typeof(MapDisplayBase), true)]
    public class MapDisplayEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var mapDisplay = target as MapDisplayBase;
            if (mapDisplay == null)
                return;

            // Boton para generar el mapa
            if (GUILayout.Button("Regenerate Map"))
                MapManager.Instance.BuildMapSequential();
            if (GUILayout.Button("Reset Seed"))
                MapManager.Instance.ResetSeed();
        }
    }
}
