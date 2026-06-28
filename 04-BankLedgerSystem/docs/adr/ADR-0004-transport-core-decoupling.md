# ADR 0004: Transport-Core Decoupling via Direct Command Handlers

## Status
✅ Phase 1 Active

## Context
Coupling network routers, JSON parsing, or HTTP status responses directly inside execution engines tightly binds business logic to the transport layer. This prevents alternative ingestion vectors—like background queue consumers or CLI utilities—from executing ledger modifications identically. Furthermore, introducing heavy third-party mediator frameworks can obscure dependency graphs and complicate debugging.

## Decision
We built `BankLedger.WriteProject.API` as a pure transport adapter layer completely independent of third-party routing libraries:
1. API Controllers act strictly as network adapters, converting inbound JSON payloads into immutable application Command Objects.
2. Instead of utilizing an in-memory mediator library, the system explicitly dispatches execution by injecting and invoking dedicated, single-purpose application service contracts (e.g., `ICommandHandler<WithdrawMoneyCommand>`).
3. The core execution handler processes the application command, coordinates with the Event Store, and updates aggregate state.
4. The API framework immediately dispatches an `HTTP 202 Accepted` status frame to the client, decoupling execution duration from web server thread lifetime limits.

## Consequences
* **Positive:** Keeps the core application and domain layers completely pure, transparent, and testable. Eliminates external library dependencies (`MediatR`) from the core architecture, making dependency injection graphs explicit and straightforward to trace during debugging.
* **Negative:** Requires manual registration of each individual `ICommandHandler<T>` implementation within the application's infrastructure dependency injection container, increasing boilerplate bootstrap setup code as the system scales.
