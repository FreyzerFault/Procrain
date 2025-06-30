using System;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
// ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable

namespace Procrain.Utils
{
	/// Curvas de Animación (funciones de Unity) en structs para mandar a hilos paralelos
	/// El AnimationCurve de Unity no puede usarse en paralelo
	public readonly struct SampledAnimationCurve : IDisposable
    {
	    public const int DefaultSampleCount = 128;
	    
        private readonly NativeArray<float> _sampledCurve;
        
        public int SampleCount => _sampledCurve.Length;
        public bool IsEmpty => !_sampledCurve.IsCreated || _sampledCurve.Length == 0;
        
        public SampledAnimationCurve(AnimationCurve curve, int samples = DefaultSampleCount)
        	: this()
        {
        	if (curve == null) return;
	        
        	Sample(curve, samples, out _sampledCurve);
        }

        
        private static void Sample(AnimationCurve curve, int numSamples, out NativeArray<float> sampledValues)
        {
	        sampledValues = new NativeArray<float>(numSamples, Allocator.Persistent);
	        
        	float timeFrom = curve.keys[0].time;
        	float timeTo = curve.keys[^1].time;
        	float timeStep = (timeTo - timeFrom) / (sampledValues.Length - 1);

        	for (var i = 0; i < sampledValues.Length; i++)
		        sampledValues[i] = curve.Evaluate(timeFrom + i * timeStep);
        }

        public NativeArray<float> Evaluate(NativeArray<float> values)
        {
	        var sampledValues = new NativeArray<float>(values.Length, Allocator.Persistent);
	        for (var i = 0; i < values.Length; i++) 
		        sampledValues[i] = Evaluate(values[i]);
	        return sampledValues;
        }
        
        public float[] Evaluate(float[] values)
        {
	        var sampledValues = new float[values.Length];
	        for (var i = 0; i < values.Length; i++) 
		        sampledValues[i] = Evaluate(values[i]);
	        return sampledValues;
        }
        
        /// <param name="time">Must be from 0 to 1</param>
        [BurstCompile]
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

        void IDisposable.Dispose()
        {
	        if (_sampledCurve.IsCreated)
		        _sampledCurve.Dispose();
        }

        public AnimationCurve ToUnityCurve()
        {
	        int sampleCount = SampleCount;
	        return new AnimationCurve(_sampledCurve.Select((value, i) => new Keyframe(i / (float) sampleCount, value)).ToArray());
        }
    }
}
