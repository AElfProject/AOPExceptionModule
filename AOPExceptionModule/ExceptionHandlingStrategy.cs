using PostSharp.Aspects;

namespace AOPExceptionModule;

public enum ExceptionHandlingStrategy
{
    Rethrow,
    Continue,
    Return,
    Default,
    ThrowException
}

public static class ExceptionHandlingStrategyExtensions
{
    public static FlowBehavior ToFlowBehavior(this ExceptionHandlingStrategy strategy)
    {
        return strategy switch
        {
            ExceptionHandlingStrategy.Rethrow => FlowBehavior.RethrowException,
            ExceptionHandlingStrategy.Continue => FlowBehavior.Continue,
            ExceptionHandlingStrategy.Return => FlowBehavior.Return,
            ExceptionHandlingStrategy.ThrowException => FlowBehavior.ThrowException,
            _ => FlowBehavior.Default
        };
    }
}