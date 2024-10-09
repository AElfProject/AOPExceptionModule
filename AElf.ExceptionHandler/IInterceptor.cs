namespace AElf.ExceptionHandler;

public interface IInterceptor
{
    Task InterceptAsync(MethodExecutionArgs args);
}