using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Procrain.UI.Minimap
{
    [RequireComponent(typeof(RectTransform))]
    public class Minimap : MonoBehaviour, IPointerClickHandler
    {
        public Camera renderCamera;

        public GameObject pinPoint;

        private Bounds mapBounds;

        public Action<Vector3, PointerEventData.InputButton> onMapClick;
        private RectTransform rectTransform;

        private void Awake()
        {
            renderCamera.cullingMask = LayerMask.NameToLayer("UI");
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            UpdateMapBounds();
        }


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
        public void OnPointerClick(PointerEventData eventData)
        {
            if (MouseInMap()) onMapClick?.Invoke(Input.mousePosition, eventData.button);
        }

        private void UpdateMapBounds()
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var center = (corners[0] + corners[2]) / 2;
            mapBounds = new Bounds(center, (center - corners[0]) * 2);
        }

        public bool MouseInMap() => mapBounds.Contains(Input.mousePosition);

        public Vector2 GetScreenSpaceMousePoint(Vector3 mousePoint)
        {
            var localPos = mousePoint - mapBounds.min;

            return new Vector2(
                Mathf.InverseLerp(0, mapBounds.size.x, localPos.x) * renderCamera.pixelWidth,
                Mathf.InverseLerp(0, mapBounds.size.y, localPos.y) * renderCamera.pixelHeight
            );
        }

        public GameObject DrawPointInMousePosition(Color color)
        {
            var point = Instantiate(pinPoint, Input.mousePosition, Quaternion.identity, transform);
            point.GetComponent<Image>().color = color;
            return point;
        }
    }
}