using Procrain.Core;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor
{
    [CustomEditor(typeof(MapManager))]
    public class MapManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var mapManager = target as MapManager;
            if (mapManager == null)
                return;

            // Si se cambio algun valor tambien generamos el mapa
            if (DrawDefaultInspector() && mapManager.autoUpdate)
            {
                mapManager.SubscribeToValuesUpdated();
                mapManager.UpdateHeightCurveThreadSafe();
                mapManager.OnValuesUpdated();
            }

            if (GUILayout.Button("Regenerate Map"))
                mapManager.BuildMap();

            if (GUILayout.Button("Reset Seed"))
                mapManager.ResetSeed();
        }
    }
}
