using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Query;
using Domium.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Benchmarks;

/// <summary>
/// Measures the dispatch overhead of the command/query pipeline — bare (no behaviors) and
/// with the standard behavior stack — so pipeline changes are judged by numbers, not vibes.
/// Run with: dotnet run -c Release --project Benchmarks/Domium.Benchmarks
/// </summary>
[MemoryDiagnoser]
public class DispatchBenchmarks
{
    private ServiceProvider _bareProvider = null!;
    private ServiceProvider _fullProvider = null!;
    private ICommandBus _bareCommands = null!;
    private ICommandBus _fullCommands = null!;
    private IQueryBus _bareQueries = null!;

    [GlobalSetup]
    public void Setup()
    {
        _bareProvider = BuildProvider(withBehaviors: false);
        _fullProvider = BuildProvider(withBehaviors: true);
        _bareCommands = _bareProvider.GetRequiredService<ICommandBus>();
        _fullCommands = _fullProvider.GetRequiredService<ICommandBus>();
        _bareQueries = _bareProvider.GetRequiredService<IQueryBus>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _bareProvider.Dispose();
        _fullProvider.Dispose();
    }

    [Benchmark(Baseline = true)]
    public Task Command_no_behaviors() => _bareCommands.ExecuteAsync(new Ping());

    [Benchmark]
    public Task Command_with_observability_validation_logging() => _fullCommands.ExecuteAsync(new Ping());

    [Benchmark]
    public Task<int> Query_no_behaviors() => _bareQueries.ExecuteAsync<CountPings, int>(new CountPings());

    private static ServiceProvider BuildProvider(bool withBehaviors)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDomium(options =>
        {
            options
                .UseLoadedAssemblyScanning(false)
                .AddApplicationAssembly(typeof(DispatchBenchmarks).Assembly);

            if (withBehaviors)
            {
                options.UseObservability().UseValidation().UseLogging();
            }
        });

        return services.BuildServiceProvider();
    }

    public sealed record Ping : ICommand;

    public sealed class PingHandler : ICommandHandler<Ping>
    {
        public Task HandleAsync(Ping command, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed record CountPings : IQuery<int>;

    public sealed class CountPingsHandler : IQueryHandler<CountPings, int>
    {
        public Task<int> HandleAsync(CountPings query, CancellationToken cancellationToken = default) =>
            Task.FromResult(42);
    }
}

public static class Program
{
    public static void Main(string[] args) => BenchmarkRunner.Run<DispatchBenchmarks>(args: args);
}
