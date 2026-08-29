using Procrain.TIN;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Procrain
{
    public class TinControlUI : MonoBehaviour
    {
        [SerializeField] private TinVisualizer tinVisualizer;
        
        [SerializeField] private Slider errorToleranceSlider;
        [SerializeField] private Text errorToleranceLabel;
        [SerializeField] private Slider maxPointsPerCycleSlider;
        [SerializeField] private TMP_Text maxPointsPerCycleLabel;

        [SerializeField] private Slider progressBarSlider;
        [SerializeField] private TMP_Text progressBarLabel;

        [SerializeField] private TMP_Text time;
        [SerializeField] private TMP_Text iterations;
        [SerializeField] private TMP_Text triangles;
        [SerializeField] private TMP_Text vertices;

        [SerializeField] private Button buildAnimationButton;
        private TMP_Text _buildAnimButtonText;

        [SerializeField] private LineRenderer lineDisplay;

        private ColorBlock _defaultColorBlock;
        private ColorBlock _stopColorBlock;

        private float _firstErrorFound;
        private float _maxPercentageReached;

        private void Awake()
        {
            tinVisualizer ??= FindAnyObjectByType<TinVisualizer>();
            
            _buildAnimButtonText = buildAnimationButton.GetComponentInChildren<TMP_Text>();

            // Setup Stop Colors for Buttons
            _defaultColorBlock = buildAnimationButton.colors;
            _stopColorBlock = _defaultColorBlock;
            _stopColorBlock.normalColor = Color.red;
            _stopColorBlock.selectedColor = Color.red;
            _stopColorBlock.highlightedColor = Color.red;
            _stopColorBlock.pressedColor = Color.red;
        }

        private void Start() => InitializeParams();

        private void OnEnable()
        {
            tinVisualizer.OnIterationEnd += HandleOnIterationEnd;
            tinVisualizer.OnGenerationEnd += HandleOnGenerationEnd;
        }

        private void OnDisable()
        {
            tinVisualizer.OnIterationEnd -= HandleOnIterationEnd;
            tinVisualizer.OnGenerationEnd -= HandleOnGenerationEnd;
        }

        private void Update() => UpdateBuildAnimButton();

        
        private void UpdateBuildAnimButton()
        {
            if (tinVisualizer.isRunning)
            {
                _buildAnimButtonText.text = "STOP";
                _buildAnimButtonText.color = Color.white;
                buildAnimationButton.colors = _stopColorBlock;
            }
            else
            {
                _buildAnimButtonText.text = "Build TIN";
                _buildAnimButtonText.color = Color.black;
                buildAnimationButton.colors = _defaultColorBlock;
            }
        }
        
        
        private void HandleOnGenerationEnd()
        {
            UpdateProgressBar(1);
        }

        private void HandleOnIterationEnd()
        {
            switch (tinVisualizer.fase)
            {
                case 0:
                    UpdateProgressBar(0);
                    _maxPercentageReached = 0;
                    return;
                // Guarda el primer punto de mayor error para comparar el progreso
                case 1:
                    _firstErrorFound = tinVisualizer.LastVertexError;
                    break;
            }

            if (tinVisualizer.LastAddedVertexCount <= 0) return;
            
            // Actualiza la Barra de Progreso con un valor entre 0% y 100%
            // 0% -> Error del Primer punto añadido
            // 100% -> Error maximo Tolerado

            float maxCurrentError = tinVisualizer.LastAddedVertexCount;
            float percentage = (maxCurrentError - tinVisualizer.errorTolerance) / _firstErrorFound;
            float inversePercentage = 1f - percentage;
            float progressValue = Mathf.Clamp(Mathf.Max(inversePercentage, _maxPercentageReached), 0, 1);
            _maxPercentageReached = Mathf.Max(progressValue, _maxPercentageReached);
            UpdateProgressBar(_maxPercentageReached);

            float mins = Mathf.FloorToInt(tinVisualizer.timeConsumed / 60);
            float secs = Mathf.FloorToInt(tinVisualizer.timeConsumed % 60);
            time.text = $"{mins}:{secs}";
            iterations.text = $"{tinVisualizer.fase} iterations";
            vertices.text = tinVisualizer.VertexCount + " vertices";
            triangles.text = tinVisualizer.TriCount + " triangles";
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
            errorToleranceLabel.text = "Error Tolerance: " + errorToleranceSlider.value.ToString("G3");
        }

        private void OnPointsPerCicleSliderChange()
        {
            tinVisualizer.maxPointsPerCycle = (int)maxPointsPerCycleSlider.value;
            maxPointsPerCycleLabel.text = "Max Points Per Cycle: " + tinVisualizer.maxPointsPerCycle;
        }

        public void ResetSeed()
        {
            tinVisualizer.ResetRandomSeed();
            tinVisualizer.ResetTin();
        }
        
        public void UpdateProgressBar(float progress)
        {
            progressBarSlider.value = progress;
            progressBarLabel.text = progress.ToString("P");
        }


        #region BUTTONS
        
        /// Start/Stop Progressive Generation
        public void ToggleBuildingProcess() => tinVisualizer.PlayPauseProgressiveGeneration();

        /// Do 1 Iteration of the TIN Generation
        public void RunOneIteration() => tinVisualizer.RunIteration();
        
        #endregion
    }
}
