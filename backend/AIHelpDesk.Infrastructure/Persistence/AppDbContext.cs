using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    public DbSet<AgentProfile> AgentProfiles => Set<AgentProfile>();
    public DbSet<AgentCategory> AgentCategories => Set<AgentCategory>();
    public DbSet<AgentApplication> AgentApplications => Set<AgentApplication>();
    public DbSet<AgentApplicationCategory> AgentApplicationCategories => Set<AgentApplicationCategory>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ─── Category ─────────────────────────────────────────────────────────
        builder.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
        });

        // ─── Priority ─────────────────────────────────────────────────────────
        builder.Entity<Priority>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(p => p.Name).IsUnique();
            e.HasIndex(p => p.Level).IsUnique();
        });

        // ─── Ticket ───────────────────────────────────────────────────────────
        builder.Entity<Ticket>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Description).IsRequired();
            e.Property(t => t.AiSummary).HasMaxLength(1000);
            e.Property(t => t.AiSuggestedResolution).HasMaxLength(2000);
            e.Property(t => t.Resolution).HasMaxLength(5000);
            e.Property(t => t.Status).IsRequired();
            e.Property(t => t.EmployeeId).HasMaxLength(450).IsRequired();
            e.Property(t => t.AssignedAgentId).HasMaxLength(450);

            // Employee FK
            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Agent FK (nullable)
            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.AssignedAgentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Category FK (nullable — not yet assigned when ticket is Created)
            e.HasOne(t => t.Category)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.CategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Priority FK (nullable)
            e.HasOne(t => t.Priority)
                .WithMany(p => p.Tickets)
                .HasForeignKey(t => t.PriorityId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasMany(t => t.Comments)
                .WithOne(c => c.Ticket)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(t => t.Attachments)
                .WithOne(a => a.Ticket)
                .HasForeignKey(a => a.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── Comment ──────────────────────────────────────────────────────────
        builder.Entity<Comment>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Content).HasMaxLength(2000).IsRequired();
            e.Property(c => c.AuthorId).HasMaxLength(450).IsRequired();

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── Attachment ───────────────────────────────────────────────────────
        builder.Entity<Attachment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.OriginalFileName).HasMaxLength(260).IsRequired();
            e.Property(a => a.StorageKey).HasMaxLength(500).IsRequired();
            e.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
            e.Property(a => a.UploaderId).HasMaxLength(450).IsRequired();

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.UploaderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

                // ─── AgentProfile ─────────────────────────────────────────────────────
        builder.Entity<AgentProfile>(e =>
        {
            e.HasKey(p => p.UserId);
            e.Property(p => p.UserId).HasMaxLength(450);
            e.Property(p => p.MaxOpenTickets).IsRequired();

            e.HasOne<ApplicationUser>()
                .WithOne()
                .HasForeignKey<AgentProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── AgentCategory (agent <-> category) ───────────────────────────────
        builder.Entity<AgentCategory>(e =>
        {
            e.HasKey(ac => new { ac.AgentId, ac.CategoryId });
            e.Property(ac => ac.AgentId).HasMaxLength(450);
            e.HasIndex(ac => ac.CategoryId);

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(ac => ac.AgentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict avoids SQL Server "multiple cascade paths" errors.
            // Categories are soft-deleted (IsActive), never hard-deleted.
            e.HasOne(ac => ac.Category)
                .WithMany()
                .HasForeignKey(ac => ac.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ─── AgentApplication ─────────────────────────────────────────────────
        builder.Entity<AgentApplication>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.UserId).HasMaxLength(450).IsRequired();
            e.Property(a => a.Message).HasMaxLength(1000);
            e.Property(a => a.ReviewNote).HasMaxLength(1000);
            e.Property(a => a.ReviewedByAdminId).HasMaxLength(450);
            e.Property(a => a.Status).IsRequired();

            // Only ONE Pending application (Status = 0) per user.
            e.HasIndex(a => a.UserId)
                .IsUnique()
                .HasFilter("[Status] = 0");

            e.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(a => a.Categories)
                .WithOne(c => c.Application)
                .HasForeignKey(c => c.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ─── AgentApplicationCategory ─────────────────────────────────────────
        builder.Entity<AgentApplicationCategory>(e =>
        {
            e.HasKey(c => new { c.ApplicationId, c.CategoryId });

            e.HasOne(c => c.Category)
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
