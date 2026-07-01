using BlogMVC.Models;

namespace BlogMVC.Tests.Helpers;

public class CreatePostDtoFactory
{
    private CreatePostDto _entity = new CreatePostDto
    {
        Title = "Title",
        Content = "Content",
    };

    public CreatePostDtoFactory WithTitle(string title)
    {
        _entity.Title = title;
        return this;
    }

    public CreatePostDtoFactory WithContent(string content)
    {
        _entity.Content = content;
        return this;
    }

    public CreatePostDto Build()
    {
        return _entity;
    }
}