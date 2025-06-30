using UnityEngine;

namespace Procrain.MapDisplay.TIN
{
    public class MapDisplayInMesh_TIN: MapDisplayInMesh
    {
        
        [ContextMenu("Update Rendered Mesh")]
        public override void DisplayMap()
        {
            base.DisplayMap();
            
            meshRenderer.material = material;
			
            if (Texture) ApplyTexture(Texture);
			
            if (MeshData != null) ApplyMeshData(MeshData);
        }
    }
}
