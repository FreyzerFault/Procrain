using System.Collections.Generic;
using UnityEngine;

// TODO: Aplicar al MapManager para modificar el shader del material usado.
// TODO: Crear un editor para modificar las regiones de altura y que se apliquen tanto al gradiente del mapa como al shader.
namespace Procrain.Regions
{

	[CreateAssetMenu(fileName = "Map Height Regions", menuName = "Procrain/Map Height Regions")]
	public class MapHeightRegions : ScriptableObject
	{
		private static readonly int MaxHeightProp = Shader.PropertyToID("Max Height");
		private readonly Material material;

		private readonly Dictionary<RegionType, Region> regions = new()
		{
			{ RegionType.Water, new Region() },
			{ RegionType.Sand, new Region() },
			{ RegionType.Grass, new Region() },
			{ RegionType.Rock, new Region() },
			{ RegionType.Snow, new Region() }
		};

		public MapHeightRegions(Material material) => this.material = material;

		public float MaxHeight
		{
			get => material.GetFloat(MaxHeightProp);
			set => material.SetFloat(MaxHeightProp, value);
		}

		public Region GetRegion(RegionType regionType) => regions[regionType];
		public Region SetRegion(RegionType regionType, Region region) => regions[regionType] = region;

		public void SetShaderValues()
		{
			foreach ((RegionType regionType, Region region) in regions) SetShaderRegionValues(region);
		}

		private void SetShaderRegionValues(Region region)
		{
			material.SetFloat(region.HeigthProp, region.height);
			material.SetFloat(region.BlendProp, region.blendFactor);
			material.SetFloat(region.DensityProp, region.density);
			material.SetTexture(region.TextureProp, region.texture);
			material.SetTexture(region.NormalMapProp, region.normalMap);
			material.SetTexture(region.MaskProp, region.mask);
			material.SetTexture(region.DisplacementMapProp, region.displacementMap);
			material.SetTexture(region.OcclusionMapProp, region.occlusionMap);
			material.SetTexture(region.RoughnessMapProp, region.roughnessMap);
		}
	}
}
