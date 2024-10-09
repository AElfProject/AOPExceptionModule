using System.Reflection;

namespace AElf.ExceptionHandler;

public class MethodExecutionArgs
{
    public object TargetObject { get; set; }
    public MethodInfo MethodInfo { get; set; }
    public object[] Arguments { get; set; }
    public object? ReturnValue { get; set; }
    public Func<Task> Invocation { get; set; }
    public Exception? Exception { get; set; }
}