using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain
{
	/// Gradientes de Color en structs para mandar a hilos paralelos
	/// El Gradient de Unity no puede usarse en paralelo, pero podemos guardar los GradientColorKey
	[BurstCompile]
    public struct Gradient_ThreadSafe : IDisposable
    {
        private NativeArray<GradientColorKey> _colorsKeys;

        public bool IsEmpty => !_colorsKeys.IsCreated || _colorsKeys.Length == 0;

        public Gradient_ThreadSafe(Gradient gradient) : this()
        {
        	if (gradient != null) SetGradient(gradient);
        }

        public void SetGradient(Gradient gradient)
        {
        	if (_colorsKeys.IsCreated && _colorsKeys.Length == gradient.colorKeys.Length) 
		        return;

        	if (_colorsKeys.IsCreated) _colorsKeys.Dispose();
        	_colorsKeys = new NativeArray<GradientColorKey>(
        		gradient.colorKeys,
        		Allocator.Persistent
        	);
        }

        [BurstCompile]
        public Color32 Evaluate(float t)
        {
        	var index = 0;
        	while (index < _colorsKeys.Length && _colorsKeys[index].time < t) index++;

        	GradientColorKey key = _colorsKeys[index];

        	if (index == 0 || index == _colorsKeys.Length)
        		return _colorsKeys[index == 0 ? 0 : _colorsKeys.Length - 1].color;

        	GradientColorKey prevKey = _colorsKeys[index - 1];

	        t = math.unlerp(prevKey.time, key.time, t);
        	return Color.Lerp(prevKey.color, key.color, t);
        }

        [BurstCompile]
        public void FromHeightMap(NativeArray<Color32> textureData, NativeArray<float> map)
        {
        	for (var i = 0; i < map.Length; i++)
		        textureData[i] = Evaluate(map[i]);
        }

        public void Dispose() => _colorsKeys.Dispose();
        
        public Gradient ToUnityGradient()
        {
        	Gradient gradient = new()
	        {
		        colorKeys = _colorsKeys.ToArray()
	        };
	        return gradient;
        }
    }
}
