using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Procrain
{
	public readonly struct PerlinNoiseParams_ThreadSafe
	{
		public readonly SampledAnimationCurve heightCurve;
		
		public readonly int size;
		public readonly float scale;
		public readonly int numOctaves;
		public readonly float persistance;
		public readonly float lacunarity;
		public readonly float2 offset;
		public readonly uint seed;

		public int SampleSize => size + 1;

		public AnimationCurve HeightCurve => heightCurve.ToUnityCurve();
		public SampledAnimationCurve SampledHeightCurve => heightCurve;

		public int Size => size;
		public float Scale => scale;
		public float2 Offset => offset;

		public PerlinNoiseParams_ThreadSafe(PerlinNoiseParams pnParams)
		{
			heightCurve = pnParams.SampledHeightCurve;
			size = pnParams.Size;
			scale = pnParams.Scale;
			numOctaves = pnParams.NumOctaves;
			persistance = pnParams.Persistance;
			lacunarity = pnParams.Lacunarity;
			offset = pnParams.Offset;
			seed = pnParams.Seed;
			
			// Scale no puede ser negativa
			if (scale <= 0) scale = 0.0001f;
		}
	}
	
	// Clase en la que se almacenan los parametros usados en el ruido
	[CreateAssetMenu(fileName = "Perlin Noise Params", menuName = "Procrain/PerlinNoise Params")]
	public class PerlinNoiseParams : AutoUpdatableSoWithBackup<PerlinNoiseParams>
	{
		// Tamaño del terreno (width = height)

#if UNITY_EDITOR
		[PowerOfTwo(5, 12, label2d: true)]
#endif
		[SerializeField] private int size = 241;

		// Resolución del sampleo del ruido
		// - => mayor suavidad
		// + => mayor detalle
		[SerializeField] private float scale = 100;

		// Octavas del ruido
		// Capas de ruido con distinta frecuencia que se suman para dar mayor complejidad
		// Persistencia = Influencia de cada octava
		// Lacunarity = Frecuencia de cada octava (+ lacunarity -> + caótico)
		[SerializeField] [Range(1, 10)] private int numOctaves = 4;

		[SerializeField] [Range(0, 2)] private float persistance = .5f;

		[SerializeField] [Range(1, 5)] private float lacunarity = 2f;

		// Desplazamiento (x,y) del ruido
		// Permite obtener distintos resultados con el mismo seed
		// O desplazar el mapa manteniendo la coherencia de forma natural
		[SerializeField] private float2 offset;

		[SerializeField] private uint seed = 1;
		
		// Numero de puntos por lado del terreno
		public int SampleSize => size + 1;

		
		// Postprocessing Curve
		[SerializeField] private AnimationCurve heightCurve;
		
		public AnimationCurve HeightCurve => heightCurve;
		public SampledAnimationCurve SampledHeightCurve => new(heightCurve);

		
		// Convert to a struct that can be used in Jobs System
		public PerlinNoiseParams_ThreadSafe ToThreadSafe() => new(this);

		
		#region UPDATABLE PROPS

		public int Size
		{
			get => size;
			set
			{
				size = value;
				NotifyUpdate();
			}
		}


		public float Scale
		{
			get => scale;
			set
			{
				scale = value;
				NotifyUpdate();
			}
		}

		public uint Seed
		{
			get => seed;
			set
			{
				seed = value;
				NotifyUpdate();
			}
		}

		public float2 Offset
		{
			get => offset;
			set
			{
				offset = value;
				NotifyUpdate();
			}
		}

		public int NumOctaves
		{
			get => numOctaves;
			set
			{
				numOctaves = value;
				NotifyUpdate();
			}
		}

		public float Persistance
		{
			get => persistance;
			set
			{
				persistance = value;
				NotifyUpdate();
			}
		}

		public float Lacunarity
		{
			get => lacunarity;
			set
			{
				lacunarity = value;
				NotifyUpdate();
			}
		}

		#endregion

		
		public void ResetSeed() => Seed = (uint)Random.Range(0, int.MaxValue);

		protected override void CopyValues(PerlinNoiseParams from, PerlinNoiseParams to)
		{
			to.size = from.size;
			to.scale = from.scale;
			to.offset = from.offset;
			to.seed = from.seed;
			to.numOctaves = from.numOctaves;
			to.lacunarity = from.lacunarity;
			to.persistance = from.persistance;
		}
	}
}
