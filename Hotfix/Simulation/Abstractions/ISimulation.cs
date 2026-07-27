namespace Hotfix.Simulation.Abstractions;

public interface ISimulation
{
    public void Run();
    public void Stop();
    public void SimTick();
}
