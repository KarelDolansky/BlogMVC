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
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

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

// API controllers only (no views).
builder.Services.AddControllers();

// Returns RFC 7807 ProblemDetails JSON for unhandled exceptions and non-successful status codes.
builder.Services.AddProblemDetails();

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
    // In production, unhandled exceptions are turned into a generic ProblemDetails JSON response
    // (registered via AddProblemDetails above) instead of the stack trace.
    app.UseExceptionHandler();
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

// Maps the attribute-routed API controllers (AuthController, BlogController).
app.MapControllers();

app.Run();