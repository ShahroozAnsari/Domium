namespace Domium.Persistence.EntityFrameworkCore;

public interface IDomiumCurrentUserAccessor
{
    string? UserId { get; }
}
