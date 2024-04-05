using System.Collections;
using DavidUtils.DebugUtils;
using DavidUtils.ThreadingUtils;
using Map;
using Procrain.MapGeneration;
using Procrain.Noise;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.MapDisplay
{
    public abstract class MapDisplayBase : MonoBehaviour
    {
        public bool autoUpdate = true;
        public bool debugTimer = true;
        public bool generateMapOnStart = true;

        public bool paralelized;

        [Space] public TerrainSettingsSo terrainSettingsSo;

        protected HeightMap map;
        protected HeightMap Map
        {
            get => MapManager.Instance == null ? map : MapManager.Instance.heightMap;
            set
            {
                map = value;
                if (MapManager.Instance != null) MapManager.Instance.heightMap = value;
            }
        }

        // TODO - Mover el HeigthMapThreadSafe al MapManager, que tenga la responsabilidad de almacenarlo
        // Para que se comparta entre toda la escena
        protected IHeightMap HeightMap => paralelized ? heightMapThreadSafe : Map;

        public virtual PerlinNoiseParams NoiseParams
        {
            get => terrainSettingsSo.NoiseParams;
            set => terrainSettingsSo.NoiseParams = value;
        }

        public AnimationCurve HeightCurve
        {
            get => terrainSettingsSo.HeightCurve;
            set
            {
                terrainSettingsSo.HeightCurve = value;
                UpdateHeightCurveThreadSafe();
            }
        }

        public float HeightMultiplier => terrainSettingsSo.HeightMultiplier;

        public virtual int LOD
        {
            get => terrainSettingsSo.LOD;
            set => terrainSettingsSo.LOD = value;
        }

        public float2 Offset
        {
            get => terrainSettingsSo.Offset;
            set => terrainSettingsSo.Offset = value;
        }

        public uint Seed
        {
            get => terrainSettingsSo.Seed;
            set => terrainSettingsSo.Seed = value;
        }

        protected virtual void Start()
        {
            if (Map == null || Map.IsEmpty) BuildMap();
            SubscribeToValuesUpdated();
        }

        protected virtual void OnDestroy()
        {
            StopAllCoroutines();

            if (!mapJobHandle.IsCompleted) mapJobHandle.Complete();

            if (terrainSettingsSo == null) return;
            terrainSettingsSo.ValuesUpdated -= OnValuesUpdated;

            heightMapThreadSafe.Dispose();
            heightCurveThreadSafe.Dispose();
        }

        protected virtual void OnValidate()
        {
            if (!autoUpdate) return;
            SubscribeToValuesUpdated();

            // OnValuesUpdated();
        }

        public void SubscribeToValuesUpdated()
        {
            if (terrainSettingsSo == null) return;

            terrainSettingsSo.ValuesUpdated -= OnValuesUpdated;
            if (autoUpdate) terrainSettingsSo.ValuesUpdated += OnValuesUpdated;
        }

        public void OnValuesUpdated()
        {
            if (!autoUpdate) return;

            BuildMap();

            // Actualiza la curva de altura para paralelizacion. La normal podria haber cambiado
            if (paralelized) UpdateHeightCurveThreadSafe();
        }

        public abstract void DisplayMap();

        public virtual void BuildMap()
        {
            if (paralelized)
            {
                StartCoroutine(BuildMapParallelizedCoroutine());
            }
            else
            {
                var size = NoiseParams.size;
                DebugTimer.DebugTime(BuildHeightMap, $"Time to build HeightMap {size} x {size}");
                DebugTimer.DebugTime(DisplayMap, $"Time to display map {size} x {size}");
            }
        }

        protected virtual void BuildHeightMap() =>
            Map = HeightMapGenerator.CreatePerlinNoiseHeightMap(NoiseParams, HeightCurve);

        public virtual void ResetSeed() => Seed = PerlinNoise.GenerateRandomSeed();

        #region THREADING

        protected HeightMapThreadSafe heightMapThreadSafe;
        protected JobHandle mapJobHandle;

        private SampledAnimationCurve heightCurveThreadSafe;
        private readonly int heightCurveSamples = 100;

        public void UpdateHeightCurveThreadSafe()
        {
            if (HeightCurve == null) return;
            heightCurveThreadSafe.Sample(HeightCurve, heightCurveSamples);
        }

        protected virtual IEnumerator BuildMapParallelizedCoroutine()
        {
            yield return BuildHeightMapParallelizedCoroutine();
            DisplayMap();
        }

        protected IEnumerator BuildHeightMapParallelizedCoroutine()
        {
            var time = Time.time;

            heightMapThreadSafe = new HeightMapThreadSafe(NoiseParams.SampleSize, NoiseParams.seed);
            if (heightCurveThreadSafe.IsEmpty) UpdateHeightCurveThreadSafe();

            if (!mapJobHandle.IsCompleted) mapJobHandle.Complete();

            // Wait for JobHandle to END
            mapJobHandle = new HeightMapGeneratorThreadSafe.PerlinNoiseMapBuilderJob
            {
                noiseParams = NoiseParams,
                heightMap = heightMapThreadSafe,
                heightCurve = heightCurveThreadSafe
            }.Schedule();

            yield return new WaitUntil(() => mapJobHandle.IsCompleted);

            mapJobHandle.Complete();

            Debug.Log($"{(Time.time - time) * 1000:F1} ms para generar el mapa");
        }

        #endregion
    }
}