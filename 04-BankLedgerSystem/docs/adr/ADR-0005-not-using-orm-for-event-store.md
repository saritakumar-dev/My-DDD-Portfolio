# ADR 0005: Eliminating ORM Frameworks in Favor of Native ADO.NET for Event Appends

## Status
✅ Phase 1 Active

## Context
In traditional CRUD (Create, Read, Update, Delete) subsystems, Entity Framework Core (EF Core) is the industry standard due to its robust Object-Relational Mapping (ORM), automated change tracking, and stateful relationship graph stitching. However, an Event Store possesses fundamentally different data mutations and runtime performance patterns compared to state-based tables. 

An Event Store operates structurally as an append-only ledger. Records are strictly inserted sequentially and are never subjected to downstream `UPDATE` or `DELETE` commands. Utilizing EF Core's heavy runtime engine to execute these continuous, high-throughput linear appends introduces massive reflection layers, heavy object-tracking state allocations on the .NET Garbage Collector, and unoptimized SQL generation paths under intense concurrent financial transaction volumes.

## Decision
We explicitly chose not to use EF Core within the core `MySQLEventStore` infrastructure engine, opting instead for pure, native ADO.NET (`DbCommand`) execution for all stream write appends:
1. We construct lightweight, explicit parameterized SQL queries manually within our data access layer.
2. We utilize native asynchronous command loops (`ExecuteNonQueryAsync`) to stream event blocks directly to the network socket.
3. We completely avoid initializing heavy context pipelines, removing the memory and performance tracking overhead of an ORM from the event ingestion path.

## Consequences
* **Positive:** Maximizes transactional write throughput and eliminates unnecessary CPU/RAM usage by completely cutting out EF Core’s change tracking engine, model parsing, and reflection overhead. This radically reduces Garbage Collector memory allocation pressure under heavy financial write loads.
* **Negative:** Not using a full-featured ORM means we do not have out-of-the-box database migrations. Database schema evolution and raw SQL query strings must be manually handled, written, and maintained directly within infrastructure boundaries.
