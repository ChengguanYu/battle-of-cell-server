using Entity.Simulation;
using Hotfix.Simulation.Abstractions.Config;

namespace Hotfix.Simulation.Abstractions;

public abstract class SimBase : ISimulation
{
    public SimBase(SimulateConfig config)
    {
        _config = config;
    }

    public SimulateConfig Config => _config;
    protected SimulateConfig _config;
    public void Run()
    {
        if (_state != SimState.Create)
        {
            throw new SimStateException(_state, SimState.Create, nameof(Run));
        }
        _state = SimState.Running;
    }

    public void Stop()
    {
        if (_state != SimState.Running)
        {
            throw new SimStateException(_state, SimState.Running, nameof(Stop));
        }
        _state = SimState.Stop;
    }

    public abstract void SimTick();

    private SimState _state = SimState.Create;
}
