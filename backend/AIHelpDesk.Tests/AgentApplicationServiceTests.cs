using AIHelpDesk.Application.AgentApplications.DTOs;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using AIHelpDesk.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIHelpDesk.Tests;

public class AgentApplicationServiceTests
{
    private static async Task<(AppDbContext Db, UserManager<ApplicationUser> Um, AgentApplicationService Service,
        FakeTicketHistoryRepository History)> CreateService()
    {
        var db = TestDb.CreateContext();
        await TestDb.SeedRolesAsync(db);
        var um = TestDb.CreateUserManager(db);
        var history = new FakeTicketHistoryRepository();
        var assignment = new AgentAssignmentService(db, history, NullLogger<AgentAssignmentService>.Instance);
        return (db, um, new AgentApplicationService(db, um, assignment), history);
    }

    private static Task<AgentApplicationResponse> CreateApplicationAsync(
        AgentApplicationService service, string userId, int categoryId)
        => service.CreateAsync(userId, new CreateAgentApplicationRequest
        {
            CategoryIds = new List<int> { categoryId },
            Message = "Please approve."
        });

    [Fact]
    public async Task ValidateCategories_Empty_Throws()
    {
        var (_, _, service, _) = await CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ValidateCategoriesAsync(Array.Empty<int>()));
    }

    [Fact]
    public async Task ValidateCategories_InactiveOrMissing_Throws()
    {
        var (db, _, service, _) = await CreateService();
        var activeId = await TestDb.AddCategoryAsync(db, "Active");
        var inactiveId = await TestDb.AddCategoryAsync(db, "Inactive", active: false);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ValidateCategoriesAsync(new[] { activeId, inactiveId }));
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ValidateCategoriesAsync(new[] { 9999 }));
    }

    [Fact]
    public async Task ValidateCategories_Duplicates_ReturnsDistinctIds()
    {
        var (db, _, service, _) = await CreateService();
        var id1 = await TestDb.AddCategoryAsync(db, "A");
        var id2 = await TestDb.AddCategoryAsync(db, "B");

        var result = await service.ValidateCategoriesAsync(new[] { id1, id2, id1 });

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Create_Employee_CreatesPendingApplication()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var categoryId = await TestDb.AddCategoryAsync(db);

        var response = await CreateApplicationAsync(service, employee.Id, categoryId);

        Assert.Equal("Pending", response.Status);
        Assert.Equal(employee.Id, response.UserId);
        Assert.Single(response.Categories);

        var application = await db.AgentApplications.SingleAsync();
        Assert.Equal(AgentApplicationStatus.Pending, application.Status);
        Assert.Equal(categoryId, application.Categories.Single().CategoryId);
    }

    [Fact]
    public async Task Create_Agent_Throws()
    {
        var (db, um, service, _) = await CreateService();
        var agent = await TestDb.CreateUserAsync(db, um, "agent@test.com", roles: new[] { "Agent" });
        var categoryId = await TestDb.AddCategoryAsync(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateApplicationAsync(service, agent.Id, categoryId));
    }

    [Fact]
    public async Task Create_SecondPendingApplication_Throws()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var categoryId = await TestDb.AddCategoryAsync(db);
        await CreateApplicationAsync(service, employee.Id, categoryId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateApplicationAsync(service, employee.Id, categoryId));
    }

    [Fact]
    public async Task Create_UnknownUser_Throws()
    {
        var (db, _, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => CreateApplicationAsync(service, "missing-user", categoryId));
    }

    [Fact]
    public async Task GetMyApplication_ReturnsNull_Or_TheApplication()
    {
        var (db, um, service, _) = await CreateService();

        Assert.Null(await service.GetMyApplicationAsync("nobody"));

        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var categoryId = await TestDb.AddCategoryAsync(db, "General");
        await CreateApplicationAsync(service, employee.Id, categoryId);

        var mine = await service.GetMyApplicationAsync(employee.Id);

        Assert.NotNull(mine);
        Assert.Equal("Pending", mine!.Status);
        Assert.Equal("Please approve.", mine.Message);
        Assert.Equal("General", mine.Categories.Single().Name);
    }

    [Fact]
    public async Task Withdraw_Pending_SetsWithdrawn()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var categoryId = await TestDb.AddCategoryAsync(db);
        await CreateApplicationAsync(service, employee.Id, categoryId);

        await service.WithdrawAsync(employee.Id);

        var application = await db.AgentApplications.SingleAsync();
        Assert.Equal(AgentApplicationStatus.Withdrawn, application.Status);
    }

    [Fact]
    public async Task Withdraw_Approved_Throws()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var categoryId = await TestDb.AddCategoryAsync(db);
        var created = await CreateApplicationAsync(service, employee.Id, categoryId);
        await service.ApproveAsync(created.Id, null, "admin-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.WithdrawAsync(employee.Id));
    }

    [Fact]
    public async Task Withdraw_NoApplication_Throws()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.WithdrawAsync(employee.Id));
    }

    [Fact]
    public async Task Approve_SwapsRole_CreatesProfileAndCategories_MarksApproved()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var categoryId = await TestDb.AddCategoryAsync(db);
        var created = await CreateApplicationAsync(service, employee.Id, categoryId);

        var approved = await service.ApproveAsync(created.Id, null, "admin-1");

        Assert.Equal("Approved", approved.Status);
        Assert.True(await um.IsInRoleAsync(employee, "Agent"));
        Assert.False(await um.IsInRoleAsync(employee, "Employee"));

        var profile = await db.AgentProfiles.SingleAsync(p => p.UserId == employee.Id);
        Assert.True(profile.IsAvailable);

        var link = await db.AgentCategories.SingleAsync(ac => ac.AgentId == employee.Id);
        Assert.Equal(categoryId, link.CategoryId);

        var application = await db.AgentApplications.SingleAsync();
        Assert.Equal(AgentApplicationStatus.Approved, application.Status);
        Assert.Equal("admin-1", application.ReviewedByAdminId);
        Assert.NotNull(application.ReviewedAt);
    }

    [Fact]
    public async Task Approve_WithCategoryOverride_ReplacesLinks()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var requestedId = await TestDb.AddCategoryAsync(db, "Requested");
        var overrideId = await TestDb.AddCategoryAsync(db, "Override");
        var created = await CreateApplicationAsync(service, employee.Id, requestedId);

        await service.ApproveAsync(created.Id,
            new ApproveAgentApplicationRequest { CategoryIds = new List<int> { overrideId } }, "admin-1");

        var links = await db.AgentCategories.Where(ac => ac.AgentId == employee.Id).ToListAsync();
        Assert.Single(links);
        Assert.Equal(overrideId, links.Single().CategoryId);
    }

    [Fact]
    public async Task Approve_RunsSweep_AssignsWaitingTickets()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var waitingTicketId = await TestDb.AddTicketAsync(db, categoryId, TicketStatus.AwaitingAssignment);

        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var created = await CreateApplicationAsync(service, employee.Id, categoryId);

        await service.ApproveAsync(created.Id, null, "admin-1");

        var ticket = await db.Tickets.SingleAsync(t => t.Id == waitingTicketId);
        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.Equal(employee.Id, ticket.AssignedAgentId);
    }

    [Fact]
    public async Task Approve_NotPending_Throws()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var categoryId = await TestDb.AddCategoryAsync(db);
        var created = await CreateApplicationAsync(service, employee.Id, categoryId);
        await service.RejectAsync(created.Id, null, "admin-1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApproveAsync(created.Id, null, "admin-1"));
    }

    [Fact]
    public async Task Approve_MissingApplication_Throws()
    {
        var (_, _, service, _) = await CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ApproveAsync(999, null, "admin-1"));
    }

    [Fact]
    public async Task Reject_MarksRejected_WithNote()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var categoryId = await TestDb.AddCategoryAsync(db);
        var created = await CreateApplicationAsync(service, employee.Id, categoryId);

        var rejected = await service.RejectAsync(
            created.Id, new RejectAgentApplicationRequest { Note = "Not yet." }, "admin-1");

        Assert.Equal("Rejected", rejected.Status);
        Assert.Equal("Not yet.", rejected.ReviewNote);

        var application = await db.AgentApplications.SingleAsync();
        Assert.Equal(AgentApplicationStatus.Rejected, application.Status);
        Assert.Equal("admin-1", application.ReviewedByAdminId);

        // The employee keeps their role and can re-apply later.
        Assert.True(await um.IsInRoleAsync(employee, "Employee"));
    }

    [Fact]
    public async Task Reject_NotPending_Throws()
    {
        var (db, um, service, _) = await CreateService();
        var employee = await TestDb.CreateUserAsync(db, um, "emp@test.com", roles: new[] { "Employee" });
        var categoryId = await TestDb.AddCategoryAsync(db);
        var created = await CreateApplicationAsync(service, employee.Id, categoryId);
        await service.ApproveAsync(created.Id, null, "admin-1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RejectAsync(created.Id, null, "admin-1"));
    }

    [Fact]
    public async Task GetApplications_FiltersByStatus_AndIncludesApplicant()
    {
        var (db, um, service, _) = await CreateService();
        var categoryId = await TestDb.AddCategoryAsync(db);
        var emp1 = await TestDb.CreateUserAsync(db, um, "emp1@test.com", roles: new[] { "Employee" });
        var emp2 = await TestDb.CreateUserAsync(db, um, "emp2@test.com", roles: new[] { "Employee" });
        var created2 = await CreateApplicationAsync(service, emp2.Id, categoryId);
        await CreateApplicationAsync(service, emp1.Id, categoryId);
        await service.RejectAsync(created2.Id, null, "admin-1");

        var pending = await service.GetApplicationsAsync("Pending", 1, 20);
        Assert.Equal(1, pending.TotalCount);
        Assert.Equal(emp1.FullName, pending.Items.Single().ApplicantName);
        Assert.Equal(emp1.Email, pending.Items.Single().ApplicantEmail);

        var rejected = await service.GetApplicationsAsync("Rejected", 1, 20);
        Assert.Equal(1, rejected.TotalCount);
        Assert.Equal(emp2.FullName, rejected.Items.Single().ApplicantName);

        var all = await service.GetApplicationsAsync(null, 1, 20);
        Assert.Equal(2, all.TotalCount);
    }

    [Fact]
    public async Task GetApplication_Missing_ReturnsNull()
    {
        var (_, _, service, _) = await CreateService();

        Assert.Null(await service.GetApplicationAsync(999));
    }
}
