using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Volo.Abp.DependencyInjection;

namespace AElf.ExceptionHandler;

public class ExceptionHandler : ITransientDependency, IInterceptor
{
    private readonly ConcurrentDictionary<string, ExceptionHandlerInfo> MethodCache;
    private readonly ConcurrentDictionary<string, Func<object, object[], Task>> FinallyCache;
    private List<ExceptionHandlerAttribute>? Attributes { get; set; } = null;
    
    public ExceptionHandler(ConcurrentDictionary<string, ExceptionHandlerInfo> methodCache, ConcurrentDictionary<string, Func<object, object[], Task>> finallyCache)
    {
        MethodCache = methodCache;
        FinallyCache = finallyCache;
    }

    public async Task InterceptAsync(MethodExecutionArgs args)
    {
        try
        {
            await args.Invocation();
        }
        catch (Exception e)
        {
            OnException(e, args);
        }
        finally
        {
            OnFinally(args);
        }
    }

    private void OnFinally(MethodExecutionArgs args)
    {
        var attributes = GetAttributes(args);
        
        if(attributes == null)
        {
            return;
        }

        foreach (var attribute in attributes)
        {
            if (HandleFinally(args, attribute))
            {
                break;
            }
        }
    }

    private void OnException(Exception ex, MethodExecutionArgs args)
    {
        var attributes = GetAttributes(args);
        
        if(attributes == null)
        {
            return;
        }

        var handled = false;

        foreach (var attribute in attributes)
        {
            // If it's an AggregateException, iterate through inner exceptions
            if (ex is AggregateException aggEx)
            {
                foreach (var innerEx in aggEx.InnerExceptions)
                {
                    handled = HandleInnerException(innerEx, args, attribute);
                }
                if (handled)
                {
                    break;
                }
            }
            else
            {
                handled = HandleInnerException(ex, args, attribute);
                if (handled)
                {
                    break;
                }
            }
        }

        if (!handled)
        {
            throw ex;
        }
    }

    private List<ExceptionHandlerAttribute>? GetAttributes(MethodExecutionArgs args)
    {
        if (Attributes != null)
        {
            return Attributes;
        }
        
        var attributes = args.MethodInfo.GetCustomAttributes<ExceptionHandlerAttribute>()
                        .OrderBy(attr => attr.GetType().MetadataToken);
        
        if (attributes == null)
        {
            return null;
        }
        
        var exceptionHandlerAttributes = attributes.ToList();
        
        if(exceptionHandlerAttributes.Count == 0)
        {
            return null;
        }
        
        Attributes = exceptionHandlerAttributes;

        return Attributes;
    }
    
    private bool HandleFinally(MethodExecutionArgs args, ExceptionHandlerAttribute attribute)
    {
        if(attribute.FinallyMethodName == null || attribute.FinallyTargetType == null)
        {
            return false;
        }
        
        var finallyMethod = GetFinallyMethod(attribute.FinallyTargetType, attribute.FinallyMethodName);
        
        var parameters = args.Arguments.ToArray();

        var task = Task.Run(() => finallyMethod(args.TargetObject, parameters));
        task.Wait();

        return true;
    }

    private bool HandleInnerException(Exception exception, MethodExecutionArgs args, ExceptionHandlerAttribute attribute)
    {
        // If the exception is not of the specified type, return early
        if (!attribute.Exceptions.Any(e => e.IsInstanceOfType(exception)))
        {
            return false;
        }
        
        var exceptionHandlerInfo = GetExceptionHandlerInfo(attribute.TargetType, attribute.MethodName);
        
        var parameters = new object[] { exception };

        if (!exceptionHandlerInfo.ContainsExceptionParamOnly)
        {
            parameters = parameters.Concat(args.Arguments.ToArray()).ToArray();
        }

        var task = Task.Run(() => exceptionHandlerInfo.Method(args.TargetObject, parameters));
        var flowBehavior = task.Result;

        if(task.Exception != null)
        {
            throw task.Exception;
        }
        
        if(flowBehavior.ExceptionHandlingStrategy == ExceptionHandlingStrategy.Rethrow)
        {
            args.Exception = exception;
            throw exception;
        }

        if (flowBehavior.ReturnValue == null)
        {
            return true;
        }

        if(flowBehavior.ExceptionHandlingStrategy == ExceptionHandlingStrategy.Throw)
        {
            var newException = (Exception)flowBehavior.ReturnValue!;
            args.Exception = newException;
            throw newException;
        }
        
        args.Exception = null;
        args.ReturnValue = flowBehavior.ReturnValue;
        return true;
    }
    
    private ExceptionHandlerInfo GetExceptionHandlerInfo(Type targetType, string methodName)
    {
        var cacheKey = CacheKey(targetType, methodName);

        // Try to get the method delegate from the cache
        if (MethodCache.TryGetValue(cacheKey, out var exceptionHandlerInfo))
        {
            // If the delegate exists in the cache, return early
            return exceptionHandlerInfo;
        }
        
        exceptionHandlerInfo = CreateExceptionHandlerInfo(targetType, methodName);

        // Cache the compiled delegate
        MethodCache.TryAdd(cacheKey, exceptionHandlerInfo);
        
        return exceptionHandlerInfo;
    }
    
    private Func<object, object[], Task> GetFinallyMethod(Type targetType, string methodName)
    {
        var cacheKey = CacheKey(targetType, methodName);

        // Try to get the method delegate from the cache
        if (FinallyCache.TryGetValue(cacheKey, out var finallyMethod))
        {
            // If the delegate exists in the cache, return early
            return finallyMethod;
        }
        
        finallyMethod = CreateFinallyMethod(targetType, methodName);

        // Cache the compiled delegate
        FinallyCache.TryAdd(cacheKey, finallyMethod);
        
        return finallyMethod;
    }

    private static string CacheKey(Type targetType, string methodName)
    {
        return $"{targetType.FullName}.{methodName}";
    }

    private static ExceptionHandlerInfo CreateExceptionHandlerInfo(Type targetType, string methodName)
    {
        Func<object, object[], Task<FlowBehavior>> methodToCall;
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

        var lambda = Expression.Lambda<Func<object, object[], Task<FlowBehavior>>>(
            Expression.Convert(methodCall, typeof(Task<FlowBehavior>)),
            instanceParameter, parametersParameter);

        methodToCall = lambda.Compile();
        
        return new ExceptionHandlerInfo()
        {
            Method = methodToCall,
            ContainsExceptionParamOnly = methodParams.Length == 1 && methodParams[0].ParameterType == typeof(Exception)
        };
    }
    
    private static Func<object, object[], Task> CreateFinallyMethod(Type targetType, string methodName)
    {
        Func<object, object[], Task> methodToCall;
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

        var lambda = Expression.Lambda<Func<object, object[], Task>>(
            Expression.Convert(methodCall, typeof(Task)),
            instanceParameter, parametersParameter);

        methodToCall = lambda.Compile();

        return methodToCall;
    }
}