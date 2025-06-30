namespace Procrain.Geometry.Mesh
{
    public readonly struct TriangleData
    {
        public readonly int index;
        
        public readonly int firstEdgeIndex;       // Índice a cualquier half-edge del triángulo

        public static TriangleData InvalidTriangle => new(-1);
        public bool IsValid => index == -1;

        public TriangleData(int index, int edgeIndex = -1)
        {
            this.index = index;
            firstEdgeIndex = edgeIndex;
        }

        public static int SizeOf() => sizeof(int) * 2;
    }
}
