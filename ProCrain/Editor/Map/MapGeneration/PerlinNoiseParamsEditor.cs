using Procrain.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.MapGeneration
{
	[CustomEditor(typeof(PerlinNoiseParams), true)]
	public class PerlinNoiseParamsEditor : AutoUpdatableSoEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			PerlinNoiseParams noiseParams = target as PerlinNoiseParams;
			if (!noiseParams)
				return;

			if (GUILayout.Button("Reset Seed"))
				noiseParams.ResetSeed();

			if (!noiseParams.dirty)
				return;

			if (GUILayout.Button("✔ Save Changes"))
				noiseParams.SaveChanges();
			if (GUILayout.Button("✖ Undo Changes"))
				noiseParams.UndoChanges();
		}
	}
}
