using UnityEngine;

namespace Procrain
{
    [CreateAssetMenu(fileName = "TIN Params", menuName = "Procrain/TIN Params")]
    public class TINParams: AutoUpdatableSoWithBackup<TINParams>, ITerrainParams
    {
        public float errorTolerance = 0.001f;
        public int maxIterations = 1000;
        
        [SerializeField] private float heightScale = 100;

        [PowerOfTwo(0, 4, true)]
        [SerializeField] private int lod;

        public int LODIndex => (int)Mathf.Log(lod, 2);

        
        #region UPDATABLE PARAMS

        public float HeightScale
        {
            get => heightScale;
            set
            {
                heightScale = value;
                NotifyUpdate();
            }
        }

        public int LOD
        {
            get => lod;
            set
            {
                lod = value;
                NotifyUpdate();
            }
        }
        
        public float ErrorTolerance
        {
            get => errorTolerance;
            set
            {
                errorTolerance = value;
                NotifyUpdate();
            }
        }

        public int MaxIterations
        {
            get => maxIterations;
            set
            {
                maxIterations = value;
                NotifyUpdate();
            }
        }

        #endregion
        

        protected override void CopyValues(TINParams from, TINParams to)
        {
            to.errorTolerance = from.errorTolerance;
            to.maxIterations = from.maxIterations;
            to.heightScale = from.heightScale;
            to.lod = from.lod;
        }

        public TINParams_ThreadSafe ToThreadSafe()
        {
            return new TINParams_ThreadSafe(errorTolerance, maxIterations);
        }
    }

    public readonly struct TINParams_ThreadSafe: ITerrainParams
    {
        public readonly float errorTolerance;
        public readonly int maxIterations;
        public readonly float heightScale;
        public readonly int lod;

        public TINParams_ThreadSafe(float heightScale = 100, int lod = 0, float errorTolerance = 0.001f, int maxIterations = 1000)
        {
            this.errorTolerance = errorTolerance;
            this.maxIterations = maxIterations;
            this.heightScale = heightScale;
            this.lod = lod;
        }

        public float HeightScale => heightScale;
        public int LOD => lod;
    }
}
