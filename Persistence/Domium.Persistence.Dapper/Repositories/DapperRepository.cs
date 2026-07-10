using Dapper;
using Domium.Domain.Abstractions.Aggregate;
using Domium.Domain.Abstractions.DomainService;
using Domium.Persistence.Abstractions;

namespace Domium.Persistence.Dapper;

/// <summary>
/// Dapper aggregate repository backed by explicit aggregate mappers.
/// </summary>
public sealed class DapperRepository<TAggregate, TId>(
    IDapperSession session,
    IDapperAggregateMapper<TAggregate, TId> mapper)
    : IRepository<TAggregate, TId>
    where TAggregate : class, IAggregateRoot<TId>
    where TId : IAggregateId
{
    private readonly IDapperSession _session =
        session ?? throw new ArgumentNullException(nameof(session));

    private readonly IDapperAggregateMapper<TAggregate, TId> _mapper =
        mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<TAggregate?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        if (id == null)
        {
            throw new ArgumentNullException(nameof(id));
        }

        var connection = await _session.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection
            .QuerySingleOrDefaultAsync<object>(CreateCommand(
                _mapper.SelectByIdSql,
                _mapper.GetIdParameters(id),
                cancellationToken))
            .ConfigureAwait(false);

        return row is null ? null : _mapper.Map(row);
    }

    public async Task AddAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        if (aggregate == null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        await ExecuteAsync(
            _mapper.InsertSql,
            _mapper.GetInsertParameters(aggregate),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        if (aggregate == null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        await ExecuteAsync(
            _mapper.UpdateSql,
            _mapper.GetUpdateParameters(aggregate),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(
        TAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        if (aggregate == null)
        {
            throw new ArgumentNullException(nameof(aggregate));
        }

        await ExecuteAsync(
            _mapper.DeleteSql,
            _mapper.GetDeleteParameters(aggregate),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteAsync(
        string sql,
        object parameters,
        CancellationToken cancellationToken)
    {
        var connection = await _session.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection
            .ExecuteAsync(CreateCommand(sql, parameters, cancellationToken))
            .ConfigureAwait(false);
    }

    private CommandDefinition CreateCommand(
        string sql,
        object? parameters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL cannot be empty.", nameof(sql));
        }

        return new CommandDefinition(
            sql,
            parameters,
            _session.CurrentTransaction,
            cancellationToken: cancellationToken);
    }
}
