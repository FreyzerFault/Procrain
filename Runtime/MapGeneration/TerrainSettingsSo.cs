using DavidUtils;
using DavidUtils.ScriptableObjectsUtils;
using Procrain.Noise;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using DavidUtils.Editor;
#endif

namespace Procrain.MapGeneration
{
	[ExecuteAlways]
	[CreateAssetMenu(menuName = "Terrain Settings", fileName = "Procrain/Terrain Settings")]
	public class TerrainSettingsSo : AutoUpdatableSoWithBackup<TerrainSettingsSo>
	{
		[SerializeField] private AnimationCurve heightCurve = AnimationCurveUtils.DefaultCurve();
		[SerializeField] private float heightScale = 100;

#if UNITY_EDITOR
		[PowerOfTwo(0, 4, true)]
#endif
		[SerializeField] private int lod;

		[SerializeField] private PerlinNoiseParams noiseParams = PerlinNoiseParams.Default();

		#region UPDATABLE PARAMS

		public PerlinNoiseParams NoiseParams
		{
			get => noiseParams;
			set
			{
				noiseParams = value;
				NotifyUpdate();
			}
		}

		public AnimationCurve HeightCurve
		{
			get => heightCurve;
			set
			{
				heightCurve = value;
				NotifyUpdate();
			}
		}

		public float HeightScale
		{
			get => heightScale;
			set
			{
				heightScale = value;
				NotifyUpdate();
			}
		}

		public int LOD
		{
			get => lod;
			set
			{
				lod = value;
				NotifyUpdate();
			}
		}

		public float2 Offset
		{
			get => noiseParams.offset;
			set
			{
				noiseParams.offset = value;
				NotifyUpdate();
			}
		}

		public uint Seed
		{
			get => noiseParams.seed;
			set
			{
				noiseParams.seed = value;
				NotifyUpdate();
			}
		}

		public void ResetSeed() => Seed = (uint)Random.Range(0, int.MaxValue);

		#endregion


		protected override void CopyValues(TerrainSettingsSo from, TerrainSettingsSo to)
		{
			to.noiseParams = from.noiseParams;
			to.heightCurve = from.heightCurve;
			to.heightScale = from.heightScale;
			to.lod = from.lod;
		}
	}
}
