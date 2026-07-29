using System.Collections.Generic;

namespace Entity.Simulation.Shape;

/// <summary>
/// 凹多边形：顶点按边界顺序（顺/逆时针）依次给出，允许存在内凹顶点。
/// 与凸多边形的 SAT 路径不同，相交判定走基类通用算法（逐边线段相交 + 顶点包含），
 /// 点包含采用射线计数法，全程整数运算，满足帧同步确定性。
/// </summary>
public sealed class ConcavePolygon : AbstShape
{
    public ConcavePolygon(List<Vec2D<uint>> vertices) : base(vertices)
    {
    }

    /// <summary>凹多边形非凸。</summary>
    public override bool IsConvex => false;

    /// <summary>
    /// 点是否落在多边形内部（含边界）。用水平射线计数法，全程整数交叉相乘，无除法。
    /// 边界点判定不保证稳定，但其相交场景的边界接触由 SegmentsIntersect 兜底。
    /// </summary>
    public override bool PointIsInShape(Vec2D<uint> point)
    {
        var v = _vecs;
        int n = v.Count;
        if (n < 3)
        {
            return false;
        }

        bool inside = false;
        long py = point.Y;
        long px = point.X;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            long xi = v[i].X;
            long yi = v[i].Y;
            long xj = v[j].X;
            long yj = v[j].Y;

            // 水平射线 y=py 是否穿越边 (j->i)
            if ((yi > py) != (yj > py))
            {
                long dy = yj - yi;
                long num = (xj - xi) * (py - yi);
                long compare = (px - xi) * dy;

                // 交点相对 xi 的偏移 num/dy 是否在 px-xi 右侧，按 dy 符号变换不等号
                if ((dy > 0 && num > compare) || (dy < 0 && num < compare))
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
