using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Serialization.Invocation;

namespace AElf.ExceptionHandler;

public class AttributeCallFilter : IIncomingGrainCallFilter
{
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (context.ImplementationMethod.IsDefined(typeof(ExceptionHandlerAttribute), true))
        {
            var exceptionHandler = context.TargetContext.ActivationServices.GetService<ExceptionHandler>();

            if (exceptionHandler == null)
            {
                throw new Exception("ExceptionHandler is not registered.");
            }

            var arguments = new object?[context.Request.GetArgumentCount()];
            for (var i = 0; i < context.Request.GetArgumentCount(); ++i)
            {
                arguments[i] = context.Request.GetArgument(i);
            }
            
            var methodExecutionArgs = new MethodExecutionArgs
            {
                TargetObject = context.Grain,
                MethodInfo = context.ImplementationMethod,
                Arguments = arguments!,
                ReturnValue = null,
                Exception = null,
                Invocation = async () =>
                {
                    await context.Invoke();
                }
            };

            await exceptionHandler.InterceptAsync(methodExecutionArgs);

            if (methodExecutionArgs.Exception != null)
            {
                context.Response = new ExceptionResponse
                {
                    Exception = methodExecutionArgs.Exception
                };
            }
            else
            {
                if (methodExecutionArgs.ReturnValue != null)
                {
                    var responseType = typeof(Response<>).MakeGenericType(methodExecutionArgs.ReturnValue.GetType());
                    var response = Activator.CreateInstance(responseType);
                    responseType.GetProperty("Result")?.SetValue(response, methodExecutionArgs.ReturnValue);
                    context.Response = (Response)response!;
                }
            }
        }
        else
        {
            await context.Invoke();
        }
    }
}