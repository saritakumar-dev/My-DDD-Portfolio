using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Domain.Aggregates;
using MySql.Data.MySqlClient;

namespace BankLedger.WriteProject.Infrastructure.Database
{
    public class MySqlSnapshotStore : ISnapshotStore
    {
        private readonly string _connectionString;

        public MySqlSnapshotStore(string connectionString)
        {
            _connectionString = connectionString;
        }
        public async Task<BankAccountSnapshot> GetLatestAsync(Guid aggregateId)
        {
            const string query = @"
            SELECT Version, BalanceAmount, BalanceCurrency, SnapshottedAt 
            FROM BankAccountSnapshots 
            WHERE AggregateId = @AggregateId 
            ORDER BY Version DESC 
            LIMIT 1;";
            using var connection = new MySqlConnection(_connectionString);
            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@AggregateId", aggregateId.ToString());

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            int versionOrdinal = reader.GetOrdinal("Version");
            int amountOrdinal = reader.GetOrdinal("BalanceAmount");
            int currencyOrdinal = reader.GetOrdinal("BalanceCurrency");
            int dateOrdinal = reader.GetOrdinal("SnapshottedAt");

            if (await reader.ReadAsync())
            {
                return new BankAccountSnapshot
                {
                    AggregateId = aggregateId,
                    Version = reader.GetInt32(versionOrdinal),
                    Balance = new Money(reader.GetDecimal(amountOrdinal), reader.GetString(currencyOrdinal)),
                    SnapshottedAt = reader.GetDateTime(dateOrdinal)
                };
            }

            return null;
        }

        public async Task SaveAsync(BankAccountSnapshot snapshot)
        {
            const string cmdText = @"
            INSERT INTO BankAccountSnapshots (AggregateId, Version, BalanceAmount, BalanceCurrency, SnapshottedAt) 
            VALUES (@AggregateId, @Version, @BalanceAmount, @BalanceCurrency, @SnapshottedAt);";

            using var connection = new MySqlConnection(_connectionString);
            using var command = new MySqlCommand(cmdText, connection);

            command.Parameters.AddWithValue("@AggregateId", snapshot.AggregateId.ToString());
            command.Parameters.AddWithValue("@Version", snapshot.Version);
            command.Parameters.AddWithValue("@BalanceAmount", snapshot.Balance.Amount);
            command.Parameters.AddWithValue("@BalanceCurrency", snapshot.Balance.Currency);
            command.Parameters.AddWithValue("@SnapshottedAt", snapshot.SnapshottedAt);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}
