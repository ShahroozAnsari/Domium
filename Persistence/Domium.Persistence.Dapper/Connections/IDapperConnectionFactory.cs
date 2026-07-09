using System.Data.Common;

namespace Domium.Persistence.Dapper;

/// <summary>
/// Creates database connections for Domium Dapper persistence.
/// </summary>
public interface IDapperConnectionFactory
{
    /// <summary>
    /// Creates a database connection. The caller owns the returned connection.
    /// </summary>
    ValueTask<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
