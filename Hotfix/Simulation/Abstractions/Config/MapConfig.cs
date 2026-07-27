namespace Hotfix.Simulation.Abstractions.Config;

public class MapConfig
{
    public ulong X { get; private set; }
    public ulong Y { get; private set; }

    public void SetSize(ulong x, ulong y)
    {
        X = x;
        Y = y;
    }
}
