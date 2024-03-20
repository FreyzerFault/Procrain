using System;
using Unity.Collections;
using UnityEngine;
using Utils.Threading;

namespace MapGeneration
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

    public readonly struct HeightMap : IHeightMap
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
            for (var i = 0; i < map.Length; i++)
                map[i] = heightCurve.Evaluate(map[i]);
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
            for (var i = 0; i < map.Length; i++)
                map[i] = heightCurve.Evaluate(map[i]);
        }

        // NO paralelizable
        public void ApplyHeightCurve(SampledAnimationCurve heightCurve = default)
        {
            if (heightCurve.IsEmpty) return;

            for (var i = 0; i < map.Length; i++)
                map[i] = heightCurve.Evaluate(map[i]);
        }

        public void Dispose() => map.Dispose();
    }
}