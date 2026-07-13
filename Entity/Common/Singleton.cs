namespace Entity.Common;

/// <summary>
/// 线程安全的泛型单例基类。
/// 子类写 <c>sealed class Foo : Singleton&lt;Foo&gt;</c>，并保留 private 构造函数即可通过 <c>Foo.Instance</c> 访问。
/// </summary>
/// <typeparam name="T">具体单例类型（自身）</typeparam>
public abstract class Singleton<T> where T : Singleton<T>
{
    // 类型初始化器保证线程安全；Lazy 提供首次访问时再创建
    private static readonly Lazy<T> LazyInstance = new(CreateInstance, LazyThreadSafetyMode.ExecutionAndPublication);

    public static T Inst => LazyInstance.Value;

    protected Singleton()
    {
        // 防止 new 出错误的派生类型：只能是 T 自身
        if (GetType() != typeof(T))
        {
            throw new InvalidOperationException(
                $"Singleton 仅允许自身作为实例类型，期望 {typeof(T).FullName}，实际为 {GetType().FullName}");
        }
    }

    private static T CreateInstance()
    {
        // 允许子类使用 private 构造函数，避免外部 new T()
        var instance = (T?)Activator.CreateInstance(typeof(T), nonPublic: true);
        if (instance is null)
        {
            throw new InvalidOperationException(
                $"{typeof(T).FullName} 必须提供无参构造函数（可为 private）才能作为 Singleton。");
        }

        return instance;
    }
}
