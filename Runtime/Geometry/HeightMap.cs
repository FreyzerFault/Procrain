using System;
using System.Linq;
using Procrain.MapParameters;
using Procrain.Noise;
using Procrain.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.Geometry
{
    public interface IHeightMap
    {
        public float[] Map { get; }
        public int Count { get; }
        public bool IsEmpty { get; }
        public int Size { get; }
        public Vector2Int Size2D { get; }
        public float[] ToArray();
        public float[,] ToArray2D();
        public float GetHeight(int x, int y);
        public float GetHeight(Vector2Int coord);
        
        public float3[] ToPoints() {
            float3[] points = new float3[Count];
            
            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
                points[x + y * Size] = new float3(x, GetHeight(x, y), y);

            return points;
        }
    }

    [Serializable]
    public class HeightMap : IHeightMap
    {
        public readonly float[] map;
        public readonly int size;
        public readonly uint seed;

        public float[] Map => map;
        public int Count => map.Length;
        public bool IsEmpty => map == null || map.Length == 0;
        
        public int Size => size;
        public Vector2Int Size2D => new(size, size);

        public HeightMap(float[] map, int size = 129, uint seed = 0)
        {
            this.map = map;
            this.size = size;
            this.seed = seed;
        }

        public HeightMap(NativeArray<float> map, int size = 129, uint seed = 0)
            : this(map.ToArray(), size, seed) { }

        public HeightMap(float[,] map, int size = 129, uint seed = 0)
        {
            this.size = size;
            this.seed = seed;
            this.map = new float[map.GetLength(0) * map.GetLength(1)];

            for (var y = 0; y < map.GetLength(1); y++)
            for (var x = 0; x < map.GetLength(0); x++)
                this.map[x + y * map.GetLength(0)] = map[x, y];
        }

        public HeightMap(Terrain terrain)
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
            ) => NormalizeToMaxHeight();

        public HeightMap_ThreadSafe ToThreadSafe() => new(this);
        
        
        private void NormalizeToMaxHeight()
        {
            var maxHeight = map.Max();
            var minHeight = map.Min();
            for (var i = 0; i < map.Length; i++)
                map[i] = Mathf.InverseLerp(minHeight, maxHeight, map[i]);
        }

        public float[] ToArray() => map;

        public float[,] ToArray2D()
        {
            var array = new float[size, size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                array[x, y] = GetHeight(x, y);
            return array;
        }

        public float[,] ToArray2DFlipped() => FlipCoordsXY(ToArray2D());

        public float GetHeight(int x, int y) => map[x + y * size];
        public float GetHeight(Vector2Int coord) => map[coord.x * size + coord.y * size];
        
        public void ApplyHeightCurve(AnimationCurve heightCurve)
        {
            for (var i = 0; i < map.Length; i++)
                map[i] = heightCurve.Evaluate(map[i]);
        }

        public static float[,] FlipCoordsXY(float[,] map)
        {
            var map2DFlipped = new float[map.GetLength(1), map.GetLength(0)];
            for (var y = 0; y < map.GetLength(1); y++)
            for (var x = 0; x < map.GetLength(0); x++)
                map2DFlipped[y, x] = map[x, y];
            return map2DFlipped;
        }
        
        public static HeightMap CreatePerlinNoiseHeightMap(
            PerlinNoiseParams np,
            AnimationCurve heightCurve = null
        )
        {
            HeightMap heigthMap = new(PerlinNoise.BuildHeightMap(np), np.Size + 1, np.Seed);

            // Animation Curve
            if (heightCurve != null)
                heigthMap.ApplyHeightCurve(heightCurve);

            return heigthMap;
        }
    }

    public struct HeightMap_ThreadSafe : IHeightMap, IDisposable
    {
        public NativeArray<float> map;
        public readonly int size;
        public readonly uint seed;

        public HeightMap_ThreadSafe(int size = 129, uint seed = 0)
        {
            this.size = size;
            this.seed = seed;
            map = new NativeArray<float>(size * size, Allocator.Persistent);
        }

        public HeightMap_ThreadSafe(HeightMap heightMap)
            : this(heightMap.size, heightMap.seed) { }
        

        public float[] Map => map.ToArray();
        
        public int Count => map.Length;
        public bool IsEmpty => map is { Length: 0 };
        public int Size => size;
        public Vector2Int Size2D => new(Size, Size);

        public float[] ToArray() => map.ToArray();

        public float[,] ToArray2D()
        {
            var array = new float[Size, Size];
            for (var x = 0; x < Size; x++)
            for (var y = 0; y < Size; y++)
                array[x, y] = GetHeight(x, y);
            return array;
        }

        [BurstCompile]
        public float GetHeight(int x, int y) => map[x + y * Size];
        
        [BurstCompile]
        public float GetHeight(int2 coord) => map[coord.x * Size + coord.y * Size];
        
        public float GetHeight(Vector2Int coord) => map[coord.x * Size + coord.y * Size];

        public void Dispose() => map.Dispose();

        
        #region JOBS

        [BurstCompile]
        public struct PerlinNoiseMapBuilderJob : IJob
        {
            [ReadOnly] public PerlinNoiseParams_ThreadSafe noiseParams;
            [WriteOnly] public HeightMap_ThreadSafe heightMap;

            public void Execute() => 
                PerlinNoise_ThreadSafe.BuildHeightMap(heightMap.map, noiseParams);
        }
        
        [BurstCompile]
        public struct ApplyAnimationCurveJob : IJob
        {
            [ReadOnly] public SampledAnimationCurve heightCurve;
            [WriteOnly] public HeightMap_ThreadSafe heightMap;

            public void Execute()
            {
                if (!heightCurve.IsEmpty)
                    for (var i = 0; i < heightMap.map.Length; i++)
                        heightMap.map[i] = heightCurve.Evaluate(heightMap.map[i]);
            }
        }

        #endregion
    }
}
