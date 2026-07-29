using Fantasy;
using Entity.Simulation.Shape;

namespace Hotfix.Simulation.Abstractions;

/// <summary>
/// 模拟器坐标生成：在合法区域内随机生成一个协调位置，默认半径 20px，超过重试次数后返回失败。
/// 验证坐标是否合法的依据是 SimBase.IsCircleValid（不与任何障碍物相交）。
/// </summary>
public static class CoordGenerator
{
    /// <summary>默认安全半径（px），确保生成坐标与障碍物保持此距离。</summary>
    public const uint DefaultRadius = 20;

    /// <summary>默认最大重试次数。</summary>
    public const int DefaultMaxAttempts = 100;

    /// <summary>
    /// 在模拟器世界范围内随机生成一个合法坐标。
    /// 合法条件：坐标在以 radius 为半径的圆范围内不越过地图边界，且不与任何形状相交。
    /// </summary>
    /// <param name="sim">模拟器实例</param>
    /// <param name="radius">安全半径（默认 20px）</param>
    /// <param name="maxAttempts">最大重试次数（默认 100）</param>
    /// <param name="coord">生成的合法坐标，失败时为 default</param>
    /// <returns>成功返回 true，超过重试次数返回 false</returns>
    public static bool TryGenerateCoord(SimBase sim, out Vec2D<uint> coord, uint radius = DefaultRadius, int maxAttempts = DefaultMaxAttempts)
    {
        coord = default!;

        if (sim.Config.World.Map.X == 0 || sim.Config.World.Map.Y == 0)
        {
            Log.Warning($"CoordGenerator.TryGenerateCoord 失败：世界尺寸无效, x={sim.Config.World.Map.X}, y={sim.Config.World.Map.Y}");
            return false;
        }

        // 安全边距：半径为安全距离，坐标不能离边界太近
        uint margin = radius;
        ulong xLimit = sim.Config.World.Map.X - margin;
        ulong yLimit = sim.Config.World.Map.Y - margin;

        // 如果安全半径超过了地图尺寸，直接失败
        if (margin >= sim.Config.World.Map.X || margin >= sim.Config.World.Map.Y)
        {
            Log.Warning($"CoordGenerator.TryGenerateCoord 失败：半径 {radius} 超过地图尺寸 ({sim.Config.World.Map.X}x{sim.Config.World.Map.Y})");
            return false;
        }

        for (int i = 0; i < maxAttempts; i++)
        {
            var candidate = new Vec2D<uint>(
                (uint)Random.Shared.NextInt64(margin, (long)xLimit),
                (uint)Random.Shared.NextInt64(margin, (long)yLimit)
            );

            if (sim.IsCircleValid(candidate, radius))
            {
                coord = candidate;
                Log.Debug($"CoordGenerator.TryGenerateCoord 成功: x={coord.X}, y={coord.Y}, attempts={i + 1}, radius={radius}");
                return true;
            }
        }

        Log.Warning($"CoordGenerator.TryGenerateCoord 失败：超过最大重试次数 {maxAttempts}, radius={radius}, map={sim.Config.World.Map.X}x{sim.Config.World.Map.Y}");
        return false;
    }
}
