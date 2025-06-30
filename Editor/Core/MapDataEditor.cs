using Procrain.Core;
using Procrain.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.Core
{
    [CustomEditor(typeof(MapData))]
    public class MapDataEditor: AutoUpdatableSoEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            MapData mapData = (MapData)target;
            
            if (GUILayout.Button("Build HeightMap"))
                mapData.BuildHeightMap();
            
            if (GUILayout.Button("Build Texture"))
                mapData.BuildTexture();
            
            if (GUILayout.Button("Build Mesh Data"))
                mapData.BuildMeshData();
        }
    }
}
