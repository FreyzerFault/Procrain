using Map;
using UnityEngine;
using UnityEngine.UI;

namespace Procrain.MapDisplay
{
    [ExecuteAlways]
    public abstract class MapDisplayInTexture : MapDisplayBase
    {
        private RawImage _image;
        protected Renderer textureRenderer;

        private void Awake()
        {
            textureRenderer = GetComponent<Renderer>();
            _image = GetComponent<RawImage>();
        }

        private void Start() => MapManager.Instance.OnTextureUpdated += SetTexture;

        private void OnDestroy() => MapManager.Instance.OnTextureUpdated -= SetTexture;

        protected void SetTexture(Texture2D texture)
        {
            texture.Apply();

            if (textureRenderer != null)
                SetTextureRenderer(texture);
            else
                SetTextureImage(texture);
        }

        private void SetTextureRenderer(Texture tex) =>
            textureRenderer.sharedMaterial.mainTexture = tex;

        private void SetTextureImage(Texture2D tex) => _image.texture = tex;
    }
}
