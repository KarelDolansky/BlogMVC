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
        //var idCounter = 1;

        foreach (var filePath in Directory.EnumerateFiles(_folderPath, "*.md"))
        {
            var fileContent = File.ReadAllText(filePath);

            if (fileContent.StartsWith("---\n") || fileContent.StartsWith("---\r\n"))
            {
                var endOfFrontMatter = fileContent.IndexOf("---", 3, StringComparison.Ordinal);

                if (endOfFrontMatter != -1)
                {
                    var yaml = fileContent.Substring(3, endOfFrontMatter - 3).Trim();
                    var content = fileContent.Substring(endOfFrontMatter + 3).Trim();

                    var metadata = deserializerBuilder.Deserialize<MarkdownMetadata>(yaml);

                    if (metadata.Draft)
                        continue;

                    var post = new Post
                    {
                        //Id = (idCounter++).ToString(),
                        Title = metadata.Title ?? "No Title",
                        Content = content,
                        Author = metadata.Author ?? "Unknown Author",
                        PublishDate = metadata.PublishDate ?? DateTime.MinValue
                    };
                    posts.Add(post);
                }
            }
        }

        return posts;
    }
}