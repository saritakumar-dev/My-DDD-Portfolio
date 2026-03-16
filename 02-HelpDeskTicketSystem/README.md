
# 🎫 Help Desk System - DDD Tactical Design

A Domain-Driven Design (DDD) implementation of a Help Desk system based on **Chapter 6 of Vlad Khononov's "Learning Domain-Driven Design"**. This project demonstrates the transition from a "Database-First" mindset to a "Domain-First" architecture using C# and EF Core with MySQL.

## 🧠 Core DDD Concepts Implemented

### 1. The Aggregate Root (`Ticket`)
The `Ticket` class acts as the **Consistency Boundary**. 
*   **Encapsulation:** Internal state (like `_messages`) is private. All changes must go through public methods (e.g., `AddMessage`, `Escalate`).
*   **Invariants:** Business rules are enforced internally. For example, a ticket cannot be reopened if it was closed more than 7 days ago.

### 2. Value Objects (Logic over Data)
We used C# `records` for immutable Value Objects:
*   **`SlaLimit`**: Contains the mathematical logic for the 33% response time reduction.
*   **`TicketId` / `UserId`**: Ensures type safety, preventing a User ID from being accidentally used as a Ticket ID.
*   **`Priority` / `Status`**: Encapsulates state-specific logic.

### 3. Entity Framework Core "Shadow Properties"
To keep the Domain model pure, we used a **Surrogate Key** strategy:
*   The database uses an `int Id` (Auto-increment) for performance.
*   The Domain uses `TicketId` (Guid) for business logic.
*   The `int Id` is a **Shadow Property**—it exists in the database but not in the C# `Ticket` class.

### 4. Time Abstraction (`TimeProvider`)
To test time-dependent rules (like the 50% reassignment rule), we injected `.NET 8's TimeProvider`.
*   **In Production:** Uses the real system clock.
*   **In Tests:** Uses a `FakeTimeProvider` to "fast-forward" time and verify deadlines in milliseconds.

### 5. Value Converters (The Mapping Bridge)
Since MySQL does not natively understand C# `records` like `TicketId` or `Priority`, we implemented **Value Converters** in the Infrastructure layer.
*   **Deconstruction:** When saving, the converter extracts the inner `Guid` or `string` from the Value Object.
*   **Rehydration:** When loading from MySQL, the converter automatically "rebuilds" the rich Value Object from the raw database value.
*   **Benefit:** This allows the Domain to use strongly-typed objects (preventing "Primitive Obsession") while keeping the Database schema simple and standard.


---

## 🛠️ Technical Stack & Patterns

- **Language:** C# 12 / .NET 8
- **ORM:** Entity Framework Core
- **Database:** MySQL (Pomelo Provider)
- **Architecture:** Clean Architecture (Domain, Application, Infrastructure, Presentation)
- **Patterns:** 
    - **Repository Pattern:** Load/Save aggregates as a single unit.
    - **Unit of Work:** Coordinated via `SaveChangesAsync`.
    - **Domain Events:** `TicketOpened`, `MessageAdded`, `TicketEscalated`.

---

## 📈 Key Learnings & Revision Notes

### Business Rules (Invariants)
1.  **Escalation Rule:** Escalation reduces the remaining response time limit by 33%.
2.  **Access Control:** Support agents cannot close escalated tickets; only Customers or Managers can.
3.  **Auto-Reassign:** If an escalated ticket isn't "Opened" by an agent within 50% of the SLA, it triggers a reassignment.
4.  **Reopen Rule:** A customer can only reopen a ticket within 7 days of closing.

### Broken Assumptions
*   **Navigation Properties:** We stopped using `public Customer Customer { get; }` and switched to **Reference by ID** (`UserId CustomerId`) to keep aggregate boundaries small.
*   **Anemic Models:** We moved logic from "Services" into the "Aggregate Root."
*   **Update Method:** We learned that EF Core's **Change Tracking** removes the need for an explicit `Update()` method in the Repository.

---

## 📂 Project Structure & Clean Architecture

The solution is divided into four distinct layers to ensure the **Domain** remains independent of technical details (like MySQL or Console logging).

### 1. HelpDesk.Domain (The "Brain")
Contains the core business logic. It has **zero dependencies** on EF Core or any external frameworks.
*   **`/Aggregates`**: The `Ticket` Aggregate Root.
*   **`/ValueObjects`**: Immutable records like `TicketId`, `SlaLimit`, and `Priority`.
*   **`/Events`**: Domain Events (`TicketOpened`, `MessageAdded`) that signal state changes.
*   **`/Common`**: Shared interfaces like `IDateTimeProvider` or `TimeProvider`.

### 2. HelpDesk.Application (The "Orchestrator")
Coordinates the flow of data between the UI and the Domain.
*   **`/Commands`**: Data transfer objects (DTOs) representing user intent (e.g., `OpenTicketCommand`).
*   **`/Handlers`**: The logic that loads an aggregate, calls its methods, and saves it.
*   **`/Interfaces`**: Defines the "Contract" for the repository (`ITicketRepository`).

### 3. HelpDesk.Infrastructure (The "Hands")
Handles technical implementation and persistence.
*   **`/Persistence`**: The `HelpDeskDbContext` and the `IDesignTimeDbContextFactory`.
*   **`/Configurations`**: Fluent API mappings where **Value Converters** and **Shadow Properties** are defined.
*   **`/Repositories`**: The concrete implementation of `ITicketRepository` using EF Core.

### 4. HelpDesk.Presentation.Console (The "Voice")
The entry point of the application.
*   **`Program.cs`**: Handles Dependency Injection (DI) and simulates the user journey (Open -> Assign -> Escalate).

---

## 🏛️ Architectural Patterns & Principles

### 1. Clean Architecture (Ports & Adapters)
The project follows the **Ports and Adapters** pattern to decouple business logic from external technologies.
*   **The Port:** `ITicketRepository` (defined in the Application layer).
*   **The Adapter:** `TicketRepository` (defined in the Infrastructure layer).
*   **Benefit:** We can swap MySQL for any other database (or a Mock for testing) without changing a single line of business logic.

### 2. Dependency Inversion Principle (DIP)
We applied the **D' in SOLID**. High-level modules (Application) do not depend on low-level modules (Infrastructure). Both depend on abstractions (`ITicketRepository`). This ensures the **Domain** remains the center of the universe.

### 3. Synchronous vs. Asynchronous Execution
The project uses a hybrid approach to maximize performance and maintainability:
*   **Synchronous Domain Logic:** All methods within the `Ticket` aggregate (e.g., `Escalate`, `AssignAgent`) are **Synchronous**. This is because they perform "In-Memory" state transitions and logic calculations that do not involve I/O.
*   **Asynchronous Infrastructure/Application:** All operations involving the database or external services (e.g., `GetTicketAsync`, `SaveChangesAsync`) are **Asynchronous**. This ensures the application remains scalable and does not block threads during I/O-bound tasks.
    **Why:** This prevents thread-blocking during database communication, allowing the system to handle more concurrent requests efficiently.
    **Implementation:** Every repository method returns a `Task` and is properly `awaited` in the Command Handlers.

---

## 🚀 How to Run
1. Update the connection string in `HelpDeskDbContextFactory`.
2. Run migrations: 
   `dotnet ef database update --project HelpDesk.Infrastructure --startup-project HelpDesk.Presentation.Console`
3. Run the Console application to simulate the Ticket lifecycle.

---
*Developed as a deep-dive into Tactical DDD patterns.*
