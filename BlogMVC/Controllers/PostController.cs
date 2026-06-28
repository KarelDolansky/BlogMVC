using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

public class PostController(IPostService postService) : Controller
{
    public async Task<IActionResult> Details(string? id)
    {
        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        return View(post);
    }
}