using Procrain.Utils;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.Utils
{
	[CustomEditor(typeof(AutoUpdatableSo), true)]
	public class AutoUpdatableSoEditor : UnityEditor.Editor
	{
		private bool _autoUpdate = true;

		public override void OnInspectorGUI()
		{
			AutoUpdatableSo data = (AutoUpdatableSo)target;

			if (DrawDefaultInspector() && _autoUpdate) data.NotifyUpdate();

			GUILayout.Space(30);

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Update")) data.NotifyUpdate();

			GUILayout.FlexibleSpace();

			_autoUpdate = EditorGUILayout.Toggle("Auto Update", _autoUpdate);

			GUILayout.EndHorizontal();
		}
	}
}
