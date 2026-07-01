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

    public async Task<IActionResult> Edit(string id)
    {
        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        var editPostDto = new EditPostDto
        {
            Title = post.Title,
            Content = post.Content
        };
        return View(editPostDto);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, EditPostDto editPostDto)
    {
        if (!ModelState.IsValid) return View(editPostDto);

        var result = await postService.EditPostAsync(id, editPostDto);
        if (!result) return View(editPostDto);
        return RedirectToAction("details", new { id });
    }
}