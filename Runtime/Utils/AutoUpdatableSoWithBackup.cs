using UnityEngine;

namespace Procrain.Utils
{
	[ExecuteAlways]
	public abstract class AutoUpdatableSoWithBackup<T> : AutoUpdatableSo
		where T : AutoUpdatableSoWithBackup<T>
	{
		[HideInInspector]
		public bool dirty;
		private T _backup;
		private bool _iAmBackup;

		private void InstantiateBackup()
		{
			_backup = CreateInstance<T>();
			_backup._iAmBackup = true;
			SaveChanges();
		}

		public override void NotifyUpdate()
		{
			base.NotifyUpdate();

			if (_iAmBackup)
				return;

			if (_backup == null)
				InstantiateBackup();

			dirty = true;
		}

		public void SaveChanges()
		{
			if (_iAmBackup || !dirty)
				return;

			CopyValues(this as T, _backup);
			dirty = false;
		}

		public void UndoChanges()
		{
			if (_iAmBackup || !dirty)
				return;

			CopyValues(_backup, this as T);
			dirty = false;

			base.NotifyUpdate();
		}

		protected abstract void CopyValues(T from, T to);
	}
}
