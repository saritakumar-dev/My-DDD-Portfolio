using System.Security.Cryptography;
using MySql.Data.MySqlClient;

public class MySqlCryptoKeyVault : ICryptoKeyVault
{
    private readonly string _connectionString;

    public MySqlCryptoKeyVault(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<string> GetOrCreateKeyAsync(Guid aggregateId)
    {
        // 1. Try to read an existing key
        const string selectQuery = "SELECT EncryptionKey FROM DomainEncryptionKeys WHERE AggregateId = @AggregateId LIMIT 1;";

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using (var selectCommand = new MySqlCommand(selectQuery, connection))
        {
            selectCommand.Parameters.AddWithValue("@AggregateId", aggregateId.ToString());
            using var reader = await selectCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return reader.GetString(0); // Key found, return it
            }
        }

        // 2. Fallback: If not found, generate a fresh cryptographic key
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.GenerateKey();
        string newKeyBase64 = Convert.ToBase64String(aes.Key);

        // 3. Save the new key securely to MySQL
        const string insertQuery = "INSERT INTO DomainEncryptionKeys (AggregateId, EncryptionKey, CreatedAt) VALUES (@AggregateId, @EncryptionKey, @CreatedAt);";
        using (var insertCommand = new MySqlCommand(insertQuery, connection))
        {
            insertCommand.Parameters.AddWithValue("@AggregateId", aggregateId.ToString());
            insertCommand.Parameters.AddWithValue("@EncryptionKey", newKeyBase64);
            insertCommand.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            await insertCommand.ExecuteNonQueryAsync();
        }

        return newKeyBase64;
    }

    public async Task ShredKeyAsync(Guid aggregateId)
    {
        // The GDPR "Right to be Forgotten" Kill-Switch
        const string deleteQuery = "DELETE FROM DomainEncryptionKeys WHERE AggregateId = @AggregateId;";

        using var connection = new MySqlConnection(_connectionString);
        using var command = new MySqlCommand(deleteQuery, connection);
        command.Parameters.AddWithValue("@AggregateId", aggregateId.ToString());

        await connection.OpenAsync();
        await command.ExecuteNonQueryAsync();
    }
}
