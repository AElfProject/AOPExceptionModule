namespace AOPExceptionModule;

public static class StaticClass
{
    public static async Task<ExceptionHandlingStrategy> HandleException(Exception e)
    {
        Console.WriteLine($"Static class handled exception: {e.Message}");
        await Task.Delay(100);
        return ExceptionHandlingStrategy.Return;
    }
}