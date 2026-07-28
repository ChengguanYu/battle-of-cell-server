using Entity.Simulation;
using Hotfix.Simulation.Abstractions.Config;

namespace Hotfix.Simulation.Abstractions;

public abstract class SimBase : ISimulation
{
    /// <summary>模拟器内部状态实体（Entity 层，跨热更）。逻辑层只读写不持有副本。</summary>
    public SimStateEntity SimState { get; }

    public SimBase(SimulateConfig config, SimStateEntity simState)
    {
        _config = config;
        SimState = simState;
    }

    public SimulateConfig Config => _config;
    protected SimulateConfig _config;
    public void Run()
    {
        if (SimState.State != Entity.Simulation.SimState.Create)
        {
            throw new SimStateException(SimState.State, Entity.Simulation.SimState.Create, nameof(Run));
        }
        SimState.State = Entity.Simulation.SimState.Running;
    }

    public void Stop()
    {
        if (SimState.State != Entity.Simulation.SimState.Running)
        {
            throw new SimStateException(SimState.State, Entity.Simulation.SimState.Running, nameof(Stop));
        }
        SimState.State = Entity.Simulation.SimState.Stop;
    }

    public abstract void SimTick();
}
