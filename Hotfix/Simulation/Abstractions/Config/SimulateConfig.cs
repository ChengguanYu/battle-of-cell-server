namespace Hotfix.Simulation.Abstractions.Config;


public class SimulateConfig : SimulateDefaultCfg
{
    void SimulateDefaultCfg()
    {
        Map.SetSize(MAP_X_SIZE,MAP_Y_SIZE);
    }

    public MapConfig Map = new ();

}
