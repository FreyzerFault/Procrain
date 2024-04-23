using System;
using System.Collections;
using System.Collections.Generic;
using DavidUtils;
using DavidUtils.DebugUtils;
using DavidUtils.ExtensionMethods;
using DavidUtils.PlayerControl;
using DavidUtils.ThreadingUtils;
using Procrain.Geometry;
using Procrain.MapGeneration;
using Procrain.MapGeneration.Mesh;
using Procrain.MapGeneration.Texture;
using Procrain.MapGeneration.TIN;
using Procrain.Noise;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Serialization;

namespace Procrain.Core
{
	public class MapManager : Singleton<MapManager>
	{
		public bool debugTimer = true;

		#region WATER

		[SerializeField] private GameObject water;
		public float WaterHeight => water == null ? 0 : water.transform.position.y;

		#endregion

		#region TERRAIN

		public static Terrain Terrain => Terrain.activeTerrain;
		public float TerrainWidth => Terrain.terrainData.size.x;
		public float TerrainHeight => Terrain.terrainData.size.z;

		#endregion

		#region PLAYER

		[SerializeField] public Player player;
		public Vector2 PlayerNormalizedPosition => Terrain.GetNormalizedPosition(player.Position);
		private float PlayerRotationAngle => player.Rotation.eulerAngles.y;
		public Quaternion PlayerRotationForUI => Quaternion.AngleAxis(90 + PlayerRotationAngle, Vector3.back);

		#endregion

		protected override void Awake()
		{
			base.Awake();

			player = FindObjectOfType<Player>(true);
			water = GameObject.FindGameObjectWithTag("Water");

			BuildMap();

			SubscribeToValuesUpdated();
		}

		private void OnDestroy()
		{
			// Cancel all processes and Threading JOBS
			StopAllCoroutines();
			if (!_heightMapJobHandle.IsCompleted) _heightMapJobHandle.Complete();
			if (!_textureJobHandle.IsCompleted) _textureJobHandle.Complete();

			// UNSUSCRIBE
			if (terrainSettings == null) return;
			terrainSettings.ValuesUpdated -= OnValuesUpdated;

			_heightMapThreadSafe.Dispose();
			_heightCurveThreadSafe.Dispose();
			_textureDataThreadSafe.Dispose();

			foreach (MeshData_ThreadSafe meshDataThreadSafe in _meshDataByLoD_ThreadSafe.Values)
				meshDataThreadSafe.Dispose();
		}

		#region SETTINGS

		[SerializeField] private PerlinNoiseParams noiseParams;
		[SerializeField] private TerrainSettingsSo terrainSettings;
		public bool autoUpdate = true;

		public PerlinNoiseParams NoiseParams
		{
			get => noiseParams;
			set
			{
				noiseParams = value;
				noiseParams.NotifyUpdate();
			}
		}
		public TerrainSettingsSo TerrainSettings
		{
			get => terrainSettings;
			set
			{
				terrainSettings = value;
				terrainSettings.NotifyUpdate();
			}
		}
		
		protected virtual void OnValidate()
		{
			if (!autoUpdate) return;
			SubscribeToValuesUpdated();

			if (paralelized) SampleGradient();
		}

		public void SubscribeToValuesUpdated()
		{
			if (terrainSettings != null)
			{
				terrainSettings.ValuesUpdated -= OnValuesUpdated;
				if (autoUpdate) terrainSettings.ValuesUpdated += OnValuesUpdated;
			}
			if (noiseParams != null)
			{
				noiseParams.ValuesUpdated -= OnValuesUpdated;
				if (autoUpdate) noiseParams.ValuesUpdated += OnValuesUpdated;
			}
		}

		public void OnValuesUpdated()
		{
			if (!autoUpdate) return;

			// Actualiza la curva de altura para paralelizacion. La normal podria haber cambiado
			if (paralelized) UpdateHeightCurveThreadSafe();

			BuildMap();
		}

		public virtual void ResetSeed() => noiseParams.ResetSeed();

		#endregion

		#region TERRAIN

		private void BuildMapFromTerrain()
		{
			ExtractTerrainHeigthMap();
			ExtractTerrainTexture();
		}

		private void ExtractTerrainHeigthMap()
		{
			_heightMap = new HeightMap(Terrain);
			OnMapUpdated?.Invoke(_heightMap);
		}

