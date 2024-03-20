using UnityEngine;

namespace Procrain.Runtime.Utils
{
    [ExecuteAlways]
    public class SingletonExecuteAlways<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                    Instance = FindObjectOfType<T>();

                return _instance;
            }
            private set => _instance = value;
        }

        protected void Awake()
        {
            if (_instance != null)
            {
                DestroyImmediate(gameObject);
                return;
            }

            Instance = gameObject.GetComponent<T>();
        }
    }
}