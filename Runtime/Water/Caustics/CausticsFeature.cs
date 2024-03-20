//━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━																												
// Copyright 2020, Alexander Ameye, All rights reserved.
// https://alexander-ameye.gitbook.io/stylized-water/
//━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━																												

#if UNIVERSAL_RENDERER
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Procrain.Runtime.Water.Caustics
{
    public class CausticsFeature : ScriptableRendererFeature
    {
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");

        private static readonly int causticsTextureID = Shader.PropertyToID("_CausticsTexture");
        private static readonly int causticsStrengthID = Shader.PropertyToID("_CausticsStrength");
        private static readonly int causticsScaleID = Shader.PropertyToID("_CausticsScale");
        private static readonly int causticsSpeedID = Shader.PropertyToID("_CausticsSpeed");
        private static readonly int causticsSplitID = Shader.PropertyToID("_CausticsSplit");
        private static readonly int shadowMaskID = Shader.PropertyToID("_CausticsShadowMask");
        private static readonly int causticsFadeID = Shader.PropertyToID("_CausticsFade");

        private static readonly int waterLevelID = Shader.PropertyToID("_WaterLevel");
        private static readonly int causticsStartID = Shader.PropertyToID("_CausticsStart");
        private static readonly int causticsEndID = Shader.PropertyToID("_CausticsEnd");

        public CausticsSettings settings = new();

        [SerializeField] [HideInInspector] private Shader causticsShader;
        private Material causticsMaterial;
        private CausticsPass causticsPass;

        public override void Create()
        {
            causticsPass = new CausticsPass(settings.waterLevel);

            if (causticsMaterial) DestroyImmediate(causticsMaterial);
            causticsShader = Shader.Find("Hidden/Stylized Water/Caustics");
            if (causticsShader != null) causticsMaterial = CoreUtils.CreateEngineMaterial(causticsShader);

            if (!causticsMaterial) return;

            causticsMaterial.SetTexture(causticsTextureID, settings.texture);
            causticsMaterial.SetFloat(causticsStrengthID, settings.strength);
            causticsMaterial.SetFloat(causticsScaleID, settings.scale);
            causticsMaterial.SetFloat(causticsSpeedID, settings.speed);
            causticsMaterial.SetFloat(causticsSplitID, settings.rgbSplit);
            causticsMaterial.SetFloat(shadowMaskID, settings.shadowMask);
            causticsMaterial.SetFloat(causticsFadeID, settings.fade);

            causticsMaterial.SetFloat(waterLevelID, settings.waterLevel);
            causticsMaterial.SetFloat(causticsStartID, settings.depth.x);
            causticsMaterial.SetFloat(causticsEndID, settings.depth.y);

            switch (settings.debug)
            {
                case CausticsSettings.DebugMode.Disabled:
                    causticsMaterial.SetFloat(SrcBlend, 2f);
                    causticsMaterial.SetFloat(DstBlend, 0f);
                    causticsMaterial.DisableKeyword("DEBUG_MASK");
                    causticsMaterial.DisableKeyword("DEBUG_CAUSTICS");
                    causticsPass.renderPassEvent = settings.renderPassEvent;
                    break;

                case CausticsSettings.DebugMode.Caustics:
                    causticsMaterial.SetFloat(SrcBlend, 1f);
                    causticsMaterial.SetFloat(DstBlend, 0f);
                    causticsMaterial.DisableKeyword("DEBUG_MASK");
                    causticsMaterial.EnableKeyword("DEBUG_CAUSTICS");
                    causticsPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                    break;

                case CausticsSettings.DebugMode.Mask:
                    causticsMaterial.SetFloat(SrcBlend, 1f);
                    causticsMaterial.SetFloat(DstBlend, 0f);
                    causticsMaterial.DisableKeyword("DEBUG_CAUSTICS");
                    causticsMaterial.EnableKeyword("DEBUG_MASK");
                    causticsPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                    break;
            }

            causticsPass.causticsMaterial = causticsMaterial;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(causticsPass);
        }

        [Serializable]
        public class CausticsSettings
        {
            [Header("Visuals")] [Range(0f, 3f)] public float strength = 3f;
            [Range(0f, 1f)] public float rgbSplit = 0.3f;
            [Range(0f, 1f)] public float shadowMask = 1f;

            [Header("Movement")] public Texture2D texture;
            [Range(0.1f, 10f)] public float scale = 5f;
            [Range(0f, 0.3f)] public float speed = 0.125f;

            [Header("Depth")] public float waterLevel;
#if UNITY_EDITOR
            [MinMaxSlider(0, 10)] public Vector2 depth = new(0f, 4f);
#else
            public Vector2 depth = new Vector2(0f, 4f);
#endif
            [Range(0f, 1f)] public float fade = 1f;

            [Header("Rendering")] public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox;

            public DebugMode debug = DebugMode.Disabled;

            public enum DebugMode
            {
                Disabled,
                Caustics,
                Mask
            }
        }
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
        public class MinMaxSliderDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var minMaxAttribute = (MinMaxSliderAttribute)attribute;
                var propertyType = property.propertyType;

                label.tooltip = minMaxAttribute.min.ToString("F2") + " to " + minMaxAttribute.max.ToString("F2");

                var controlRect = EditorGUI.PrefixLabel(position, label);

                var splittedRect = SplitRect(controlRect, 3);

                if (propertyType == SerializedPropertyType.Vector2)
                {
                    EditorGUI.BeginChangeCheck();

                    var vector = property.vector2Value;
                    var minVal = vector.x;
                    var maxVal = vector.y;

                    minVal = EditorGUI.FloatField(splittedRect[0], float.Parse(minVal.ToString("F2")));
                    maxVal = EditorGUI.FloatField(splittedRect[2], float.Parse(maxVal.ToString("F2")));

                    EditorGUI.MinMaxSlider(
                        splittedRect[1],
                        ref minVal,
                        ref maxVal,
                        minMaxAttribute.min,
                        minMaxAttribute.max
                    );

                    if (minVal < minMaxAttribute.min) minVal = minMaxAttribute.min;
                    if (maxVal > minMaxAttribute.max) maxVal = minMaxAttribute.max;

                    vector = new Vector2(minVal > maxVal ? maxVal : minVal, maxVal);

                    if (EditorGUI.EndChangeCheck()) property.vector2Value = vector;
                }
            }

            private Rect[] SplitRect(Rect rectToSplit, int n)
            {
                var rects = new Rect[n];

                for (var i = 0; i < n; i++)
                    rects[i] = new Rect(
                        rectToSplit.position.x + i * rectToSplit.width / n,
                        rectToSplit.position.y,
                        rectToSplit.width / n,
                        rectToSplit.height
                    );

                var padding = (int)rects[0].width - 40;
                var space = 5;

                rects[0].width -= padding + space;
                rects[2].width -= padding + space;

                rects[1].x -= padding;
                rects[1].width += padding * 2;

                rects[2].x += padding + space;

                return rects;
            }
        }

        public class MinMaxSliderAttribute : PropertyAttribute
        {
            public float max;
            public float min;

            public MinMaxSliderAttribute(float min, float max)
            {
                this.min = min;
                this.max = max;
            }
        }
#endif
    }
}
#endif