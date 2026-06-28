using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

public class PostController(IPostService postService) : Controller
{
    public async Task<IActionResult> Details(string id)
    {
        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        return View(post);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePostDto createPostDto)
    {
        if (!ModelState.IsValid) return View(createPostDto);
        var createdPost = await postService.AddPostAsync(createPostDto);
        return RedirectToAction("details", new { id = createdPost.Id });
    }
}