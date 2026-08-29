using System;
using Unity.Mathematics;

namespace Procrain.Mesh
{
    public readonly struct HalfEdgeVertex: IEquatable<HalfEdgeVertex>
    {
        public readonly int index;
        
        public readonly float3 position;
        
        public readonly int firstEdgeIndex; // Índice al primer half-edge que sale de este vértice
    
        public static HalfEdgeVertex InvalidHalfVertex => new(-1, float3.zero);
        public bool IsValid => index == -1;

        public HalfEdgeVertex(int index, float3 position, int firstEdgeIndex = -1)
        {
            this.index = index;
            this.position = position;
            this.firstEdgeIndex = firstEdgeIndex;
        }

        public bool Equals(HalfEdgeVertex other) => index == other.index;

        public override bool Equals(object obj) => 
            obj is HalfEdgeVertex other && Equals(other);

        public override int GetHashCode() => index;


        public static int SizeOf() => sizeof(float) * 3 + sizeof(int) * 2;
    }
}
