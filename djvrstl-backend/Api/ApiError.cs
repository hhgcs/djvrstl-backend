namespace Djvrstl.Backend.Api;

public sealed record ApiErrorEnvelope(ApiError Error);

public sealed record ApiError(string Code, string Message, IReadOnlyDictionary<string, string>? Fields = null);

public static class ApiErrors
{
    public static ApiErrorEnvelope Validation(string message, IReadOnlyDictionary<string, string> fields)
    {
        return new ApiErrorEnvelope(new ApiError("VALIDATION_ERROR", message, fields));
    }

    public static ApiErrorEnvelope BusinessRule(string message)
    {
        return new ApiErrorEnvelope(new ApiError("BUSINESS_RULE_ERROR", message));
    }
}
