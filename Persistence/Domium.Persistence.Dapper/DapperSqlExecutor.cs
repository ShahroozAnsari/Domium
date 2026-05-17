using Dapper;

namespace Domium.Persistence.Dapper;

/// <summary>
/// Default SQL executor backed by Dapper.
/// </summary>
public sealed class DapperSqlExecutor(IDapperSession session) : IDapperSqlExecutor
{
    private readonly IDapperSession _session =
        session ?? throw new ArgumentNullException(nameof(session));

    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await _session.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        return await connection
            .ExecuteAsync(CreateCommand(sql, parameters, cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await _session.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection
            .QueryAsync<T>(CreateCommand(sql, parameters, cancellationToken))
            .ConfigureAwait(false);

        return rows.AsList();
    }

    public async Task<T> QuerySingleAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await _session.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        return await connection
            .QuerySingleAsync<T>(CreateCommand(sql, parameters, cancellationToken))
            .ConfigureAwait(false);
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await _session.GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        return await connection
            .QuerySingleOrDefaultAsync<T>(CreateCommand(sql, parameters, cancellationToken))
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
