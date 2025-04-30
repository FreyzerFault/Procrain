using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.Utils
{
	// Curvas de Animación (funciones de Unity) en structs para mandar a hilos paralelos
	// El AnimationCurve de Unity no puede usarse en paralelo
	[BurstCompile]
    public struct SampledAnimationCurve : IDisposable
    {
        	private NativeArray<float> _sampledCurve;
    
        	public bool IsEmpty => !_sampledCurve.IsCreated || _sampledCurve.Length == 0;
    
        	public SampledAnimationCurve(AnimationCurve curve, int samples)
        		: this()
        	{
        		if (curve == null) return;
    
        		Sample(curve, samples);
        	}
    
        	/// <param name="samples">Must be 2 or higher</param>
        	public void Sample(AnimationCurve curve, int samples)
        	{
        		if (!_sampledCurve.IsCreated || _sampledCurve.Length != samples)
        		{
        			_sampledCurve.Dispose();
        			_sampledCurve = new NativeArray<float>(samples, Allocator.Persistent);
        		}
    
        		float timeFrom = curve.keys[0].time;
        		float timeTo = curve.keys[^1].time;
        		float timeStep = (timeTo - timeFrom) / (samples - 1);
    
        		for (var i = 0; i < samples; i++) _sampledCurve[i] = curve.Evaluate(timeFrom + i * timeStep);
        	}
    
        	public void Dispose() => _sampledCurve.Dispose();
    
        	/// <param name="time">Must be from 0 to 1</param>
        	public float Evaluate(float time)
        	{
        		int len = _sampledCurve.Length - 1;
        		float clamp01 =
        			time < 0
        				? 0
        				: time > 1
        					? 1
        					: time;
        		float floatIndex = clamp01 * len;
        		var floorIndex = (int)math.floor(floatIndex);
        		if (floorIndex == len) return _sampledCurve[len];
    
        		float lowerValue = _sampledCurve[floorIndex];
        		float higherValue = _sampledCurve[floorIndex + 1];
        		return math.lerp(lowerValue, higherValue, math.frac(floatIndex));
        	}
    }
}
