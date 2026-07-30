# ADR 0006: Performance Optimization via State Snapshots

## Status
Accepted

## Context
In our Event Sourced financial ledger system, reconstructing a `BankAccount` requires loading and replaying its entire history from disk into memory. As accounts get older and handle more transactions, this load time grows linearly ($O(N)$). This causes high read latency, high CPU usage, and long database connection hold times.

We need a way to keep state reconstruction fast and constant ($O(1)$) regardless of how old an account is or how many transactions it has processed.

## Decision
We will use the **State Snapshot Pattern** as an optimization strategy.

When an aggregate's version hits a distinct interval threshold (every 100 events), a flat state checkpoint (`BankAccountSnapshot`) is generated. To maximize write speed and keep things lightweight, we rejected Entity Framework. Instead, we use native **MySQL ADO.NET database commands** utilizing fixed `reader.GetOrdinal()` lookup indices.

During reconstruction, the repository runs a fast indexed query (`ORDER BY Version DESC LIMIT 1`) to grab only the latest snapshot. The system boots the aggregate from this checkpoint and queries the event store strictly for trailing events where `Version > CheckpointVersion`.

## Consequences

### Upsides
*   **Constant-Time Reads ($O(1)$)**: Loading an account stays fast and predictable. The database reads a maximum of 99 trailing events, no matter how large the total history is.
*   **Lower I/O Overhead**: Drastically cuts down memory allocation and payload sizes when streaming events from disk.
*   **Bare-Metal Performance**: Using raw ADO.NET with integer column ordinals bypasses heavy ORM change-tracking overhead.

### Downsides
*   **Data Duplication**: State data is stored twice (once in the Event Store and once in the Snapshot table), using slightly more disk space.
*   **Dual-Write Handling**: The application layer or repository must coordinate saving to both tables. A snapshot write failure must fail silently or lazily to avoid blocking the main transaction.
