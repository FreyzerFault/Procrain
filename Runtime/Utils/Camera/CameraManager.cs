using System.Collections.Generic;
using UnityEngine;

namespace Utils.Camera
{
    public class CameraManager : MonoBehaviour
    {
        public List<UnityEngine.Camera> cameras;
        public int defaultCameraIndex;
        public int activeCameraIndex;

        public UnityEngine.Camera ActiveCamera
        {
            get => cameras[activeCameraIndex];
            set => activeCameraIndex = cameras.IndexOf(value);
        }

        private void Awake()
        {
            if (cameras.Count == 0)
                cameras = new List<UnityEngine.Camera>(GetComponentsInChildren<UnityEngine.Camera>());

            // Camara por defecto activada => 0
            activeCameraIndex = defaultCameraIndex;
        }

        public void SwitchCamera(int i)
        {
            if (i >= 0 && i < cameras.Count)
            {
                // Desactivo la Anterior
                ActiveCamera.gameObject.SetActive(false);

                // Activo la nueva
                activeCameraIndex = i;
                ActiveCamera.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("Camera " + i + " no existe");
            }
        }
    }
}