using Procrain.Geometry.Mesh;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Procrain.MapDisplay
{
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
	public class MapDisplayInMesh : MapDisplayBase
	{
		protected MeshCollider meshCollider;
		protected MeshFilter meshFilter;
		protected MeshRenderer meshRenderer;

		protected virtual void Awake()
		{
			meshFilter = GetComponent<MeshFilter>();
			meshCollider = GetComponent<MeshCollider>();
			meshRenderer = GetComponent<MeshRenderer>();
		}
		
		protected override void HandleTextureUpdated(Texture2D texture) => ApplyTexture(Texture);
		protected override void HandleMeshDataUpdated(IMeshData meshData) => ApplyMeshData(meshData);

		[ContextMenu("Update Rendered Mesh")]
		public override void DisplayMap()
		{
			meshRenderer.material = material;
			
			if (Texture) ApplyTexture(Texture);
			
			if (MeshData != null) ApplyMeshData(MeshData);
		}

		protected void ApplyTexture(Texture2D texture)
		{
			texture.Apply();
			meshRenderer.sharedMaterial.mainTexture = texture;
		}

		protected void ApplyMeshData(IMeshData meshData)
		{
			Mesh mesh = meshData.CreateMesh();

			meshFilter.sharedMesh = mesh;
			meshCollider.sharedMesh = mesh;
		}

		
		#region DEBUG

		private bool _drawNormals;

		protected virtual void OnDrawGizmos()
		{
			if (_drawNormals)
				OnDrawGizmosNormals();
		}

		private void OnDrawGizmosNormals()
		{
			if (!meshFilter || !_drawNormals) return;

			Mesh mesh = meshFilter.sharedMesh;

			if (mesh == null) return;

			for (var i = 0; i < mesh.vertices.Length; i++)
			{
				Gizmos.color = Color.yellow;
				Gizmos.DrawLine(mesh.vertices[i], mesh.vertices[i] + mesh.normals[i] * 10);
			}
		}

		#endregion
	}
}
