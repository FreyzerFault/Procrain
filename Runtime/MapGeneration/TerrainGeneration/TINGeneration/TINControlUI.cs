using UnityEngine;
using UnityEngine.UI;

namespace Procrain.Runtime.MapGeneration.TerrainGeneration.TINGeneration
{
    public class TinControlUI : MonoBehaviour
    {
        public Slider errorToleranceSlider;
        public Text errorToleranceLabel;
        public Slider maxPointsPerCycleSlider;
        public Text maxPointsPerCycleLabel;

        public Slider progressBarSlider;
        public Text progressBarLabel;

        public Text time;
        public Text iterations;
        public Text triangles;
        public Text vertices;

        public Button buildAnimationButton;

        public LineRenderer lineDisplay;

        private ColorBlock defaultColorBlock;
        private ColorBlock stopColorBlock;
        private TinVisualizer tinVisualizer;

        private void Awake()
        {
            tinVisualizer = GetComponent<TinVisualizer>();

            defaultColorBlock = buildAnimationButton.colors;
            stopColorBlock = defaultColorBlock;
            stopColorBlock.normalColor = Color.red;
            stopColorBlock.selectedColor = Color.red;
            stopColorBlock.highlightedColor = Color.red;
            stopColorBlock.pressedColor = Color.red;
        }

        private void Start() => InitializeParams();

        private void Update()
        {
            if (tinVisualizer.isRunning)
            {
                buildAnimationButton.GetComponentInChildren<Text>().text = "STOP";
                buildAnimationButton.GetComponentInChildren<Text>().color = Color.white;
                buildAnimationButton.colors = stopColorBlock;
            }
            else
            {
                buildAnimationButton.GetComponentInChildren<Text>().text = "Build TIN";
                buildAnimationButton.GetComponentInChildren<Text>().color = Color.black;
                buildAnimationButton.colors = defaultColorBlock;
            }
        }

        private void InitializeParams()
        {
            OnErrorToleranceSliderChange();
            OnPointsPerCicleSliderChange();
            time.text = "00:00";
            iterations.text = "0 iterations";
            vertices.text = "4 vertices";
            triangles.text = "2 triangles";
        }

        private void OnErrorToleranceSliderChange()
        {
            tinVisualizer.errorTolerance = errorToleranceSlider.value;
            tinVisualizer.ResetTin();
            errorToleranceLabel.text = "Error Tolerance: " + errorToleranceSlider.value.ToString("G3");
        }

        private void OnPointsPerCicleSliderChange()
        {
            tinVisualizer.maxPointsPerCycle = (int)maxPointsPerCycleSlider.value;
            tinVisualizer.ResetTin();
            maxPointsPerCycleLabel.text = "Max Points Per Cycle: " + tinVisualizer.maxPointsPerCycle;
        }

        public void ResetSeed()
        {
            tinVisualizer.ResetRandomSeed();
            tinVisualizer.ResetTin();
        }

        // Start/Stop Progressive Generation
        public void ToggleBuildingProcess() => tinVisualizer.PlayPauseProgressiveGeneration();

        // Do 1 Iteration of the TIN Generation
        public void RunOneIteration() => tinVisualizer.RunIteration();

        /// <summary>
        ///     Modifica la linea de la vuelta ciclista para visualizarla de perfil.
        ///     Como esta en un plano siempre podemos rotar ese plano y dejarlo en Z = 0
        /// </summary>
        public void UpdateLine(Vector3[] points)
        {
            if (points.Length == 0)
            {
                lineDisplay.positionCount = 0;
                return;
            }

            // Para verla de perfil hay que hacer una Rotacion Inversa en el eje Y para poner todos los puntos en Z = 0
            var dir = (points[1] - points[0]).normalized;
            var angle = Mathf.Asin(dir.z);
            if (dir.x <= 0)
                angle = -angle + Mathf.PI;
            var rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

            // Calculamos la longitud que ocupa si contar la altura
            var initToEnd = points[^1] - points[0];
            initToEnd.y = 0;
            var width = initToEnd.magnitude;

            var orig = points[0];

            for (var i = 0; i < points.Length; i++)
            {
                // Lo devolvermos primero a su origen, lo rotamos para dejarlo en Z = 0 y lo movemos la mitad de su anchura para centrarlo
                points[i] = rotation * (points[i] - orig) - Vector3.right * width / 2;

                // Lo colocamos en panel donde lo visualizamos
                points[i] = lineDisplay.GetComponent<RectTransform>().TransformPoint(points[i]);
            }

            lineDisplay.positionCount = points.Length;
            lineDisplay.SetPositions(points);
        }


        public void UpdateProgressBar(float progress)
        {
            progressBarSlider.value = progress;
            progressBarLabel.text = progress.ToString("P");
        }
    }
}