namespace AOPExceptionModule;

public class BooClass
{
    public async Task Execute(int i)
    {
        await Task.Delay(100);

        // Simulate an exception
        if (i % 2 == 0)
        {
            throw new ArgumentException($"Boo Argument exception {i}!");
        }
        throw new InvalidOperationException($"Boo Invalid Operation exception {i}!");
    }
}