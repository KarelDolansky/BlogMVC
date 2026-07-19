using BlogMVC.Models;
using BlogMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogMVC.Controllers;

/// <summary>
///     MVC controller for CRUD operations on blog posts through the web UI (Razor views).
///     Post details are public; creating/editing/deleting require the user to be logged in
///     and to own the post (AuthorId is checked against the logged-in user).
/// </summary>
public class PostController(IPostService postService) : BaseController
{
    /// <summary>Shows the details of a single post. Returns 404 if the Id is invalid or the post doesn't exist.</summary>
    public async Task<IActionResult> Details(string id)
    {
        if (!IsValidObjectId(id)) return NotFound();
        var post = await postService.GetPostAsync(id);
        if (post == null) return NotFound();
        return View(post);
    }

    /// <summary>Shows the form for creating a new post. Requires the user to be logged in.</summary>
    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    /// <summary>
    ///     Handles the submitted "create post" form. The currently logged-in user (from the
    ///     identity claims) becomes the author. Redirects to the details page on success.
    /// </summary>
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

    /// <summary>
    ///     Shows the edit form, pre-filled with the post's current title, description and content.
    ///     Requires the user to be logged in and to be the post's author (otherwise 403 Forbid).
    /// </summary>
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
            Content = post.Content,
            Description = post.Description
        };
        return View(editPostDto);
    }

    /// <summary>
    ///     Handles the submitted "edit post" form. Requires the user to be logged in and to be the author.
    ///     Returns the form again on validation failure or a failed update, otherwise redirects to details.
    /// </summary>
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

    /// <summary>
    ///     Shows the confirmation page for deleting a post. Requires the user to be logged in and to be the author.
    /// </summary>
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

    /// <summary>
    ///     Confirmed post deletion (POST). Mapped to the "Delete" action via <see cref="ActionNameAttribute" />
    ///     so the form on the confirmation page can post to the same action URL as the GET Delete.
    ///     Redirects to the home page after a successful deletion.
    /// </summary>
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