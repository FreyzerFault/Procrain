using MapDisplay;
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
            if (mapDisplay == null) return;

            // Si se cambio algun valor tambien generamos el mapa
            if (DrawDefaultInspector() && mapDisplay.autoUpdate)
            {
                mapDisplay.SubscribeToValuesUpdated();
                mapDisplay.UpdateHeightCurveThreadSafe();
                mapDisplay.OnValuesUpdated();
            }

            // Boton para generar el mapa
            if (GUILayout.Button("Generate Random Map")) mapDisplay.BuildMap();

            if (GUILayout.Button("Reset Seed")) mapDisplay.ResetSeed();
        }
    }
}