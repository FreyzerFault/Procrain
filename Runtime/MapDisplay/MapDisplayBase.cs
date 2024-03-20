using System.Collections;
using MapGeneration;
using Noise;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Utils;
using Utils.Threading;

namespace MapDisplay
{
    public abstract class MapDisplayBase : MonoBehaviour
    {
        public bool autoUpdate = true;
        public bool debugTimer = true;

        public bool paralelized;

        [Space] public TerrainSettingsSo terrainSettingsSo;

        protected HeightMap heightMap;

        protected IHeightMap HeightMap => paralelized ? heightMapThreadSafe : heightMap;

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
            SubscribeToValuesUpdated();
        }

        protected virtual void OnDestroy()
        {
            StopAllCoroutines();

            if (!mapJobHandle.IsCompleted)
                mapJobHandle.Complete();

            if (terrainSettingsSo == null) return;
            terrainSettingsSo.onValuesUpdated -= OnValuesUpdated;

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

            terrainSettingsSo.onValuesUpdated -= OnValuesUpdated;
            if (autoUpdate) terrainSettingsSo.onValuesUpdated += OnValuesUpdated;
        }

        public void OnValuesUpdated()
        {
            if (!autoUpdate) return;

            // Solo regenera el mapa si ya se habia generado por 1º vez
            if (!HeightMap.IsEmpty) BuildMap();

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
            heightMap = HeightMapGenerator.CreatePerlinNoiseHeightMap(NoiseParams, HeightCurve);

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