using Entity.Simulation;
using Hotfix.Simulation.Abstractions;
using Hotfix.Simulation.Abstractions.Config;

namespace Hotfix.Simulation.Versions.V1;

public class BattleOfCellV1 : SimBase
{
    public BattleOfCellV1(SimulateConfig config) : base(config)
    {
    }

    public override void SimTick()
    {
        // TODO: 实现 V1 的仿真步进逻辑
    }
}
