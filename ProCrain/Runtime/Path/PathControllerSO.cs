using System;
using UnityEngine;

namespace Procrain
{
    [CreateAssetMenu(fileName = "Path Controller", menuName = "Procrain/Path Controller")]
    public class PathControllerSO : ScriptableObject
    {
        public event Action OnStartChanged;
        public event Action OnEndChanged;

        [SerializeField] private Vector3 startPoint;
        [SerializeField] private Vector3 endPoint;

        private Vector3[] _terrainProfile;
        
        public Vector3 StartPoint
        {
            get => startPoint;
            set
            {
                startPoint = value;
                OnStartChanged?.Invoke();
            }
        }
        
        public Vector3 EndPoint
        {
            get => endPoint;
            set
            {
                endPoint = value;
                OnEndChanged?.Invoke();
            }
        }

        public Vector2 StartPoint2D
        {
            get => new(startPoint.x, startPoint.z);
            set =>
                StartPoint = new Vector3(value.x, startPoint.z, value.y)
                {
                    y = Terrain.activeTerrain.GetInterpolatedHeight(value)
                };
        }

        public Vector2 EndPoint2D
        {
            get => new(endPoint.x, endPoint.z);
            set =>
                EndPoint = new Vector3(value.x, endPoint.z, value.y)
                {
                    y = Terrain.activeTerrain.GetInterpolatedHeight(value)
                };
        }

        public Vector3[] TerrainProfile => _terrainProfile ??= BuildTerrainProfile();

        private Vector3[] BuildTerrainProfile() => _terrainProfile = Terrain.activeTerrain.ProjectPathToTerrain(new[] { startPoint, endPoint });


        #region DEBUG

        private void OnDrawGizmosPath()
        {
            // Extremos de la linea
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(startPoint, 1);
            Gizmos.DrawSphere(endPoint, 1);

            // Punto de control de la linea
            Gizmos.color = Color.blue;
            foreach (Vector3 intersection in TerrainProfile)
                Gizmos.DrawSphere(intersection, 1);
        }

        #endregion
    }
}