		private void ExtractTerrainTexture()
		{
			texture = TextureGenerator.BuildTexture2D(_heightMap, heightGradient);
			OnTextureUpdated?.Invoke(texture);
		}

		#endregion

		#region MAP BUILDER

		public bool buildTexture = true;
		public bool buildMesh = true;

		public event Action<IHeightMap> OnMapUpdated;
		public event Action<Texture2D> OnTextureUpdated;
		public event Action<int, IMeshData> OnMeshUpdated;

		public void BuildMap()
		{
			if (terrainSettings == null)
				BuildMapFromTerrain();
			else if (paralelized) StartCoroutine(BuildMapParallelizedCoroutine());
			else BuildMapSequential();
		}

		public void BuildMapSequential()
		{
			DebugTimer.DebugTime(BuildHeightMap_Sequential, $"Time to build HeightMap {MapSampleSize} x {MapSampleSize}");
			
			if (buildTexture)
				DebugTimer.DebugTime(BuildTexture2D_Sequential, $"Time to build Texture {MapSize} x {MapSize}");
			
			if (buildMesh)
				DebugTimer.DebugTime(() => BuildMeshData_Sequential(), $"Time to build MeshData {MapSampleSize} x {MapSampleSize}");
		}

		private IEnumerator BuildMapParallelizedCoroutine()
		{
			yield return BuildHeightMap_ParallelizedCoroutine();
			
			if (buildTexture)
				yield return BuildTexture2D_ParallelizedCoroutine();
			
			if (buildMesh)
				yield return BuildMeshData_ParallelizedCoroutine();
		}

		#region HEIGHT MAP

		private HeightMap _heightMap;
		public int MapSampleSize => NoiseParams.SampleSize;
		private int MapSize => NoiseParams.Size;

		public void BuildHeightMap()
		{
			if (terrainSettings == null)
				ExtractTerrainHeigthMap();
			else if (paralelized) StartCoroutine(BuildHeightMap_ParallelizedCoroutine());
			else
				DebugTimer.DebugTime(
					BuildHeightMap_Sequential,
					$"Time to build HeightMap {MapSampleSize} x {MapSampleSize}"
				);
		}

		private void BuildHeightMap_Sequential()
		{
			_heightMap = terrainSettings != null
				? HeightMapGenerator.CreatePerlinNoiseHeightMap(noiseParams, terrainSettings.HeightCurve)
				: new HeightMap(Terrain);
			OnMapUpdated?.Invoke(_heightMap);
		}

		#endregion

		#region TEXTURE

		public Gradient heightGradient = new();
		[NonSerialized] public Color32[] textureData;
		public Texture2D texture;

		private void BuildTexture2D()
		{
			if (terrainSettings == null)
				ExtractTerrainTexture();
			else if (paralelized) StartCoroutine(BuildTexture2D_ParallelizedCoroutine());
			else DebugTimer.DebugTime(BuildTexture2D_Sequential, $"Time to build Texture {MapSize} x {MapSize}");
		}

		public void BuildTexture2D_Sequential()
		{
			textureData = TextureGenerator.BuildTextureData32(_heightMap, heightGradient);
			texture = TextureGenerator.BuildTexture2D(textureData, MapSampleSize, MapSampleSize);
			OnTextureUpdated?.Invoke(texture);
		}

		// Usa una resolucion distinta
		public void BuildTexture2D_Sequential(Vector2Int resolution)
		{
			textureData = TextureGenerator.BuildTextureData(noiseParams, heightGradient, resolution);
			texture = TextureGenerator.BuildTexture2D(textureData, resolution.x, resolution.y);
			OnTextureUpdated?.Invoke(texture);
		}

		#endregion

		#region MESH

		private readonly Dictionary<int, IMeshData> _meshDataByLoD = new();
		private IMeshData MeshData => paralelized ? MeshData_ThreadSafe : _meshDataByLoD[terrainSettings.LOD];

		private Mesh mesh;

