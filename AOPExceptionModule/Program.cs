using AOPExceptionModule;

public class Program
{
    public static async Task Main(string[] args)
    {
        var foo = new FooClass(50);
        
        /*await foo.ExecuteBoo(42, new BooData
        {
            Message = "Boo! Boo!",
            Value = 200
        });*/
        
        await foo.Execute(40);
        
        Console.WriteLine("End of program.");
    }
}