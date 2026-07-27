public interface ICryptoKeyVault
{
    // Retrieves an existing key, or automatically generates a new one if it doesn't exist yet
    Task<string> GetOrCreateKeyAsync(Guid aggregateId);

    // The GDPR "Forget Me" Kill-Switch
    Task ShredKeyAsync(Guid aggregateId);
}
