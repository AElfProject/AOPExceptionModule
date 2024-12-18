using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AElf.ExceptionHandler
{

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ExceptionHandlerAttribute : Attribute, IExceptionHandlerAttribute
    {
        public Type TargetType { get; set; }
        public string MethodName { get; set; }
        public Type[] Exceptions { get; set; }
        public bool LogOnly { get; set; } = false;
        public LogLevel LogLevel { get; set; } = LogLevel.Error;
        public Type? FinallyTargetType { get; set; } = null;
        public string? FinallyMethodName { get; set; } = null;
        public string? Message { get; set; } = null;
        public ReturnDefault ReturnDefault { get; set; } = ReturnDefault.None;
        public string[]? LogTargets { get; set; } = null;

        public ExceptionHandlerAttribute(params Type[] exceptions)
        {
            // loop through to check if all types are exceptions
            if (exceptions.Any(exception => !typeof(Exception).IsAssignableFrom(exception)))
            {
                throw new ArgumentException("All types must be exceptions");
            }

            Exceptions = exceptions;
        }
    }
}