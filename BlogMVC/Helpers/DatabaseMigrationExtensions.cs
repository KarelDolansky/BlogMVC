using BlogMVC.Data;
using Microsoft.EntityFrameworkCore;

namespace BlogMVC.Helpers;

/// <summary>
///     Startup extension that applies pending EF Core migrations to the Identity SQLite database, creating
///     the schema on a brand-new database file (e.g. a fresh Docker volume). Called once from
///     <see cref="Program" /> right after the app is built, before <see cref="IdentityRoleSeederExtensions" />
///     seeds any roles into it.
/// </summary>
public static class DatabaseMigrationExtensions
{
    /// <summary>
    ///     Applies every pending migration for <see cref="ApplicationDbContext" />; a no-op if the schema is already up
    ///     to date.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}