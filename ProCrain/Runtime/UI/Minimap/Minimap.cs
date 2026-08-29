using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Procrain.Minimap
{
    [RequireComponent(typeof(RectTransform))]
    public class Minimap : MonoBehaviour, IPointerClickHandler
    {
        public Camera renderCamera;
        public GameObject pinPointPrefab;

        private Bounds _mapBounds;
        private RectTransform _rectTransform;

        public event Action<Vector3, PointerEventData.InputButton> OnMapClick;

        private void Awake()
        {
            renderCamera.cullingMask = LayerMask.NameToLayer("Terrain");
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            UpdateMapBounds();
        }


        public void OnPointerClick(PointerEventData eventData)
        {
            if (MouseInMap()) OnMapClick?.Invoke(Input.mousePosition, eventData.button);
        }

        private void UpdateMapBounds()
        {
            var corners = new Vector3[4];
            _rectTransform.GetWorldCorners(corners);

            Vector3 center = (corners[0] + corners[2]) / 2;
            _mapBounds = new Bounds(center, (center - corners[0]) * 2);
        }

        public bool MouseInMap() => _mapBounds.Contains(Input.mousePosition);

        public Vector2 GetScreenSpaceMousePoint(Vector3 mousePoint)
        {
            Vector3 localPos = mousePoint - _mapBounds.min;

            return new Vector2(
                Mathf.InverseLerp(0, _mapBounds.size.x, localPos.x) * renderCamera.pixelWidth,
                Mathf.InverseLerp(0, _mapBounds.size.y, localPos.y) * renderCamera.pixelHeight
            );
        }
        
        
        /// <summary>
        ///     Punto del mundo en 2D (X,Z) al que apunta el mouse
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Necesita que haya un Camera Manager y una Camara con Indice 1 (Cenital)</exception>
        public Vector2 GetMousePoint2D(Vector3 mousePoint)
        {
            // Coordenada del Raton relativa a la camara del minimapa
            Vector2 screenPoint = GetScreenSpaceMousePoint(mousePoint);

            // La Z sera la distancia desde la camara al terreno
            Vector3 screenPoint3D = new(
                screenPoint.x,
                screenPoint.y,
                renderCamera.WorldToScreenPoint(transform.position).z
            );

            // Lo cambiamos a Coordenadas del mundo y en 2D
            Vector3 worldPoint = renderCamera.ScreenToWorldPoint(screenPoint3D);
            Vector2 worldPoint2D = new(worldPoint.x, worldPoint.z);
            return worldPoint2D;
        }


        #region DRAWING POINTS

        public GameObject DrawPointInMousePosition(Color color)
        {
            GameObject point = Instantiate(pinPointPrefab, Input.mousePosition, Quaternion.identity, transform);
            point.GetComponent<Image>().color = color;
            return point;
        }

        #endregion
        
        
        // private void OnDrawGizmos()
        // {
        //     rectTransform = GetComponent<RectTransform>();
        //     
        //     Vector3[] corners = new Vector3[4];
        //     rectTransform.GetWorldCorners(corners);
        //         
        //     Debug.Log(corners.Length);
        //
        //     Vector3 center = (corners[0] + corners[2]) / 2;
        //     Bounds bounds = new Bounds(center, (center - corners[0]) * 2);
        //     
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawSphere(Input.mousePosition, 5);
        //     
        //     Gizmos.DrawLine(mapBounds.min, mapBounds.min + (Input.mousePosition - mapBounds.min));
        //     
        //     Gizmos.DrawSphere(center, 5);
        // }
    }
}
