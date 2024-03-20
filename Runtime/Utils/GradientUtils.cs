using UnityEngine;

namespace Utils
{
    public static class GradientUtils
    {
        public static Gradient CopyGradient(this Gradient gradient) =>
            new()
            {
                alphaKeys = gradient.alphaKeys,
                colorKeys = gradient.colorKeys
            };

        /// <summary>
        ///     Gradiente por defecto
        /// </summary>
        /// <returns>[negro -> blanco] [0,1]</returns>
        public static Gradient BuildGradientDefault() => BuildGradient(Color.black, Color.white);

        private static Gradient BuildGradient(Color a, Color b)
        {
            var gradient = new Gradient();

            var colors = new GradientColorKey[2]
                { new(a, 0), new(b, 1) };

            var alphas = new GradientAlphaKey[2]
                { new(1, 0), new(1, 1) };
            gradient.SetKeys(colors, alphas);

            return gradient;
        }

        /// <summary>
        ///     Copia de un Gradiente para paralelizar su uso
        /// </summary>
        /// <returns>Copia de un Gradiente</returns>
        public static Gradient GetGradientCopy(this Gradient gradient)
        {
            var gradCopy = new Gradient();
            gradCopy.SetKeys(gradient.colorKeys, gradient.alphaKeys);

            return gradCopy;
        }
    }
}