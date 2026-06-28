# 🏛️ BankLedgerSystem (Phase 1)

An enterprise-grade, high-performance financial ledger system built using **Domain-Driven Design (DDD)**, an **Append-Only Event Store**, and **Command-Query Responsibility Segregation (CQRS)** boundaries. 

This project demonstrates production-ready strategies for maintaining absolute data integrity and extreme write throughput without the overhead of heavy ORM engines or monolithic third-party frameworks.

---

## 🗺️ Project Roadmap & Evolutionary Architecture

To simulate a real-world enterprise system expansion, this repository follows a strict multi-phase rollout matrix:

### 🚀 Phase 1 (Current Active Commit): High-Performance In-Process Core
*   **Focus:** Strong data integrity, low-latency event ingestion, and strict aggregate boundary enforcement.
*   **Storage Execution:** Pure, native ADO.NET with storage-level Optimistic Concurrency Control (OCC).
*   **Process Coordination:** Stateful application-layer Saga state machine tracking with inline handler dispatching.

### 🔮 Phase 2 (Future Work): Distributed Scale & Read Optimization
*   **Focus:** Deconstructing the write side via out-of-process messaging and horizontal scaling.
*   **Planned Changes:** Offloading inbound commands to distributed message brokers (RabbitMQ/Kafka) and projecting asynchronous, denormalized read-models into a fast cache tier (Redis/NoSQL).

---

## 📂 System Architecture & Directory Layout

The physical folder structure enforces a dependency flow that looks strictly inward toward the domain layer:

```text
📂 04-BankLedgerSystem
 ┣ 📂 docs/adr/                      # Formal Architectural Decision Records (ADRs 0001-0005)
 ┣ 📂 src/
 ┃ ┣ 📂 BankLedger.Core              # Pure Bounded Domain Context (Aggregates, Invariants, Events)
 ┃ ┣ 📂 BankLedger.WriteProject      # Application Core (Explicit Command Handlers, Stateful Saga)
 ┃ ┗ 📂 BankLedger.WriteProject.API  # Transport Infrastructure Adapter (Thin Controllers, HTTP 202)
 ┣ 📂 tests/                         # Isolated xUnit Testing Suites
 ┗ 📜 BankLedgerSystem.sln           # Phase 1 Target-Only Compilation Reference
```

---

## 🏛️ Architectural Decision Records (ADR Registry)

Every architectural choice in this system is deliberately selected to manage real hardware and database constraints. Read the deep-dive technical reasoning behind each decision:

| ID | Key Architectural Decision | Core Engineering Focus Area | Status |
| :--- | :--- | :--- | :--- |
| **[ADR-0001](./docs/adr/ADR-0001-optimistic-concurrency.md)** | Storage-Level OCC via MySQL Unique Constraints (Error 1062) | Data Integrity & Lock Reduction | ✅ Phase 1 Active |
| **[ADR-0002](./docs/adr/ADR-0002-saga-orchestration.md)** | Stateful Saga Orchestration for Cross-Aggregate Transfers | Eventual Consistency Matrix | ✅ Phase 1 Active |
| **[ADR-0003](./docs/adr/ADR-0003-static-factory-reconstitution.md)** | Aggregates Invariant Protection via Static Factory Methods | Memory-State Protection | ✅ Phase 1 Active |
| **[ADR-0004](./docs/adr/ADR-0004-transport-core-decoupling.md)** | Transport-Core Decoupling via Direct Command Handlers | Clean Architecture Boundaries | ✅ Phase 1 Active |
| **[ADR-0005](./docs/adr/ADR-0005-not-using-orm-for-event-store.md)** | Eliminating ORM Frameworks in Favor of Native ADO.NET | Memory & High Throughput Optimization | ✅ Phase 1 Active |

---

## ⚡ Technical Highlights From The Implementation

### 1. Zero ORM Overhead in the Ingestion Path
Unlike traditional CRUD state tracking, the event store uses an append-only sequence pattern. Rather than loading Entity Framework Core—which introduces reflection penalties, object change tracking overhead, and unnecessary Garbage Collector pressure—the write pipeline uses **native ADO.NET (`DbCommand`)**. Events are flushed using raw parameterized scripts directly to the database connection socket via `ExecuteNonQueryAsync`.

### 2. Lockless Concurrency Safety
The engine avoids heavy database range/gap locking penalties under high-frequency writes by lowering transaction limits down to `ReadCommitted`. Concurrency race conditions are handled deterministically at the database index layer using a composite unique key constraint on `(AggregateId, Version)`. If a conflict occurs:
* The database engine throws error code **`1062` (Duplicate Entry)**.
* The low-level infrastructure catches it natively (`ex.Number == 1062`).
* An explicit `await transaction.RollbackAsync()` is dispatched.
* A clear application-level `InvalidOperationException` bubbles up safely.

### 3. Stateful Process Orchestration
Cross-account transfers are handled entirely by an asynchronous application-layer Saga state machine (`MoneyTransferSaga`). The system listens for aggregate events, correlates tracking indices via a unique tracking string (`sagaId`), hydrates tracking entities using a dedicated state repository, and drives the workflow forward through explicit state transitions using custom command handlers. This approach preserves absolute aggregate isolation while replacing traditional distributed two-phase database locks (2PC) with non-blocking eventual consistency.

---

## 🧪 Test Automation & Domain Invariant Verification

Because the core domain logic is entirely decoupled from external frameworks and database drivers, the system is fully testable. The testing matrix uses xUnit and FluentAssertions to validate constraints without mocking overhead where possible.

### 1. Domain Layer Invariant Testing (`tests/BankLedger.Domain.Tests`)
*   **Focus:** Core business rules, bounds checking, and failure state consistency.
*   **Key Coverage:** 
    *   Verifying negative or zero-dollar transaction attempts immediately throw custom domain exceptions.
    *   Ensuring overdraft limits are strictly enforced inside the aggregate root memory before events are generated.
    *   Validating that historical event replay sequences reconstitute a mathematically perfect account state.

### 2. Application Layer Workflow Testing (`tests/BankLedger.Application.Tests`)
*   **Focus:** Saga state machine transitions and command handler routing.
*   **Key Coverage:**
    *   Asserting that `MoneyTransferSaga` rejects out-of-order events using explicit state machine guard clauses.
    *   Verifying that successful withdrawal events automatically trigger and route the downstream deposit command via explicit handlers.

### 3. Infrastructure Exception Mapping (`tests/BankLedger.Infrastructure.Tests`)
*   **Focus:** Storage optimization verification.
*   **Key Coverage:**
    *   Validating that the low-level ADO.NET Event Store accurately intercepts MySQL Error `1062` and transforms it into an application-level `InvalidOperationException` for concurrency management.

---

## 🛠️ Local Development & Quick Start

### Prerequisites
*   .NET 8.0 SDK or newer
*   MySQL Database Server Instance

### Setup and Compilation
1. Restore your application package dependencies:
   ```bash
   dotnet restore BankLedgerSystem.sln
   ```
2. Build the Phase 1 system locally:
   ```bash
   dotnet build BankLedgerSystem.sln --configuration Release
   ```
3. Execute the automated validation and invariant testing suites:
   ```bash
   dotnet test BankLedgerSystem.sln
   ```
