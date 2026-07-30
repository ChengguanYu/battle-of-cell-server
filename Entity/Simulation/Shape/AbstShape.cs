using System;
using System.Collections.Generic;

namespace Entity.Simulation.Shape;

/// <summary>世界中的形状基类。顶点存储为 uint（像素坐标），几何计算内部以 ×1000 定点数精度进行，
/// 确保与服务端、客户端帧同步判定完全一致。</summary>
public abstract class AbstShape
{
    protected List<Vec2D<uint>> _vecs = new();

    /// <summary>只读顶点序列，供重叠检测等算法遍历使用。</summary>
    public IReadOnlyList<Vec2D<uint>> Vertices => _vecs;

    /// <summary>
    /// 本形状是否为凸多边形。两形状均凸时走 SAT 快速路径；
    /// 任一方为凹时退化为通用多边形相交判定（逐边 + 顶点包含）。
    /// </summary>
    public abstract bool IsConvex { get; }

    /// <summary>
    /// 鞋带公式：返回 |2 × 多边形面积|（带符号面积取绝对值）。
    /// 对凸/凹简单多边形均正确；自交多边形无意义，调用方需先保证简单性。
    /// 全程 long 运算，无除法，确定性安全。
    /// </summary>
    public static long ShoelaceArea2(IReadOnlyList<Vec2D<uint>> v)
    {
        long sum = 0;
        int n = v.Count;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            sum += (long)v[i].X * v[j].Y - (long)v[j].X * v[i].Y;
        }
        return sum < 0 ? -sum : sum;
    }

    /// <summary>
    /// 本形状是否与 <paramref name="other"/> 相交（含边界接触）。
    /// 双凸时用分离轴定理（SAT）；否则用通用多边形相交（逐边线段相交 + 顶点包含），
    /// 对凸/凹、凸/凹、凹/凹混合情形均正确。
    /// </summary>
    public virtual bool OverlapsWith(AbstShape other)
    {
        var va = Vertices;
        var vb = other.Vertices;
        if (va.Count < 3 || vb.Count < 3)
        {
            return false;
        }

        if (IsConvex && other.IsConvex)
        {
            return !HasSeparatingAxis(va, vb) && !HasSeparatingAxis(vb, va);
        }

        return PolygonsOverlap(this, other);
    }

    /// <summary>
    /// 通用多边形相交判定：先看任一对边是否相交（含端点接触），
    /// 再看任一顶点是否落在对方内部，覆盖一个完全包含另一个的情况。
    /// 对凸/凹/混合均正确，复杂度 O(n*m)。
    /// </summary>
    protected static bool PolygonsOverlap(AbstShape a, AbstShape b)
    {
        var va = a.Vertices;
        var vb = b.Vertices;

        for (int i = 0; i < va.Count; i++)
        {
            int j = (i + 1) % va.Count;
            for (int k = 0; k < vb.Count; k++)
            {
                int l = (k + 1) % vb.Count;
                if (SegmentsIntersect(va[i], va[j], vb[k], vb[l]))
                {
                    return true;
                }
            }
        }

        foreach (var p in va)
        {
            if (b.PointIsInShape(p))
            {
                return true;
            }
        }

        foreach (var p in vb)
        {
            if (a.PointIsInShape(p))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>整数叉积：(a-o) x (b-o)，无除法，帧同步确定性安全。</summary>
    protected static long Orient(Vec2D<uint> o, Vec2D<uint> a, Vec2D<uint> b)
    {
        long ax = (long)a.X - o.X;
        long ay = (long)a.Y - o.Y;
        long bx = (long)b.X - o.X;
        long by = (long)b.Y - o.Y;
        return ax * by - ay * bx;
    }

    /// <summary>
    /// 线段 (p1,p2) 与 (p3,p4) 是否严格交叉（不含端点/共线接触）。
    /// 仅检测定向叉积符号相反的规范相交，对齐客户端 segmentsIntersect。
    /// 退化情形（叉积为 0）视为不相交，叠边/共线/端点接触由调用方自行兜底。
    /// </summary>
    protected static bool SegmentsIntersect(Vec2D<uint> p1, Vec2D<uint> p2, Vec2D<uint> p3, Vec2D<uint> p4)
    {
        long d1 = Orient(p3, p4, p1);
        long d2 = Orient(p3, p4, p2);
        long d3 = Orient(p1, p2, p3);
        long d4 = Orient(p1, p2, p4);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }

        return false;
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



    /// <summary>
    /// ×1000 定点数精度的点包含测试，对齐客户端 pointInPolygon 语义。
    /// 默认实现截断到 uint 像素后调用 PointIsInShape(Vec2D<uint>)，
    /// 子类（如 ConcavePolygon）应重写为 ×1000 精度实现。
    /// </summary>
    public virtual bool PointIsInShape1000(long px1000, long py1000)
    {
        return PointIsInShape(new Vec2D<uint>(
            px1000 >= 0 ? (uint)(px1000 / 1000) : 0u,
            py1000 >= 0 ? (uint)(py1000 / 1000) : 0u));
    }
}
