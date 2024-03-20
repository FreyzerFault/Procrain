using Procrain.Runtime.MapGeneration;
using Procrain.Runtime.Noise;
using UnityEngine;

namespace Procrain.Runtime.MapDisplay
{
    public class RandomMapDisplayInTexture : MapDisplayInTexture
    {
        [Range(1, 1024)] public int size = 256;
        [Space] public uint seed = 256;

        protected override void BuildHeightMap() =>
            heightMap = new HeightMap(RandomNoise.BuildHeightMapRandom(size, seed), size, seed);

        public override void ResetSeed() => seed = PerlinNoise.GenerateRandomSeed();
    }
}