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

var builder = WebApplication.CreateBuilder(args);

// --- Identity / SQLite ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Email confirmation is skipped; new accounts are created locked out instead (see AuthController.Register).
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
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

builder.Services.AddControllers();
builder.Services.AddProblemDetails();


const string frontendCorsPolicy = "Frontend";
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy =>
    {
        policy.WithOrigins(["http://localhost:5173", "http://localhost:4173", .. corsAllowedOrigins])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// --- MongoDB (blog post storage) ---
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDb"));
var connectionStringMongoDb = builder.Configuration.GetSection("MongoDb").GetSection("ConnectionString").Value;
builder.Services.AddSingleton(new MongoClient(connectionStringMongoDb));

// --- Application services ---
// Singletons: stateless, just delegate to the MongoDB client.
builder.Services.AddSingleton<IPostService, PostService>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddSingleton<IPostRepository, PostRepository>();
builder.Services.AddSingleton<ITokenProvider, TokenProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors(frontendCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.Run();