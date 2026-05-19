using Domium.Application.Abstractions.Command;
using Domium.Application.Abstractions.Query;
using Domium.Extensions.DependencyInjection;
using Domium.Facade;
using Domium.Facade.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.Facade;

public sealed class FacadeTests
{
    [Fact]
    public async Task AddDomium_registers_application_facades_from_application_assembly()
    {
        RenameHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium();

        await using var provider = services.BuildServiceProvider();
        var facade = provider.GetRequiredService<INameFacade>();

        await facade.RenameAsync("bounded-context");
        var name = await facade.GetNameAsync();

        Assert.Equal("bounded-context", name);
    }

    [Fact]
    public async Task DomiumFacade_base_dispatches_commands_and_queries_through_application_layer()
    {
        RenameHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium();

        await using var provider = services.BuildServiceProvider();
        var commandFacade = provider.GetRequiredService<INameCommandFacade>();
        var queryFacade = provider.GetRequiredService<INameQueryFacade>();

        await commandFacade.RenameAsync("separate-bases");
        var name = await queryFacade.GetNameAsync();

        Assert.Equal("separate-bases", name);
    }

    public sealed class RenameCommand(string name) : ICommand
    {
        public string Name { get; } = name;
    }

    public sealed class GetNameQuery : IQuery<NameResult>
    {
    }

    public sealed class NameResult(string value)
    {
        public string Value { get; } = value;
    }

    public sealed class RenameHandler : ICommandHandler<RenameCommand>
    {
        public static string? Name { get; private set; }

        public static void Reset()
        {
            Name = null;
        }

        public Task HandleAsync(RenameCommand command, CancellationToken cancellationToken = default)
        {
            Name = command.Name;
            return Task.CompletedTask;
        }
    }

    public sealed class GetNameHandler : IQueryHandler<GetNameQuery, NameResult>
    {
        public Task<NameResult> HandleAsync(GetNameQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new NameResult(RenameHandler.Name ?? string.Empty));
        }
    }

    public interface INameFacade : IFacade
    {
        Task RenameAsync(string name, CancellationToken cancellationToken = default);

        Task<string> GetNameAsync(CancellationToken cancellationToken = default);
    }

    public interface INameCommandFacade : IFacade
    {
        Task RenameAsync(string name, CancellationToken cancellationToken = default);
    }

    public interface INameQueryFacade : IFacade
    {
        Task<string> GetNameAsync(CancellationToken cancellationToken = default);
    }

    public sealed class NameFacade(ICommandBus commandBus, IQueryBus queryBus)
        : DomiumFacade(commandBus, queryBus), INameFacade
    {
        public Task RenameAsync(string name, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(new RenameCommand(name), cancellationToken);
        }

        public async Task<string> GetNameAsync(CancellationToken cancellationToken = default)
        {
            var result = await QueryAsync<GetNameQuery, NameResult>(new GetNameQuery(), cancellationToken);
            return result.Value;
        }
    }

    public sealed class NameCommandFacade(ICommandBus commandBus, IQueryBus queryBus)
        : DomiumFacade(commandBus, queryBus), INameCommandFacade
    {
        public Task RenameAsync(string name, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(new RenameCommand(name), cancellationToken);
        }
    }

    public sealed class NameQueryFacade(ICommandBus commandBus, IQueryBus queryBus)
        : DomiumFacade(commandBus, queryBus), INameQueryFacade
    {
        public async Task<string> GetNameAsync(CancellationToken cancellationToken = default)
        {
            var result = await QueryAsync<GetNameQuery, NameResult>(new GetNameQuery(), cancellationToken);
            return result.Value;
        }
    }
}
