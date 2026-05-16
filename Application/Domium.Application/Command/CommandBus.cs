using System.Diagnostics;
using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Command.PipeLines;
using Domium.Observability;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Application.Command;

public sealed class CommandBus(IServiceProvider serviceProvider) : ICommandBus
{
    public async Task ExecuteAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        var commandName = typeof(TCommand).FullName ?? typeof(TCommand).Name;
        var stopwatch = Stopwatch.StartNew();

        using var activity = DomiumTelemetry.ActivitySource.StartActivity(
            "domium.command.execute",
            ActivityKind.Internal);

        activity?.SetTag("domium.command.name", commandName);

        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        var behaviors = serviceProvider.GetServices<ICommandPipelineBehavior<TCommand>>().Reverse().ToArray();

        CommandHandlerDelegate pipeline = () => handler.HandleAsync(command, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            pipeline = () => behavior.HandleAsync(command, cancellationToken, next);
        }

        try
        {
            await pipeline().ConfigureAwait(false);
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