		// Query Mesh by LoD. If not built, build it.
		// If paralellized, return null. So caller may wait for it to get built.
		public IMeshData GetMeshData(int lod = -1)
		{
			if (lod == -1) lod = terrainSettings.LOD;
			if (paralelized)
			{
				if (_meshDataByLoD_ThreadSafe.TryGetValue(lod, out MeshData_ThreadSafe meshData))
					return meshData;

				// No hay MeshData para el este LoD => La generamos
				StartCoroutine(BuildMeshData_ParallelizedCoroutine(lod));
				return null;
			}
			else
			{
				if (_meshDataByLoD.TryGetValue(lod, out IMeshData meshData))
					return meshData;

				BuildMeshData(lod);
				return _meshDataByLoD[lod];
			}
		}

		public void BuildMeshData(int lod = -1)
		{
			if (lod == -1) lod = terrainSettings.LOD;
			if (paralelized) StartCoroutine(BuildMeshData_ParallelizedCoroutine(lod));
			else
				DebugTimer.DebugTime(
					() => BuildMeshData_Sequential(lod),
					$"Time to build MeshData {MapSampleSize} x {MapSampleSize}"
				);
		}

		private void BuildMeshData_Sequential(int lod = -1)
		{
			if (lod == -1) lod = terrainSettings.LOD;

			IMeshData meshData = MeshGenerator.BuildMeshData(_heightMap, lod, terrainSettings.HeightScale);
			_meshDataByLoD[lod] = meshData;
			mesh = meshData.CreateMesh();
			mesh.hideFlags = HideFlags.HideAndDontSave;
			OnMeshUpdated?.Invoke(lod, meshData);
		}

		#endregion

		#region TIN MESH

		// Generar Malla del TIN
		public Tin BuildTin(float errorTolerance, int maxIterations)
		{
			_meshDataByLoD[0] = TinGenerator.BuildTinMeshData(
				out Tin tin,
				_heightMap,
				errorTolerance,
				terrainSettings.HeightScale,
				maxIterations
			);
			return tin;
		}

		#endregion

		#region THREADING

		public bool paralelized;

		#region HEIGHT MAP THREADING

		private HeightMap_ThreadSafe _heightMapThreadSafe;
		public IHeightMap HeightMap => paralelized ? _heightMapThreadSafe : _heightMap;

		private JobHandle _heightMapJobHandle;

		// Heigth Curve for Threading (sampled to a Look Up Table)
		private SampledAnimationCurve _heightCurveThreadSafe;
		private readonly int _heightCurveSamples = 100;

		public AnimationCurve HeightCurve
		{
			get => terrainSettings.HeightCurve;
			set
			{
				terrainSettings.HeightCurve = value;
				UpdateHeightCurveThreadSafe();
			}
		}

		public void UpdateHeightCurveThreadSafe()
		{
			if (terrainSettings.HeightCurve == null) return;
			_heightCurveThreadSafe.Sample(HeightCurve, _heightCurveSamples);
		}

		protected IEnumerator BuildHeightMap_ParallelizedCoroutine()
		{
			float time = Time.time;

			int sampleSize = noiseParams.SampleSize;
			uint seed = noiseParams.Seed;

			// Initialize HeightMapThreadSafe
			_heightMapThreadSafe = new HeightMap_ThreadSafe(sampleSize, seed);

			// Sample Curve if empty
			if (_heightCurveThreadSafe.IsEmpty)
				UpdateHeightCurveThreadSafe();

			// If last Job didn't end, wait for it
			if (!_heightMapJobHandle.IsCompleted)
				_heightMapJobHandle.Complete();

			// Wait for JobHandle to END
			_heightMapJobHandle = new HeightMapGenerator_ThreadSafe.PerlinNoiseMapBuilderJob
			{
				noiseParams = noiseParams.ToThreadSafe(),
				heightMap = _heightMapThreadSafe,
				heightCurve = _heightCurveThreadSafe
			}.Schedule();

			yield return new WaitUntil(() => _heightMapJobHandle.IsCompleted);

			// MAP GENERATED!!!
			_heightMapJobHandle.Complete();
			OnMapUpdated?.Invoke(_heightMapThreadSafe);

			if (debugTimer) Debug.Log($"{(Time.time - time) * 1000:F1} ms para generar el mapa");
		}

		#endregion

		#region TEXTURE THREADING

		private NativeArray<Color32> _textureDataThreadSafe;
		private GradientThreadSafe _gradientThreadSafe;
		private JobHandle _textureJobHandle;

		protected IEnumerable<Color32> TextureData => paralelized ? _textureDataThreadSafe : textureData;

