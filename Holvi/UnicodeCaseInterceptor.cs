using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Holvi;

/// <summary>
/// SQLite's built-in lower() and upper() only handle ASCII, replace them with .NET versions on each connection,
/// so that case-insensitive search (string.ToLower() in LINQ queries) works for Cyrillic etc.
/// </summary>
public class UnicodeCaseInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        Register(connection);
    }

    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Register(connection);
        return Task.CompletedTask;
    }

    private static void Register(DbConnection connection)
    {
        var sqlite = (SqliteConnection)connection;
        sqlite.CreateFunction("lower", (string? s) => s?.ToLowerInvariant(), isDeterministic: true);
        sqlite.CreateFunction("upper", (string? s) => s?.ToUpperInvariant(), isDeterministic: true);
    }
}
