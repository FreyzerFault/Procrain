using Unity.Mathematics;

namespace Procrain.Noise
{
    public static class RandomNoise
    {
        public static float[] BuildHeightMapRandom(int size, uint seed)
        {
            var map = new float[size * size];

            var rand = new Random(seed);

            for (var x = 0; x < size; x++)
            for (var y = 0; y < size; y++)
                map[x + y * size] = rand.NextFloat();

            return map;
        }
    }
}