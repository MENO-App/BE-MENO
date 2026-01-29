namespace Application.Common.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}

public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized") : base(message) { }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Forbidden") : base(message) { }
}

public sealed class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message) { }
}
