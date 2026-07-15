using System.Text;
using BlogMVC.Data;
using BlogMVC.Infrastructure.Interfaces;
using BlogMVC.Infrastructure.Providers;
using BlogMVC.Infrastructure.Repositories;
using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

// Application entry point (minimal hosting model). Wires up the DI container,
// the middleware pipeline, and starts the web server.
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// --- Identity / SQLite ---
// User accounts and sign-in (ASP.NET Core Identity) run on top of a SQLite database.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Register Identity: requires a confirmed account (email) to sign in.
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

// MVC controllers + views; Razor Runtime Compilation allows editing .cshtml files without a rebuild.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// --- MongoDB (blog post storage) ---
// Settings (connection string, database and collection names) are read from the "MongoDb" section in appsettings.json.
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));
var connectionStringMongoDb = builder.Configuration.GetSection("MongoDb").GetSection("ConnectionString").Value;
// MongoClient is safe to share as a singleton across the whole application (it pools connections internally).
builder.Services.AddSingleton(new MongoClient(connectionStringMongoDb));

// --- Application services (Dependency Injection) ---
// Registered as singletons since they are stateless and simply delegate to the MongoDB client.
builder.Services.AddSingleton<IPostService, PostService>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddSingleton<IPostRepository, PostRepository>();
builder.Services.AddSingleton<ITokenService, TokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // In development, allow running EF Core migrations directly from the browser error page.
    app.UseMigrationsEndPoint();
}
else
{
    // In production, unhandled exceptions redirect to the generic error page and the stack trace is hidden.
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// Maps static files (wwwroot) with fingerprinting/caching support (.NET 9 MapStaticAssets).
app.MapStaticAssets();

// Default conventional route: /{Controller}/{Action}/{id?}, defaulting to HomeController.Index.
app.MapControllerRoute(
        "default",
        "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Razor Pages – used for the built-in Identity pages (registration, login, account management).
app.MapRazorPages()
    .WithStaticAssets();

app.Run();