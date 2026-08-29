using System;
using System.Collections;
using System.Collections.Generic;
using Procrain.Mesh;
using Procrain.Texture;
using Procrain.TIN;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Procrain
{
	[ExecuteAlways]
	public class MapManager : Singleton<MapManager>
	{
		public bool debugInfo = true;
		
		public event Action<IHeightMap> OnMapUpdated;
		public event Action<Texture2D> OnTextureUpdated;
		public event Action<int, IMeshData> OnMeshUpdated;
		
		
		#region SETTINGS

		public bool autoUpdate = true;
		[SerializeField] private PerlinNoiseParams noiseParams;
		[SerializeField] private TerrainParams terrainSettings;
		[SerializeField] private TextureParams textureParams;

		public static PerlinNoiseParams NoiseParams
		{
			get => Instance?.noiseParams ?? Resources.Load<PerlinNoiseParams>("Settings/Perlin Noise Default");
			set
			{
				if (Instance == null) return;
				Instance.noiseParams = value;
				Instance.noiseParams.NotifyUpdate();
			}
		}
		public static TerrainParams TerrainSettings
		{
			get => Instance?.terrainSettings ?? Resources.Load<TerrainParams>("Settings/Default TSettings");
			set
			{
				
				Instance.terrainSettings = value;
				Instance.terrainSettings.NotifyUpdate();
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
			if (terrainSettings)
			{
				terrainSettings.OnValuesUpdated -= OnValuesUpdated;
				if (autoUpdate) terrainSettings.OnValuesUpdated += OnValuesUpdated;
			}

			if (noiseParams)
			{
				noiseParams.OnValuesUpdated -= OnValuesUpdated;
				if (autoUpdate) noiseParams.OnValuesUpdated += OnValuesUpdated;
			}
		}

		private void UnsuscribeToValuesUpdated()
		{
			if (terrainSettings)
				terrainSettings.OnValuesUpdated -= OnValuesUpdated;
			
			if (noiseParams)
				noiseParams.OnValuesUpdated -= OnValuesUpdated;
		}

		public void OnValuesUpdated()
		{
			if (!autoUpdate) return;

			BuildMap();
		}

		public virtual void ResetSeed() => noiseParams.ResetSeed();

		#endregion


		#region INITIALIZATION

		protected override void Awake()
		{
			base.Awake();

			BuildMap();

			SubscribeToValuesUpdated();
		}

		private void OnDestroy()
		{
			// Cancel all processes and Threading JOBS
			StopAllCoroutines();
			if (!_heightMapJobHandle.IsCompleted) _heightMapJobHandle.Complete();
			if (!_textureJobHandle.IsCompleted) _textureJobHandle.Complete();

			UnsuscribeToValuesUpdated();

			_heightMapThreadSafe.Dispose();
			_heightCurveThreadSafe.Dispose();
			_textureDataThreadSafe.Dispose();

			foreach (MeshData_ThreadSafe meshDataThreadSafe in _meshDataByLoD_ThreadSafe.Values)
				meshDataThreadSafe.Dispose();
		}

		#endregion
		
		
		#region TERRAIN
		
		private void BuildMapFromTerrain()
		{
			ExtractTerrainHeigthMap();
			BuildTexture();
		}

		private void ExtractTerrainHeigthMap()
		{
			_heightMap = new HeightMap_ThreadSafe(UnityEngine.Terrain.activeTerrain);
			OnMapUpdated?.Invoke(_heightMap);
		}

		#endregion
		

		#region MAP BUILDER

		public bool buildTexture = true;
		public bool buildMesh = true;

		public void BuildMap()
		{
			if (!terrainSettings)
				BuildMapFromTerrain();
			else if (paralelized) StartCoroutine(BuildMapParallelizedCoroutine());
			else BuildMapSequential();
		}

		public void BuildMapSequential()
		{
			if (debugInfo)
			{
				DebugTimer.DebugTime(
					BuildHeightMap_Sequential,
					$"Time to build HeightMap {MapSampleSize} x {MapSampleSize}"
				);

				if (buildTexture)
					DebugTimer.DebugTime(BuildTexture_Sequential, $"Time to build Texture {MapSize} x {MapSize}");

				if (buildMesh)
					DebugTimer.DebugTime(
						() => BuildMeshData_Sequential(),
						() => $"Time to build MeshData {_meshDataByLoD[LOD]}"
					);
			}
			else
			{
				BuildHeightMap_Sequential();
				if (buildTexture)
					BuildTexture_Sequential();
				if (buildTexture)
					BuildMeshData_Sequential();
			}
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

		private HeightMap_ThreadSafe _heightMap;
		public int MapSampleSize => NoiseParams.SampleSize;
		private int MapSize => NoiseParams.Size;

		public void BuildHeightMap()
		{
			if (terrainSettings == null)
				ExtractTerrainHeigthMap();
			else if (paralelized)
				StartCoroutine(BuildHeightMap_ParallelizedCoroutine());
			else
			{
				if (debugInfo)
					DebugTimer.DebugTime(
						BuildHeightMap_Sequential,
						$"Time to build HeightMap {MapSampleSize} x {MapSampleSize}"
					);
				else
					BuildHeightMap_Sequential();
			}
		}

		private void BuildHeightMap_Sequential()
		{
			_heightMap = noiseParams != null
				? new HeightMap_ThreadSafe(noiseParams)
				: new HeightMap_ThreadSafe(UnityEngine.Terrain.activeTerrain);
			OnMapUpdated?.Invoke(_heightMap);
		}

		#endregion

		
		#region TEXTURE

		public Gradient heightGradient = new();
		[NonSerialized] private Color32[] textureData;
		public Texture2D texture;

		public void BuildTexture()
		{
			if (terrainSettings == null)
			{
				texture = TextureGenerator.BuildTexture2D(_heightMap, heightGradient);
				OnTextureUpdated?.Invoke(texture);
			}
			else if (paralelized)
			{
				StartCoroutine(BuildTexture2D_ParallelizedCoroutine());
			}
			else
			{
				if (debugInfo)
					DebugTimer.DebugTime(BuildTexture_Sequential, $"Time to build Texture {MapSize} x {MapSize}");
				else
					BuildTexture_Sequential();
			}
		}

		private void BuildTexture_Sequential()
		{
			textureData = TextureGenerator.BuildTextureData32(noiseParams, heightGradient);
			texture = TextureGenerator.BuildTexture2D(textureData, NoiseParams.Size, NoiseParams.Size);
			OnTextureUpdated?.Invoke(texture);
		}

		// Usa una resolucion distinta
		public void BuildTexture_Sequential(Vector2Int resolution)
		{
			textureData = TextureGenerator.BuildTextureData32(noiseParams, heightGradient);
			texture = TextureGenerator.BuildTexture2D(textureData, resolution.x, resolution.y);
			OnTextureUpdated?.Invoke(texture);
		}

		#endregion

		
		#region MESH

		private readonly Dictionary<int, IMeshData> _meshDataByLoD = new();
		private IMeshData MeshData =>
			paralelized 
			? MeshData_ThreadSafe
			: _meshDataByLoD[LOD];

		private UnityEngine.Mesh _mesh;

		private int LOD => terrainSettings != null ? terrainSettings.LOD : 0;

		// Query Mesh by LoD. If not built, build it.
		// If paralellized, return null. So caller may wait for it to get built.
		public IMeshData GetMeshData(int lod = -1)
		{
			if (lod == -1) lod = LOD;
			
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
			if (lod == -1) lod = LOD;
			
			if (paralelized) StartCoroutine(BuildMeshData_ParallelizedCoroutine(lod));
			else
			{
				if (debugInfo)
					DebugTimer.DebugTime(
						() => BuildMeshData_Sequential(lod),
						$"Time to build MeshData {_meshDataByLoD[lod]}"
					);
				else
					BuildMeshData_Sequential(lod);
			}
		}

		private void BuildMeshData_Sequential(int lod = -1)
		{
			if (lod == -1) lod = LOD;

			IMeshData meshData = MeshGenerator.BuildMeshData(_heightMap, terrainSettings, heightGradient);
			_meshDataByLoD[lod] = meshData;
			_mesh = meshData.CreateMesh();
			_mesh.hideFlags = HideFlags.HideAndDontSave;
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


		protected IEnumerator BuildHeightMap_ParallelizedCoroutine()
		{
			float time = Time.time;

			int sampleSize = noiseParams.SampleSize;
			uint seed = noiseParams.Seed;

			// Initialize HeightMapThreadSafe
			_heightMapThreadSafe = new HeightMap_ThreadSafe(sampleSize, seed);

			// If last Job didn't end, wait for it
			if (!_heightMapJobHandle.IsCompleted)
				_heightMapJobHandle.Complete();

			// Wait for JobHandle to END
			_heightMapJobHandle = new HeightMap_ThreadSafe.PerlinNoiseMapBuilderJob
			{
				noiseParams = noiseParams.ToThreadSafe(),
				heightMap = _heightMapThreadSafe,
			}.Schedule();

			yield return new WaitUntil(() => _heightMapJobHandle.IsCompleted);

			// MAP GENERATED!!!
			_heightMapJobHandle.Complete();
			OnMapUpdated?.Invoke(_heightMapThreadSafe);

			if (debugInfo) Debug.Log($"{(Time.time - time) * 1000:F1} ms para generar el mapa");
		}

		#endregion
		
		
		#region TEXTURE THREADING

		private NativeArray<Color32> _textureDataThreadSafe;
		private Gradient_ThreadSafe _gradientThreadSafe;
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
			_textureJobHandle = new TextureGenerator.MapToTextureJob
			{
				heightMap = _heightMapThreadSafe,
				textureData = _textureDataThreadSafe,
				textureParams = new TextureParams_ThreadSafe(textureParams),
			}.Schedule();

			yield return new WaitUntil(() => _textureJobHandle.IsCompleted);

			_textureJobHandle.Complete();
			
			// TODO es necesario aplicar los datos de la textura?
			// texture.SetPixelData(_textureDataThreadSafe, 0);

			OnTextureUpdated?.Invoke(texture);

			if (debugInfo) Debug.Log($"{(Time.time - time) * 1000:F1} ms para generar la textura");
		}

		#endregion

		
		#region MESH THREADING

		private readonly Dictionary<int, MeshData_ThreadSafe> _meshDataByLoD_ThreadSafe = new();
		private MeshData_ThreadSafe MeshData_ThreadSafe
		{
			get => _meshDataByLoD_ThreadSafe[LOD];
			set => _meshDataByLoD_ThreadSafe[LOD] = value;
		}

		private void InitializeMeshDataThreadSafe(int lod)
		{
			// If no Mesh with this LoD, or Mesh Size changed, reinitialize MeshData
			if (!_meshDataByLoD_ThreadSafe.TryGetValue(lod, out MeshData_ThreadSafe meshData)
			    || meshData.IsEmpty)
				_meshDataByLoD_ThreadSafe[lod] = new MeshData_ThreadSafe(MapSize, MapSize, lod);
			else
				_meshDataByLoD_ThreadSafe[lod].Reset();
		}

		private IEnumerator BuildMeshData_ParallelizedCoroutine(int lod = -1)
		{
			float time = Time.time;

			InitializeMeshDataThreadSafe(lod);

			MeshData_ThreadSafe meshData = _meshDataByLoD_ThreadSafe[lod];

			JobHandle meshJob = new MeshGeneratorThreadSafe.GenerateMeshDataJob
			{
				meshData = meshData,
				prebuiltHeightMap = _heightMapThreadSafe,
				terrainParams = new TerrainParams_ThreadSafe(terrainSettings.HeightScale, lod)
			}.Schedule();

			yield return new WaitWhile(() => !meshJob.IsCompleted);

			meshJob.Complete();

			_mesh = meshData.CreateMesh();

			OnMeshUpdated?.Invoke(lod, meshData);

			if (debugInfo)
				Debug.Log(
					$"{(Time.time - time) * 1000:F1} ms para generar la Malla {MapSampleSize} x {MapSampleSize}, LoD {lod}"
				);
		}

		#endregion

		
		#endregion

		#endregion

		
		#region DEBUG

		private void OnDrawGizmosSelected()
		{
			if (buildTexture)
			{
				const int textureSize = 10;
				Vector3 textureOffset = new Vector3(-1, 1, 0) * textureSize / 2;
				Rect textureRect = new Rect(transform.position + textureOffset,
					Vector2.one * textureSize * new Vector2(1, -1));
				Gizmos.DrawGUITexture(textureRect, texture);
			}
			if (buildMesh)
			{
				float terrainScale = terrainSettings.HeightScale;
				Vector3 meshOffset = Vector3.down * 6f + Vector3.back * 6f;
				Quaternion meshRotation = Quaternion.Euler(0, 0, 0);
				Vector3 meshScale = new(terrainScale * 0.0002f, terrainScale * 0.0002f, terrainScale * 0.0002f);
				Gizmos.color = Color.grey;
				Gizmos.DrawMesh(_mesh, transform.position + meshOffset, meshRotation, meshScale);
			}
		}

		#endregion
	}
}
