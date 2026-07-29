using System.Collections.Generic;

namespace Entity.Simulation.Shape;

/// <summary>
/// 轴对齐矩形。由最小/最大两个角点确定，4 个顶点按顺时针给出。
/// 点包含判定直接走 AABB 大小比较，O(1)。
/// </summary>
public sealed class Rectangle : AbstShape
{
    public Rectangle(Vec2D<uint> min, Vec2D<uint> max)
        : base(new List<Vec2D<uint>>
        {
            new Vec2D<uint>(min.X, min.Y),
            new Vec2D<uint>(max.X, min.Y),
            new Vec2D<uint>(max.X, max.Y),
            new Vec2D<uint>(min.X, max.Y)
        })
    {
    }

    /// <summary>矩形恒凸。</summary>
    public override bool IsConvex => true;

    /// <summary>
    /// AABB 点包含判定。O(1) 常数时间。
    /// </summary>
    public override bool PointIsInShape(Vec2D<uint> point)
    {
        var v = _vecs;
        return point.X >= v[0].X && point.X <= v[2].X
            && point.Y >= v[0].Y && point.Y <= v[2].Y;
    }
}
