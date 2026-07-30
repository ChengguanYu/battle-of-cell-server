using Fantasy.Async;

namespace Entity.Simulation;

/// <summary>
/// 模拟器抽象契约。下沉到 Entity 层作为稳定契约，
/// 实现在 Hotfix 层可热更新，实体组件可安全持有本接口引用。
/// </summary>
public interface ISimulation
{
    public void Run();
    public void Stop();
    public FTask SimTickAsync();
}
