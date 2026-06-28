# ADR 0001: Storage-Level OCC via MySQL Unique Constraints (Error 1062)

## Status
✅ Phase 1 Active

## Context
Multi-threaded writes to the event store table can cause race conditions where two threads attempt to append events to the same aggregate version sequence. Using heavy ORM track-and-modify patterns or MySQL's default `RepeatableRead` gap locks introduces severe blocking bottlenecks and database deadlocks under concurrent load.

## Decision
We implemented a high-performance Optimistic Concurrency Control (OCC) mechanism utilizing low-level ADO.NET commands (`DbCommand`) inside an explicit database transaction block:
1. Enforced a composite unique key constraint on `(AggregateId, Version)` in the underlying MySQL table schemas.
2. Appended events sequentially using parameterized asynchronous calls (`ExecuteNonQueryAsync`).
3. Captured race condition failures by intercepting native `MySqlException` filters explicitly checking for error code `1062` (`ex.Number == 1062`).
4. On a `1062` violation, we execute an explicit `await transaction.RollbackAsync()` and bubble up a domain-meaningful `InvalidOperationException` stating: `"Concurrency Exception: Aggregate {aggregateId} was modified by another request."`.

## Consequences
* **Positive:** Drastically reduces locking overhead by avoiding range/gap locks. Leverages the database engine's native indexing speed to catch concurrency race conditions safely.
* **Negative:** Because multiple events in a single batch use an immutable `expectedVersion` mapping check per loop step, concurrency conflicts result in an immediate transaction abort, shifting batch retry orchestration entirely up to the application's command dispatch boundary.
