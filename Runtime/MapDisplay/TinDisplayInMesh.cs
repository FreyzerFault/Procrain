using Procrain.Runtime.Geometry;
using Procrain.Runtime.MapGeneration.TerrainGeneration.TINGeneration;

namespace Procrain.Runtime.MapDisplay
{
    public class TinDisplayInMesh : MapDisplayInMesh
    {
        public float errorTolerance = 1;
        public int maxIterations = 10;
        private Tin tin;

        protected override void Start()
        {
            paralelized = false;
            base.Start();
        }

        private void OnDrawGizmos() => tin.OnDrawGizmos();

        // Generar Malla del TIN
        protected override void BuildMeshData() =>
            meshData = TinGenerator.BuildTinMeshData(
                out tin,
                heightMap,
                errorTolerance,
                HeightMultiplier,
                maxIterations
            );
    }
}