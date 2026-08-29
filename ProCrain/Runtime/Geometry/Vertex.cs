using System;
using Unity.Mathematics;

namespace Procrain
{
	[Serializable]
	public struct Vertex: IEquatable<Vertex>
	{
		public readonly int index;

		public readonly float x;
		public readonly float y;
		public readonly float z;

		// public readonly float2 xy; // 2D (x,y)
		public readonly float2 xz; // 2.5D (x,z)
		public readonly float3 xyz;

		public static Vertex InvalidVertex => new Vertex(0,0,0);
		public bool IsInvalid => index == -1;
		
		public Vertex(float x, float y, float z, int index = -1)
		{
			this.index = index;

			this.x = x;
			this.y = y;
			this.z = z;

			xyz = new float3(x, y, z);
			xz = new float2(x, z);
			// xy = new float2(x, y);
		}

		public Vertex(float3 v, int index = -1) : this(v.x, v.y, v.z, index)
		{ }

		public override string ToString() => $"{nameof(index)}: {index}, {nameof(xyz)}: {xyz}";
		
		public string ToString(bool withCoords) =>
			ToString() + (withCoords ? "v" + "(" + x + ", " + z + ") H = " + y : "");

		public bool Equals(Vertex other) => xyz.Equals(other.xyz);

		/// Se identifica por su coordenada 2D en el plano X,Z.
		/// No puede haber mas de 1 punto con distinta altura
		public override int GetHashCode() => xyz.GetHashCode();
		
		public static implicit operator float3(Vertex v) => v.xyz;
	}
}
