using System;
using UnityEngine;

namespace Procrain
{
    [ExecuteAlways]
    public abstract class AutoUpdatableSo : ScriptableObject
    {
        public event Action OnValuesUpdated;

        public virtual void NotifyUpdate() => OnValuesUpdated?.Invoke();
    }
}
