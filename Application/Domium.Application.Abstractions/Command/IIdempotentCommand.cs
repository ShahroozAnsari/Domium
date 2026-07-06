namespace Domium.Application.Abstractions.Command;

public interface IIdempotentCommand : ICommand
{
    string IdempotencyKey { get; }
}
