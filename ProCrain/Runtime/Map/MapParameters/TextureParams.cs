using System;
using Unity.Burst;
using UnityEngine;

namespace Procrain
{
    public interface ITextureParams
    {
        public enum InterpolationFilter { Nearest, Linear }
        
        public int Width { get; }
        public int Height { get; }
        public Gradient Gradient { get; }
        public InterpolationFilter Filter { get; }
    }
    
    [CreateAssetMenu(fileName = "Texture Params", menuName = "Procrain/Texture Params")]
    public class TextureParams: AutoUpdatableSoWithBackup<TextureParams>, ITextureParams
    {

        public Vector2Int resolution;
        public Gradient gradient;
        public ITextureParams.InterpolationFilter filter;
        
        public int Width => resolution.x;
        public int Height => resolution.y;
        public ITextureParams.InterpolationFilter Filter => filter;
        
        [SerializeField] private Gradient defaultGradient = new();
        public Gradient Gradient => gradient ?? defaultGradient;
        
        public Gradient_ThreadSafe GradientThreadSafe => new(gradient);
        
        public Color Evaluate(float input) => gradient.Evaluate(input);

        /// To (0,1) float
        public Vector2 GetTexCoord(Vector2Int pos) =>
            new((float)pos.x / resolution.x, (float)pos.y / resolution.y);
        
        /// To (0,Width) int
        public Vector2Int GetTexel(Vector2 texCoord) =>
            new(Mathf.RoundToInt(texCoord.x * resolution.x), Mathf.RoundToInt(texCoord.y * resolution.y));

        protected override void CopyValues(TextureParams from, TextureParams to)
        {
            to.resolution = from.resolution;
            to.gradient = from.gradient;
        }
        
        
        #region THREADING
        
        public TextureParams_ThreadSafe ToThreadSafe() => new(this);
		
        /// Heigth Curve for Threading (sampled to a Look Up Table)
        private Gradient_ThreadSafe _gradientThreadSafe;
		
        public Gradient_ThreadSafe SampledSampledHeightCurve =>
            _gradientThreadSafe.IsEmpty
                ? _gradientThreadSafe = new Gradient_ThreadSafe(gradient)
                : _gradientThreadSafe;

        #endregion
    }

    [BurstCompile]
    public struct TextureParams_ThreadSafe: ITextureParams, IDisposable
    {
        public readonly ITextureParams.InterpolationFilter filter;
        public readonly int width;
        public readonly int height;
        public Gradient_ThreadSafe gradient;
        
        public int Width => width;
        public int Height => height;
        public ITextureParams.InterpolationFilter Filter => filter;
        public Gradient Gradient => gradient.ToUnityGradient();
        
        public TextureParams_ThreadSafe(TextureParams textureParams)
        {
            width = textureParams.Width;
            height = textureParams.Height;
            gradient = textureParams.SampledSampledHeightCurve;
            filter = textureParams.filter;
        }

        public void Dispose() => gradient.Dispose();
    }
}
