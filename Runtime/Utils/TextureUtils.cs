using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Utils
{
    public static class TextureUtils
    {
        public static Texture2D ColorDataToTexture2D(IEnumerable<Color> colorData, int width, int height)
        {
            var texture = new Texture2D(width, height);
            texture.SetPixels(colorData.ToArray());
            texture.Apply();
            return texture;
        }

        public static Texture2D ColorDataToTexture2D(IEnumerable<Color32> colorData, int width, int height)
        {
            var texture = new Texture2D(width, height);
            texture.SetPixels32(colorData.ToArray());
            texture.Apply();
            return texture;
        }

        public static Texture2D ColorDataToTexture2D(NativeArray<Color32> colorData, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.GetRawTextureData<Color32>().CopyFrom(colorData);
            texture.Apply();
            return texture;
        }

        public static Color32 ToColor32(this Color color) => color;
        public static Color ToColor(this Color32 color) => color;
    }
}