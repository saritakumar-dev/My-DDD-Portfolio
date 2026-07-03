# Technical Design Document: Event-Sourced Banking Ledger System

**Author:** Sarita Kumar 
**Target Architecture State:** Production-Ready CQRS Ecosystem  
**Framework Version:** .NET 8 / MySQL 8 / Azure Cosmos DB Live Cluster  

---

## 1. Executive Summary & Problem Statement

Traditional banking databases face major challenges when managing account balances directly using standard CRUD operations:

* **Concurrency Bottlenecks:** When multiple transactions hit the same account at the exact same millisecond, the database engine suffers from heavy data locks, timing race conditions, or complete timeouts.
* **Performance Degradation:** Recalculating account balances globally by scanning massive transaction tables causes the database performance to slow down significantly as data scales over time.
* **Audit Trail Loss:** Overwriting a balance column directly destroys the historical timeline, making it very difficult to execute a clean trace audit for regulatory compliance.

### Architectural Solution

This platform resolves these issues by using **Command Query Responsibility Separation (CQRS)** and an append-only **Event Sourced Write Model**. The system state is never changed in place. Instead, the balance is calculated dynamically by replaying a clean history of unalterable business events. The Write Side focuses entirely on append speed and security validation, while a completely separate Read Side projects records onto a NoSQL document database optimized for single-digit millisecond query access.

---

## 2. Core Solution Architecture & Component Design

The system boundaries are split into separate .NET Class Libraries to enforce clean dependency boundaries.

### 2.1 The Write Model (Command Domain)

* **Domain Layer (`BankLedger.Domain`)**: Pure C# logic with zero external database dependencies. The `BankAccount` Aggregate Root handles core business rules (like checking for insufficient funds) and records changes by creating `BankEvent` records.
* **Infrastructure Persistence (`BankLedger.Infrastructure`)**: We use a targeted hybrid persistence model to maximize performance and code maintainability:
  * **The Event Store Engine**: Built using raw **ADO.NET**. It completely avoids Entity Framework's change-tracking overhead, using pre-compiled parameterized statements and an explicit `IsolationLevel.ReadCommitted` configuration to eliminate database lock delays during batch writes.
  * **The Saga State Engine**: Built using **Entity Framework Core (`DbContext`)**, leveraging clean Object-Relational Mapping to manage the short-lived CRUD operational checklists.

### 2.2 The Read Model (Query Projection)

* **Projection Engine (`BankLedger.ReadModel.Projection`)**: Acts as a state denormalizer. It intercepts events across process boundaries and maps them directly into flat document snapshots.
* **Storage Fabric**: Utilizes **Azure Cosmos DB** hosting key-value document records (`AccountBalanceDocument`). The structure maps properties to a lowercase `"id"` to satisfy native NoSQL index routes, allowing users to pull balances without executing expensive table joins.

---

## 3. Distributed Process Orchestration (The Saga Pattern)

Modifying two separate bank accounts simultaneously cannot be done in a single database transaction without causing major latency bottlenecks. This platform coordinates multi-account transfers using an asynchronous **MoneyTransferSaga** process manager.

### 3.1 Lifecycle Tracking and Consistency

* **State Checkpointing**: The Saga operates as an in-process state machine, tracking its milestones inside a dedicated database table named `SagaStates`. It breaks complex operations into step-by-step milestones (such as `WithdrawalStarted` and `WithdrawalCompleted`), updating the database row at every stage before triggering downstream actions. This checkpoint record serves as a durable log of exactly how far the transaction progressed.
* **Idempotency Safeguards**: To prevent duplicate asset allocations if an execution thread retries a step, command handling logic checks past event reference strings in the account history to automatically drop duplicate requests before running any writes.

---

## 4. Key Cross-Cutting Solutions & Bug Remedies

During development and integration testing, multiple subtle architectural edge cases were uncovered and resolved:

1. **Circular Graph Dependency Elimination**: In-process handlers originally coupled the command execution pipeline directly back to the calling Saga class instance, creating a tight architectural loop. This was resolved by implementing an in-process messaging interface layer (`IMessageBus`) that lets components publish events blindly to break the graph loop.
2. **Implicit SQL Full Table Scans**: A missing variable signifier (`@`) inside the repository's native select syntax changed a parameterized where check into a self-referencing column filter (`WHERE AggregateId = aggregateId`), returning all event records in the table log. Restoring the identifier syntax explicitly re-established fast, isolated index seeking.

---

## 5. Non-Functional Requirements & Performance Analysis

* **Latency Profile**: Write paths return an immediate `202 Accepted` network frame inside a microsecond window, handing execution tasks to processing threads. Read queries process via Azure Cosmos DB `ConnectionMode.Direct` TCP points, guaranteeing flat balance retrievals under 10 milliseconds.
* **Data Integrity**: Enforced via a composite unique key database index on `UQ_Aggregate_Version (AggregateId, Version)`. This guarantees that if two separate user requests attempt to assert identical stream sequence version counters, the storage framework blocks the execution path instantly with an Optimistic Concurrency Exception, ensuring absolute financial consistency.
