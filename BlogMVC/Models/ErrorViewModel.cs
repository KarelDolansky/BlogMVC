namespace BlogMVC.Models;

/// <summary>
/// Model for the error page (Views/Shared/Error.cshtml).
/// Carries the current request id, which can be shown to the user for diagnostics/support.
/// </summary>
public class ErrorViewModel
{
    /// <summary>Identifier of the current HTTP request (Activity.Current.Id or TraceIdentifier).</summary>
    public string? RequestId { get; set; }

    /// <summary>Whether the view should display the RequestId (true if it is not empty).</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}