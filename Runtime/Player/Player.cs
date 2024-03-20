using UnityEngine;

namespace Procrain.Runtime.Player
{
    public class Player : MonoBehaviour
    {
        private void Update()
        {
            var move = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
                move = transform.forward;
            if (Input.GetKey(KeyCode.A))
                move = -transform.right;
            if (Input.GetKey(KeyCode.S))
                move = -transform.forward;
            if (Input.GetKey(KeyCode.D))
                move = transform.right;

            //rb.AddForce(move);
            transform.position += move * (Time.deltaTime * 1000);
        }
    }
}