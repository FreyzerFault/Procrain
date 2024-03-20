using UnityEngine;

namespace Utils.Camera
{
    public class CameraIdleRotation : MonoBehaviour
    {
        public Transform target;
        public Bounds targetBounds;
        public float angularVel = 1f;

        private void Start()
        {
            targetBounds = target.GetComponent<MeshFilter>().mesh.bounds;
        }

        private void LateUpdate()
        {
            transform.RotateAround(targetBounds.center, Vector3.up, Time.deltaTime * angularVel);
        }
    }
}