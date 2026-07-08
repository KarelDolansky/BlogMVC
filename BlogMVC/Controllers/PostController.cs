using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

public class PostController(IPostService postService) : BaseController
{
    public async Task<IActionResult> Details(string id)
    {
        if (!IsValidObjectId(id)) return NotFound();
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
        return RedirectToAction("Details", new { id = createdPost.Id });
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (!IsValidObjectId(id)) return NotFound();
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
        if (!IsValidObjectId(id)) return NotFound();
        if (!ModelState.IsValid) return View(editPostDto);

        var result = await postService.EditPostAsync(id, editPostDto);
        if (!result) return View(editPostDto);
        return RedirectToAction("Details", new { id });
    }

    public async Task<IActionResult> Delete(string id)
    {
        if (!IsValidObjectId(id)) return NotFound();
        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        return View(post);
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeletePost(string id)
    {
        if (!IsValidObjectId(id)) return NotFound();
        var result = await postService.DeletePostAsync(id);
        if (!result) return NotFound();
        return RedirectToAction("Index", "Home");
    }
}