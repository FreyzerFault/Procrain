using System;

namespace Procrain.Geometry.Mesh
{
    /// Half Edge con los indices de la topología conectada
    /// (Vertices, HalfEdges adyacentes y Face a la que pertenece)
    public struct HalfEdgeData: IEquatable<HalfEdgeData>
    {
        public readonly int index;
        
        public readonly int beginVertexIndex;        // Índice al vértice origen
        public readonly int nextEdgeIndex;           // Índice al siguiente half-edge
        public readonly int prevEdgeIndex;           // Índice al half-edge previo
        
        public int twinEdgeIndex;           // Índice al half-edge gemelo
        public int faceIndex;               // Índice al triángulo al que pertenece
        
        public static HalfEdgeData InvalidHalfEdge => new(-1);
        public bool IsValid => index == -1;
        
        public HalfEdgeData(int index,
            int beginVertexIndex = -1,
            int twinEdgeIndex = -1,
            int nextEdgeIndex = -1,
            int prevEdgeIndex = -1,
            int faceIndex = -1
            )
        {
            this.index = index;
            this.beginVertexIndex = beginVertexIndex;
            this.twinEdgeIndex = twinEdgeIndex;
            this.nextEdgeIndex = nextEdgeIndex;
            this.prevEdgeIndex = prevEdgeIndex;
            this.faceIndex = faceIndex;       
        }

        public bool Equals(HalfEdgeData other) => index == other.index;

        public override bool Equals(object obj) => 
            obj is HalfEdgeData other && Equals(other);

        public override int GetHashCode() => index;


        public static int SizeOf() => sizeof(int) * 6;
    }
}
