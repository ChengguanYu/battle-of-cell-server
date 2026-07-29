namespace Hotfix.Simulation.Abstractions.Config;


public class SimulateConfig : SimulateDefaultCfg
{
    public SimulateConfig(int seed = WORLD_SEED)
    {
        World.Map.SetSize(MAP_X_SIZE, MAP_Y_SIZE);
        Seed = seed;
        World.ShapeGen.SetTotalCount(SHAPE_TOTAL_COUNT);
        World.ShapeGen.SetMapMargin(SHAPE_MAP_MARGIN);
        World.ShapeGen.SetTriangleMinSpan(SHAPE_TRI_MIN_SPAN);
        World.ShapeGen.SetTriangleMaxSpan(SHAPE_TRI_MAX_SPAN);
        World.ShapeGen.SetMaxGenerateAttempts(SHAPE_MAX_ATTEMPTS);
        World.ShapeGen.SetTargetShapeArea(SHAPE_TARGET_AREA);
        World.ShapeGen.SetMinInteriorAngleDeg(SHAPE_MIN_ANGLE_DEG);
        World.ShapeGen.SetVertexPool(SHAPE_VERTEX_POOL);
    }

    public int Seed { get; }
    public WorldConfig World = new ();
}
