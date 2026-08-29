namespace BlogMVC.Responses;

/// <summary>Response body reporting one or more human-readable error messages for a failed request.</summary>
public class ErrorResponse
{
    /// <summary>The reasons the request failed.</summary>
    public required IEnumerable<string> Errors { get; init; }
}