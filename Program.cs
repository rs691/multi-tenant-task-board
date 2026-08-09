using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MultiTenantTaskBoard.Auth;
using MultiTenantTaskBoard.Data;
using MultiTenantTaskBoard.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<TaskBoardContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=taskboard.db"));

builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddSingleton<TokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure the SQLite file + schema exist on startup (fine for a demo project;
// a real app would use EF migrations instead of EnsureCreated).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskBoardContext>();
    db.Database.EnsureCreated();

    if (!db.Tenants.Any())
    {
        db.Tenants.AddRange(
            new Tenant { Name = "Acme Co" },
            new Tenant { Name = "Globex Inc" });
        db.SaveChanges();
    }
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Enabled in all environments (not just dev) so the deployed Azure demo
// has a real, browsable landing point instead of a bare 404 on "/".
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Multi-Tenant Task Board API";
});

app.MapGet("/", () => Results.Redirect("/scalar/v1"));

// --- Auth: issue a demo token scoped to a tenant ---
// POST /auth/token/{tenantId}  ->  { "token": "..." }
// In a real system this would validate real credentials; here it just
// demonstrates the tenant claim flowing through to every downstream query.
app.MapPost("/auth/token/{tenantId:int}", async (int tenantId, TaskBoardContext rawDb, TokenService tokens) =>
{
    var tenant = await rawDb.Tenants.FindAsync(tenantId);
    if (tenant is null) return Results.NotFound($"No tenant with id {tenantId}");

    var token = tokens.GenerateToken(tenant.Id, tenant.Name);
    return Results.Ok(new { token, tenant = tenant.Name });
});

// GET /tenants  -> list tenants (so you can grab an id to log in as)
app.MapGet("/tenants", async (TaskBoardContext db) =>
    await db.Tenants.Select(t => new { t.Id, t.Name }).ToListAsync());

// --- Tasks: every one of these is automatically tenant-scoped ---
// via the global query filter in TaskBoardContext — these endpoints
// never reference TenantId directly when reading, which is the point.

app.MapGet("/tasks", async (TaskBoardContext db) =>
    await db.Tasks.ToListAsync())
    .RequireAuthorization();

app.MapGet("/tasks/{id:int}", async (int id, TaskBoardContext db) =>
    await db.Tasks.FindAsync(id) is TaskItem task
        ? Results.Ok(task)
        : Results.NotFound())
    .RequireAuthorization();

app.MapPost("/tasks", async (
    [FromBody] TaskCreateRequest request,
    TaskBoardContext db,
    ITenantProvider tenantProvider) =>
{
    if (tenantProvider.TenantId is null) return Results.Unauthorized();

    var task = new TaskItem
    {
        TenantId = tenantProvider.TenantId.Value,
        Title = request.Title,
        Description = request.Description,
    };

    db.Tasks.Add(task);
    await db.SaveChangesAsync();
    return Results.Created($"/tasks/{task.Id}", task);
})
.RequireAuthorization();

app.MapPut("/tasks/{id:int}", async (int id, [FromBody] TaskUpdateRequest request, TaskBoardContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null) return Results.NotFound();

    task.Title = request.Title ?? task.Title;
    task.Description = request.Description ?? task.Description;
    task.IsCompleted = request.IsCompleted ?? task.IsCompleted;

    await db.SaveChangesAsync();
    return Results.Ok(task);
})
.RequireAuthorization();

app.MapDelete("/tasks/{id:int}", async (int id, TaskBoardContext db) =>
{
    var task = await db.Tasks.FindAsync(id);
    if (task is null) return Results.NotFound();

    db.Tasks.Remove(task);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.RequireAuthorization();

app.Run();

record TaskCreateRequest(string Title, string? Description);
record TaskUpdateRequest(string? Title, string? Description, bool? IsCompleted);
