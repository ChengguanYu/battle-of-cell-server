using System.Collections.Generic;

namespace Entity.Simulation.Shape;

/// <summary>
/// 凹多边形：顶点按边界顺序（顺/逆时针）依次给出，允许存在内凹多边形。
/// 与凸多边形的 SAT 路径不同，相交判定走基类通用算法（逐边线段相交 + 顶点包含），
/// 点包含采用射线计数法，内部以 ×1000 定点数精度计算射线交点（对齐客户端 idiv 语义），
/// 全程整数运算，满足帧同步确定性。
/// </summary>
public sealed class ConcavePolygon : AbstShape
{
    /// <summary>
    public ConcavePolygon(List<Vec2D<uint>> vertices) : base(vertices)
    {
    }

    /// <summary>凹多边形非凸。</summary>
    public override bool IsConvex => false;

    /// 点是否落在多边形内部（含边界）。uint 入口→×1000 委托给 PointIsInShape1000。
    /// </summary>
    public override bool PointIsInShape(Vec2D<uint> point)
    {
        return PointIsInShape1000((long)point.X * 1000, (long)point.Y * 1000);
    }

    /// <summary>
    /// ×1000 定点数精度的点包含测试（射线计数法），对齐客户端 pointInPolygon。
    /// 顶点转为 ×1000 再参与计算，保证 sub-pixel 偏移也能正确判定。
    /// </summary>
    public override bool PointIsInShape1000(long px1000, long py1000)
    {
        var v = _vecs;
        int n = v.Count;
        if (n < 3) return false;

        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            // 顶点升到 ×1000 再比较，保证跨尺度一致性
            long xi1000 = (long)v[i].X * 1000;
            long yi1000 = (long)v[i].Y * 1000;
            long xj1000 = (long)v[j].X * 1000;
            long yj1000 = (long)v[j].Y * 1000;

            if ((yi1000 > py1000) != (yj1000 > py1000))
            {
                // 在 ×1000 空间下计算射线交点
                long num = (xj1000 - xi1000) * (py1000 - yi1000);
                long dy = yj1000 - yi1000;
                long intersectX1000 = xi1000 + num / dy;
                if (px1000 < intersectX1000)
                {
                    inside = !inside;
                }
            }
        }
        return inside;
    }

    /// <summary>
    /// 凹性自检：相邻边叉积符号存在差异即视为凹多边形。
    /// 仅做简单多边形（非自交）的基础校验；自交多边形也会命中，模板需保证简单性。
    /// </summary>
    public bool IsConcaveShape
    {
        get
        {
            int n = _vecs.Count;
            if (n < 4)
            {
                return false;
            }

            int pos = 0;
            int neg = 0;
            for (int i = 0; i < n; i++)
            {
                var o = _vecs[i];
                var a = _vecs[(i + 1) % n];
                var b = _vecs[(i + 2) % n];
                long cr = Orient(o, a, b);
                if (cr > 0) pos++;
                else if (cr < 0) neg++;
            }

            return pos > 0 && neg > 0;
        }
    }
}
