using System.Diagnostics;
using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Domium.Observability;

namespace Domium.Application.Command.Pipelines.Behaviors;

/// <summary>
/// Wraps command execution in an Activity span, counts executed commands, and records the
/// operation duration. Registered first, so the span covers every other behavior and the
/// handler itself.
/// </summary>
public sealed class ObservabilityCommandBehavior<TCommand> : ICommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    public async Task HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate next)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (next == null) throw new ArgumentNullException(nameof(next));

        var commandName = typeof(TCommand).FullName ?? typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        using var activity = DomiumTelemetry.ActivitySource.StartActivity(
            "domium.command.execute",
            ActivityKind.Internal);

        activity?.SetTag("domium.command.name", commandName);

        try
        {
            await next().ConfigureAwait(false);
            DomiumTelemetry.CommandsExecuted.Add(
                1,
                new KeyValuePair<string, object?>("domium.command.name", commandName));
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.SetTag("exception.type", exception.GetType().FullName);
            activity?.SetTag("exception.message", exception.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            DomiumTelemetry.OperationDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("domium.operation.type", "command"),
                new KeyValuePair<string, object?>("domium.command.name", commandName));
        }
    }
}

public sealed class ObservabilityCommandBehavior<TCommand, TResult> : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken,
        CommandHandlerDelegate<TResult> next)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (next == null) throw new ArgumentNullException(nameof(next));

        var commandName = typeof(TCommand).FullName ?? typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        using var activity = DomiumTelemetry.ActivitySource.StartActivity(
            "domium.command.execute",
            ActivityKind.Internal);

        activity?.SetTag("domium.command.name", commandName);

        try
        {
            var result = await next().ConfigureAwait(false);
            DomiumTelemetry.CommandsExecuted.Add(
                1,
                new KeyValuePair<string, object?>("domium.command.name", commandName));
            return result;
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.SetTag("exception.type", exception.GetType().FullName);
            activity?.SetTag("exception.message", exception.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            DomiumTelemetry.OperationDuration.Record(
                stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("domium.operation.type", "command"),
                new KeyValuePair<string, object?>("domium.command.name", commandName));
        }
    }
}
