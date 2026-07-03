# Engineering Post-Mortem: Resolving Asynchronous Event Inversion & Scope Corruption

## 1. Symptom & System Defect
During end-to-end integration testing of the CQRS multi-account transfer workflow, a critical data-drift anomaly was detected across storage fabrics:
* **The Symptom:** The Write Model (MySQL Event Store) recorded the chronological event ledger entries perfectly. However, the Read Model (Azure Cosmos DB) randomly failed to update account balances, or incorrectly credited the source account while skipping the target account entirely.
* **The Error Trail:** The console terminal logged intermittent `System.ObjectDisposedException: Cannot access a disposed object` errors tracing back to the infrastructure data context wrappers during background event projection sweeps.

---

## 2. Root Cause Analysis (The Architecture Inversion)

A deep thread-line trace revealed that the system was suffering from of **Asynchronous Task Inversion** (causing the deposit event to reach Cosmos DB before the withdrawal event could complete its work) inside the messaging layer.

### 2.1 The Fire-and-Forget Race Condition
The initial implementation of the `InMemoryMessageBus` utilized a fire-and-forget multi-threaded dispatch structure using `Task.Run()` to avoid blocking the primary Web API gateway thread:
```csharp
// Stale Buggy Implementation
 var handlerInterfaceType = typeof(IDomainEventHandler<>).MakeGenericType(runtimeType);

 var handlers = _serviceProvider.GetServices<IDomainEventHandler<TEvent>>();

 foreach (var handler in handlers)
 {
     var method = handlerInterfaceType.GetMethod("HandleAsync");
     if (method != null)
     {
         var invocationResult = method.Invoke(handler , new object[] {@event, cancellationToken});

         if (invocationResult is Task task)
         {
             await task;

             /// remaining code
```
This introduced a severe execution race condition. When a user triggered a transfer, the withdrawal event completed its MySQL save and was pushed to the bus. The bus kicked off a background task for the withdrawal and immediately released the thread. 

The **MoneyTransferSaga** process manager intercepted the event on that background task and instantly dispatched the subsequent `DepositMoneyCommand`. Because the deposit pipeline ran synchronously, it successfully wrote to MySQL and generated a `MoneyDepositedEvent`. 

This secondary deposit event reached the Read Model projector **before** the initial withdrawal background thread could complete its work, completely flipping the chronological timeline of a financial system.

### 2.2 Scope Disposal Execution Collision
Because the background tasks were detached from the primary execution thread, they outlived the lifecycle of the incoming HTTP network request. The ASP.NET Core runtime completed the web request and automatically called `.Dispose()` on the scoped dependency injection container pool. 

When the background task finally woke up to invoke the projection logic, its underlying database infrastructure connections had already been destroyed by the web server, triggering the `ObjectDisposedException`.

---

## 3. The Solution: Pragmatic Synchronous Refactoring

To guarantee absolute data consistency without introducing heavy message-queue synchronization overhead in Phase 2, the application's runtime dispatch engine was refactored from a dynamic multi-threaded structure to a **Sequential, Compile-Time Safe Blocking Pipeline**.

```text
[Stale Way]: [Withdrawal Event] ──► Task.Run (Background) ──┐ ──► (Race condition / Inversion)
             [Deposit Event]    ──► Synchronous Core    ────┘

[Fixed Way]: [Withdrawal Event] ──► Sync Read Update ──► Sync Saga Advance ──► [Deposit Loop Begins]
```

### 3.1 The Pattern-Matching Bridge
We removed all dynamic reflection logic (`method.Invoke`) and background tasks (`Task.Run`), replacing them with clean C# pattern matching inside the message broker:

```csharp
public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken) where TEvent : class
{
    switch (@event)
    {
        case MoneyWithdrawnEvent withdrawnEvent:
            await _projector.HandleAsync(withdrawnEvent, cancellationToken); // Line 1
            await _saga.HandleAsync(withdrawnEvent, cancellationToken);      // Line 2
            break;
        // ... subsequent cases
    }
}
```
The switch statement uses pure synchronous, sequential blocking. There is no Task.Run shifting threads to the background.

---

## 4. Architectural Justification & Post-Mortem Verification

### 4.1 Guaranteed Chronological Ordering
By executing the steps sequentially on a single blocking execution thread, C# forces your code to act like a row of falling dominoes. Line 1 is guaranteed to finish writing the withdrawal balance to Azure Cosmos DB before Line 2 can tell the Saga to kick off the deposit handler. 

### 4.2 Absolute Scope Isolation
Because the execution pipeline blocks the primary thread sequentially, all database connections and scoped repository instances are guaranteed to remain alive for the entire duration of the transaction loop. The `ObjectDisposedException` is entirely mitigated by design.

### 4.3 Type-Safe Compilation Guards
Moving from dynamic string-based class lookups to compile-time `case` statements allowed the compiler to enforce strict property boundaries. This completely blocked polymorphic variable pollution across concurrent multi-account transfer streams, ensuring that Alice's and Bob's identities remained fully isolated at the persistence barrier.
