using System.Collections.Generic;
using UnityEngine;

namespace Procrain.Runtime.Utils.Camera
{
    public class SwitchCamera : MonoBehaviour
    {
        public List<UnityEngine.Camera> cameras;

        private int currentCam;

        // Cambia la camara actual a la siguiente
        public void Switch()
        {
            cameras[currentCam].gameObject.SetActive(false);
            currentCam = (currentCam + 1) % cameras.Count;
            cameras[currentCam].gameObject.SetActive(true);
        }
    }
}