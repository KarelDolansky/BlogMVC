using System.Diagnostics;
using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>
/// Controller for the application's landing pages: post listing on the home page,
/// the privacy policy page, and the generic error page.
/// </summary>
public class HomeController(IPostService postService) : BaseController
{
    /// <summary>Shows the home page with a list of all blog posts.</summary>
    public async Task<IActionResult> Index()
    {
        var posts = await postService.GetPostsAsync();
        return View(posts);
    }

    /// <summary>Shows the static privacy policy page.</summary>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Generic error page the app redirects to in production
    /// (see app.UseExceptionHandler in Program.cs). The response is never cached.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}