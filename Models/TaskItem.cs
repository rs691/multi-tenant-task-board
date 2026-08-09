namespace MultiTenantTaskBoard.Models;

public class TaskItem
{
    public int Id { get; set; }

    // Tenant isolation key — every query gets filtered on this automatically
    // via the global query filter configured in TaskBoardContext.
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
