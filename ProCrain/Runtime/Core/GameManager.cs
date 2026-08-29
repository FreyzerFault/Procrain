using UnityEngine;

namespace Procrain
{
    public class GameManager : Singleton<GameManager>
    {
        public static Terrain Terrain => Terrain.activeTerrain;


        protected override void Awake()
        {
            base.Awake();
            water = GameObject.FindGameObjectWithTag("Water");
        }
        
        #region PLAYER
		
        private IPlayer _player;
        public IPlayer Player => _player ??= IPlayer.Player;

        public Vector2 PlayerNormalizedPosition => Terrain.GetNormalizedPosition(Player.Position);
        private float PlayerRotationAngle => Player.Rotation.eulerAngles.y;
        public Quaternion PlayerRotationForUI => Quaternion.AngleAxis(90 + PlayerRotationAngle, Vector3.back);

        #endregion
        
        
        #region WATER

        [SerializeField] private GameObject water;
        public float WaterHeight => water ? water.transform.position.y : 0;

        #endregion
    }
}
