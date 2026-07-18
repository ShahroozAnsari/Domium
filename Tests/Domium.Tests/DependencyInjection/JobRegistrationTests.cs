using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Job;
using Domium.Extensions.DependencyInjection;
using Domium.Persistence.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.DependencyInjection;

public sealed class JobRegistrationTests
{
    [Fact]
    public void Job_handler_is_registered_as_job_handler()
    {
        var services = new ServiceCollection();

        services.AddDomium();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IJobHandler<TrackingJob>) &&
            descriptor.ImplementationType == typeof(TrackingJobHandler));
    }

    [Fact]
    public void Job_handler_is_not_registered_as_a_command_handler()
    {
        var services = new ServiceCollection();

        services.AddDomium();

        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<>) &&
            descriptor.ServiceType.GetGenericArguments()[0] == typeof(TrackingJob));

        Assert.DoesNotContain(services, descriptor =>
            descriptor.ImplementationType == typeof(TrackingJobHandler) &&
            descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
    }

    [Fact]
    public async Task Job_bus_executes_the_registered_job_handler()
    {
        TrackingJobHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium();

        await using var provider = services.BuildServiceProvider();
        var jobBus = provider.GetRequiredService<IJobBus>();

        await jobBus.ExecuteAsync(new TrackingJob());

        Assert.Equal(1, TrackingJobHandler.ExecutionCount);
    }

    [Fact]
    public async Task Job_execution_runs_inside_the_unit_of_work_when_transactions_are_enabled()
    {
        TrackingJobHandler.Reset();
        var unitOfWork = new RecordingUnitOfWork();
        var services = new ServiceCollection();

        services.AddSingleton<IUnitOfWork>(unitOfWork);
        services.AddDomium(options => options.UseTransactions());

        await using var provider = services.BuildServiceProvider();
        var jobBus = provider.GetRequiredService<IJobBus>();

        await jobBus.ExecuteAsync(new TrackingJob());

        Assert.Equal(1, TrackingJobHandler.ExecutionCount);
        Assert.Equal(1, unitOfWork.ExecuteCount);
    }

    public sealed class TrackingJob : IJob;

    public sealed class TrackingJobHandler : IJobHandler<TrackingJob>
    {
        public static int ExecutionCount { get; private set; }

        public static void Reset()
        {
            ExecutionCount = 0;
        }

        public Task HandleAsync(TrackingJob job, CancellationToken cancellationToken = default)
        {
            ExecutionCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int ExecuteCount { get; private set; }

        public Task BeginAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        {
            ExecuteCount++;
            return operation();
        }
    }
}
