using Entity.Simulation;
using Fantasy;
using Hotfix.Simulation.Abstractions;
using Hotfix.Simulation.Abstractions.Config;
using Hotfix.Simulation.Generation;

namespace Hotfix.Simulation.Versions.V1;

public class BattleOfCellV1 : SimBase
{
    public BattleOfCellV1(SimulateConfig config, SimStateEntity simState) : base(config, simState)
    {
        WorldGenerator.Generate(SimState, Config.World, Config.Seed);
    }

    public override void SimTick()
    {
        // TODO: 实现 V1 的仿真步进逻辑
    }
}
