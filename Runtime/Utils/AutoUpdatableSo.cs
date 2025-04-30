using System;
using UnityEngine;

namespace Procrain.Utils
{
    [ExecuteAlways]
    public abstract class AutoUpdatableSo : ScriptableObject
    {
        public Action valuesUpdated;

        public virtual void NotifyUpdate() => valuesUpdated?.Invoke();
    }
}
