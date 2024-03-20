using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.Utils
{
    [CustomPropertyDrawer(typeof(PowerOfTwoAttribute))]
    public class PowerOfTwoDrawer : PropertyDrawer
    {
        private PowerOfTwoAttribute powerOfTwoAttribute;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();

            var powAtt = attribute as PowerOfTwoAttribute;
            if (powAtt == null) return;

            var value = property.intValue;
            var currentIndex = Array.IndexOf(powAtt.options, value);
            if (currentIndex == -1) currentIndex = 0;
            var newIndex = EditorGUI.Popup(position, label.text, currentIndex, powAtt.optionLabels);

            if (!EditorGUI.EndChangeCheck()) return;
            property.intValue = powAtt.options[newIndex];
        }
    }

    public class PowerOfTwoAttribute : PropertyAttribute
    {
        public readonly string[] optionLabels = Enumerable.Range(0, 12).Select(x => $"{1 << x}x{1 << x}").ToArray();
        public readonly int[] options = Enumerable.Range(0, 12).Select(x => 1 << x).ToArray();

        public PowerOfTwoAttribute(int min, int max, bool includeZero = false, bool label2d = false)
        {
            options = Enumerable.Range(min, max + 1 - min).Select(x => 1 << x).ToArray();
            optionLabels = Enumerable.Range(min, max + 1 - min)
                .Select(x => label2d ? $"{1 << x}x{1 << x}" : (1 << x).ToString())
                .ToArray();

            if (includeZero)
            {
                options = options.Prepend(0).ToArray();
                optionLabels = optionLabels.Prepend("0").ToArray();
            }
        }

        public PowerOfTwoAttribute()
        {
        }
    }
}