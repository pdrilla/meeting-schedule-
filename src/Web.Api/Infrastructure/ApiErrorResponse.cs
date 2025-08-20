namespace Web.Api.Infrastructure;

public record ApiErrorResponse(
    string Title,
    string Detail,
    int Status,
    string Type,
    Dictionary<string, object?>? Extensions = null);

public sealed record ValidationErrorResponse(
    string Title,
    string Detail,
    int Status,
    string Type,
    Dictionary<string, string[]> Errors) : ApiErrorResponse(Title, Detail, Status, Type);

public static class ApiErrorResponses
{
    public static ApiErrorResponse BadRequest(string detail) => new(
        "Bad Request",
        detail,
        400,
        "https://tools.ietf.org/html/rfc7231#section-6.5.1");

    public static ApiErrorResponse NotFound(string detail) => new(
        "Not Found",
        detail,
        404,
        "https://tools.ietf.org/html/rfc7231#section-6.5.4");

    public static ApiErrorResponse Conflict(string detail) => new(
        "Conflict",
        detail,
        409,
        "https://tools.ietf.org/html/rfc7231#section-6.5.8");

    public static ValidationErrorResponse ValidationError(Dictionary<string, string[]> errors) => new(
        "Validation Error",
        "One or more validation errors occurred",
        400,
        "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        errors);
}