using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.ExceptionHandler;

public class ExceptionHandler : ITransientDependency, IInterceptor
{
    private class Result
    {
        public bool Handled { get; set; } = false;
        public bool Rethrow { get; set; } = false;
    }
    
    private readonly ConcurrentDictionary<string, ExceptionHandlerInfo> MethodCache;
    private readonly ConcurrentDictionary<string, Func<object, object[], Task>> FinallyCache;
    private readonly ConcurrentDictionary<string, List<ExceptionHandlerAttribute>> AttributeCache;
    private readonly IServiceProvider _serviceProvider;
    
    public ExceptionHandler(ConcurrentDictionary<string, ExceptionHandlerInfo> methodCache, ConcurrentDictionary<string, Func<object, object[], Task>> finallyCache, IServiceProvider serviceProvider, ConcurrentDictionary<string, List<ExceptionHandlerAttribute>> attributeCache)
    {
        MethodCache = methodCache;
        FinallyCache = finallyCache;
        _serviceProvider = serviceProvider;
        AttributeCache = attributeCache;
    }

    public async Task InterceptAsync(MethodExecutionArgs args)
    {
        try
        {
            await args.Invocation();
        }
        catch (Exception e)
        {
            var result = OnException(e, args);
            if (result == null || result.Rethrow || !result.Handled)
            {
                throw;
            }
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

    private Result? OnException(Exception ex, MethodExecutionArgs args)
    {
        var attributes = GetAttributes(args);
        
        if(attributes == null)
        {
            return null;
        }

        var result = new Result
        {
            Handled = false,
            Rethrow = false
        };

        foreach (var attribute in attributes)
        {
            // If it's an AggregateException, iterate through inner exceptions
            if (ex is AggregateException aggEx)
            {
                foreach (var innerEx in aggEx.InnerExceptions)
                {
                    result = HandleInnerException(innerEx, args, attribute);
                }
                if (result.Handled)
                {
                    break;
                }
            }
            else
            {
                result = HandleInnerException(ex, args, attribute);
                if (result.Handled)
                {
                    break;
                }
            }
        }

        return result;
    }

    private List<ExceptionHandlerAttribute>? GetAttributes(MethodExecutionArgs args)
    {
        var key = CacheKey(args.TargetObject.GetType(), args.MethodInfo.Name);
        
        if (AttributeCache != null && !AttributeCache.IsEmpty)
        {
            // Try to get the attributes from the cache
            if (AttributeCache.TryGetValue(key, out var cached))
            {
                return cached;
            }
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
        
        AttributeCache.TryAdd(key, exceptionHandlerAttributes);

        return exceptionHandlerAttributes;
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

    private Result HandleInnerException(Exception exception, MethodExecutionArgs args, ExceptionHandlerAttribute attribute)
    {
        // If the exception is not of the specified type, return early
        if (!attribute.Exceptions.Any(e => e.IsInstanceOfType(exception)))
        {
            return new Result
            {
                Handled = false,
                Rethrow = false
            };
        }
        
        var loggerType = typeof(ILogger<>).MakeGenericType(args.TargetObject.GetType());
        var logger = _serviceProvider.GetService(loggerType);
        // Log the exception
        if(logger != null)
        {
            if (attribute.Message != null)
            {
                ((ILogger)logger).Log(attribute.LogLevel, exception, "Message: {Message} Exception Message: {ExceptionMessage}", attribute.Message, exception.Message);
            }
            else
            {
                ((ILogger)logger).Log(attribute.LogLevel, exception, "Exception Message: {ExceptionMessage}", exception.Message);
            }
        }

        if (attribute.LogOnly)
        {
            args.Exception = exception;
            return new Result
            {
                Handled = true,
                Rethrow = true
            };
        }
        
        if(attribute.ReturnDefault != ReturnDefault.None)
        {
            var returnType = args.MethodInfo.ReturnType;
            Type? genericType = null;

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                genericType = returnType.GetGenericArguments()[0];
            }

            if (genericType != null)
            {
                args.ReturnValue = attribute.ReturnDefault switch
                {
                    ReturnDefault.Default => GetDefaultValue(genericType),
                    ReturnDefault.New => Activator.CreateInstance(genericType)
                };
                
                return new Result
                {
                    Handled = true,
                    Rethrow = false
                };
            }
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
            return new Result
            {
                Handled = true,
                Rethrow = true
            };
        }

        if (flowBehavior.ReturnValue == null)
        {
            return new Result
            {
                Handled = true,
                Rethrow = false
            };
        }

        if(flowBehavior.ExceptionHandlingStrategy == ExceptionHandlingStrategy.Throw)
        {
            var newException = (Exception)flowBehavior.ReturnValue!;
            args.Exception = newException;
            throw newException;
        }
        
        args.Exception = null;
        args.ReturnValue = flowBehavior.ReturnValue;
        return new Result
        {
            Handled = true,
            Rethrow = false
        };
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
    
    private static object GetDefaultValue(Type type)
    {
        if(type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }
}