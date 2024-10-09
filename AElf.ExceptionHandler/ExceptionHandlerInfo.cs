namespace AElf.ExceptionHandler;

public class ExceptionHandlerInfo
{
    public Func<object, object[], Task<FlowBehavior>> Method { get; set; }
    public bool ContainsExceptionParamOnly { get; set; }
}