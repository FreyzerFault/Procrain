using Procrain.Core;
using Procrain.MapDisplay;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor
{
	[CustomEditor(typeof(MapDisplayBase), true)]
	public class MapDisplayEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			var mapDisplay = target as MapDisplayBase;
			if (mapDisplay == null)
				return;

			if (DrawDefaultInspector()) mapDisplay.DisplayMap();

			// Boton para generar el mapa
			if (GUILayout.Button("Regenerate Map"))
			{
				MapManager.Instance.BuildMap();
				mapDisplay.DisplayMap();
			}

			if (GUILayout.Button("Reset Seed"))
			{
				MapManager.Instance.ResetSeed();
				mapDisplay.DisplayMap();
			}
		}
	}
}
