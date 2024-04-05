using Procrain.MapGeneration;
using Procrain.Noise;
using UnityEngine;

namespace Procrain.MapDisplay
{
    public class RandomMapDisplayInTexture : MapDisplayInTexture
    {
        [Range(1, 1024)] public int size = 256;
        [Space] public uint seed = 256;

        protected override void BuildHeightMap() =>
            Map = new HeightMap(RandomNoise.BuildHeightMapRandom(size, seed), size, seed);

        public override void ResetSeed() => seed = PerlinNoise.GenerateRandomSeed();
    }
}