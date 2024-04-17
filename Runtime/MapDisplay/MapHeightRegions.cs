using System.Collections.Generic;
using UnityEngine;

// TODO: Aplicar al MapManager para modificar el shader del material usado.
// TODO: Crear un editor para modificar las regiones de altura y que se apliquen tanto al gradiente del mapa como al shader.
namespace Procrain.MapDisplay
{
	public enum RegionType { Water, Sand, Grass, Rock, Snow }

	public struct Region
	{
		private static readonly string HeightPropSufix = "Height";
		private static readonly string BlendPropSufix = "Blend";
		private static readonly string DensityPropSufix = "Density";
		private static readonly string TexturePropSufix = "Texture";
		private static readonly string NormalMapPropSufix = "NormalMap";
		private static readonly string MaskPropSufix = "Mask";
		private static readonly string DisplacementMapPropSufix = "DisplacementMap";
		private static readonly string OclussionMapPropSufix = "OcclusionMap";
		private static readonly string RoughnessMapPropSufix = "RoughnessMap";

		public RegionType type;
		public readonly string Name => type.ToString();

		public float height;
		public float blendFactor;
		public float density;

		public Texture2D texture;
		public Texture2D normalMap;
		public Texture2D mask;
		public Texture2D displacementMap;
		public Texture2D occlusionMap;
		public Texture2D roughnessMap;

		public string HeigthProp => Name + HeightPropSufix;
		public string BlendProp => Name + BlendPropSufix;
		public string DensityProp => Name + DensityPropSufix;
		public string TextureProp => Name + TexturePropSufix;
		public string NormalMapProp => Name + NormalMapPropSufix;
		public string MaskProp => Name + MaskPropSufix;
		public string DisplacementMapProp => Name + DisplacementMapPropSufix;
		public string OcclusionMapProp => Name + OclussionMapPropSufix;
		public string RoughnessMapProp => Name + RoughnessMapPropSufix;

		// Default Constructor
		// Texture Order: { Diffuse, Normal, Mask, Displacement, Occlusion, Roughness }
		public Region(RegionType type, float height, float blendFactor, float density, Texture2D[] textures)
		{
			this.type = type;
			this.height = height;
			this.blendFactor = blendFactor;
			this.density = density;
			texture = textures[0];
			normalMap = textures[1];
			mask = textures[2];
			displacementMap = textures[3];
			occlusionMap = textures[4];
			roughnessMap = textures[5];
		}

		public override string ToString() =>
			$"Region [{Name}]: {'{'} Max Height: {height}, Blend Factor: {blendFactor}, Density: {density} {'}'}";
	}

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
