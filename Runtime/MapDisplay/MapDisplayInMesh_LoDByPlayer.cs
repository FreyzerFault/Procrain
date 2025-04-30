using System;
using Procrain.Core;
using Procrain.Utils;
using UnityEngine;

namespace Procrain.MapDisplay
{
	// Actualiza el LOD del Mapa en Tiempo Real dependiendo de la distancia del Jugador
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	public class MapDisplayInMesh_LoDByPlayer : MapDisplayInMesh
	{
		protected override void Awake()
		{
			base.Awake();
			
			if (MapManager.Instance.Player == null)
				throw new Exception(
					"El Player no tiene un componente con la interface IPlayer." +
					" No se puede calcular el LOD por la posicion del Player."
				);

			// Usa un LoD local ignorando el LoD global del MapManager
			useLocalLoD = true;

			// Solo actualiza el LoD cuando el Player se mueve
			MapManager.Instance.Player.OnPlayerMove += OnPlayerMove;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			MapManager.Instance.Player.OnPlayerMove -= OnPlayerMove;
		}

		private void OnPlayerMove(Vector2 moveInput)
		{
			localLoD = CalculateLoDByPlayerPos(MapManager.Instance.Player.Position);
			OnLocalLoDUpdate(localLoD);
		}

		private int CalculateLoDByPlayerPos(Vector3 playerPos)
		{
			Vector2 playerPos2D = new(playerPos.x, playerPos.z);
			Vector3 position = transform.position;
			var terrainWorldPos = new Vector2(position.x, position.z);
			return Mathf.FloorToInt(
				(terrainWorldPos - playerPos2D).magnitude / MapManager.Instance.NoiseParams.Size
			);
		}
	}
}
