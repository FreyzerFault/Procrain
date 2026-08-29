using UnityEngine;
using UnityEngine.EventSystems;

namespace Procrain.Minimap
{
    [RequireComponent(typeof(Minimap))]
    public class MinimapPathBuilder : MonoBehaviour
    {
        private Minimap _minimap;
        
        [SerializeField]private PathControllerSO pathController;

        [SerializeField] private Color startPointColor = Color.red;
        [SerializeField] private Color endPointColor = Color.green;
        
        private GameObject _startPointObj;
        private GameObject _endPointObj;

        private void Awake()
        {
            _minimap  = GetComponent<Minimap>();
        }

        private void OnEnable() => _minimap.OnMapClick += HandleMapClick;
        private void OnDisable() => _minimap.OnMapClick -= HandleMapClick;
        

        private void HandleMapClick(Vector3 mousePos, PointerEventData.InputButton inputButton)
        {
            bool placeStart = inputButton == PointerEventData.InputButton.Left;
            bool placeEnd = inputButton == PointerEventData.InputButton.Right;

            if (!placeStart && !placeEnd) return;
            
            Vector2 point = _minimap.GetMousePoint2D(mousePos);

            // Substitute Sprite with a new in the position selected
            GameObject pointSprite = placeStart ? _startPointObj : _endPointObj;
            
            Destroy(pointSprite);
            
            pointSprite = _minimap.DrawPointInMousePosition(placeStart ? startPointColor : endPointColor);
            pointSprite.name = placeStart ? "Start Point" : "End Point";
            
            if (placeStart)
                pathController.StartPoint2D = point;
            else
                pathController.EndPoint2D = point;
        }
    }
}
