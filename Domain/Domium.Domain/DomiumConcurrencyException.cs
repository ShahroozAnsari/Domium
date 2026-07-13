namespace Domium.Domain;

/// <summary>
/// Thrown when an aggregate protected by optimistic concurrency was modified by another
/// operation between load and save. Callers typically retry the whole operation or surface
/// a conflict (HTTP 409) to the client.
/// </summary>
public sealed class DomiumConcurrencyException : DomainException
{
    public DomiumConcurrencyException(string message) : base(message)
    {
    }

    public DomiumConcurrencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
