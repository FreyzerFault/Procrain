using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Procrain
{
    public abstract class MapDisplayInImage : MapDisplayBase
    {
        private Image _image;
        private Renderer _textureRenderer;

        private void Awake()
        {
            _textureRenderer = GetComponent<Renderer>();
            _image = GetComponent<Image>();
        }
        
        protected override void HandleTextureUpdated(Texture2D texture) => UpdateTexture(texture);

        [ContextMenu("Update Rendered Texture")]
        public override void DisplayMap()
        {
            if (Texture) UpdateTexture(Texture);
        }

        
        private void UpdateTexture(Texture2D texture)
        {
            texture.Apply();

            if (_textureRenderer)
                SetTextureRenderer(texture);
            else
                SetTextureImage(texture);
        }

        private void SetTextureRenderer(UnityEngine.Texture tex) =>
            _textureRenderer.sharedMaterial.mainTexture = tex;

        private void SetTextureImage(Texture2D tex) => 
            _image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
    }
}
