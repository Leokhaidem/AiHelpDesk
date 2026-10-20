using AIHelpDesk.Application.Common.Interfaces;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AIHelpDesk.Tests;

/// <summary>In-memory stand-in for the MongoDB ticket history repository.</summary>
public sealed class FakeTicketHistoryRepository : ITicketHistoryRepository
{
    public List<TicketHistoryEvent> Events { get; } = new();

    public Task AddAsync(TicketHistoryEvent historyEvent, CancellationToken ct = default)
    {
        Events.Add(historyEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TicketHistoryEvent>> GetByTicketIdAsync(int ticketId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<TicketHistoryEvent>>(
            Events.Where(e => e.TicketId == ticketId).ToList());
}

/// <summary>Creates in-memory databases, Identity users and domain seed data for tests.</summary>
public static class TestDb
{
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static UserManager<ApplicationUser> CreateUserManager(AppDbContext db)
        => new(
            new UserStore<ApplicationUser>(db),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            new IUserValidator<ApplicationUser>[0],
            new IPasswordValidator<ApplicationUser>[0],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance);

    public static async Task SeedRolesAsync(AppDbContext db)
    {
        if (await db.Roles.AnyAsync())
            return;

        db.Roles.AddRange(
            new IdentityRole { Id = "ADMIN", Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Id = "EMPLOYEE", Name = "Employee", NormalizedName = "EMPLOYEE" },
            new IdentityRole { Id = "AGENT", Name = "Agent", NormalizedName = "AGENT" });

        await db.SaveChangesAsync();
    }

    public static async Task<ApplicationUser> CreateUserAsync(
        AppDbContext db, UserManager<ApplicationUser> um, string email,
        bool active = true, params string[] roles)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = email,
            IsActive = active
        };

        var result = await um.CreateAsync(user, "Password123!");
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        if (roles.Length > 0)
        {
            foreach (var role in roles)
            {
                if (!await db.Roles.AnyAsync(r => r.Name == role))
                {
                    // Identity looks roles up by their normalized name, so set it
                    // explicitly (RoleManager.CreateAsync does this in production).
                    var normalizedName = role.ToUpperInvariant();
                    db.Roles.Add(new IdentityRole { Id = normalizedName, Name = role, NormalizedName = normalizedName });
                }
            }
            await db.SaveChangesAsync();

            var addResult = await um.AddToRolesAsync(user, roles);
            if (!addResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", addResult.Errors.Select(e => e.Description)));
        }

        return user;
    }

    public static void AddAgentProfile(AppDbContext db, string userId, bool available = true, int maxOpen = 10)
        => db.AgentProfiles.Add(new AgentProfile { UserId = userId, IsAvailable = available, MaxOpenTickets = maxOpen });

    public static void AddAgentCategory(AppDbContext db, string userId, int categoryId)
        => db.AgentCategories.Add(new AgentCategory { AgentId = userId, CategoryId = categoryId });

    public static async Task<int> AddCategoryAsync(AppDbContext db, string name = "General", bool active = true)
    {
        var category = new Category { Name = name, IsActive = active };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    public static async Task<int> AddTicketAsync(
        AppDbContext db, int categoryId, TicketStatus status,
        string? assignedAgentId = null, string? employeeId = null)
    {
        var ticket = new Ticket
        {
            Title = $"Ticket {Guid.NewGuid():N}",
            Description = "Test ticket",
            EmployeeId = employeeId ?? Guid.NewGuid().ToString(),
            CategoryId = categoryId,
            Status = status,
            AssignedAgentId = assignedAgentId
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return ticket.Id;
    }
}
