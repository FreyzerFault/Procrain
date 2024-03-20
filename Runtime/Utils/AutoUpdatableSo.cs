using System;
using UnityEngine;

namespace Procrain.Runtime.Utils
{
    [ExecuteAlways]
    public abstract class AutoUpdatableSo : ScriptableObject
    {
        public bool autoUpdate = true;

        public Action onValuesUpdated;


        private void Awake() => onValuesUpdated = null;

#if UNITY_EDITOR
        public void OnValidate() => ValidationUtility.SafeOnValidate(OnUpdateValues);
#endif

        public virtual void OnUpdateValues()
        {
            if (autoUpdate)
                NotifyUpdate();
        }

        public void NotifyUpdate() => onValuesUpdated?.Invoke();
    }
}