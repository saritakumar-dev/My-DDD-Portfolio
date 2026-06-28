# ADR 0003: Aggregates Invariant Protection via Static Factory Methods

## Status
✅ Phase 1 Active

## Context
In an event-sourced ledger system, an aggregate's memory state must be reconstituted cleanly by replaying historical events sequentially. Allowing public parameterless initialization (`new BankAccount()`) exposes a structural vulnerability where an invalid, completely empty aggregate root could accidentally be passed into the business handling layer.

## Decision
We locked the instantiation lifecycle within `BankLedger.Core`:
1. Sealed the `BankAccount` aggregate root constructor as `private`.
2. Exposed state reconstitution solely through a public static factory method: `BankAccount.LoadFromHistory(IEnumerable<BankEvent> history)`.
3. Separated historical event replaying routines from runtime business validation rules so that historical log processing never triggers modern validation invariants.

## Consequences
* **Positive:** Guarantees absolute domain model integrity in memory. A developer cannot accidentally instantiate or save an uninitialized or broken ledger entity.
* **Negative:** Bypasses default parameterless construction patterns used by standard ORM mappers, requiring explicit manual instantiation factories inside our infrastructure layer.
