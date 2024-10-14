namespace AElf.ExceptionHandler;

public class ReturnTypeMismatchException : Exception
{
    public ReturnTypeMismatchException(string message) : base(message)
    {
    }
}