using System;

namespace Hotfix.Simulation.Abstractions.Config;

/// <summary>
/// 世界形状生成参数配置。同 MapConfig 模式：无属性默认值，
/// 默认值由 SimulateConfig 构造器通过 Set* 方法注入。
/// </summary>
public class ShapeGenConfig
{
    public int TotalCount { get; private set; }
    public int MapMargin { get; private set; }
    public int TriangleMinSpan { get; private set; }
    public int TriangleMaxSpan { get; private set; }
    public int MaxGenerateAttempts { get; private set; }
    public long TargetShapeArea { get; private set; }
    public double MinInteriorAngleDeg { get; private set; }
    public int[] VertexPool { get; private set; } = Array.Empty<int>();

    public long TargetShapeArea2 => 2L * TargetShapeArea;
    public double MinInteriorAngleRad => MinInteriorAngleDeg * Math.PI / 180.0;

    public void SetTotalCount(int v) => TotalCount = v;
    public void SetMapMargin(int v) => MapMargin = v;
    public void SetTriangleMinSpan(int v) => TriangleMinSpan = v;
    public void SetTriangleMaxSpan(int v) => TriangleMaxSpan = v;
    public void SetMaxGenerateAttempts(int v) => MaxGenerateAttempts = v;
    public void SetTargetShapeArea(long v) => TargetShapeArea = v;
    public void SetMinInteriorAngleDeg(double v) => MinInteriorAngleDeg = v;
    public void SetVertexPool(int[] v) => VertexPool = v;
}
