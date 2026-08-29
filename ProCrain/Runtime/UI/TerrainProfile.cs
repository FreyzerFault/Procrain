using UnityEngine;

namespace Procrain
{
    [RequireComponent(typeof(RectTransform), typeof(LineRenderer))]
    public class TerrainProfile : MonoBehaviour
    {
        [SerializeField] private PathControllerSO _pathController;
        private LineRenderer _lineRenderer;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            _pathController.OnStartChanged += HandlePathChanged;
            _pathController.OnEndChanged += HandlePathChanged;
        }

        private void HandlePathChanged() => UpdateLine(_pathController.TerrainProfile);

        /// <summary>
        ///     Modifica la linea de la vuelta ciclista para visualizarla de perfil.
        ///     Como esta en un plano siempre podemos rotar ese plano y dejarlo en Z = 0
        /// </summary>
        public void UpdateLine(Vector3[] points)
        {
            if (points.Length == 0)
            {
                _lineRenderer.positionCount = 0;
                return;
            }

            // Para verla de perfil hay que hacer una Rotacion Inversa en el eje Y para poner todos los puntos en Z = 0
            Vector3 dir = (points[1] - points[0]).normalized;
            float angle = Mathf.Asin(dir.z);
            if (dir.x <= 0) angle = -angle + Mathf.PI;
            Quaternion rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

            // Calculamos la longitud que ocupa si contar la altura
            Vector3 initToEnd = points[^1] - points[0];
            initToEnd.y = 0;
            float width = initToEnd.magnitude;

            Vector3 orig = points[0];

            for (var i = 0; i < points.Length; i++)
            {
                // Lo devolvermos primero a su origen, lo rotamos para dejarlo en Z = 0 y lo movemos la mitad de su anchura para centrarlo
                points[i] = rotation * (points[i] - orig) - Vector3.right * width / 2;

                // Lo colocamos en panel donde lo visualizamos
                points[i] = _lineRenderer.GetComponent<RectTransform>().TransformPoint(points[i]);
            }

            _lineRenderer.positionCount = points.Length;
            _lineRenderer.SetPositions(points);
        }
    }
}
