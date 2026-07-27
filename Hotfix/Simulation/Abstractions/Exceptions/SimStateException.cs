namespace Hotfix.Simulation.Abstractions;

public class SimStateException : InvalidOperationException
{
    public SimStateException(SimState current, SimState expected, string operation)
        : base($"模拟器状态转移错误：当前状态为 {current}，无法执行 '{operation}' 操作（期望状态 {expected}）")
    {
        CurrentState = current;
        ExpectedState = expected;
        Operation = operation;
    }

    public SimState CurrentState { get; }

    public SimState ExpectedState { get; }

    public string Operation { get; }
}
