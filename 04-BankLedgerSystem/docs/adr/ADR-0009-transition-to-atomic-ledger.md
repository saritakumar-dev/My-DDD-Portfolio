### ADR 0009: Moving from Distributed Sequential Steps to an Atomic Ledger Boundary

### Status

Accepted 

### Context & Problem Statement

Our old setup transferred money by executing database operations in sequence: debiting Account A first, saving the change, and then trying to credit Account B. 

This caused two major issues: 

1. **Network Drops**: If the system crashed or lost connection between the debit and the credit, money vanished in transit, throwing the ledger completely out of balance.
2. **Dirty History**: If the target account was closed, the Saga had to run a manual "money reversal" credit back to the source account. This created artificial transaction rows that made financial auditing confusing.

### Decision Drivers

* Stop money from getting lost in transit during network glitches.
* Get rid of messy database rollback and reversal code.
* Ensure total debits always equal total credits before saving anything.

### The Decision

We refactored the pipeline to handle multi-account transfers inside a single, balanced JournalEntry domain aggregate. 

The MoneyTransferSaga no longer manages step-by-step database rollbacks. Instead, it acts as a smart validation gate that works entirely in memory: 

* It looks up all affected account balances at the same time (Task.WhenAll).
* It groups the rows in memory to catch net overdrafts and checks if any account is closed.
* If everything passes, it dispatches a single command to save the entire balanced entry at once.

If a validation fails, the system throws an exception immediately. The database is never touched, making complex asynchronous compensation loops obsolete. 

### Consequences

* **Good**: Zero chance of money vanishing mid-transit.
* **Good**: Code is significantly cleaner because we deleted the rollback and reversal loops.
* **Good**: We can write simple, fast unit tests for our transfer logic without mocking database states.
* **Watch out**: Processing huge lists of transfers will require a bit more memory upfront to handle the grouping dictionaries, but our performance benchmarks show this is well within acceptable limits.