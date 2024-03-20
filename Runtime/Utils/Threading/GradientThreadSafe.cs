using System;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace Utils.Threading
{
    [BurstCompile]
    public struct GradientThreadSafe : IDisposable
    {
        public NativeArray<GradientColorKey> colorsKeys;

        public bool IsEmpty => !colorsKeys.IsCreated || colorsKeys.Length == 0;

        public GradientThreadSafe(Gradient gradient) : this()
        {
            if (gradient != null) SetGradient(gradient);
        }

        public void SetGradient(Gradient gradient)
        {
            if (colorsKeys.IsCreated && colorsKeys.Length == gradient.colorKeys.Length) return;

            if (colorsKeys.IsCreated)
                colorsKeys.Dispose();
            colorsKeys = new NativeArray<GradientColorKey>(gradient.colorKeys, Allocator.Persistent);
        }

        public Color32 Evaluate(float t)
        {
            var index = 0;
            while (index < colorsKeys.Length && colorsKeys[index].time < t)
                index++;

            var key = colorsKeys[index];

            if (index == 0 || index == colorsKeys.Length)
                return colorsKeys[index == 0 ? 0 : colorsKeys.Length - 1].color;

            var prevKey = colorsKeys[index - 1];

            t = Mathf.InverseLerp(prevKey.time, key.time, t);
            return Color.Lerp(prevKey.color, key.color, t);
        }

        public void FromHeightMap(NativeArray<Color32> textureData, NativeArray<float> map)
        {
            for (var i = 0; i < map.Length; i++)
                textureData[i] = Evaluate(map[i]);
        }

        public void Dispose() => colorsKeys.Dispose();
    }
}