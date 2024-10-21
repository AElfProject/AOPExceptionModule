using System.Threading.Tasks;

namespace AElf.ExceptionHandler
{
    public interface IInterceptor
    {
        Task InterceptAsync(MethodExecutionArgs args);
    }
}