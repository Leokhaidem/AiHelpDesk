
using AIHelpDesk.Application.Admin.DTOs;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.API.Controllers;

[ApiController]
[Route("api/admin/categories")]
[Authorize(Roles = "Admin")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public CategoriesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategories()
    {
        var categories = await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(
        CreateCategoryRequest request)
    {
        var name = request.Name.Trim();

        var alreadyExists = await _dbContext.Categories
            .AnyAsync(category => category.Name.ToLower() == name.ToLower());

        if (alreadyExists)
        {
            return Conflict(new
            {
                message = "A category with this name already exists."
            });
        }

        var category = new Category
        {
            Name = name,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync();

        var response = new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };

        return CreatedAtAction(
            nameof(GetCategories),
            new { },
            response);
    }
    
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(
        int id,
        UpdateCategoryRequest request)
    {
        var category = await _dbContext.Categories.FindAsync(id);

        if (category is null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        var name = request.Name.Trim();

        var duplicateExists = await _dbContext.Categories
            .AnyAsync(c =>
                c.Id != id &&
                c.Name.ToLower() == name.ToLower());

        if (duplicateExists)
        {
            return Conflict(new
            {
                message = "Another category already uses this name."
            });
        }

        category.Name = name;
        category.Description = request.Description?.Trim();

        await _dbContext.SaveChangesAsync();

        return Ok(new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateCategoryStatus(
        int id,
        UpdateCategoryStatusRequest request)
    {
        var category = await _dbContext.Categories.FindAsync(id);

        if (category is null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        category.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync();

        return Ok(new
        {
            message = request.IsActive
                ? "Category activated successfully."
                : "Category deactivated successfully.",
            categoryId = category.Id,
            isActive = category.IsActive
        });
    }


}
