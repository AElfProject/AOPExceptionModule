using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.ExceptionHandler;

public class AOPExceptionModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<ExceptionHandlerInterceptor>()
            .AddTransient<IInterceptor, ExceptionHandler>()
            .AddSingleton<ConcurrentDictionary<string, ExceptionHandlerInfo>>()
            .AddSingleton<ConcurrentDictionary<string, Func<object, object[], Task>>>()
            .AddSingleton<IIncomingGrainCallFilter, AttributeCallFilter>();
                        
        context.Services.OnRegistered(options =>
        {
            var methodInfos = options.ImplementationType.GetMethods();
            // Check if any of the class methods is decorated with the ExceptionHandlerAttribute
            foreach (var methodInfo in methodInfos)
            {
                if (methodInfo.IsDefined(typeof(ExceptionHandlerAttribute), true))
                {
                    var result = options.Interceptors.TryAdd<ExceptionHandlerInterceptor>();
                    break;
                }
            }
        });
    }
}