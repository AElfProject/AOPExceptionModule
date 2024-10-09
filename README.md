# AOP Exception Module

A demo of AOP Exception Handling.

- [About The Project](#about-the-project)
- [Getting Started](#getting-started)
  - [Setup](#setup)
  - [Usage](#usage)
  - [Exception Handling Strategies](#exception-handling-strategies)
  - [Multiple Exception Handling](#multiple-exception-handling)
  - [Callback Method](#callback-method)
  - [Finally](#finally)
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

### Setup

Add the following dependency to your project's Module class:

```cs
using AElf.ExceptionHandler;

[DependsOn(
    typeof(AOPExceptionModule)
)]
public class MyTemplateModule : AbpModule
```

This will automatically register the AOPException module and setup your project for AOP Exception Handling.

### Usage
1. Define a Method Returning `FlowBehavior`:
   Create a method in your target class that handles exceptions and returns a Task<ExceptionHandlingStrategy>. The strategy will dictate how the flow of the program should behave (e.g., return, rethrow, throw).

```csharp
public class ExceptionHandlingService
{
    public static async Task<FlowBehavior> HandleException(Exception ex, int i)
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
[ExceptionHandler(typeof(ArgumentNullException), TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException))]
public void SomeMethod(int i)
{
    // Business logic that may throw exceptions
}
```

### Exception Handling Strategies
There are 3 ways to return the exception through the Flow Behavior:
1. Return: The method will return the ReturnValue implemented.
```csharp
public async Task<FlowBehavior> HandleException(Exception ex, string message)
 {
     return new FlowBehavior
     {
         ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
         ReturnValue = true
     }
  }
```
2. Rethrow: The method will rethrow the exception.
```csharp
public async Task<FlowBehavior> HandleException(Exception ex, string message)
{
   return new FlowBehavior
   {
        ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
   }
}
```
3. Throw: The method will throw a new exception based on the ReturnValue implemented.
```csharp
public async Task<FlowBehavior> HandleException(Exception ex, string message)
{
   return new FlowBehavior
   {
        ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
        ReturnValue = new Exception("New Exception")
   }
}
```

### Multiple Exception Handling
You can stack multiple ExceptionHandler attributes on a method to handle multiple exceptions.
```csharp
[ExceptionHandler([typeof(InvalidOperationException), typeof(ArgumentException)], TargetType = typeof(BookAppService), MethodName = nameof(HandleSpecificException))]
[ExceptionHandler(typeof(Exception), TargetType = typeof(BookAppService), MethodName = nameof(HandleException))]
public async Task<BookDto> CreateAsync(CreateBookInput input)
{
    // Business logic that may throw exceptions
}
```
From the example above, the method CreateAsync will handle InvalidOperationException and ArgumentException with the HandleSpecificException method and handle any other exceptions with the HandleException method.

### Callback Method
Signature of the callback method can be either of the following:
1. The callback method must have the same parameter as the method that an exception is thrown from with an addition leading Exception parameter.
```csharp
public async Task<FlowBehavior> HandleSpecificException(Exception ex, CreateBookInput message)
{
    return new FlowBehavior
    {
        ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        ReturnValue = new BookDto()
    };
}
```
2. The callback method must have only the Exception parameter.
```csharp
public async Task<FlowBehavior> HandleException(Exception ex)
{
    return new FlowBehavior
    {
        ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
        ReturnValue = new BookDto()
    };
}
```

### Finally
The Finally method is called after the method execution is completed. The method signature should follow the same signature as the method that an exception was thrown from with a return type of Task instead.
```csharp
[ExceptionHandler(typeof(Exception), TargetType = typeof(BookAppService), MethodName = nameof(HandleException), 
                    FinallyTargetType = typeof(BookAppService), FinallyMethodName = nameof(Finally))]
public async Task<BookDto> CreateAsync(CreateBookInput input)
{
    // Business logic that may throw exceptions
}

public async Task Finally(CreateBookInput message)
{
    // cleanup code
    Console.WriteLine("Finally block");
}
```

## Examples

Example with multiple exception handler:
```csharp
[ExceptionHandler(typeof(InvalidOperationException), TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException))]
[ExceptionHandler(typeof(ArgumentNullException), TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException))]
public void SomeMethod(int i)
{
    // Business logic that may throw exceptions
}
```

Or you can have multiple Exceptions:
```csharp
[ExceptionHandler([typeof(ArgumentNullException), typeof(InvalidOperationException)], TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException))]
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
