using Procrain.Core;
using Procrain.MapGeneration.Mesh;
using UnityEngine;

namespace Procrain.MapDisplay
{
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
	public class MapDisplayInMesh : MapDisplayBase
	{
		public enum TextureMode
		{
			SetTexture,
			UseShader
		}

		public TextureMode textureMode = TextureMode.UseShader;

		public bool useLocalLoD;

		[Range(0, 16)] [SerializeField]
		protected int localLoD;
		protected int LoD
		{
			get => useLocalLoD ? localLoD : MapManager.Instance.TerrainSettings.LOD;
			set
			{
				if (LoD == value)
					return;

				if (useLocalLoD)
				{
					OnLocalLoDUpdate(value);
					localLoD = value;
				}
				else
				{
					MapManager.Instance.TerrainSettings.LOD = value;
				}
			}
		}

		private MeshCollider _meshCollider;
		private MeshFilter _meshFilter;
		private MeshRenderer _meshRenderer;

		protected virtual void Awake()
		{
			_meshFilter = GetComponent<MeshFilter>();
			_meshCollider = GetComponent<MeshCollider>();
			_meshRenderer = GetComponent<MeshRenderer>();
		}

		protected override void Start()
		{
			base.Start();

			IMeshData meshData = MapManager.Instance.GetMeshData(LoD);
			if (meshData != null)
				ApplyMeshData(LoD, meshData);

			Texture2D texture = MapManager.Instance.texture;
			if (texture != null)
				ApplyTexture(texture);
		}

		protected override void OnTextureUpdated(Texture2D texture) => ApplyTexture(Texture);
		protected override void OnMeshDataUpdated(int lod, IMeshData meshData) => ApplyMeshData(lod, meshData);

		public override void DisplayMap()
		{
			if (Texture != null)
				ApplyTexture(Texture);

			if (MeshData != null)
				ApplyMeshData(LoD, MeshData);
		}

		protected void ApplyTexture(Texture2D texture)
		{
			if (textureMode != TextureMode.SetTexture)
				return;
			texture.Apply();
			_meshRenderer.sharedMaterial.mainTexture = texture;
		}

		protected void ApplyMeshData(int lod, IMeshData meshData)
		{
			if (useLocalLoD && localLoD != lod) return;

			Mesh mesh = meshData.CreateMesh();

			_meshFilter.sharedMesh = mesh;
			_meshCollider.sharedMesh = mesh;
		}

		protected virtual void OnLocalLoDUpdate(int newLod)
		{
			IMeshData meshData = MapManager.Instance.GetMeshData(LoD);
			if (meshData != null) ApplyMeshData(LoD, meshData);
		}
	}
}
