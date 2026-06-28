# ADR 0002: Stateful Saga Orchestration for Cross-Aggregate Transfers

## Status
✅ Phase 1 Active

## Context
Executing a funds transfer requires modifying balances across separate, isolated account aggregate roots (the source and target accounts). Direct cross-aggregate manipulation within a single transaction boundary violates aggregate isolation rules and causes severe database row-locking contention. We need a way to manage this multi-step workflow reliably without distributed database locks.

## Decision
We implemented a stateful **Saga (Process Manager)** pattern in the application layer via `MoneyTransferSaga`. The workflow operates as follows:
1. The Saga acts as an asynchronous event handler reacting to core ledger domain events (e.g., `MoneyWithdrawnEvent`, `MoneyDepositedEvent`).
2. It extracts a tracking correlation key from the event payload via `ExtractSagaIdFromReference(@event.Reference)`.
3. It fetches and updates a persistent workflow state model (`_sagaStateRepository`) to track the exact lifecycle progress via an explicit enum state machine (`TransferWorkflowState`).
4. It enforces strict guard clauses before advancing state transitions (e.g., verifying state is exactly `WithdrawalStarted` before marking it `WithdrawalCompleted`).
5. Upon successful state persistence, it triggers the next step in the pipeline by mapping state variables to a new down-stream command execution context (`_depositHandler.HandleAsync(depositMoneyCommand, ...)`).

## Consequences
* **Positive:** Preserves absolute aggregate boundary isolation; the source account knows nothing about the target account. Replacing rigid 2-Phase Commit (2PC) database locks with an explicit, persistent state machine ensures high write availability.
* **Negative:** Introduces an intermediate state window where funds have cleared the source account but have not yet hit the destination target. Since commands are invoked inline via injected handlers rather than a distributed message queue in Phase 1, an unhandled infrastructure crash immediately following `SaveAsync` but prior to `HandleAsync` requires manual or scripted audit reconciliation to resume the pending state.
