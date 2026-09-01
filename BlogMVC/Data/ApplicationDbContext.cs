using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlogMVC.Data;

/// <summary>
///     EF Core database context for ASP.NET Core Identity (user accounts, roles, sign-in).
///     Blog posts are NOT part of this context – they are stored separately in MongoDB
///     (see <see cref="BlogMVC.Infrastructure.Repositories.PostRepository"/>). This context runs on SQLite.
/// </summary>
/// <param name="options">EF Core options (connection string, provider) configured in <c>Program.cs</c>.</param>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
}