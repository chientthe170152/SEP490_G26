namespace MTCA.Application.Common.Models;

public enum ErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Locked,
    TooManyRequests,
    Unexpected
}

public sealed record Error(string Code, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, ErrorType.Unexpected);

    public static Error Validation(string code) => new(code, ErrorType.Validation);
    public static Error Unauthorized(string code) => new(code, ErrorType.Unauthorized);
    public static Error Forbidden(string code) => new(code, ErrorType.Forbidden);
    public static Error NotFound(string code) => new(code, ErrorType.NotFound);
    public static Error Conflict(string code) => new(code, ErrorType.Conflict);
    public static Error Locked(string code) => new(code, ErrorType.Locked);
    public static Error TooManyRequests(string code) => new(code, ErrorType.TooManyRequests);
    public static Error Unexpected(string code) => new(code, ErrorType.Unexpected);
}
