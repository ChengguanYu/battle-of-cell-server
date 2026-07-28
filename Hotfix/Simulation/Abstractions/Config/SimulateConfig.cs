namespace Hotfix.Simulation.Abstractions.Config;


public class SimulateConfig : SimulateDefaultCfg
{
    public SimulateConfig()
    {
        Map.SetSize(MAP_X_SIZE, MAP_Y_SIZE);
    }

    public MapConfig Map = new ();
}
