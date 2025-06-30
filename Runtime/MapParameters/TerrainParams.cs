using Procrain.Utils;
using UnityEngine;

namespace Procrain.MapParameters
{
	[CreateAssetMenu(menuName = "Map Generation", fileName = "Procrain/Terrain Params")]
	public class TerrainParams : AutoUpdatableSoWithBackup<TerrainParams>, ITerrainParams
	{
		[SerializeField] private float heightScale = 100;

		[PowerOfTwo(0, 4, true)]
		[SerializeField] private int lod;

		public int LODIndex => (int)Mathf.Log(lod, 2);


		#region UPDATABLE PARAMS

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
		

		protected override void CopyValues(TerrainParams from, TerrainParams to)
		{
			to.heightScale = from.heightScale;
			to.lod = from.lod;
		}
		
		public TerrainParams_ThreadSafe ToThreadSafe() => new(this);
	}

	// Convertido en struct para usarlo en hilos (Jobs)
	public readonly struct TerrainParams_ThreadSafe: ITerrainParams
	{
		public readonly float heightScale;
		public readonly int lod;

		public TerrainParams_ThreadSafe(TerrainParams terrainParams)
		: this(terrainParams.HeightScale, terrainParams.LOD) { }
		
		public TerrainParams_ThreadSafe(float heightScale = 100, int lod = 0)
		{
			this.heightScale = heightScale;
			this.lod = lod;
		}


		public float HeightScale => heightScale;
		public int LOD => lod;
		
		public int LODIndex => (int)Mathf.Log(lod, 2);
	}
}
