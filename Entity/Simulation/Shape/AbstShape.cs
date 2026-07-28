using System.Collections.Generic;

namespace Entity.Simulation.Shape;

/// <summary>世界中的形状基类。顶点为整数坐标，满足帧同步确定性。</summary>
public abstract class AbstShape
{
    protected List<Vec2D<uint>> _vecs = new();

    /// <summary>只读顶点序列，供重叠检测等算法遍历使用。</summary>
    public IReadOnlyList<Vec2D<uint>> Vertices => _vecs;

    /// <summary>
    /// 本形状是否与 <paramref name="other"/> 相交（含边界接触）。
    /// 默认实现用分离轴定理（SAT）对凸多边形通用；
    /// 子类可重写以提供更高效的特化判定。
    /// </summary>
    public virtual bool OverlapsWith(AbstShape other)
    {
        var va = Vertices;
        var vb = other.Vertices;
        if (va.Count < 3 || vb.Count < 3)
        {
            return false;
        }

        return !HasSeparatingAxis(va, vb) && !HasSeparatingAxis(vb, va);
    }

    /// <summary>
    /// 在多边形 <paramref name="poly"/> 的每条边法线方向上找分离轴；
    /// 任一轴上两多边形投影区间严格分离即视为不相交。
    /// </summary>
    protected static bool HasSeparatingAxis(
        IReadOnlyList<Vec2D<uint>> poly,
        IReadOnlyList<Vec2D<uint>> other)
    {
        int n = poly.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;

            long nx = -((long)poly[j].Y - poly[i].Y);
            long ny = (long)poly[j].X - poly[i].X;

            long aMin = long.MaxValue;
            long aMax = long.MinValue;
            foreach (var p in poly)
            {
                long proj = (long)p.X * nx + (long)p.Y * ny;
                if (proj < aMin) aMin = proj;
                if (proj > aMax) aMax = proj;
            }

            long bMin = long.MaxValue;
            long bMax = long.MinValue;
            foreach (var p in other)
            {
                long proj = (long)p.X * nx + (long)p.Y * ny;
                if (proj < bMin) bMin = proj;
                if (proj > bMax) bMax = proj;
            }

            if (aMax < bMin || bMax < aMin)
            {
                return true;
            }
        }

        return false;
    }

    public AbstShape()
    {
    }

    public AbstShape(List<Vec2D<uint>> vecs)
    {
        _vecs = vecs;
    }

    public abstract bool PointIsInShape(Vec2D<uint> point);
}