		private void SampleGradient()
		{
			if (heightGradient == null) return;
			_gradientThreadSafe.SetGradient(heightGradient);
		}

		private void InitializeTextureDataThreadSafe()
		{
			// Inicializamos la Textura o la reinicializamos si cambia su tamaño
			if (texture == null || texture.width != _heightMapThreadSafe.Size)
			{
				int size = _heightMapThreadSafe.Size;
				texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
			}

			_textureDataThreadSafe = texture.GetRawTextureData<Color32>();
		}

		private IEnumerator BuildTexture2D_ParallelizedCoroutine()
		{
			float time = Time.time;

			// Sample Gradient if empty
			if (_gradientThreadSafe.IsEmpty) SampleGradient();

			// Get TextureData reference from Texture2D to modify it
			InitializeTextureDataThreadSafe();

			// If last Job didn't end, wait for it
			if (!_textureJobHandle.IsCompleted) _textureJobHandle.Complete();

			// Wait for JobHandle to END
			_textureJobHandle = new TextureGeneratorThreadSafe.MapToTextureJob
			{
				heightMap = _heightMapThreadSafe.map,
				textureData = _textureDataThreadSafe,
				gradient = _gradientThreadSafe
			}.Schedule();

			yield return new WaitUntil(() => _textureJobHandle.IsCompleted);

			_textureJobHandle.Complete();

			OnTextureUpdated?.Invoke(texture);

			if (debugTimer) Debug.Log($"{(Time.time - time) * 1000:F1} ms para generar la textura");
		}

		#endregion

		#region MESH THREADING

		private readonly Dictionary<int, MeshData_ThreadSafe> _meshDataByLoD_ThreadSafe = new();
		private MeshData_ThreadSafe MeshData_ThreadSafe
		{
			get => _meshDataByLoD_ThreadSafe[terrainSettings.LOD];
			set => _meshDataByLoD_ThreadSafe[terrainSettings.LOD] = value;
		}

		private void InitializeMeshDataThreadSafe(int lod)
		{
			// If no Mesh with this LoD, or Mesh Size changed, reinitialize MeshData
			if (!_meshDataByLoD_ThreadSafe.TryGetValue(lod, out MeshData_ThreadSafe meshData)
			    || meshData.IsEmpty
			    || meshData.width != MapSampleSize)
				MeshData_ThreadSafe = new MeshData_ThreadSafe(MapSize, MapSize);
			else
				MeshData_ThreadSafe.Reset();
		}

		private IEnumerator BuildMeshData_ParallelizedCoroutine(int lod = -1)
		{
			float time = Time.time;

			InitializeMeshDataThreadSafe(lod);

			MeshData_ThreadSafe meshData = _meshDataByLoD_ThreadSafe[lod];

			JobHandle meshJob = new MeshGeneratorThreadSafe.BuildMeshDataJob
			{
				meshData = meshData,
				heightMap = _heightMapThreadSafe,
				lod = lod,
				heightScale = terrainSettings.HeightScale
			}.Schedule();

			yield return new WaitWhile(() => !meshJob.IsCompleted);

			meshJob.Complete();

			mesh = meshData.CreateMesh();

			OnMeshUpdated?.Invoke(lod, meshData);

			if (debugTimer)
				Debug.Log(
					$"{(Time.time - time) * 1000:F1} ms para generar la Malla {MapSampleSize} x {MapSampleSize}, LoD {lod}"
				);
		}

		#endregion

		#endregion

		#endregion

		#region DEBUG

		private void OnDrawGizmos()
		{
			var textureSize = 10;
			Vector3 textureOffset = - new Vector3(1, 1, 0) * textureSize / 2;
			Vector3 meshOffset = Vector3.back * 2;
			Quaternion meshRotation = Quaternion.Euler(90, 0, 0);
			var meshScale = new Vector3(0.01f, 0.04f, 0.01f);
			if (buildTexture)
				Gizmos.DrawGUITexture(new Rect(transform.position + textureOffset, Vector3.one * textureSize), texture);
			if (buildMesh)
				// Gizmos.DrawMesh(mesh, transform.position + meshOffset, Quaternion.identity, meshScale);
				Gizmos.DrawWireMesh(mesh, transform.position + meshOffset, meshRotation, meshScale);
		}

		#endregion
	}
}
