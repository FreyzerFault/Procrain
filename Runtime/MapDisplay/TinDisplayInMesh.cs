using Procrain.Geometry;

namespace Procrain.MapDisplay
{
    public class TinDisplayInMesh : MapDisplayInMesh
    {
        public float errorTolerance = 1;
        public int maxIterations = 10;
        private Tin _tin;

        private void OnDrawGizmos() => _tin.OnDrawGizmos();
    }
}
