using System.Collections.Generic;
using System.Linq;
using Procrain.Mesh;
using UnityEngine;

namespace Procrain
{
    /// <summary>
    /// Renderer de Mapa general.
    /// Heredar para implementar sobre qué componente de Unity y cómo renderizar el Mapa.
    /// </summary>
    /// <remarks>
    /// Los datos generados se almacenan en un MapData que, al ser ScriptableObject, se puede compartir
    /// con otros objetos, como otro MapDisplay.
    /// Escucha a los eventos de actualización de datos del MapData para ejecutar DisplayMap().
    /// DisplayMap() actualiza los elementos del objeto que renderizan el mapa, como la Textura y la Malla.
    /// Para reconstruir el mapa ejecuta RebuildMap()
    /// </remarks>
    /// <example>
    ///     Por ejemplo:
    ///         MapDisplayInMesh para el terreno
    ///         MapDisplayInImage para mostrar un minimapa en UI
    ///         Varias clases que hereden de MapDisplayInMesh con distintas formas de generar la Malla (Rect, TIN, Hex...)
    /// </example> 
    [ExecuteAlways]
    public abstract class MapDisplayBase : MonoBehaviour
    {
        public MapData mapData;
        
        public IHeightMap HeightMap => mapData.heightMap;
        public Texture2D Texture => mapData.texture;
        public virtual IMeshData MeshData => mapData.MeshData;
        public IMeshData MeshDataByLOD(int lodIndex) => mapData.MeshDataByLOD(lodIndex);
        public IMeshData MeshDataByLOD_Pow2(int lod) => mapData.MeshDataByLOD(lod);
        public IMeshData[] MeshData_AllLods => mapData.MeshData_AllLods;
        
        [SerializeField] protected Material material;
        
        protected virtual void Start()
        {
            SubEvents();
            DisplayMap();
        }

        protected virtual void OnDestroy() => UnsubEvents();

        
        #region LISTENING MAP DATA EVENTS

        private void SubEvents()
        {
            if (!mapData) return;
            mapData.OnHeightMapUpdated += HandleHeightMapUpdated;
            mapData.OnTextureUpdated += HandleTextureUpdated;
            mapData.OnMeshDataUpdated += HandleMeshDataUpdated;
        }

        private void UnsubEvents()
        {
            if (!mapData) return;
            mapData.OnHeightMapUpdated -= HandleHeightMapUpdated;
            mapData.OnTextureUpdated -= HandleTextureUpdated;
            mapData.OnMeshDataUpdated -= HandleMeshDataUpdated;
        }
        
        /// Track mapData changes and re-subscribe event handlers when mapData reference changes
        private MapData _previousMapData;
        protected virtual void OnValidate()
        {
            if (_previousMapData == mapData) return;
            
            UnsubEvents();
            _previousMapData = mapData;
            SubEvents();
        }
        
        protected virtual void HandleHeightMapUpdated(IHeightMap heightMap) => DisplayMap();
        protected virtual void HandleTextureUpdated(Texture2D texture) => DisplayMap();
        protected virtual void HandleMeshDataUpdated(IMeshData meshData) => DisplayMap();

        #endregion
        
        
        /// Rebuild All Data Entirely
        [ContextMenu("Rebuild Map Data")]
        public void RebuildMapData() => mapData.BuildAllData();


        /// Force to Apply all the Data in the Renderer you use
        /// This is what you have to implement in heirs
        public virtual void DisplayMap() { }
        
        
        #region THREADING
        
		
		#region MESH THREADING
        
        // TODO Creo que todo esto NO es necesario mmmm....

		private readonly Dictionary<int, MeshData_ThreadSafe> _meshDataByLoD_ThreadSafe = new();
		public MeshData_ThreadSafe MeshData_ThreadSafe => (MeshData_ThreadSafe)MeshData;
		public MeshData_ThreadSafe MeshDataByLOD_ThreadSafe(int lodIndex) => (MeshData_ThreadSafe)MeshDataByLOD(lodIndex);
		public MeshData_ThreadSafe MeshDataByLOD_Pow2_ThreadSafe(int lod) => (MeshData_ThreadSafe)MeshDataByLOD(lod);
		public MeshData_ThreadSafe[] MeshData_ThreadSafe_AllLods => mapData.MeshData_AllLods.Cast<MeshData_ThreadSafe>().ToArray();

		#endregion

		
		#endregion
    }
}
