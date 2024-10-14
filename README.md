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
  - [Logging](#logging)
  - [Message](#message)
  - [Return Value](#return-value)
  - [Logging Method Parameters](#logging-method-parameters)
  - [Return Type Exception](#return-type-exception)
- [Limitations](#limitations)
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
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = true
        }
    }
}
```

2. Apply the Aspect to Your Methods:
   Use the ExceptionHandler aspect on the methods where you want to handle exceptions.

```csharp
[ExceptionHandler(typeof(ArgumentNullException), TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException))]
protected virtual Task SomeMethod(int i)
{
    // Business logic that may throw exceptions
}
```

All methods with the ExceptionHandler attribute have 2 requirements:
1. The method must return a Task.
2. The method must be virtual or abstract.

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
4. Continue: The method will continue to the next exception specified or rethrow if there are no other exceptions.
```csharp
public async Task<FlowBehavior> HandleException(Exception ex, string message)
{
   return new FlowBehavior
   {
        ExceptionHandlingStrategy = ExceptionHandlingStrategy.Continue
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

### Logging
The `LogOnly` attribute is used to log exceptions without handling them. This will result in a rethrow. All logs are logged by `ILogger`. If `LogOnly` is set to `false`, the exception will still be logged and will continue to handle the exception through the assigned method. By default, `LogOnly = false`.
```csharp
[ExceptionHandler(typeof(Exception), LogOnly = true)]
protected virtual async Task<bool> SomeMethod(string message)
{
    throw new Exception("boo!");
    return false;
}
```

You may also set the LogLevel for the output of the exception. For example:
```csharp
[ExceptionHandler(typeof(Exception), 
    LogLevel = LogLevel.Information, 
    TargetType = typeof(BookAppService), 
    MethodName = nameof(HandleException))]
protected virtual async Task<bool> SomeMethod(string message)
{
    throw new Exception("boo!");
    return false;
}
```

### Message

You may set customised message for the exception. For example:
```csharp
[ExceptionHandler(typeof(Exception), 
    Message = "Customised message", 
    TargetType = typeof(BookAppService), 
    MethodName = nameof(HandleException))]
protected virtual async Task<bool> SomeMethod(string message)
{
    throw new Exception("boo!");
    return false;
}
```
This would output "Customised message" along with the exception message.

### Return Value

Instead of a callback method, you may set ReturnValue directly in the attribute through ReturnDefault property. For example:
```csharp
[ExceptionHandler([typeof(InvalidOperationException)], ReturnDefault = ReturnDefault.New)]
public override async Task<PagedResultDto<BookDto>> GetListAsync(PagedAndSortedResultRequestDto input)
{
    var thrown = await ShouldThrowInvalidOperationException();
    return await base.GetListAsync(input);
}
```
The above demonstrates how to return a new instance of the return type of the method.

The ReturnDefault property can be set to the following:
1. New: Returns a new instance of the return type.
2. Default: Returns the default value of the return type (i.e. default(T)).
3. None: Indicates not to use the ReturnDefault property.

### Logging Method Parameters

You may log the method parameters by specifying through the `LogTargets`. For example:
```csharp
[ExceptionHandler(typeof(Exception), 
    ReturnDefault = ReturnDefault.Default, 
    LogTargets = ["i", "dummy"])]
protected virtual async Task<decimal> Boo(int i, Dummy dummy, string name = "some name")
{
    throw new Exception("boo!");
    return 10;
}
```
The above will log the values of i and dummy when the exception is thrown.

### Return Type Exception

The program will throw a `ReturnTypeMismatchException` when the return type specified in your `FlowBehavior.ReturnValue` is not the corresponding return type to the method that has thrown.

## Limitations
1. The method must return a Task.
2. The method must be virtual or abstract.
3. Unit Test class methods are not supported.
4. `internal` methods are not supported.

## Examples

Example with multiple exception handler:
```csharp
[ExceptionHandler(typeof(InvalidOperationException), TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException))]
[ExceptionHandler(typeof(ArgumentNullException), TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException))]
public virtual Task SomeMethod(int i)
{
    // Business logic that may throw exceptions
}
```

Or you can have multiple Exceptions:
```csharp
[ExceptionHandler([typeof(ArgumentNullException), typeof(InvalidOperationException)], TargetType = typeof(ExceptionHandlingService), MethodName = nameof(ExceptionHandlingService.HandleException))]
public virtual Task SomeMethod(int i)
{
    // Business logic that may throw exceptions
}
```

## Contributing

If you encounter a bug or have a feature request, please use the [Issue Tracker](https://github.com/AElfProject/aelf-dapp-factory/issues/new). The project is also open to contributions, so feel free to fork the project and open pull requests.

## License

Distributed under the Apache License. See [License](LICENSE) for more information.
Distributed under the MIT License. See [License](LICENSE) for more information.
