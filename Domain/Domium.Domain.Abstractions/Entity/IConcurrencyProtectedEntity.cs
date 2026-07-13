namespace Domium.Domain.Abstractions.Entity;

/// <summary>
/// Marks an aggregate as protected by optimistic concurrency. The persistence layer adds a
/// version concurrency token; a concurrent modification surfaces as a
/// <c>DomiumConcurrencyException</c> instead of silently overwriting the other write.
/// </summary>
public interface IConcurrencyProtectedEntity
{
}
