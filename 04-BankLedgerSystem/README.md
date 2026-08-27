# BankLedger System

A secure, immutable, and high-throughput financial ledger system designed to track balances and transactions.

## Features
* **Event Sourcing**: Records all account changes as an immutable sequence of events, ensuring a perfect historical record. 
* **CQRS Architecture**: Separates the write-heavy ledger transactions (MySQL) from high-speed read queries (Cosmos DB).
* **Double-Entry Bookkeeping**: Ensures financial accuracy and zero-sum balancing across all accounts.
* **Debit / Credit Transactions**: Supports secure deposits, withdrawals, and balance transfers for individual accounts.
* **Idempotency**: Prevents duplicate transaction processing and accidental double-spend events.
* **Snapshot Pattern**: Optimizes performance by reducing account history replay time from $O(N)$ to $O(1)$.
* **Immutable Audit Trail**: Maintains a permanent, chronological log of all balance changes for compliance tracking.
* **GDPR Compliance**: Protects user privacy with data minimization and secure "right to be forgotten" handling for personal records.
* **GoBD Compliance**: Meets strict European (German) requirements for immutable, orderly, and verifiable digital record-keeping.

## Project Structure 
The repository follows Domain-Driven Design (DDD) and Clean Architecture principles: 

```text 
src/ 
├── BankLedger.Domain/                     # Aggregates, Entities, Value Objects, Domain Events, Exceptions
├── BankLedger.WriteProject.Application/   # CQRS Commands, Event Handlers, Bus/DB Interfaces, Sagas, Factories
├── BankLedger.WriteProject.Infrastructure/# MySQL Event Store, InMemoryMessageBus, DbContext, Repositories
├── BankLedger.WriteProject.API/           # Write API, Thin Controllers, Middleware (HTTP 202)
├── BankLedger.ReadModel.Projection/       # Cosmos DB Projections, Background Workers, Polly Resilience
└── BankLedger.ReadModel.API/              # Read API, Minimal API Balances/Queries Gateway

tests/ 
├── BankLedger.Domain.Tests/               # Unit tests for Domain logic & invariant boundaries
├── BankLedger.Application.Tests/          # Unit tests for Sagas & workflow state machines
├── BankLedger.ReadModel.Tests/            # Unit tests for Read-Model queries
└── BankLedger.IntegrationTests/           # E2E tests for MySQL, Cosmos DB, and Concurrency (Error 1062)
```

## 🏛️ Architectural Decision Records (ADR Registry)

Every critical technical choice in this financial ledger is explicitly tracked to document how we manage hardware, storage, and consistency boundaries. 

| ID | Key Architectural Decision | Core Engineering Focus Area | Status |
| :--- | :--- | :--- | :--- |
| **[ADR-0001](./docs/adr/ADR-0001-optimistic-concurrency.md)** | Storage-Level OCC via MySQL Unique Key Constraints | Relational Data Integrity | ✅ Active |
| **[ADR-0002](./docs/adr/ADR-0002-saga-orchestration.md)** | Stateful Saga Orchestration for Cross-Aggregate Transfers | Eventual Consistency Matrix | ⚠️ Superseded by ADR-0009 |
| **[ADR-0003](./docs/adr/ADR-0003-static-factory-reconstitution.md)** | Aggregates Invariant Protection via Static Factory Methods | Memory-State Protection | ✅ Active |
| **[ADR-0004](./docs/adr/ADR-0004-transport-core-decoupling.md)** | Transport-Core Decoupling via Direct Command Handlers | Clean Architecture Boundaries | ✅ Active |
| **[ADR-0005](./docs/adr/ADR-0005-not-using-orm-for-event-store.md)** | Eliminating ORM Frameworks in Favor of Native ADO.NET | Memory & High Throughput Optimization | ✅ Active |
| **[ADR-0006](./docs/adr/ADR-0006-performance-snapshots.md)** | Performance Optimization via State Snapshots | Aggregation Hydration Bottlenecks | ✅ Active |
| **[ADR-0007](./docs/adr/ADR-0007-gdpr-crypto-shredding.md)** | GDPR Compliance via Crypto-Shredding Patterns | Data Privacy & Immutability Coexistence | ✅ Active |
| **[ADR-0008](./docs/adr/ADR-0008-money-value-object.md)** | Domain Integrity via Money Value Object | Floating-Point Precision Protection | ✅ Active |
| **[ADR-0009](./docs/adr/ADR-0009-atomic-ledger-boundary.md)** | Moving from Distributed Sequential Steps to an Atomic Ledger Boundary | Absolute Relational Transaction Isolation | ✅ Active |

