using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common
{
    public sealed class Error
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public IDictionary<string, string[]>? Details { get; init; }

        public static Error NotFound(string message = "Not found") =>
            new() { Code = "NotFound", Message = message };

        public static Error Validation(string message = "Validation failed", IDictionary<string, string[]>? details = null) =>
            new() { Code = "ValidationError", Message = message, Details = details };

        public static Error Conflict(string message = "Conflict") =>
            new() { Code = "Conflict", Message = message };

        public static Error BadRequest(string message = "Bad request") =>
            new() { Code = "BadRequest", Message = message };
    }

    public readonly struct Result
    {
        public bool IsSuccess { get; init; }
        public Error? Error { get; init; }

        public static Result Success() => new() { IsSuccess = true };
        public static Result Failure(Error error) => new() { IsSuccess = false, Error = error };
    }

    public readonly struct Result<T>
    {
        public bool IsSuccess { get; init; }
        public T? Value { get; init; }
        public Error? Error { get; init; }

        public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
        public static Result<T> Failure(Error error) => new() { IsSuccess = false, Error = error };
    }
}

