using System;
using System.Collections.Generic;
using System.Linq;
using Procrain.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Procrain.Geometry
{
    [Serializable]
    public struct AABB_2D
    {
        public enum Side { Left, Right, Top, Bottom }

        public float2 min;
        public float2 max;
        public float2 Center => (min + max) / 2;

        public float Width => max.x - min.x;
        public float Height => max.y - min.y;
        public float2 Extent => Size / 2;
        public float2 Size
        {
            get => new(Width, Height);
            set
            {
                float2 halfSize = value / 2;
                float2 center = Center;
                min = center - halfSize;
                max = center + halfSize;
            }
        }

        public float2 BL => min;
        public float2 BR => new(max.x, min.y);
        public float2 TL => new(min.x, max.y);
        public float2 TR => max;
        public float2[] Corners => new[] { BL, BR, TR, TL }; // CCW

        public bool IsNormalized => min.Equals(float2.zero) && max.Equals(new float2(1));

        public static AABB_2D NormalizedAABB => new(float2.zero, new float2(1));

        public AABB_2D(float2 min, float2 max)
        {
            this.min = min;
            this.max = max;
        }

        public AABB_2D(IEnumerable<float2> pointsInsideBound)
        {
            IEnumerable<float2> pointsEnumerable = pointsInsideBound as float2[] ?? pointsInsideBound.ToArray();
            min = pointsEnumerable.MinPosition();
            max = pointsEnumerable.MaxPosition();
        }

        public AABB_2D(Bounds bounds3D, bool isXZplane = true)
            : this(
                isXZplane ? bounds3D.min.ToV2XZ() : bounds3D.min.ToV2XY(),
                isXZplane ? bounds3D.max.ToV2XZ() : bounds3D.max.ToV2XY()
            )
        {
        }

        #region TEST INSIDE

        public readonly bool Contains(float2 p) => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y;
        public bool OutOfBounds(float2 p) => !Contains(p);

        #endregion
    }
}
