using Procrain.Utils;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Procrain.MapGeneration
{
	[ExecuteAlways]
	[CreateAssetMenu(menuName = "Terrain Settings", fileName = "Procrain/Terrain Settings")]
	public class TerrainSettingsSo : AutoUpdatableSoWithBackup<TerrainSettingsSo>
	{
		[SerializeField] private AnimationCurve heightCurve = DefaultCurve;
		[SerializeField] private float heightScale = 100;

#if UNITY_EDITOR
		[PowerOfTwo(0, 4, true)]
#endif
		[SerializeField] private int lod;


		#region UPDATABLE PARAMS

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

		#endregion

		protected override void CopyValues(TerrainSettingsSo from, TerrainSettingsSo to)
		{
			to.heightCurve = from.heightCurve;
			to.heightScale = from.heightScale;
			to.lod = from.lod;
		}
		
		
		public static AnimationCurve DefaultCurve => new(new Keyframe(0, 0), new Keyframe(1, 1));
	}
}
