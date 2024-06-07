using DavidUtils.Editor.DevTools.ScriptableObjects;
using Procrain.MapGeneration;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.MapGeneration
{
	[CustomEditor(typeof(TerrainSettingsSo), true)]
	public class TerrainSettingsSoEditor : AutoUpdatableSoEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var terrainParamsSo = target as TerrainSettingsSo;
			if (terrainParamsSo == null)
				return;

			if (!terrainParamsSo.dirty)
				return;

			if (GUILayout.Button("✔ Save Changes"))
				terrainParamsSo.SaveChanges();
			if (GUILayout.Button("✖ Undo Changes"))
				terrainParamsSo.UndoChanges();
		}
	}
}
