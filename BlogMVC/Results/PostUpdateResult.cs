namespace BlogMVC.Results;

/// <summary>Outcome of an optimistic-concurrency update attempt, e.g. <see cref="Services.IPostService.EditPostAsync" />.</summary>
public enum PostUpdateResult
{
    /// <summary>The document existed with the expected version and was updated.</summary>
    Success,

    /// <summary>No document with the given Id exists.</summary>
    NotFound,

    /// <summary>The document exists, but someone else already changed it since the caller last read it.</summary>
    Conflict
}