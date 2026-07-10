namespace Domium.Application.Abstractions.Command;

public interface IIdempotentCommand : ICommand
{
    public string IdempotencyKey { get; set; }
}
