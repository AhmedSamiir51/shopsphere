using Npgsql;

namespace ShopSphere.BuildingBlocks.EventBus.Inbox;

/// <summary>
/// Idempotent consumer store: remembers which integration events each consumer has
/// already handled, so broker redeliveries (at-least-once) don't apply twice.
/// </summary>
public interface IInboxStore
{
    Task<bool> HasProcessedAsync(Guid messageId, string consumer, CancellationToken cancellationToken);
    Task MarkProcessedAsync(Guid messageId, string consumer, CancellationToken cancellationToken);
}

/// <summary>Raw-ADO inbox store over the service's own PostgreSQL database.</summary>
public sealed class NpgsqlInboxStore : IInboxStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initGate = new(1, 1);
    private bool _initialized;

    public NpgsqlInboxStore(string connectionString) => _connectionString = connectionString;

    public async Task<bool> HasProcessedAsync(Guid messageId, string consumer, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            "SELECT 1 FROM inbox_messages WHERE message_id = @id AND consumer = @consumer", connection);
        command.Parameters.AddWithValue("id", messageId);
        command.Parameters.AddWithValue("consumer", consumer);

        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task MarkProcessedAsync(Guid messageId, string consumer, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO inbox_messages (message_id, consumer, processed_on_utc)
            VALUES (@id, @consumer, @now)
            ON CONFLICT (message_id, consumer) DO NOTHING
            """, connection);
        command.Parameters.AddWithValue("id", messageId);
        command.Parameters.AddWithValue("consumer", consumer);
        command.Parameters.AddWithValue("now", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await _initGate.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(
                """
                CREATE TABLE IF NOT EXISTS inbox_messages (
                    message_id uuid NOT NULL,
                    consumer text NOT NULL,
                    processed_on_utc timestamptz NOT NULL,
                    CONSTRAINT pk_inbox_messages PRIMARY KEY (message_id, consumer)
                )
                """, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);

            _initialized = true;
        }
        finally
        {
            _initGate.Release();
        }
    }
}
