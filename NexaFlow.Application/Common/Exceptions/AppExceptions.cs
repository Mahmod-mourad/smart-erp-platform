namespace NexaFlow.Application.Common.Exceptions;

/// <summary>Base for expected application errors that map to a specific HTTP status.</summary>
public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
    public abstract string ErrorType { get; }
}

public sealed class NotFoundException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.NotFound;
    public override string ErrorType => "not_found";
}

public sealed class ConflictException(string message) : AppException(message)
{
    public override int StatusCode => StatusCodes.Conflict;
    public override string ErrorType => "conflict";
}

public sealed class ForbiddenException(string message = "You do not have access to this resource.")
    : AppException(message)
{
    public override int StatusCode => StatusCodes.Forbidden;
    public override string ErrorType => "forbidden";
}

public sealed class UnauthorizedAppException(string message = "Invalid credentials.")
    : AppException(message)
{
    public override int StatusCode => StatusCodes.Unauthorized;
    public override string ErrorType => "unauthorized";
}

/// <summary>Aggregated field validation errors (from FluentValidation or business rules).</summary>
public sealed class ValidationException : AppException
{
    public override int StatusCode => StatusCodes.UnprocessableEntity;
    public override string ErrorType => "validation_error";
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.") => Errors = errors;

    public ValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = new[] { error } }) { }
}

internal static class StatusCodes
{
    public const int Unauthorized = 401;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int Conflict = 409;
    public const int UnprocessableEntity = 422;
}
