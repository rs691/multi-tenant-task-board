namespace MultiTenantTaskBoard.Data;

// Resolves the current tenant from the request (JWT claim).
// This is what makes the isolation automatic rather than something
// every endpoint has to remember to apply manually.
public interface ITenantProvider
{
    int? TenantId { get; }
}

public class TenantProvider : ITenantProvider
{
    public int? TenantId { get; private set; }

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        var claim = httpContextAccessor.HttpContext?.User
            .FindFirst("tenant_id")?.Value;

        if (claim is not null && int.TryParse(claim, out var id))
        {
            TenantId = id;
        }
    }
}
