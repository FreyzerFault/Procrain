﻿//━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━																												
// Copyright 2020, Alexander Ameye, All rights reserved.
// https://alexander-ameye.gitbook.io/stylized-water/
//━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━	

using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Procrain.Editor.Water
{
    public static class GradientTextureMaker
    {
        public static int width = 128;
        public static int height = 4; // needs to be multiple of 4 for DXT1 format compression

        public static Texture2D CreateGradientTexture(Material targetMaterial, Gradient gradient)
        {
            var gradientTexture = new Texture2D(width, height, TextureFormat.ARGB32, false, false)
            {
                name = "_gradient",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var j = 0; j < height; j++)
            for (var i = 0; i < width; i++)
                gradientTexture.SetPixel(i, j, gradient.Evaluate(i / (float)width));

            gradientTexture.Apply(false);
            gradientTexture = SaveAndGetTexture(targetMaterial, gradientTexture);
            return gradientTexture;
        }

        private static Texture2D SaveAndGetTexture(Material targetMaterial, Texture2D sourceTexture)
        {
            var targetFolder = AssetDatabase.GetAssetPath(targetMaterial);
            targetFolder = targetFolder.Replace(targetMaterial.name + ".mat", string.Empty);

            targetFolder += "Gradient Textures/";

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                AssetDatabase.Refresh();
            }

            var path = targetFolder + targetMaterial.name + sourceTexture.name + ".png";
            File.WriteAllBytes(path, sourceTexture.EncodeToPNG());
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.Default);
            sourceTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
            return sourceTexture;
        }
    }
}