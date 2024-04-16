using DavidUtils.ScriptableObjectsUtils;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using DavidUtils.Editor;
#endif

namespace Procrain.Noise
{
	public struct PerlinNoiseParams_ThreadSafe
	{
		public int size;
		public float scale;
		public int numOctaves;
		public float persistance;
		public float lacunarity;
		public float2 offset;
		public uint seed;
		
		public int SampleSize => size + 1;
	}
	
	// Clase en la que se almacenan los parametros usados en el ruido
	[CreateAssetMenu(menuName = "Procrain/Perlin Noise Params", fileName = "Perlin Noise Params")]
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
		
		// Convert to a struct that can be used in Jobs System
		public PerlinNoiseParams_ThreadSafe ToThreadSafe() => new PerlinNoiseParams_ThreadSafe
		{
			size = size,
			scale = scale,
			numOctaves = numOctaves,
			persistance = persistance,
			lacunarity = lacunarity,
			offset = offset,
			seed = seed
		};
		
		
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
		
		public void ResetSeed() => Seed = (uint)Random.Range(0, int.MaxValue);
		
		
		protected override void CopyValues(PerlinNoiseParams from, PerlinNoiseParams to)
		{
			to.size = from.size;
			to.scale= from.scale;
			to.offset = from.offset;
			to.seed = from.seed;
			to.numOctaves = from.numOctaves;
			to.lacunarity = from.lacunarity;
			to.persistance = from.persistance;
		}
	}
}
