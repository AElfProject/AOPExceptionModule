# AOP Exception Module

A demo of AOP Exception Handling.

- [About The Project](#about-the-project)
- [Getting Started](#getting-started)
- [Examples](#examples)
- [Contributing](#contributing)
- [License](#license)

## About The Project

This is a C# class named ExceptionHandler which is an aspect designed to handle exceptions in methods through PostSharp's OnMethodBoundaryAspect. It intercepts methods when they throw an exception and allows for custom exception handling logic. The code leverages aspects to capture method execution and provides a strategy to deal with specific types of exceptions.

### Key Components
1. Inheritance from OnMethodBoundaryAspect of PostSharp:  
   The ExceptionHandler class extends OnMethodBoundaryAspect, which allows intercepting the method execution at predefined points, specifically when exceptions are thrown (OnException method).

2. Attributes and Fields:
  - TargetType, MethodName, and Exception define the target method and exception type to handle.

3. Asynchronous Methods:
   Able to catch exceptions from nested asynchronous methods and supports TPL.

### Pros and Cons
Pros:
1. Separation of Concerns:
  - Exception handling logic is decoupled from the business logic, improving code readability and maintainability.

2. Reusability:
  - The aspect can be reused across multiple methods and classes, making it a scalable solution for consistent exception handling.

3. Dynamic Method Invocation:
  - The code uses reflection and expression trees to invoke any method dynamically, allowing for flexibility in how exceptions are handled.

4. Aspect-Oriented Approach:
  - Reduces code duplication related to exception handling by centralizing the logic in one place.

5. Quicker Onboarding:
  - The learning cost is reduced because exception handling is encapsulated in one place without the need to learn PostSharp and reflection related code.

Cons:
1. Performance Overhead:
  - Reflection and dynamic invocation introduce some performance overhead, which might be noticeable in high-throughput systems.

2. Limited Compile-time Safety:
  - Errors related to method names or parameter mismatches will only be detected at runtime, increasing the potential for runtime exceptions.

## Getting Started

Simply run the program to see the demo.

To use the Aspect:
1. Define a Method Returning ExceptionHandlingStrategy:
   Create a method in your target class that handles exceptions and returns a Task<ExceptionHandlingStrategy>. The strategy will dictate how the flow of the program should behave (e.g., retry, rethrow, suppress).

```csharp
public class ExceptionHandlingService
{
    public static async Task<ExceptionHandlingStrategy> HandleException(Exception ex, int i)
    {
        Console.WriteLine($"Handled exception: {ex.Message}");
        await Task.Delay(100);
        return ExceptionHandlingStrategy.Rethrow;
    }
}
```

2. Apply the Aspect to Your Methods:
   Use the ExceptionHandler aspect on the methods where you want to handle exceptions.

```csharp
[ExceptionHandler(TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException), Exception = typeof(ArgumentNullException))]
public void SomeMethod(int i)
{
    // Business logic that may throw exceptions
}
```

## Examples

Example with multiple exception handler:
```csharp
[ExceptionHandler(TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException), Exception = typeof(InvalidOperationException))]
[ExceptionHandler(TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException), Exception = typeof(ArgumentNullException))]
public void SomeMethod(int i)
{
    // Business logic that may throw exceptions
}
```

## Contributing

If you encounter a bug or have a feature request, please use the [Issue Tracker](https://github.com/AElfProject/aelf-dapp-factory/issues/new). The project is also open to contributions, so feel free to fork the project and open pull requests.

## License

Distributed under the Apache License. See [License](LICENSE) for more information.
Distributed under the MIT License. See [License](LICENSE) for more information.
