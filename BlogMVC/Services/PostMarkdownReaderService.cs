using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using BlogMVC.Models;

namespace BlogMVC.Services;

public interface IPostMarkdownReaderService
{
    public List<Post> GetAllPostsFromMarkdown();
}
public class PostMarkdownReaderService(IDeserializer deserializerBuilder):IPostMarkdownReaderService
{
    private readonly string _folderPath="Blog";
    public List<Post> GetAllPostsFromMarkdown()
    {
        var posts = new List<Post>();
        int idCounter = 1;
        
        foreach (var filePath in Directory.EnumerateFiles(_folderPath, "*.md"))
        {
            string fileContent = File.ReadAllText(filePath);

            if (fileContent.StartsWith("---\n") || fileContent.StartsWith("---\r\n"))
            {
                int endOfFrontMatter = fileContent.IndexOf("---", 3, StringComparison.Ordinal);
                
                if (endOfFrontMatter != -1)
                {
                    string yaml = fileContent.Substring(3, endOfFrontMatter - 3).Trim();
                    string content = fileContent.Substring(endOfFrontMatter + 3).Trim();

                    var metadata = deserializerBuilder.Deserialize<MarkdownMetadata>(yaml);

                    if (metadata.Draft) 
                        continue; 

                    var post = new Post(
                        idCounter++,
                        metadata.Title ?? "No Title",
                        content,
                        metadata.Author ?? "Unknown Author",
                        metadata.PublishDate ?? DateTime.MinValue
                    );

                    posts.Add(post);
                }
            }
        }

        return posts;
    }
}