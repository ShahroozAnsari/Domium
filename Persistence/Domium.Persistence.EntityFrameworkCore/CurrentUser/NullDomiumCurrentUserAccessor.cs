namespace Domium.Persistence.EntityFrameworkCore;

public sealed class NullDomiumCurrentUserAccessor : IDomiumCurrentUserAccessor
{
    public string? UserId => null;
}
