using System.Data.Common;

namespace Domium.Persistence.Dapper;

/// <summary>
/// Provides the current Dapper connection and transaction for a DI scope.
/// </summary>
public interface IDapperSession
{
    /// <summary>
    /// Gets an open connection for the current scope.
    /// </summary>
    ValueTask<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active transaction when a unit of work has been started.
    /// </summary>
    DbTransaction? CurrentTransaction { get; }
}
