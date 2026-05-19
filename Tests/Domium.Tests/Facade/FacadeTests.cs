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
    public async Task AddDomium_registers_separate_command_and_query_facades()
    {
        RenameHandler.Reset();
        var services = new ServiceCollection();

        services.AddDomium();

        await using var provider = services.BuildServiceProvider();
        var commandFacade = provider.GetRequiredService<ICommandFacade>();
        var queryFacade = provider.GetRequiredService<IQueryFacade>();

        await commandFacade.ExecuteAsync(new RenameCommand("split"));
        var result = await queryFacade.QueryAsync<GetNameQuery, NameResult>(new GetNameQuery());

        Assert.Equal("split", RenameHandler.Name);
        Assert.Equal("split", result.Value);
    }

    [Fact]
    public void AddDomium_does_not_register_combined_domium_facade_contract()
    {
        var services = new ServiceCollection();

        services.AddDomium();

        var combinedFacadeRegistration = services.SingleOrDefault(
            service => service.ServiceType.Name == "IDomiumFacade");

        Assert.Null(combinedFacadeRegistration);
    }

    [Fact]
    public void Command_and_query_dispatch_facades_are_not_application_facade_markers()
    {
        Assert.False(typeof(IFacade).IsAssignableFrom(typeof(ICommandFacade)));
        Assert.False(typeof(IFacade).IsAssignableFrom(typeof(IQueryFacade)));
        Assert.False(typeof(IFacade).IsAssignableFrom(typeof(CommandFacadeBase)));
        Assert.False(typeof(IFacade).IsAssignableFrom(typeof(QueryFacadeBase)));
    }

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
    public async Task AddDomium_registers_application_facades_with_separate_command_and_query_bases()
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

    public sealed class NameFacade(ICommandFacade commandFacade, IQueryFacade queryFacade) : INameFacade
    {
        public Task RenameAsync(string name, CancellationToken cancellationToken = default)
        {
            return commandFacade.ExecuteAsync(new RenameCommand(name), cancellationToken);
        }

        public async Task<string> GetNameAsync(CancellationToken cancellationToken = default)
        {
            var result = await queryFacade.QueryAsync<GetNameQuery, NameResult>(new GetNameQuery(), cancellationToken);
            return result.Value;
        }
    }

    public sealed class NameCommandFacade(ICommandFacade facade) : CommandFacadeBase(facade), INameCommandFacade
    {
        public Task RenameAsync(string name, CancellationToken cancellationToken = default)
        {
            return ExecuteAsync(new RenameCommand(name), cancellationToken);
        }
    }

    public sealed class NameQueryFacade(IQueryFacade facade) : QueryFacadeBase(facade), INameQueryFacade
    {
        public async Task<string> GetNameAsync(CancellationToken cancellationToken = default)
        {
            var result = await QueryAsync<GetNameQuery, NameResult>(new GetNameQuery(), cancellationToken);
            return result.Value;
        }
    }
}
