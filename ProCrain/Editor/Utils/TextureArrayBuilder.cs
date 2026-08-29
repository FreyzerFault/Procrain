using UnityEditor;
using UnityEngine;

namespace Procrain.Editor.Utils
{
	public class TextureArrayBuilderEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var builder = (TextureArrayBuilder)target;

			if (GUILayout.Button("Create Texture Array")) builder.CreateTextureArray();
		}
	}

	public class TextureArrayBuilder : MonoBehaviour
	{
		private Texture2D[] textures;

		public void CreateTextureArray() =>
			AssetDatabase.CreateAsset(BuildTextureArray(), "Assets/Textures/TerrainTextureArray.asset");

		private Texture2DArray BuildTextureArray()
		{
			int width = textures[0].width;
			int height = textures[0].height;
			var texArray = new Texture2DArray(width, height, textures.Length, TextureFormat.RGBA64, true);

			for (var i = 0; i < textures.Length; i++) Graphics.CopyTexture(textures[i], 0, 0, texArray, i, 0);

			return texArray;
		}
	}
}
