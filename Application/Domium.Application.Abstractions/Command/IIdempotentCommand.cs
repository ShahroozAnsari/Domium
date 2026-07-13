namespace Domium.Application.Abstractions.Command;

/// <summary>
/// A command carrying a caller-supplied idempotency key. The key is immutable so it cannot
/// change after the idempotency reservation has been made.
/// </summary>
public interface IIdempotentCommand : ICommand
{
    string IdempotencyKey { get; }
}
