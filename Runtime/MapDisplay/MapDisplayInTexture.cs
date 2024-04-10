using UnityEngine;
using UnityEngine.UI;

namespace Procrain.MapDisplay
{
    public abstract class MapDisplayInTexture : MapDisplayBase
    {
        protected Image image;
        protected Renderer textureRenderer;

        private void Awake()
        {
            textureRenderer = GetComponent<Renderer>();
            image = GetComponent<Image>();
        }
        
        protected override void OnTextureUpdated(Texture2D texture) => UpdateTexture(texture);

        public override void DisplayMap()
        {
            if (Texture == null) return;
            UpdateTexture(Texture);
        }

        public void UpdateTexture(Texture2D texture)
        {
            texture.Apply();

            if (textureRenderer != null)
                SetTextureRenderer(texture);
            else
                SetTextureImage(texture);
        }

        private void SetTextureRenderer(Texture tex) =>
            textureRenderer.sharedMaterial.mainTexture = tex;

        private void SetTextureImage(Texture2D tex) => image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
    }
}
