using System.Collections;
using MapGeneration.TextureGeneration;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Utils.Threading;

namespace MapDisplay
{
    [ExecuteAlways]
    public abstract class MapDisplayInTexture : MapDisplayBase
    {
        [Space] public Gradient gradient;

        private RawImage image;

        protected Texture2D texture;
        protected Color32[] textureData;
        protected Renderer textureRenderer;

        private void Awake()
        {
            textureRenderer = GetComponent<Renderer>();
            image = GetComponent<RawImage>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (!textureJobHandle.IsCompleted)
                textureJobHandle.Complete();
            textureDataThreadSafe.Dispose();
        }

        protected override void OnValidate()
        {
            if (!autoUpdate) return;
            if (paralelized) UpdateGradientThreadSafe();
            base.OnValidate();
        }

        public override void BuildMap()
        {
            if (paralelized)
            {
                StartCoroutine(BuildMapParallelizedCoroutine());
            }
            else
            {
                var size = NoiseParams.size;
                DebugTimer.DebugTime(BuildHeightMap, $"Time to build HeightMap {size} x {size}");
                DebugTimer.DebugTime(BuildTextureData, $"Time to build TextureData {size} x {size}");
                DebugTimer.DebugTime(DisplayMap, "Time to display map");
            }
        }

        public void BuildTextureData() => textureData = TextureGenerator.BuildTextureData32(heightMap, gradient);

        #region DISPLAY

        public override void DisplayMap() => SetTexture();

        protected void SetTexture()
        {
            if (!paralelized)
                texture = TextureUtils.ColorDataToTexture2D(textureData, HeightMap.Size, HeightMap.Size);

            texture.Apply();

            if (textureRenderer != null)
                SetTextureRenderer(texture);
            else
                SetTextureImage(texture);
        }

        private void SetTextureRenderer(Texture tex)
        {
            textureRenderer.sharedMaterial.mainTexture = tex;
            // textureRenderer.transform.localScale = new Vector3(HeightMap.Size, 1, HeightMap.Size);
        }

        private void SetTextureImage(Texture2D tex)
        {
            image.texture = tex;
            // image.rectTransform.localScale = new Vector3(HeightMap.Size, HeightMap.Size, 1);
        }

        #endregion


        #region THREADING

        private NativeArray<Color32> textureDataThreadSafe;
        private GradientThreadSafe gradientThreadSafe;
        private JobHandle textureJobHandle;

        // protected IEnumerable<Color32> TextureData => paralelized ? textureDataThreadSafe : textureData;

        protected override IEnumerator BuildMapParallelizedCoroutine()
        {
            yield return BuildHeightMapParallelizedCoroutine();
            yield return BuildTextureParallelizedCoroutine();
            DisplayMap();
        }

        private void UpdateGradientThreadSafe()
        {
            if (gradient == null) return;
            gradientThreadSafe.SetGradient(gradient);
        }

        private void UpdateTextureDataThreadSafe()
        {
            if (texture == null || texture.width != heightMapThreadSafe.Size)
                texture = new Texture2D(
                    heightMapThreadSafe.Size,
                    heightMapThreadSafe.Size,
                    TextureFormat.RGBA32,
                    false
                );

            textureDataThreadSafe = texture.GetRawTextureData<Color32>();
        }

        protected IEnumerator BuildTextureParallelizedCoroutine()
        {
            var time = Time.time;

            if (gradientThreadSafe.IsEmpty) UpdateGradientThreadSafe();

            UpdateTextureDataThreadSafe();

            if (!textureJobHandle.IsCompleted) textureJobHandle.Complete();

            textureJobHandle = new TextureGeneratorThreadSafe.MapToTextureJob
            {
                heightMap = heightMapThreadSafe.map,
                textureData = textureDataThreadSafe,
                gradient = gradientThreadSafe
            }.Schedule();

            yield return new WaitUntil(() => textureJobHandle.IsCompleted);

            textureJobHandle.Complete();

            Debug.Log($"{(Time.time - time) * 1000:F1} ms para generar la textura");
        }

        #endregion
    }
}