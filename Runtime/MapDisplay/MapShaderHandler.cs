using System.Collections.Generic;
using UnityEngine;

namespace Procrain.MapDisplay
{
    public enum RegionType { Water, Sand, Grass, Rock, Snow }

    public struct Region
    {
        private const string HeightPropSufix = "Height";
        private const string BlendPropSufix = "Blend";
        private const string DensityPropSufix = "Density";
        private const string TexturePropSufix = "Texture";
        private const string NormalMapPropSufix = "NormalMap";
        private const string MaskPropSufix = "Mask";
        private const string DisplacementMapPropSufix = "DisplacementMap";
        private const string OclussionMapPropSufix = "OcclusionMap";
        private const string RoughnessMapPropSufix = "RoughnessMap";

        private RegionType type;
        private readonly string Name => type.ToString();

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
    }

    public class MapShaderHandler
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

        public MapShaderHandler(Material material) => this.material = material;

        public float MaxHeight
        {
            get => material.GetFloat(MaxHeightProp);
            set => material.SetFloat(MaxHeightProp, value);
        }

        public Region GetRegion(RegionType regionType) => regions[regionType];

        public void SetShaderValues()
        {
            foreach (var (regionType, region) in regions) SetShaderRegionValues(region);
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