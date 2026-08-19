using BankLedger.Core.Common.Events;
using BankLedger.Domain;
using BankLedger.Domain.Common;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text.Json;

namespace BankLedger.WriteProject.Infrastructure.Database
{
    public class MySqlEventStore : IEventStore
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<MySqlEventStore> _logger;

        public MySqlEventStore(string connectionString, ICryptoKeyVault keyVault, ILogger<MySqlEventStore> logger)
        {
            _connectionString = connectionString;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

                WriteIndented = false
            };
            _jsonOptions.Converters.Add(new GdprEncryptionConverterFactory(keyVault));

            _logger = logger;
        }
        public async Task AppendEventsAsync(Guid aggregateId, string streamCategory, int expectedVersion, IEnumerable<BankEvent> events, CancellationToken cancellationToken)
        {
            // Set the thread-local context boundary
            AmbientContext.CurrentAggregateId = aggregateId;

            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            const string query = @"
                    INSERT INTO eventstore (EventId, AggregateId, Version, EventType, EventData, OccuredAt, StreamCategory)
                    VALUES (@EventId, @AggregateId, @Version, @EventType, @EventData, @OccuredAt, @StreamCategory);";

            // 1. PERFORMANCE FIX: Create the command object ONCE outside the loop to enable statement reuse
            using var command = new MySqlCommand(query, connection, transaction);

            // Pre-allocate the parameters to optimize database execution plans
            command.Parameters.Add("@EventId", MySqlDbType.VarChar, 36);
            command.Parameters.Add("@AggregateId", MySqlDbType.VarChar, 36);
            command.Parameters.Add("@StreamCategory", MySqlDbType.VarChar, 50);
            command.Parameters.Add("@Version", MySqlDbType.Int32);
            command.Parameters.Add("@EventType", MySqlDbType.VarChar, 255);
            command.Parameters.Add("@EventData", MySqlDbType.JSON);
            command.Parameters.Add("@OccuredAt", MySqlDbType.DateTime);

            try
            {
                foreach (var @event in events)
                {
                    var trackingVersion = expectedVersion;

                    command.Parameters["@EventId"].Value = @event.EventId.ToString();
                    command.Parameters["@AggregateId"].Value = aggregateId;
                    command.Parameters["@StreamCategory"].Value = streamCategory;
                    command.Parameters["@Version"].Value = trackingVersion;
                    command.Parameters["@EventType"].Value = @event.GetType().FullName;
                    command.Parameters["@EventData"].Value = JsonSerializer.Serialize(@event, @event.GetType(), _jsonOptions);
                    command.Parameters["@OccuredAt"].Value = @event.OccuredAt;

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                await transaction.CommitAsync(cancellationToken);
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ApplicationDomainException(
                    title: "Concurrency Conflict Detected",
                    detail: $"The financial aggregate resource '{aggregateId}' was modified by another process. Please reload your dataset and retry.",
                    statusCode: 409
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogError(ex, "FATAL DB FAULT: An unhandled database infrastructure error occurred while appending events for aggregate {AggregateId}", aggregateId);

                throw;
            }
            finally
            {
                //  Clear memory boundary safety
                AmbientContext.CurrentAggregateId = Guid.Empty;
            }
        }

        public async Task<List<BankEvent>> GetEventsAsync(Guid aggregateId, int version, CancellationToken cancellationToken = default)
        {
            var events = new List<BankEvent>();

            using var connection = new MySqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);

            const string query = "SELECT EventType, EventData, Version FROM EventStore where AggregateId = @aggregateId and Version > @version ORDER BY Version Asc";

            using var command = new MySqlCommand(query, connection);

            command.Parameters.AddWithValue("@AggregateId", aggregateId.ToString());
            command.Parameters.AddWithValue("@Version", version);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var eventTypeString = reader.GetString("EventType");
                var eventDataJson = reader.GetString("EventData");

                // Load the shared assembly explicitly using the type of any event inside it
                var coreAssembly = typeof(AccountOpenedEvent).Assembly;

                var type = coreAssembly.GetType(eventTypeString);
                if (type == null) throw new InvalidOperationException($"Unknown event type metadata: {eventTypeString}");

                var @event = JsonSerializer.Deserialize(eventDataJson, type, _jsonOptions) as BankEvent;

                if (@event != null)
                {
                    events.Add(@event);
                }
            }
            return events;
        }
    }
}
