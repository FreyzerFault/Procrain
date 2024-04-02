using System.Globalization;
using Procrain.MapGeneration.TIN;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.MapGeneration
{
    [CustomEditor(typeof(TinVisualizer))]
    internal class TinVisualizerEditor : UnityEditor.Editor
    {
        [SerializeField] private bool showHeightInfo;
        [SerializeField] private bool showVertexIndices;
        [SerializeField] private bool showEdgeInfo;
        [SerializeField] private bool showTriInfo;

        public void OnSceneGUI()
        {
            var tinVisualizer = target as TinVisualizer;


            if (!tinVisualizer) return;

            var tin = tinVisualizer.tin;
            if (tin == null) return;

            var cyanStyle = new GUIStyle();
            var magentaStyle = new GUIStyle();
            var greenStyle = new GUIStyle();
            var whiteStyle = new GUIStyle();
            cyanStyle.normal.textColor = Color.cyan;
            magentaStyle.normal.textColor = Color.magenta;
            greenStyle.normal.textColor = Color.green;
            whiteStyle.normal.textColor = Color.white;

            if (tin.lastVertexAdded is { Count: > 0 })
                for (var i = 0; i < tin.lastVertexAdded.Count; i++)
                {
                    Handles.Label(tin.lastVertexAdded[i].v3D, tin.lastVertexAdded[i].v3D.ToString(), cyanStyle);
                    Handles.Label(
                        tin.lastVertexAdded[i].v3D + Vector3.up * 10,
                        tin.lastVertexError[i].ToString(CultureInfo.InvariantCulture),
                        magentaStyle
                    );
                }

            if (showHeightInfo)
                foreach (var v in tin.vertices)
                {
                    Handles.color = Color.yellow;
                    Handles.Label(v.v3D + Vector3.up * 10, v.y.ToString(CultureInfo.InvariantCulture), cyanStyle);
                }

            if (showVertexIndices || showEdgeInfo)
                foreach (var e in tin.edges)
                {
                    if (showEdgeInfo) Handles.Label((e.end.v3D + e.begin.v3D) / 2, e.ToString(), magentaStyle);

                    if (!showVertexIndices) continue;

                    Handles.Label(e.begin.v3D, e.begin.ToString(), greenStyle);
                    Handles.Label(e.end.v3D, e.end.ToString(), greenStyle);
                }

            if (!showTriInfo) return;

            foreach (var tri in tin.triangles)
            {
                var triCenter = (tri.v1.v3D + tri.v2.v3D + tri.v3.v3D) / 3;

                Handles.Label(triCenter, tri.ToString(), cyanStyle);
            }
        }

        public override void OnInspectorGUI()
        {
            var tinVisualizer = target as TinVisualizer;
            if (!tinVisualizer) return;

            // Si se cambio algun valor tambien generamos el mapa
            if (DrawDefaultInspector() && tinVisualizer.autoUpdate) tinVisualizer.BuildHeightMap();

            // Boton para generar el mapa
            if (GUILayout.Button("Reset Terrain")) tinVisualizer.ResetTin();

            if (GUILayout.Button("Reset Seed"))
            {
                tinVisualizer.ResetRandomSeed();
                tinVisualizer.ResetTin();
            }

            if (GUILayout.Button("Next Point")) tinVisualizer.RunIteration();

            if (GUILayout.Button("Animated Generation")) tinVisualizer.PlayPauseProgressiveGeneration();


            // if (GUILayout.Button("Load From File"))
            // {
            //     String filePath = EditorUtility.OpenFilePanel("Load TIN", "", "txt");
            //     tinVisualizer.LoadFromFile(filePath);
            // }
        }
    }
}