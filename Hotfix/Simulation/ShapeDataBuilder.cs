using System.Collections.Generic;
using Entity.Simulation.Shape;
using Fantasy;

namespace Hotfix.Simulation;

/// <summary>
/// 将模拟器内部 AbstShape 列表转换为协议层 ShapeData 列表。
/// 定点数约定：uint 世界坐标即定点数（单位 0.001），直接以 int64 透传，不做缩放。
/// </summary>
public static class ShapeDataBuilder
{
    /// <summary>
    /// 构造 ShapeData 列表。调用方负责释放（随 WorldInit.Dispose 一并回收）。
    /// </summary>
    public static List<ShapeData> Build(IReadOnlyList<AbstShape> shapes)
    {
        var result = new List<ShapeData>(shapes.Count);
        foreach (var shape in shapes)
        {
            var proto = ShapeData.Create();
            foreach (var v in shape.Vertices)
            {
                var vertex = ShapeVertex.Create();
                vertex.x = (long)v.X;
                vertex.y = (long)v.Y;
                proto.vertices.Add(vertex);
            }
            result.Add(proto);
        }
        return result;
    }
}
