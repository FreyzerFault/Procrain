using Procrain.Runtime.Utils;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.Utils
{
    [CustomEditor(typeof(AutoUpdatableSo), true)]
    public class AutoUpdatableSoEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var autoUpdatableSo = target as AutoUpdatableSo;
            if (autoUpdatableSo == null) return;

            if (GUILayout.Button("Update")) autoUpdatableSo.NotifyUpdate();
        }
    }
}