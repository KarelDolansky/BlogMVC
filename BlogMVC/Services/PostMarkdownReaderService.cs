using BlogMVC.Models;
using YamlDotNet.Serialization;

namespace BlogMVC.Services;

public interface IPostMarkdownReaderService
{
    public List<Post> GetAllPostsFromMarkdown();
}

public class PostMarkdownReaderService(IDeserializer deserializerBuilder) : IPostMarkdownReaderService
{
    private readonly string _folderPath = "Blog";

    public List<Post> GetAllPostsFromMarkdown()
    {
        var posts = new List<Post>();

        foreach (var filePath in Directory.EnumerateFiles(_folderPath, "*.md"))
        {
            var fileContent = File.ReadAllText(filePath);
            var post = StringToPost(fileContent);
            if (post != null) posts.Add(post);
        }

        return posts;
    }

    private Post? StringToPost(string content)
    {
        if (content.StartsWith("---\n") || content.StartsWith("---\r\n"))
        {
            var endOfFrontMatter = content.IndexOf("---", 3, StringComparison.Ordinal);

            if (endOfFrontMatter != -1)
            {
                var yaml = content.Substring(3, endOfFrontMatter - 3).Trim();
                var postContent = content.Substring(endOfFrontMatter + 3).Trim();

                var metadata = deserializerBuilder.Deserialize<MarkdownMetadata>(yaml);

                if (metadata.Draft)
                    return null;

                var post = new Post
                {
                    Title = metadata.Title ?? "No Title",
                    Content = postContent,
                    Author = metadata.Author ?? "Unknown Author",
                    PublishDate = metadata.PublishDate ?? DateTime.MinValue
                };
                return post;
            }
        }

        return null;
    }
}