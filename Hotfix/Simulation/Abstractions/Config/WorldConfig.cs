namespace Hotfix.Simulation.Abstractions.Config;

/// <summary>
/// 世界配置：承载与游戏世界相关的参数（地图尺寸、形状生成参数）。
/// 同 MapConfig/ShapeGenConfig 模式：无属性默认值，由 SimulateConfig 构造器注入。
/// </summary>
public class WorldConfig
{
    public MapConfig Map = new();
    public ShapeGenConfig ShapeGen = new();
}