## Getting Started

### Prerequisites
* **Language / Runtime**: .NET 8 SDK
* **Primary Database**: MySQL 8.0+ (Relational store for ledger accounts)
* **Read-Model Database**: Azure Cosmos DB (NoSQL store for CQRS query side)
* **Message Broker**: In-Memory Bus 
* **Resilience Framework**: Polly (For exponential backoff and retry strategies)


### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/saritakumar-dev/My-DDD-Portfolio.git
   cd 04-BankLedgerSystem
   ```
2. Configure environment variables:
   ```bash
   cp .env.example .env
   ```
3. Run migrations:
   ```bash
   # Add your migration command here
   ```

### Running the Application

To execute the full CQRS loop locally, both the Write and Read API applications must be active simultaneously. Open two separate terminal windows or use Visual Studio's Multi-Project Startup feature:

**Window 1: Start the Write API (Command Side)**
```bash
dotnet run --project src/BankLedger.WriteProject.API
```

**Window 2: Start the Read API (Query Side)**
```bash
dotnet run --project src/BankLedger.ReadModel.API
```
## API Documentation

The system completely decouples operations by separating endpoints into two physically isolated API services following strict CQRS patterns.

### 🟢 1. Command Service (Write Side)
* **Project Boundary**: `src/BankLedger.WriteProject.API/`
* **Local Hosting Port**: `http://localhost:5100/swagger`
* **Endpoints**:
  * `POST /api/accounts` - Opens a new Bank Account and initializes the append-only event stream.
  * `POST /api/deposits` - Processes money deposits using low-level ADO.NET and storage-level OCC.
  * `POST /api/deleteaccount` - Closes the Bank Account (emits an account-closed event to maintain immutable history).
  * `POST /api/jounalentrytransfer` - Posts a new balanced double-entry Journal Entry Transfer transaction.

### 🔵 2. Query Service (Read Side)
* **Project Boundary**: `src/BankLedger.ReadModel.API/`
* **Local Hosting Port**: `http://localhost:5292/swagger`
* **Endpoints**:
  * `GET /api/balances/{id}` - Fetches the denormalized, up-to-date calculated account balance directly from Azure Cosmos DB.

## Running Tests

The test suite validates domain invariants, consistency boundaries, distributed workflow states, and compliance rules using a distinct unit and integration testing strategy.

### Test Stack
* **Framework**: xUnit
* **Mocking Engine**: Moq
* **Assertions**: FluentAssertions

### Execute Complete Test Suite
```bash
dotnet test
```

### Targeted Test Projects Execution

* **Core Domain Invariants (`tests/BankLedger.Domain.Tests/`)**
  Validates pure aggregate boundary rules, currency precision tracking, and memory-state protection logic without hitting external databases.
  ```bash
  dotnet test tests/BankLedger.Domain.Tests
  ```

* **Workflow & Saga State Transitions (`tests/BankLedger.Application.Tests/`)**
  Verifies `MoneyTransferSaga` lifecycle state checks, message routing patterns, and SagaParser processing logic.
  ```bash
  dotnet test tests/BankLedger.Application.Tests
  ```

* **Read-Model Projections (`tests/BankLedger.ReadModel.Tests/`)**
  Validates `AccountBalanceProjector` tracking rules and Cosmos DB state generation accuracy.
  ```bash
  dotnet test tests/BankLedger.ReadModel.Tests
  ```

* **Infrastructure & Integration Verification (`tests/BankLedger.IntegrationTests/`)**
  Executes end-to-end loops verifying `MySqlEventStore` concurrency logic (Error 1062 tracking), `SnapshotPattern` execution speeds, and `GdprErasure` compliance routines.
  ```bash
  dotnet test tests/BankLedger.IntegrationTests
  ```
## License
Distributed under the MIT License. See `LICENSE` for more information.
