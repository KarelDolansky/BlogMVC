using BlogMVC.Models;

namespace BlogMVC.Tests.Helpers;

public class PostFactory
{
    private Post _entity = new Post
    {
        Title = "Title",
        Content = "Content",
        AuthorId = "AuthorId",
        Author = "Author",
    };

    public PostFactory WithId(string id)
    {
        _entity.Id = id;
        return this;
    }

    public PostFactory WithTitle(string title)
    {
        _entity.Title = title;
        return this;
    }

    public PostFactory WithContent(string content)
    {
        _entity.Content = content;
        return this;
    }

    public PostFactory WithAuthorId(string authorId)
    {
        _entity.AuthorId = authorId;
        return this;
    }

    public PostFactory WithAuthor(string author)
    {
        _entity.Author = author;
        return this;
    }

    public PostFactory WithPublishDate(DateTime publishDate)
    {
        _entity.PublishDate = publishDate;
        return this;
    }

    public PostFactory WithModifiedDate(DateTime modifiedDate)
    {
        _entity.ModifiedDate = modifiedDate;
        return this;
    }

    public Post Build()
    {
        return _entity;
    }
}