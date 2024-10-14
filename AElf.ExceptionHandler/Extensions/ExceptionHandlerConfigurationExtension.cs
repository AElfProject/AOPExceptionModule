using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AElf.ExceptionHandler.Extensions;

public static class ExceptionHandlerConfigurationExtension
{
    public static IServiceCollection AddExceptionHandler(this IServiceCollection services)
    {
        services.TryAddTransient<IInterceptor, ExceptionHandler>();
        services.TryAddSingleton<ConcurrentDictionary<string, ExceptionHandlerInfo>>();
        services.TryAddSingleton<ConcurrentDictionary<string, Func<object, object[], Task>>>();
        services.TryAddSingleton<ConcurrentDictionary<string, List<ExceptionHandlerAttribute>>>();
        return services;
    }
}