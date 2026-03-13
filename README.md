# Domain-Driven Design (DDD) Learning Portfolio

A collection of projects exploring tactical and strategic DDD patterns, clean architecture, and advanced persistence strategies in .NET.

---

## 📂 Project Catalog

### 1. [Event-Sourced-Domain-Model Credit Account](./01-CreditAccount-ES/)
**Focus:** Event Sourcing & Tactical DDD
- **Concepts:** Aggregate Roots, Immutable Domain Events, State Rehydration.
- **Key Feature:** Implements a credit account where every transaction is a "fact" in a ledger.
- **Tech:** .NET 8, xUnit, Moq, In-Memory Event Store.

### 2. [Tactical-Domain-Driven-Design HelpDesk Ticket Management](./02-HelpDeskTicketSystem)
**Focus:** Solving Complex Domain Logic using Domain Model Pattern
- **Concepts:** Aggregate Roots, Value Objects, Consistency Boundary, Immutable Domain Events, Value Convereters
- **Key Feature:** Implements a complete lifecycle of a ticket in a Helpdesk System including features like Escalations and SLAs calculations.
- **Tech:** .NET 8, Domain Pattern, EF Core, Code-First, MySQL, xUnit, MOQ, Repository Pattern  

### 2. [Warehouse Management](./02-Warehouse-Hexagonal/) (Upcoming)
**Focus:** Hexagonal Architecture (Ports & Adapters)
- **Concepts:** Isolation of Core Logic from external Infrastructure (Adapters).
- **Tech:** Entity Framework Core, SQL Server.

---

## 🛠️ Global Setup
Each project is self-contained. To run a specific project:
1. Navigate to the project folder: `cd EventSourcingDomainModel`
2. Restore dependencies: `dotnet restore`
3. Run tests: `dotnet test`

## 🧠 Learning Goals
The goal of this repository is to master the transition from CRUD-based thinking to **Behavior-Driven** and **Event-Centric** systems, following the principles outlined by **Vlad Khononov** and **Eric Evans**.
