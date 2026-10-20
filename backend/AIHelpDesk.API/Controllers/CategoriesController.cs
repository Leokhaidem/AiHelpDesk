using System.Security.Claims;
using AIHelpDesk.Application.Categories.DTOs;
using AIHelpDesk.Application.Categories.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers;

/// <summary>
/// Category management — Admin only for writes; GET is open to all authenticated users
/// (needed by employees to see categories when creating tickets).
/// Cache is invalidated automatically on every write via CategoryService.
/// </summary>
[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
        => _categoryService = categoryService;

    /// <summary>List all categories. Pass ?activeOnly=true to get only active ones.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool activeOnly = false, CancellationToken ct = default)
    {
        var result = await _categoryService.GetAllAsync(activeOnly, ct);
        return Ok(result);
    }

    /// <summary>Get a single category by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _categoryService.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new category (Admin only). Invalidates the lookup cache.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
        /// <summary>
    /// Public list of active categories (id, name, description only).
    /// Used by the sign-up form before the user has logged in.
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> ListPublic(CancellationToken ct)
    {
        var categories = await _categoryService.GetAllAsync(activeOnly: true, ct);

        return Ok(categories.Select(c => new
        {
            c.Id,
            c.Name,
            c.Description
        }));
    }

    /// <summary>Update a category (Admin only). Invalidates the lookup cache.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateCategoryRequest request, CancellationToken ct)
    {
        var result = await _categoryService.UpdateAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>Activate or deactivate a category (Admin only). Invalidates the lookup cache.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetStatus(
        int id, [FromBody] UpdateCategoryStatusRequest request, CancellationToken ct)
    {
        var result = await _categoryService.SetStatusAsync(id, request.IsActive, ct);
        return Ok(result);
    }
}
