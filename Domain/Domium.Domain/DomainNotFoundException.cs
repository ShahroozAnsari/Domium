namespace Domium.Domain;

/// <summary>
/// Thrown when a requested aggregate does not exist. Maps to 404 at the API boundary,
/// while plain <see cref="DomainException"/> rule violations map to 409.
/// </summary>
public class DomainNotFoundException : DomainException
{
    public DomainNotFoundException(string message) : base(message)
    {
    }
}
