namespace Hotfix.Simulation.Abstractions.Config;

public class SimulateDefaultCfg
{
    protected const ulong MAP_X_SIZE = 15000;
    protected const ulong MAP_Y_SIZE = 15000;
    //========================================
    // 世界形状生成默认参数
    //========================================
    protected const int SHAPE_TOTAL_COUNT = 12;
    protected const int SHAPE_MAP_MARGIN = 50;
    protected const int SHAPE_TRI_MIN_SPAN = 20;
    protected const int SHAPE_TRI_MAX_SPAN = 500;
    protected const int SHAPE_MAX_ATTEMPTS = 5000;
    protected const long SHAPE_TARGET_AREA = 50000;
    protected const double SHAPE_MIN_ANGLE_DEG = 15.0;
    // 顶点数池（3=三角形，4~12=凸/凹多边形）
    protected static readonly int[] SHAPE_VERTEX_POOL = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
    protected const int WORLD_SEED = 20260728;
}
