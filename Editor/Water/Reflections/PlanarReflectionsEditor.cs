﻿//━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━																												
// Copyright 2020, Alexander Ameye, All rights reserved.
// https://alexander-ameye.gitbook.io/stylized-water/
//━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━	

#if UNIVERSAL_RENDERER
using Procrain.Water.Reflections;
using UnityEditor;

namespace Procrain.Editor.Water.Reflections
{
    [CustomEditor(typeof(PlanarReflections))]
    public class PlanarReflectionsEditor : UnityEditor.Editor
    {
        public bool reflectionsEnabled;
        private SerializedProperty hideReflectionCamera, renderScale, reflectionTarget;

        private SerializedProperty reflectionLayer, reflectSkybox, reflectionPlaneOffset;

        public void OnEnable()
        {
            reflectionLayer = serializedObject.FindProperty("reflectionLayer");
            reflectSkybox = serializedObject.FindProperty("reflectSkybox");
            reflectionPlaneOffset = serializedObject.FindProperty("reflectionPlaneOffset");
            hideReflectionCamera = serializedObject.FindProperty("hideReflectionCamera");
            renderScale = serializedObject.FindProperty("renderScale");
            reflectionTarget = serializedObject.FindProperty("reflectionTarget");
        }

        public override void OnInspectorGUI()
        {
            var planarReflections = (PlanarReflections)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("General", EditorStyles.helpBox);
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 160;
            EditorGUILayout.PropertyField(reflectionTarget, EditorGUIUtility.TrTextContent("Water Object"));
            EditorGUILayout.PropertyField(
                hideReflectionCamera,
                EditorGUIUtility.TrTextContent("Hide Reflection Camera")
            );
            EditorGUIUtility.labelWidth = labelWidth;

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visuals", EditorStyles.helpBox);
            EditorGUILayout.PropertyField(renderScale, EditorGUIUtility.TrTextContent("Render Scale"));
            EditorGUILayout.PropertyField(reflectionPlaneOffset, EditorGUIUtility.TrTextContent("Height Offset"));
            EditorGUILayout.PropertyField(reflectionLayer, EditorGUIUtility.TrTextContent("Layer Mask"));
            EditorGUILayout.PropertyField(reflectSkybox, EditorGUIUtility.TrTextContent("Reflect Skybox"));

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Render scale, height offset, layer mask and skybox reflection settings are shared between all water objects.",
                MessageType.Info
            );
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif