using System.Data.Common;
using Domium.Domain;
using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.DomainService;
using Domium.Extensions.DependencyInjection;
using Domium.Persistence.Abstractions;
using Domium.Persistence.Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Tests.Persistence;

public sealed class DapperPersistenceTests
{
    [Fact]
    public async Task Dapper_executor_uses_unit_of_work_transaction()
    {
        await using var database = await SqliteMemoryDatabase.CreateAsync();
        await using var provider = CreateProvider(database.ConnectionString);

        using (var scope = provider.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var executor = scope.ServiceProvider.GetRequiredService<IDapperSqlExecutor>();

            await unitOfWork.BeginAsync();
            await executor.ExecuteAsync(
                "insert into People (Id, Name) values (@Id, @Name);",
                new { Id = 1, Name = "Ada" });
            await unitOfWork.CommitAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var executor = scope.ServiceProvider.GetRequiredService<IDapperSqlExecutor>();

            var name = await executor.QuerySingleAsync<string>(
                "select Name from People where Id = @Id;",
                new { Id = 1 });

            Assert.Equal("Ada", name);
        }
    }

    [Fact]
    public async Task Dapper_unit_of_work_rolls_back_transaction()
    {
        await using var database = await SqliteMemoryDatabase.CreateAsync();
        await using var provider = CreateProvider(database.ConnectionString);

        using (var scope = provider.CreateScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var executor = scope.ServiceProvider.GetRequiredService<IDapperSqlExecutor>();

            await unitOfWork.BeginAsync();
            await executor.ExecuteAsync(
                "insert into People (Id, Name) values (@Id, @Name);",
                new { Id = 1, Name = "Ada" });
            await unitOfWork.RollbackAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var executor = scope.ServiceProvider.GetRequiredService<IDapperSqlExecutor>();

            var count = await executor.QuerySingleAsync<long>("select count(*) from People;");

            Assert.Equal(0, count);
        }
    }

    [Fact]
    public void AddDomium_with_transactions_accepts_dapper_unit_of_work()
    {
        var services = new ServiceCollection();

        services.AddDomiumDapper(options =>
            options.UseConnectionFactory(_ =>
                new SqliteConnectionFactory("Data Source=:memory:")));

        services.AddDomium(options => options.UseTransactions());

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IUnitOfWork>());
    }

    [Fact]
    public async Task Dapper_can_be_selected_as_aggregate_repository_provider()
    {
        await using var database = await SqliteMemoryDatabase.CreateAsync();
        var services = new ServiceCollection();

        services.AddSingleton<IDapperAggregateMapper<Person, PersonId>, PersonMapper>();
        services.AddDomiumDapper(options =>
            options
                .UseConnectionFactory(_ => new SqliteConnectionFactory(database.ConnectionString))
                .UseAggregateRepositories());
        services.AddDomium(options => options.UseTransactions());

        await using var provider = services.BuildServiceProvider();
        var id = new PersonId(1);

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Person, PersonId>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await unitOfWork.BeginAsync();
            await repository.AddAsync(new Person(id, "Ada"));
            await unitOfWork.CommitAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Person, PersonId>>();

            var loaded = await repository.GetByIdAsync(id);

            Assert.NotNull(loaded);
            Assert.Equal("Ada", loaded.Name);

            loaded.Rename("Grace");
            await repository.UpdateAsync(loaded);

            var updated = await repository.GetByIdAsync(id);

            Assert.Equal("Grace", updated?.Name);

            await repository.RemoveAsync(updated!);

            Assert.Null(await repository.GetByIdAsync(id));
        }
    }

    private static ServiceProvider CreateProvider(string connectionString)
    {
        var services = new ServiceCollection();

        services.AddDomiumDapper(options =>
            options.UseConnectionFactory(_ => new SqliteConnectionFactory(connectionString)));

        services.AddDomium(options => options.UseTransactions());

        return services.BuildServiceProvider();
    }

    private sealed class SqliteConnectionFactory(string connectionString) : IDapperConnectionFactory
    {
        public ValueTask<DbConnection> CreateConnectionAsync(
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<DbConnection>(new SqliteConnection(connectionString));
        }
    }

    private sealed class SqliteMemoryDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _keeperConnection;

        private SqliteMemoryDatabase(SqliteConnection keeperConnection)
        {
            _keeperConnection = keeperConnection;
            ConnectionString = keeperConnection.ConnectionString;
        }

        public string ConnectionString { get; }

        public static async Task<SqliteMemoryDatabase> CreateAsync()
        {
            var connectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
            var keeperConnection = new SqliteConnection(connectionString);
            await keeperConnection.OpenAsync();

            await using var command = keeperConnection.CreateCommand();
            command.CommandText = """
                create table People (
                    Id integer primary key,
                    Name text not null
                );
                """;
            await command.ExecuteNonQueryAsync();

            return new SqliteMemoryDatabase(keeperConnection);
        }

        public ValueTask DisposeAsync()
        {
            return _keeperConnection.DisposeAsync();
        }
    }

    private sealed class PersonId(int value) : AggregateId<int>(value);

    private sealed class Person(PersonId id, string name) : AggregateRoot<PersonId>(id)
    {
        public string Name { get; private set; } = name;

        public void Rename(string name)
        {
            Name = name;
        }
    }

    private sealed class PersonMapper : IDapperAggregateMapper<Person, PersonId>
    {
        public string SelectByIdSql => "select Id, Name from People where Id = @Id;";

        public string InsertSql => "insert into People (Id, Name) values (@Id, @Name);";

        public string UpdateSql => "update People set Name = @Name where Id = @Id;";

        public string DeleteSql => "delete from People where Id = @Id;";

        public object GetIdParameters(PersonId id)
        {
            return new { Id = id.Value };
        }

        public object GetInsertParameters(Person aggregate)
        {
            return new { Id = aggregate.Id.Value, aggregate.Name };
        }

        public object GetUpdateParameters(Person aggregate)
        {
            return new { Id = aggregate.Id.Value, aggregate.Name };
        }

        public object GetDeleteParameters(Person aggregate)
        {
            return new { Id = aggregate.Id.Value };
        }

        public Person Map(object row)
        {
            var values = (IDictionary<string, object>)row;
            var id = Convert.ToInt32(values["Id"]);
            var name = Convert.ToString(values["Name"]) ?? string.Empty;

            return new Person(new PersonId(id), name);
        }
    }
}
