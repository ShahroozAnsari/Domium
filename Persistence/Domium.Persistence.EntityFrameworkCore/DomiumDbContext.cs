using Domium.Eventing.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Domium.Persistence.EntityFrameworkCore;

/// <summary>
/// Base EF Core DbContext with optional domain event dispatch after saving changes.
/// </summary>
public abstract class DomiumDbContext : DbContext
{
    protected DomiumDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected DomiumDbContext(
        DbContextOptions options,
        IEventBus? eventBus)
        : base(options)
    {
    }
}
