namespace AElf.ExceptionHandler;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ExceptionHandlerAttribute : Attribute
{
    public Type TargetType { get; set; }
    public string MethodName { get; set; }
    public Type[] Exceptions { get; set; }
    public Type? FinallyTargetType { get; set; } = null;
    public string? FinallyMethodName { get; set; } = null;

    public ExceptionHandlerAttribute(params Type [] exceptions)
    {
        // loop through to check if all types are exceptions
        if (exceptions.Any(exception => !typeof(Exception).IsAssignableFrom(exception)))
        {
            throw new ArgumentException("All types must be exceptions");
        }

        Exceptions = exceptions;
    }
}