using System;
using System.Linq;
using DavidUtils.ThreadingUtils;
using Unity.Collections;
using UnityEngine;

namespace Procrain.MapGeneration
{
    public interface IHeightMap
    {
        public int Length { get; }
        public bool IsEmpty { get; }
        public int Size { get; }
        public float[,] ToArray2D();
        public float GetHeight(int x, int y);
        public void ApplyHeightCurve(AnimationCurve heightCurve);
    }

    [Serializable]
    public class HeightMap : IHeightMap
    {
        public readonly float[] map;
        public readonly uint seed;

        public int Length => map.Length;
        public bool IsEmpty => map == null || map.Length == 0;
        public int Size { get; }

        public HeightMap(float[] map, int size = 129, uint seed = 0)
        {
            this.map = map;
            Size = size;
            this.seed = seed;
        }

        public HeightMap(NativeArray<float> map, int size = 129, uint seed = 0)
            : this(map.ToArray(), size, seed)
        {
        }

        public HeightMap(float[,] map, int size = 129, uint seed = 0)
        {
            Size = size;
            this.seed = seed;
            this.map = new float[map.GetLength(0) * map.GetLength(1)];

            for (var y = 0; y < map.GetLength(1); y++)
            for (var x = 0; x < map.GetLength(0); x++)
                this.map[x + y * map.GetLength(0)] = map[x, y];
        }

        public HeightMap(UnityEngine.Terrain terrain)
            : this(
                FlipCoordsXY(
                    terrain.terrainData.GetHeights(
                        0,
                        0,
                        terrain.terrainData.heightmapResolution,
                        terrain.terrainData.heightmapResolution
                    )
                ),
                terrain.terrainData.heightmapResolution
            )
        {
            NormalizeToMaxHeight();
        }

        private void NormalizeToMaxHeight()
        {
            var maxHeight = map.Max();
            var minHeight = map.Min();
            for (var i = 0; i < map.Length; i++) map[i] = Mathf.InverseLerp(minHeight, maxHeight, map[i]);
        }

        public float[,] ToArray2D()
        {
            var array = new float[Size, Size];
            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
                array[x, y] = GetHeight(x, y);
            return array;
        }

        public float[,] ToArray2DFlipped() => FlipCoordsXY(ToArray2D());

        public float GetHeight(int x, int y) => map[x + y * Size];

        public void ApplyHeightCurve(AnimationCurve heightCurve)
        {
            for (var i = 0; i < map.Length; i++) map[i] = heightCurve.Evaluate(map[i]);
        }

        public static float[,] FlipCoordsXY(float[,] map)
        {
            var map2DFlipped = new float[map.GetLength(1), map.GetLength(0)];
            for (var y = 0; y < map.GetLength(1); y++)
            for (var x = 0; x < map.GetLength(0); x++)
                map2DFlipped[y, x] = map[x, y];
            return map2DFlipped;
        }
    }

    public struct HeightMapThreadSafe : IHeightMap, IDisposable
    {
        public NativeArray<float> map;
        public readonly uint seed;

        public HeightMapThreadSafe(int size = 129, uint seed = 0)
        {
            Size = size;
            this.seed = seed;
            map = new NativeArray<float>(size * size, Allocator.Persistent);
        }

        public int Length => map.Length;
        public bool IsEmpty => map is { Length: 0 };
        public int Size { get; }

        public float[,] ToArray2D()
        {
            var array = new float[Size, Size];
            for (var x = 0; x < Size; x++)
            for (var y = 0; y < Size; y++)
                array[x, y] = GetHeight(x, y);
            return array;
        }

        public float GetHeight(int x, int y) => map[x + y * Size];

        public void ApplyHeightCurve(AnimationCurve heightCurve)
        {
            for (var i = 0; i < map.Length; i++) map[i] = heightCurve.Evaluate(map[i]);
        }

        // NO paralelizable
        public void ApplyHeightCurve(SampledAnimationCurve heightCurve = default)
        {
            if (heightCurve.IsEmpty) return;

            for (var i = 0; i < map.Length; i++) map[i] = heightCurve.Evaluate(map[i]);
        }

        public void Dispose() => map.Dispose();
    }
}