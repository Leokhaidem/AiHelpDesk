using AIHelpDesk.Application.AgentProfile.DTOs;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using AIHelpDesk.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIHelpDesk.Tests;

public class AgentProfileServiceTests
{
    private static async Task<(AppDbContext Db, UserManager<ApplicationUser> Um, AgentProfileService Service,
        FakeTicketHistoryRepository History)> CreateService()
    {
        var db = TestDb.CreateContext();
        await TestDb.SeedRolesAsync(db);
        var um = TestDb.CreateUserManager(db);
        var history = new FakeTicketHistoryRepository();
        var assignment = new AgentAssignmentService(db, history, NullLogger<AgentAssignmentService>.Instance);
        return (db, um,
            new AgentProfileService(db, um, assignment, history, NullLogger<AgentProfileService>.Instance),
            history);
    }

    [Fact]
    public async Task GetMyProfile_NoProfile_ReturnsDefaults()
    {
        var (db, um, service, _) = await CreateService();
        var agent = await TestDb.CreateUserAsync(db, um, "agent@test.com", roles: new[] { "Agent" });

        var profile = await service.GetMyProfileAsync(agent.Id);

        Assert.Equal(agent.Id, profile.Id);
        Assert.True(profile.IsAvailable);
        Assert.Equal(10, profile.MaxOpenTickets);
        Assert.Equal(0, profile.OpenTicketCount);
        Assert.Empty(profile.Categories);
    }

    [Fact]
    public async Task GetMyProfile_CountsOnlyOpenTickets()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent = await TestDb.CreateUserAsync(db, um, "agent@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id);
        await TestDb.AddTicketAsync(db, categoryId, TicketStatus.Assigned, assignedAgentId: agent.Id);
        await TestDb.AddTicketAsync(db, categoryId, TicketStatus.InProgress, assignedAgentId: agent.Id);
        await TestDb.AddTicketAsync(db, categoryId, TicketStatus.Resolved, assignedAgentId: agent.Id);

        var profile = await service.GetMyProfileAsync(agent.Id);

        Assert.Equal(2, profile.OpenTicketCount);
    }

