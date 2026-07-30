namespace Entity.Simulation.Shape;

/// <summary>二维向量。用泛型承载整数坐标类型。uint 版为原始像素坐标，long 版为 ×1000 定点数。
/// 几何运算以 ×1000 精度对齐客户端 FP=1000 语义，保证帧同步确定性。</summary>
public class Vec2D<T>
{
    public T X { get; set; } = default!;
    public T Y { get; set; } = default!;

    public Vec2D()
    {
    }

    public Vec2D(T v1, T v2)
    {
        X = v1;
        Y = v2;
    }
}
