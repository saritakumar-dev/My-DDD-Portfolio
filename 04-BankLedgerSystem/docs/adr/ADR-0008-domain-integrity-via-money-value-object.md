# ADR 0008: Domain Integrity via Money Value Object

## Status
Accepted

## Context
Our `BankAccount` aggregate root originally tracked balances using a loose `decimal Balance` and a `string Currency`. This pattern introduced **Primitive Obsession**, making it easy for critical bugs to slip past the compiler. For example, without duplicate validation checks inside every method, the system could accidentally add `USD` directly to a `EUR` balance, causing corrupted financial states.

We need to enforce currency and balance validation at the type layer and completely separate business validation from property mutations.

## Decision
We will encapsulate our financial primitives into an immutable **`Money` Value Object** using a C# `record`.

The `Money` constructor will act as a strict gatekeeper, blocking negative amounts or empty currency codes from being created anywhere in the system. The record handles its own math logic via explicit `.Plus()` and `.Minus()` methods, which automatically verify that currencies match before executing any math.

We will also enforce a strict **Segregation of Concerns** inside the `BankAccount` aggregate:
1.  **Business Methods** (`Deposit`, `Withdraw`) will act strictly as guards and event emitters. They will handle no direct property assignments.
2.  **Mutator Handlers** (`ApplyEvent` switch cases) will be the *only* place allowed to modify properties, delegating the math directly to the `Money` record operators.

## Consequences

### Upsides
*   **Compile-Time Type Safety**: Eliminates primitive obsession. Mismatched or invalid calculations short-circuit at the type layer before hitting the database.
*   **Deterministic State**: Separating evaluation from the `ApplyEvent` loop removes the double-addition memory bug. Calculations stay identical during live updates and history replays.
*   **Reusable Component**: The rich type-safety logic is completely decoupled from the `BankAccount`, allowing future services (billing, fees, invoicing) to reuse it instantly.

### Downsides
*   **Memory Allocations**: Because the `Money` record is strictly immutable, every mathematical operation creates a new instance, slightly increasing garbage collection load under high transaction rates.
*   **Mapping Overhead**: Saving or loading snapshots requires mapping the encapsulated object back and forth into flat database columns (`BalanceAmount`, `BalanceCurrency`) inside the ADO.NET query layer.
