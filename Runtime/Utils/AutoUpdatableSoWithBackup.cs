using UnityEngine;

namespace Procrain.Runtime.Utils
{
    [ExecuteAlways]
    public abstract class AutoUpdatableSoWithBackup<T> : AutoUpdatableSo where T : AutoUpdatableSoWithBackup<T>
    {
        [HideInInspector] public bool dirty;
        private T backup;
        private bool iAmBackup;

        private void InstantiateBackup()
        {
            backup = CreateInstance<T>();
            backup.iAmBackup = true;
            SaveChanges();
        }

        public override void OnUpdateValues()
        {
            base.OnUpdateValues();

            if (iAmBackup) return;

            if (backup == null)
                InstantiateBackup();

            dirty = true;
        }

        public void SaveChanges()
        {
            if (iAmBackup || !dirty) return;

            CopyValues(this as T, backup);
            dirty = false;
        }

        public void UndoChanges()
        {
            if (iAmBackup || !dirty) return;

            CopyValues(backup, this as T);
            dirty = false;

            if (autoUpdate) base.OnUpdateValues();
        }

        protected abstract void CopyValues(T from, T to);
    }
}