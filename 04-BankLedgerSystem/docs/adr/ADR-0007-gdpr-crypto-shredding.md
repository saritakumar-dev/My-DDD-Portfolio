# ADR 0007: GDPR Compliance via Crypto-Shredding

## Status
Accepted

## Context
Under EU GDPR law (Article 17), customers can request full deletion of their personal data (PII), such as their name. However, our system uses an append-only, immutable Event Store. Running `UPDATE` or `DELETE` queries directly on our event tables is not an option—it would corrupt our financial audit trail and break event link verification.

We need a way to fully delete user PII without touching or changing our immutable event logs.

## Decision
We will use **Client-Side Crypto-Shredding** to solve this.

Any personal data inside our events (like `CustomerName` in `AccountOpenedEvent`) will be tagged with a custom `[GdprEncrypted]` attribute. During serialization, a custom `JsonConverterFactory` will intercept these fields. It reads the current `AccountId` from an `AsyncLocal` context and pulls a unique **AES-256 key** for that account from a separate `DomainEncryptionKeys` MySQL table to encrypt the text string.

When a deletion request comes in, we perform a hard delete **only on the row in our encryption keys table** and publish a `UserForgottenEvent` to our Service Bus so our Cosmos DB read model can delete its cache. The Event Store remains untouched, but because the AES key is gone, the encrypted name payload in our logs instantly becomes unreadable, anonymous noise. This fully satisfies GDPR erasure laws.

## Consequences

### Upsides
*   **Ledger Immutability**: We comply with GDPR without modifying a single byte of our historical event store.
*   **Security at Rest**: If the event store database is ever leaked, the PII remains completely secure since it is stored as ciphertext.
*   **Decoupled Domain**: The encryption and masking logic is handled inside infrastructure serialization converters, keeping our core aggregate code perfectly clean.

### Downsides
*   **Latency**: Saving or reading an encrypted event requires an extra database lookup to fetch the active AES key from our key vault table.
*   **Key Loss Risk**: If we accidentally lose or corrupt a row in the keys table, that user's event data becomes permanently unreadable with no way to recover it.
