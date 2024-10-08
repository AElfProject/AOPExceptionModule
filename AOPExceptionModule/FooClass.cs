namespace AOPExceptionModule;

public class BooData
{
    public string Message = "Boo!";
    public int Value = 100;
}

public class FooClass
{
    private readonly int _value;
    private readonly BooClass _boo = new BooClass();

    public FooClass(int value)
    {
        _value = value;
    }

    public async Task Execute(int index)
    {
        Console.WriteLine("Hello, World!");

        await Task.Delay(100);
        
        for(int i = 0; i < 10; i++)
        {
            await BooExecute(i);
        }
        // Simulate an exception
        //throw new Exception("Test exception");
    }

    [ExceptionHandler(typeof(Exception), 
        TargetType = typeof(FooClass), 
        MethodName = nameof(HandleException))]
    [ExceptionHandler([typeof(ArgumentException), typeof(InvalidOperationException)], 
                        TargetType = typeof(StaticClass), 
                        MethodName = nameof(StaticClass.HandleException))]
    private async Task BooExecute(int i)
    {
        await _boo.Execute(i);
    }


    public async Task<ExceptionHandlingStrategy> HandleException(Exception ex, int index)
    {
        Console.WriteLine($"Handled exception with index: {_value - index} with exception: {ex.Message}");
        await Task.Delay(100);
        return ExceptionHandlingStrategy.Continue;
    }
    
    [ExceptionHandler(typeof(Exception), TargetType = typeof(FooClass), MethodName = nameof(HandleBooException))]
    public async Task ExecuteBoo(int index, BooData data)
    {
        Console.WriteLine("Hello, World!");

        await Task.Delay(100);
        // Simulate an exception
        throw new Exception("Test exception");
    }
    
    public async Task<ExceptionHandlingStrategy> HandleBooException(Exception ex, int index, BooData boo)
    {
        Console.WriteLine($"Boo Handled exception with index: {_value - index} with exception: {ex.Message}");
        Console.WriteLine($"Boo data: {boo.Message}, {boo.Value}");
        await Task.Delay(100);
        return ExceptionHandlingStrategy.Continue;
    }
}