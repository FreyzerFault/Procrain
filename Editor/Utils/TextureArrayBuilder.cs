using MyBox;
using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.Utils
{
    public class TextureArrayBuilder : MonoBehaviour
    {
        private Texture2D[] textures;

        [ButtonMethod]
        public void CreateTextureArray()
        {
            AssetDatabase.CreateAsset(BuildTextureArray(), "Assets/Textures/TerrainTextureArray.asset");
        }

        private Texture2DArray BuildTextureArray()
        {
            var width = textures[0].width;
            var height = textures[0].height;
            var texArray = new Texture2DArray(width, height, textures.Length, TextureFormat.RGBA64, true);

            for (var i = 0; i < textures.Length; i++) Graphics.CopyTexture(textures[i], 0, 0, texArray, i, 0);

            return texArray;
        }
    }
}