    [Fact]
    public async Task GetMyProfile_MissingUser_Throws()
    {
        var (_, _, service, _) = await CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetMyProfileAsync("missing-user"));
    }

    [Fact]
    public async Task SetAvailability_NonAgent_Throws()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SetAvailabilityAsync(
                employee.Id, new SetAgentAvailabilityRequest { IsAvailable = false }));
    }

    [Fact]
    public async Task SetCategories_ReplacesExistingLinks()
    {
        var (db, um, service, _) = await CreateService();
        var categoryIdA = await TestDb.AddCategoryAsync(db, "A");
        var categoryIdB = await TestDb.AddCategoryAsync(db, "B");
        var categoryIdC = await TestDb.AddCategoryAsync(db, "C");
        var agent = await TestDb.CreateUserAsync(db, um, "agent@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id);
        TestDb.AddAgentCategory(db, agent.Id, categoryIdA);
        TestDb.AddAgentCategory(db, agent.Id, categoryIdB);
        await db.SaveChangesAsync();

        var result = await service.SetCategoriesAsync(agent.Id, new List<int> { categoryIdB, categoryIdC });

        Assert.Equal(2, result.Count);

        var linkIds = await db.AgentCategories
            .Where(ac => ac.AgentId == agent.Id)
            .Select(ac => ac.CategoryId)
            .OrderBy(id => id)
            .ToListAsync();
        Assert.Equal(new[] { categoryIdB, categoryIdC }, linkIds);
    }

    [Fact]
    public async Task SetCategories_Empty_Throws()
    {
        var (db, um, service, _) = await CreateService();
        var agent = await TestDb.CreateUserAsync(db, um, "agent@test.com", roles: new[] { "Agent" });

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetCategoriesAsync(agent.Id, Array.Empty<int>()));
    }

    [Fact]
    public async Task SetCategories_UnknownCategory_Throws()
    {
        var (db, um, service, _) = await CreateService();
        var agent = await TestDb.CreateUserAsync(db, um, "agent@test.com", roles: new[] { "Agent" });

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetCategoriesAsync(agent.Id, new List<int> { 9999 }));
    }

    [Fact]
    public async Task SetAvailability_Unavailable_SweepAssignsToOtherAgent()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent1 = await TestDb.CreateUserAsync(db, um, "agent1@test.com", roles: new[] { "Agent" });
        var agent2 = await TestDb.CreateUserAsync(db, um, "agent2@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent1.Id);
        TestDb.AddAgentProfile(db, agent2.Id);
        TestDb.AddAgentCategory(db, agent1.Id, categoryId);
        TestDb.AddAgentCategory(db, agent2.Id, categoryId);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AwaitingAssignment);

        var profile = await service.SetAvailabilityAsync(
            agent1.Id, new SetAgentAvailabilityRequest { IsAvailable = false });

        Assert.False(profile.IsAvailable);

        var ticket = await db.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.Equal(agent2.Id, ticket.AssignedAgentId);
    }

    [Fact]
    public async Task SetAvailability_BecomingAvailable_SweepPicksUpWaitingTicket()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent = await TestDb.CreateUserAsync(db, um, "agent@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id, available: false);
        TestDb.AddAgentCategory(db, agent.Id, categoryId);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AwaitingAssignment);

        var profile = await service.SetAvailabilityAsync(
            agent.Id, new SetAgentAvailabilityRequest { IsAvailable = true });

        Assert.True(profile.IsAvailable);

        var ticket = await db.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.Equal(agent.Id, ticket.AssignedAgentId);
    }

    [Fact]
    public async Task SetAvailability_InvalidCapacity_Throws()
    {
        var (db, um, service, _) = await CreateService();
        var agent = await TestDb.CreateUserAsync(db, um, "agent@test.com", roles: new[] { "Agent" });

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetAvailabilityAsync(
                agent.Id, new SetAgentAvailabilityRequest { IsAvailable = true, MaxOpenTickets = 0 }));
    }

    [Fact]
    public async Task EnsureAgent_CreatesProfile_AndOptionallyLinks()
    {
        var (db, um, service, _) = await CreateService();
        var categoryIdA = await TestDb.AddCategoryAsync(db, "A");
        var categoryIdB = await TestDb.AddCategoryAsync(db, "B");
        var agent = await TestDb.CreateUserAsync(db, um, "agent@test.com", roles: new[] { "Agent" });

        var withoutCategories = await service.EnsureAgentAsync(agent.Id, null);
        Assert.Empty(withoutCategories);
        Assert.Equal(1, await db.AgentProfiles.CountAsync(p => p.UserId == agent.Id));

        var withCategories = await service.EnsureAgentAsync(agent.Id, new List<int> { categoryIdA, categoryIdB });
        Assert.Equal(2, withCategories.Count);
        Assert.Equal(2, await db.AgentCategories.CountAsync(ac => ac.AgentId == agent.Id));

        // Idempotent: running again does not duplicate the profile row.
        await service.EnsureAgentAsync(agent.Id, null);
        Assert.Equal(1, await db.AgentProfiles.CountAsync(p => p.UserId == agent.Id));
    }

    [Fact]
    public async Task Deactivate_RequeuesAssigned_AndFlagsInProgress()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent = await TestDb.CreateUserAsync(db, um, "agent@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id);
        TestDb.AddAgentCategory(db, agent.Id, categoryId);
        var assignedId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.Assigned, assignedAgentId: agent.Id);
        var inProgressId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.InProgress, assignedAgentId: agent.Id);

        var result = await service.DeactivateAgentAsync(agent.Id, "admin-1");

        Assert.Equal(1, result.RequeuedTicketCount);
        Assert.Equal(1, result.FlaggedTicketCount);

        var requeued = await db.Tickets.SingleAsync(t => t.Id == assignedId);
        Assert.Equal(TicketStatus.AwaitingAssignment, requeued.Status);
        Assert.Null(requeued.AssignedAgentId);

        var inProgress = await db.Tickets.SingleAsync(t => t.Id == inProgressId);
        Assert.Equal(TicketStatus.InProgress, inProgress.Status);
        Assert.Equal(agent.Id, inProgress.AssignedAgentId);

        Assert.Equal(0, await db.AgentCategories.CountAsync(ac => ac.AgentId == agent.Id));

        var profile = await db.AgentProfiles.SingleAsync(p => p.UserId == agent.Id);
        Assert.False(profile.IsAvailable);
    }

    [Fact]
    public async Task Deactivate_SweepReassignsRequeuedTickets()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var departing = await TestDb.CreateUserAsync(db, um, "agent1@test.com", roles: new[] { "Agent" });
        var remaining = await TestDb.CreateUserAsync(db, um, "agent2@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, departing.Id);
        TestDb.AddAgentProfile(db, remaining.Id);
        TestDb.AddAgentCategory(db, departing.Id, categoryId);
        TestDb.AddAgentCategory(db, remaining.Id, categoryId);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.Assigned, assignedAgentId: departing.Id);

        var result = await service.DeactivateAgentAsync(departing.Id, "admin-1");

        Assert.Equal(1, result.RequeuedTicketCount);
        Assert.Equal(1, result.AutoAssignedTicketCount);

        var ticket = await db.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.Equal(remaining.Id, ticket.AssignedAgentId);
    }
}
