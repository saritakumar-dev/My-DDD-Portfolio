
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

## 🚀 How to Run
1. Update the connection string in `HelpDeskDbContextFactory`.
2. Run migrations: 
   `dotnet ef database update --project HelpDesk.Infrastructure --startup-project HelpDesk.Presentation.Console`
3. Run the Console application to simulate the Ticket lifecycle.

---
*Developed as a deep-dive into Tactical DDD patterns.*
