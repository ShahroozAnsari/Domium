namespace Domium.Application.Abstractions.Command;

/// <summary>A state-changing operation with no return value.</summary>
public interface ICommand
{
}

/// <summary>A state-changing operation that returns a result (e.g. the created id).</summary>
public interface ICommand<out TResult> : ICommand
{
}
