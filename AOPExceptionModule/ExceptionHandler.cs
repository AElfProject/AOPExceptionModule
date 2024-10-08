using System.Collections.Concurrent;
using System.Linq.Expressions;
using PostSharp.Aspects;
using PostSharp.Serialization;

namespace AOPExceptionModule;

[PSerializable]
public class ExceptionHandler : OnMethodBoundaryAspect
{
    private class MethodInfo
    {
        public Func<object, object[], Task<ExceptionHandlingStrategy>> Method { get; set; }
        public bool ContainsExceptionParamOnly { get; set; }
    }
    
    public Type TargetType { get; set; }
    public string MethodName { get; set; }
    public Type Exception { get; set; }

    private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache = new();

    public override void OnException(MethodExecutionArgs args)
    {
        // If it's an AggregateException, iterate through inner exceptions
        if (args.Exception is AggregateException aggEx)
        {
            foreach (var innerEx in aggEx.InnerExceptions)
            {
                HandleInnerException(innerEx, args);
            }
        }
        else
        {
            HandleInnerException(args.Exception, args);
        }
    }
    
    private void HandleInnerException(Exception exception, MethodExecutionArgs args)
    {
        if(!Exception.IsInstanceOfType(args.Exception))
        {
            return;
        }
        
        var methodInfo = GetMethodInfo(TargetType, MethodName);
        
        var parameters = new object[] { args.Exception };

        if (!methodInfo.ContainsExceptionParamOnly)
        {
            parameters = parameters.Concat(args.Arguments.ToArray()).ToArray();
        }

        var strategyTask = Task.Run(() => methodInfo.Method(args.Instance, parameters));
        var strategy = strategyTask.Result;

        // Apply the ExceptionHandlingStrategy returned by the delegate
        args.FlowBehavior = strategy.ToFlowBehavior();
    }
    
    private MethodInfo GetMethodInfo(Type targetType, string methodName)
    {
        var cacheKey = $"{targetType.FullName}.{methodName}";

        // Try to get the method delegate from the cache
        if (MethodCache.TryGetValue(cacheKey, out var methodInfo))
        {
            // If the delegate exists in the cache, return early
            return methodInfo;
        }
        
        methodInfo = CreateMethodInfo(targetType, methodName);

        // Cache the compiled delegate
        MethodCache.TryAdd(cacheKey, methodInfo);
        
        return methodInfo;
    }

    private static MethodInfo CreateMethodInfo(Type targetType, string methodName)
    {
        Func<object, object[], Task<ExceptionHandlingStrategy>> methodToCall;
        var methodInfo = targetType.GetMethod(methodName);
        var methodParams = methodInfo.GetParameters();

        var instanceParameter = Expression.Parameter(typeof(object), "instance");
        var parametersParameter = Expression.Parameter(typeof(object[]), "parameters");

        // We will prepend the exception to the parameters array directly
        var convertedParams = methodParams.Select((param, index) =>
            Expression.Convert(
                Expression.ArrayIndex(parametersParameter, Expression.Constant(index)),
                param.ParameterType)).ToArray();

        var instanceCast = methodInfo.IsStatic ? null : Expression.Convert(instanceParameter, targetType);

        var methodCall = methodInfo.IsStatic
            ? Expression.Call(methodInfo, convertedParams)
            : Expression.Call(instanceCast, methodInfo, convertedParams);

        var lambda = Expression.Lambda<Func<object, object[], Task<ExceptionHandlingStrategy>>>(
            Expression.Convert(methodCall, typeof(Task<ExceptionHandlingStrategy>)),
            instanceParameter, parametersParameter);

        methodToCall = lambda.Compile();
        
        return new MethodInfo
        {
            Method = methodToCall,
            ContainsExceptionParamOnly = methodParams.Length == 1 && methodParams[0].ParameterType == typeof(Exception)
        };
    }
}