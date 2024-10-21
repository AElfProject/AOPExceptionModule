using System;
using Microsoft.Extensions.Logging;

namespace AElf.ExceptionHandler
{
    public interface IExceptionHandlerAttribute
    {
        public Type TargetType { get; set; }
        public string MethodName { get; set; }
        public Type[] Exceptions { get; set; }
        public bool LogOnly { get; set; }
        public LogLevel LogLevel { get; set; }
        public Type? FinallyTargetType { get; set; }
        public string? FinallyMethodName { get; set; }
        public string? Message { get; set; }
        public ReturnDefault ReturnDefault { get; set; }
        public string[]? LogTargets { get; set; }
    }
}