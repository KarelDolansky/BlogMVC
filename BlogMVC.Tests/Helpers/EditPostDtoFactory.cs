using BlogMVC.Models;

namespace BlogMVC.Tests.Helpers;

public class EditPostDtoFactory
{
    private EditPostDto _entity = new EditPostDto
    {
        Title = "Title",
        Content = "Content",
    };

    public EditPostDtoFactory WithTitle(string title)
    {
        _entity.Title = title;
        return this;
    }

    public EditPostDtoFactory WithContent(string content)
    {
        _entity.Content = content;
        return this;
    }

    public EditPostDto Build()
    {
        return _entity;
    }
}