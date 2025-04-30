using System;
using Procrain.Utils;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.Utils
{
	[CustomPropertyDrawer(typeof(PowerOfTwoAttribute))]
	public class PowerOfTwoDrawer : PropertyDrawer
	{
		private PowerOfTwoAttribute _powerOfTwoAttribute;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginChangeCheck();

			if (attribute is not PowerOfTwoAttribute powAtt) return;

			int value = property.intValue;
			int currentIndex = Array.IndexOf(powAtt.options, value);
			if (currentIndex == -1) currentIndex = 0;
			int newIndex = EditorGUI.Popup(position, label.text, currentIndex, powAtt.optionLabels);

			if (!EditorGUI.EndChangeCheck()) return;
			property.intValue = powAtt.options[newIndex];
		}
	}
}
