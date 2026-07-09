using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePostDto createPostDto)
    {
        if (!ModelState.IsValid) return View(createPostDto);
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var userName = GetUserName();
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var createdPost = await postService.AddPostAsync(createPostDto, userId, userName);
        return RedirectToAction("Details", new { id = createdPost.Id });
    }

    [Authorize]
    public async Task<IActionResult> Edit(string id)
    {
        if (!IsValidObjectId(id)) return NotFound();

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        if (post.AuthorId != userId) return Forbid();

        var editPostDto = new EditPostDto
        {
            Title = post.Title,
            Content = post.Content
        };
        return View(editPostDto);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Edit(string id, EditPostDto editPostDto)
    {
        if (!IsValidObjectId(id)) return NotFound();
        if (!ModelState.IsValid) return View(editPostDto);

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        if (post.AuthorId != userId) return Forbid();

        var result = await postService.EditPostAsync(id, editPostDto);
        if (!result) return View(editPostDto);
        return RedirectToAction("Details", new { id });
    }

    [Authorize]
    public async Task<IActionResult> Delete(string id)
    {
        if (!IsValidObjectId(id)) return NotFound();

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        if (post.AuthorId != userId) return Forbid();

        return View(post);
    }

    [Authorize]
    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeletePost(string id)
    {
        if (!IsValidObjectId(id)) return NotFound();

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        if (post.AuthorId != userId) return Forbid();

        var result = await postService.DeletePostAsync(id);
        if (!result) return NotFound();
        return RedirectToAction("Index", "Home");
    }
}