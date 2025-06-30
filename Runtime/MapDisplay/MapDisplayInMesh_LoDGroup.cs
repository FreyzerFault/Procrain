using System.Linq;
using Procrain.Core;
using Procrain.Geometry.Mesh;
using UnityEngine;
using UnityEngine.Serialization;

namespace Procrain.MapDisplay
{
	// Actualiza el LOD del Mapa en Tiempo Real dependiendo de la distancia del Jugador
	[RequireComponent(typeof(LODGroup))]
	public class MapDisplayInMesh_LoDGroup: MapDisplayBase
	{
		private static readonly int UsePrebakedTexture = Shader.PropertyToID("_usePrebakedTexture");
		
		protected LODGroup lodGroup;
		
		protected virtual void Awake() => lodGroup = GetComponent<LODGroup>();

		protected override void Start()
		{
			MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
			lodGroup.SetLODs(new LOD[]
			{
				new(0.8f, new Renderer[] { renderers[0] }),
				new(0.6f, new Renderer[] { renderers[1] }),
				new(0.4f, new Renderer[] { renderers[2] }),
				new(0.2f, new Renderer[] { renderers[3] }),
				new(0.05f, new Renderer[] { renderers[4] }),
			});
		}

		protected override void HandleTextureUpdated(Texture2D texture) => ApplyTexture(Texture);
		protected override void HandleMeshDataUpdated(IMeshData meshData) => ApplyAllMeshes();


		[ContextMenu("Update All LOD Meshes")]
		public override void DisplayMap()
		{
			ApplyMaterial(material);
			
			if (Texture) ApplyTexture(Texture);
			
			ApplyAllMeshes();
		}

		protected void ApplyMaterial(Material mat)
		{
			foreach (Renderer r in lodGroup.GetLODs().Select(l => l.renderers).SelectMany(r => r)) 
				r.material = mat;
		}

		protected void ApplyTexture(Texture2D texture)
		{
			foreach (LOD lod in lodGroup.GetLODs()) 
				lod.renderers[0].sharedMaterial.mainTexture = texture;
		}

		protected void ApplyAllMeshes()
		{
			LOD[] lods = lodGroup.GetLODs();
			
			for (var i = 0; i < lodGroup.lodCount; i++)
			{
				IMeshData meshData = GetMeshData(i);
				lods[i].renderers[0].GetComponent<MeshFilter>().mesh = meshData.CreateMesh();
			}
		}

		protected virtual IMeshData GetMeshData(int lod) => mapData.MeshDataByLOD(lod);
	}
}
