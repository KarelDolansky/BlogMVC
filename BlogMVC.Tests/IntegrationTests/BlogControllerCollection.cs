namespace BlogMVC.Tests.IntegrationTests;

/// <summary>
///     Defines the xUnit "BlogController" collection with parallelization disabled.
///     Tests sharing this collection (integration tests against a real MongoDB instance)
///     run sequentially so they don't collide over the posts collection's data.
/// </summary>
[CollectionDefinition("BlogController", DisableParallelization = true)]
public class BlogControllerCollection
{
}