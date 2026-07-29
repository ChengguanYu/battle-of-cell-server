using System.Collections.Generic;

namespace Entity.Simulation.Shape;

/// <summary>
/// 三角形：<see cref="AbstShape"/> 的凸多边形子类，由三个顶点确定。
/// 顶点采用 uint 整数坐标，与帧同步确定性保持一致。
/// </summary>
public sealed class Triangle : AbstShape
{
    public Triangle(Vec2D<uint> a, Vec2D<uint> b, Vec2D<uint> c)
        : base(new List<Vec2D<uint>> { a, b, c })
    {
    }

    /// <summary>三角形恒凸。</summary>
    public override bool IsConvex => true;

    public Vec2D<uint> A => _vecs[0];
    public Vec2D<uint> B => _vecs[1];
    public Vec2D<uint> C => _vecs[2];

    /// <summary>
    /// 有符号面积的两倍（叉积）。为 0 表示三点共线（退化三角形）。
    /// </summary>
    public long SignedArea2()
    {
        // (B-A) x (C-A)
        long abx = (long)B.X - A.X;
        long aby = (long)B.Y - A.Y;
        long acx = (long)C.X - A.X;
        long acy = (long)C.Y - A.Y;

        return abx * acy - aby * acx;
    }

    /// <summary>三点共线时为 true，属不可用的退化三角形。</summary>
    public bool IsDegenerate => SignedArea2() == 0;

    /// <summary>
    /// 点是否落在三角形内部（含边界）。用叉积同侧法判定。
    /// </summary>
    public override bool PointIsInShape(Vec2D<uint> point)
    {
        long pa = Cross(A, B, point);
        long pb = Cross(B, C, point);
        long pc = Cross(C, A, point);

        bool allNonNeg = pa >= 0 && pb >= 0 && pc >= 0;
        bool allNonPos = pa <= 0 && pb <= 0 && pc <= 0;

        return allNonNeg || allNonPos;
    }

    private static long Cross(Vec2D<uint> o, Vec2D<uint> a, Vec2D<uint> b)
    {
        long ax = (long)a.X - o.X;
        long ay = (long)a.Y - o.Y;
        long bx = (long)b.X - o.X;
        long by = (long)b.Y - o.Y;

        return ax * by - ay * bx;
    }
}
