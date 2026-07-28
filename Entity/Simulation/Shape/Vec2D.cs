namespace Entity.Simulation.Shape;

/// <summary>二维向量。用泛型承载整数坐标类型，满足帧同步确定性。</summary>
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
