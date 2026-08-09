using Microsoft.EntityFrameworkCore;
using MultiTenantTaskBoard.Models;

namespace MultiTenantTaskBoard.Data;

public class TaskBoardContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public TaskBoardContext(DbContextOptions<TaskBoardContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The core multi-tenant pattern: every query against Tasks is
        // automatically scoped to the current tenant. No endpoint can
        // accidentally leak another tenant's data by forgetting a filter —
        // the isolation lives in one place, not scattered across controllers.
        modelBuilder.Entity<TaskItem>()
            .HasQueryFilter(t => t.TenantId == _tenantProvider.TenantId);

        base.OnModelCreating(modelBuilder);
    }
}
