using System.Collections.Generic;

namespace Entity.Simulation.Shape;

/// <summary>
/// 通用凸多边形。顶点按边界顺序（顺/逆时针）给出，构造方需确保凸性。
/// 点包含判定用叉积同侧法；与其它凸多边形碰撞走 SAT 快速路径。
/// </summary>
public sealed class ConvexPolygon : AbstShape
{
    public ConvexPolygon(List<Vec2D<uint>> vertices) : base(vertices)
    {
    }

    /// <summary>凸多边形恒凸。</summary>
    public override bool IsConvex => true;

    /// <summary>
    /// 点是否在凸多边形内部（含边界）。
    /// 遍历每条边，计算 (当前顶点→下一顶点) 与 (当前顶点→point) 的叉积；
    /// 全部叉积同号（≥0 或 ≤0）则表示点在各边同一侧，即点在内。
    /// </summary>
    public override bool PointIsInShape(Vec2D<uint> point)
    {
        var v = _vecs;
        int n = v.Count;
        if (n < 3) return false;

        long? sign = null;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            long cr = Orient(v[i], v[j], point);
            if (cr != 0)
            {
                long s = cr > 0 ? 1 : -1;
                if (sign == null)
                    sign = s;
                else if (sign != s)
                    return false;
            }
        }

        return true;
    }
}
