using System;
using UnityEngine;

namespace Procrain
{
    public class PlayerController: Singleton<PlayerController>, IPlayer
    {
        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

        public event Action<Vector2> OnPlayerMove;
        
        private void Update()
        {
            Vector3 move = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) move = transform.forward;
            if (Input.GetKey(KeyCode.A)) move = -transform.right;
            if (Input.GetKey(KeyCode.S)) move = -transform.forward;
            if (Input.GetKey(KeyCode.D)) move = transform.right;

            transform.position += move * (Time.deltaTime * 1000);
            
            if (move != Vector3.zero) OnPlayerMove?.Invoke(new Vector2(move.x, move.z));
        }
    }
}
