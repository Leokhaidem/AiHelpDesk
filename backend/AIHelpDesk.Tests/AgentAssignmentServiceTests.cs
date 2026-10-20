using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using AIHelpDesk.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIHelpDesk.Tests;

public class AgentAssignmentServiceTests
{
    private static async Task<(AppDbContext Db, UserManager<ApplicationUser> Um, AgentAssignmentService Service,
        FakeTicketHistoryRepository History)> CreateService()
    {
        var db = TestDb.CreateContext();
        await TestDb.SeedRolesAsync(db);
        var um = TestDb.CreateUserManager(db);
        var history = new FakeTicketHistoryRepository();
        return (db, um, new AgentAssignmentService(db, history, NullLogger<AgentAssignmentService>.Instance), history);
    }

    [Fact]
    public async Task TryAssign_AssignsEligibleAgent_WritesAgentAssignedEvent()
    {
        var (db, um, service, history) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent = await TestDb.CreateUserAsync(db, um, "agent1@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id);
        TestDb.AddAgentCategory(db, agent.Id, categoryId);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AnalysisInProgress);
        await db.SaveChangesAsync();

        var assignedId = await service.TryAssignTicketAsync(ticketId);

        Assert.Equal(agent.Id, assignedId);

        var ticket = await db.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.Equal(agent.Id, ticket.AssignedAgentId);
        Assert.Contains(history.Events, e => e.TicketId == ticketId && e.EventType == TicketEventType.AgentAssigned);
    }

    [Fact]
    public async Task TryAssign_PrefersAgentWithFewestOpenTickets()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var busy = await TestDb.CreateUserAsync(db, um, "busy@test.com", roles: new[] { "Agent" });
        var idle = await TestDb.CreateUserAsync(db, um, "idle@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, busy.Id);
        TestDb.AddAgentProfile(db, idle.Id);
        TestDb.AddAgentCategory(db, busy.Id, categoryId);
        TestDb.AddAgentCategory(db, idle.Id, categoryId);
        await TestDb.AddTicketAsync(db, categoryId, TicketStatus.Assigned, assignedAgentId: busy.Id);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AnalysisInProgress);

        var assignedId = await service.TryAssignTicketAsync(ticketId);

        Assert.Equal(idle.Id, assignedId);
    }

    [Fact]
    public async Task TryAssign_SkipsUnavailableAgent()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent = await TestDb.CreateUserAsync(db, um, "agent1@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id, available: false);
        TestDb.AddAgentCategory(db, agent.Id, categoryId);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AnalysisInProgress);

        Assert.Null(await service.TryAssignTicketAsync(ticketId));

        var ticket = await db.Tickets.SingleAsync(t => t.Id == ticketId);
        Assert.Equal(TicketStatus.AnalysisInProgress, ticket.Status);
        Assert.Null(ticket.AssignedAgentId);
    }

    [Fact]
    public async Task TryAssign_SkipsAgentAtCapacity()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent = await TestDb.CreateUserAsync(db, um, "agent1@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id, maxOpen: 1);
        TestDb.AddAgentCategory(db, agent.Id, categoryId);
        await TestDb.AddTicketAsync(db, categoryId, TicketStatus.Assigned, assignedAgentId: agent.Id);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AnalysisInProgress);

        Assert.Null(await service.TryAssignTicketAsync(ticketId));
    }

    [Fact]
    public async Task TryAssign_SkipsInactiveUser()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent = await TestDb.CreateUserAsync(db, um, "agent1@test.com", active: false, roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id);
        TestDb.AddAgentCategory(db, agent.Id, categoryId);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AnalysisInProgress);

        Assert.Null(await service.TryAssignTicketAsync(ticketId));
    }

    [Fact]
    public async Task TryAssign_SkipsUserWithoutAgentRole()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        TestDb.AddAgentProfile(db, employee.Id);
        TestDb.AddAgentCategory(db, employee.Id, categoryId);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AnalysisInProgress);

        Assert.Null(await service.TryAssignTicketAsync(ticketId));
    }

    [Fact]
    public async Task TryAssign_ReturnsNull_WhenNoAgentLinkedToCategory()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AnalysisInProgress);

        Assert.Null(await service.TryAssignTicketAsync(ticketId));
    }

    [Fact]
    public async Task TryAssign_ReturnsNull_WhenTicketHasNoCategory()
    {
        var (db, um, service, _) = await CreateService();
        var ticket = new AIHelpDesk.Domain.Entities.Ticket
        {
            Title = "No category",
            Description = "desc",
            EmployeeId = Guid.NewGuid().ToString(),
            Status = TicketStatus.AnalysisInProgress
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        Assert.Null(await service.TryAssignTicketAsync(ticket.Id));
    }

    [Fact]
    public async Task TryAssign_ReturnsNull_ForUnassignableStatus()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent = await TestDb.CreateUserAsync(db, um, "agent1@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id);
        TestDb.AddAgentCategory(db, agent.Id, categoryId);
        var ticketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.Closed, assignedAgentId: agent.Id);

        Assert.Null(await service.TryAssignTicketAsync(ticketId));
    }

    [Fact]
    public async Task TryAssign_RespectsExcludedAgent()
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

        Assert.Equal(agent2.Id, await service.TryAssignTicketAsync(ticketId, excludeAgentId: agent1.Id));
    }

    [Fact]
    public async Task TryAssign_ThrowsForMissingTicket()
    {
        var (_, _, service, _) = await CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.TryAssignTicketAsync(999));
    }

    [Fact]
    public async Task Sweep_AssignsAllWaitingTickets_WhenCapacityAllows()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent = await TestDb.CreateUserAsync(db, um, "agent1@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id, maxOpen: 10);
        TestDb.AddAgentCategory(db, agent.Id, categoryId);
        await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AwaitingAssignment);
        await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AwaitingAssignment);

        var assigned = await service.SweepAsync();

        Assert.Equal(2, assigned);

        var tickets = await db.Tickets.ToListAsync();
        Assert.All(tickets, t => Assert.Equal(TicketStatus.Assigned, t.Status));
        Assert.All(tickets, t => Assert.Equal(agent.Id, t.AssignedAgentId));
    }

    [Fact]
    public async Task Sweep_RespectsExcludedAgent()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var agent = await TestDb.CreateUserAsync(db, um, "agent1@test.com", roles: new[] { "Agent" });
        TestDb.AddAgentProfile(db, agent.Id);
        TestDb.AddAgentCategory(db, agent.Id, categoryId);
        await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AwaitingAssignment);

        Assert.Equal(0, await service.SweepAsync(excludeAgentId: agent.Id));
    }
}
