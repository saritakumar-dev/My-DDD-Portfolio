Title:  Distributed-Order-Fulfilment-System
1.Project Overview
Inspired by Chapter 9 of Vlad Khononov's Learning Domain-Driven Design, this project implements a microservices-based fulfilment system focused on distributed consistency and domain boundary protection.
Unlike typical e-commerce tutorials that rely on external message brokers (RabbitMQ/Kafka), this project demonstrates how to achieve Reliable Messaging without a Message Bus using standard RDBMS patterns and REST.
Core Services & Architecture
•	OrderService (Producer): Acts as the Open Host Service (OHS) and source of truth.
•	PaymentService (Orchestrator): The "brain" that polls for events and drives the lifecycle.
•	ShippingCoordinator (Consumer): A consumer, reacting to payment confirmations and ensuring the local shipment state is consistent before acknowledging the coordinator.
Key Learning Objectives
•	Reliable Data Exchange: Solving the "Dual-Write" problem via Outbox/Inbox patterns.
•	Boundary Protection: Using Anti-Corruption Layers (ACL) to prevent "model bleed."
•	Orchestration: Managing the order lifecycle via a local coordinator rather than pure choreography.
•	Strategic DDD: Implementing stable APIs as a Published Language.
The Fulfilment Flow
1.	Order Service: Persists an Order and an OutboxMessage in one atomic transaction.
2.	Payment Service: Polls the outbox, translates data via an ACL, charges the customer, and pushes to Shipping.
3.	Shipping Coordinator Service: Records the intent in an Inbox for idempotency and acknowledges the request.
4.	Completion: Payment Service acknowledges the event back to Order Service and commits the local transaction.

2. Distributed Data Patterns (The "Star" of the project)
•	Transactional Outbox Pattern
Solves the "Dual-Write" Problem: Prevents the scenario where a database update succeeds but the corresponding message fails to publish. By saving events to a MySQL OutboxMessages table within the same transaction as the Order entity, we ensure "at-least-once" delivery and zero data loss.
•	Inbox Pattern (Idempotency) Pattern
Solves the "Duplicate Processing" Problem: Ensures that if a message is redelivered due to a network glitch, the system doesn't process it twice. The PaymentService maintains an InboxMessages table to track processed Event IDs, guaranteeing a customer is never charged double.
•	API Polling (The Relay)
Solves the "Service Coupling" Problem: Provides a reliable way to move data between services without requiring a complex Message Broker (like RabbitMQ) for smaller implementations. We use a custom .NET BackgroundService (Worker) that pulls unprocessed events via an internal REST API at regular intervals. 
•	Unit of Work & Shared Transactions
Solves the "Partial Failure" Problem: Prevents data inconsistency where one part of a process succeeds but another fails. This demonstrates how to wrap multiple operations—Payment + Outbox + Inbox—into a single atomic SQL transaction for "All or Nothing" reliability.
3. Tactical DDD Patterns
•	Anti-Corruption Layer (ACL): A dedicated layer in the Payment Service that translates external OrderPlaced events into internal Payment domain models, keeping the core logic "pure.
•	Stateless Translation: Logic within the ACL to map external currency codes to internal gateway-compatible formats (e.g., "RS" to "INR").
•	Aggregate Roots: Clear boundaries between Order, Payment and Shipping entities, each with its own database and lifecycle.
•	Enums for State Management: Using different Status Enums (with EF Core string conversion) instead of magic strings.
4. Technical Implementation Details
•	Tech Stack: .NET 8 / C#, Entity Framework Core, MySQL.
•	Background/Hosted Services (Infrastructure Layer)
•	Resiliency: Use of IHttpClientFactory to prevent socket exhaustion and handle DNS updates.
•	Scoped Services in Singletons: Demonstration of manual IServiceScope creation within a BackgroundService to resolve DbContext safely.
•	Atomic Transactions: Using BeginTransactionAsync to wrap business logic and Outbox/Inbox updates together.
5. Architectural Patterns Practiced
 Service A: Order Service (The Upstream)
•	Source of Truth: Responsible for capturing customer intent and serving as the primary source of truth for orders.

•	Outbox Pattern: Ensures transactional consistency by saving the Order entity and an `OutboxMessage` in a single atomic transaction. This prevents the "dual-write" problem where a database update succeeds but message publication fails.

•	Open Host Service (OHS): Exposes a stable API using DTOs as a Published Language. This decouples the public API from the internal database schema, allowing for internal refactors without breaking downstream consumers.
Service B: Payment Service (The Main Coordinator)
•	Local Orchestrator: Acts as the "Watchdog" and driver for the middle of the order lifecycle. It polls the Order Service for unprocessed events and drives the transition to Shipping.

•	Anti-Corruption Layer (ACL): Uses a translation layer to map incoming OrderPlaced events from Service A into internal domain objects. This protects the Payment domain from external schema changes.

•	Transactional Inbox Pattern: Maintains an InboxMessages table to ensure idempotency. It tracks processed Event IDs with statuses like “Completed” or “Data Error” to prevent duplicate charges or infinite retry loops.

•	The Push-Ack Chain
This is a synchronous hand-off with asynchronous reliability.
1.	The "Push": The Payment Service (Orchestrator) actively pushes a request to the Shipping Service via a REST call.
2.	The "Ack" (Acknowledgment): The Payment Service only marks its own task as "Complete" and acknowledges the upstream Order Service after it receives a successful response from Shipping.
3.	Why it matters: It ensures that no step in the chain is "forgotten." If Shipping is down, the Payment Service never sends the final Ack, causing the background worker to retry the entire step until the hand-off is confirmed.





Service C: Shipping Coordinator (The Consumer)
•	Passive Fulfilment: Acts as a downstream worker that listens for validated "intents to ship" from the Payment Service.

•	Inbox Pattern: Implements its own idempotency check using an InboxMessages table to prevent duplicate shipping requests.

•	Background Fulfilment (Future Work): Uses an Infrastructure-level Hosted Service (Worker) to poll for Pending shipping records, simulate logistics (labelling/tracking), and transition the status to ReadyForDispatch.

6. Resiliency & Error Handling Strategy
In a distributed system, not all errors are equal. Our implementation distinguishes between Transient and Permanent failures to ensure the system is self-healing without becoming stuck.
1. Transient Failures (Temporary Glitches)
Examples: Network timeouts, Service B is restarting (503 Service Unavailable), or Database deadlocks.
The Strategy: Rollback & retry.
Implementation:
The Worker catches the exception or detects the timeout.
It calls await transaction.RollbackAsync(). This "undoes" any local database changes (like the Payment record).
The Worker uses continue to move to the next item in the batch.
Because the InboxMessage was never committed, the next polling cycle (10 seconds later) will pick up the same event and try again.
2. Permanent Failures (Poison Messages)
Examples: Corrupt JSON data, missing required fields (e.g., No Shipping Address), or a 400 Bad Request from a downstream API.
The Strategy: Dead-Lettering (Acknowledge & Archive).
Implementation:
The Worker identifies that retrying will not fix the issue.
It records an InboxMessage with a specific status: Status = "Data Error".
Crucial Step: It sends an Acknowledgement (Ack) back to the Upstream Service (Order Service) to "silence" the event.
It calls await transaction.CommitAsync().
Failure Type	Example	Strategy	Action
Transient	Network Timeout	Rollback & Retry	transaction.RollbackAsync()
Permanent	Data Corruption	Dead-Letter (Ack)	transaction.CommitAsync()

7. Project Structure
The solution follows a clean, folder-based organization within each service to separate technical plumbing from business logic: 
Domain/: Core entities (e.g. Order, OrderStatus) and business abstractions.
Data/: Persistence layer including AppDbContext, EF Core Migrations, and the OutboxMessage infrastructure entity.
Dtos/: Public-facing contracts (Requests/Responses) used for the OHS pattern.
In modern .NET (C# 9+), records are preferred for DTOs because they are immutable by default (DTOs should be immutable. This prevents data from being accidentally changed after the API has received or prepared it. Dtos are the perfect place to add attributes (like [Required] or [MinLength]) for automated input validation.
Interfaces/: Repository abstractions (IOrderRepository) to facilitate testing and decoupling.
In Payment Service, The Application Layer contains the PaymentOrchestrator, which manages the business transaction. It ensures the atomicity of the Payment and Outbox records, 
8. Getting Started & Running the System
To see the distributed flow in action, follow these steps to set up your local environment.
Prerequisites
•	.NET 8 SDK installed.
•	MySQL Server (Ensure it is running and you have permissions to create databases).
Step 1: Configuration
Update the ConnectionStrings in the appsettings.json file for each of the three services:
•	OrderService/appsettings.json
•	PaymentService/appsettings.json
•	ShippingService/appsettings.json

Step 2: Database Initialization
Each service maintains its own Bounded Context (boundary) and database schema. Open your terminal in each project folder and run:
1.	Run Migrations: Open your terminal in each of the following project folders and execute the update command:
dotnet ef database update 
Note: This command will automatically create the local MySQL databases (if they don't exist) and apply the schema history based on the included migration files.
2.	Verify Tables: Using a database tool (like MySQL Workbench), verify that each database contains its respective business tables along with the infrastructure tables:
	Order DB: Look for orders and outboxmessages.
	Payment DB: Look for payments and inboxmessages.
	Shipping DB: Look for shipments and inboxmessages

3.	Troubleshooting: If the command fails, ensure your MySQL server is running and that the credentials in each appsettings.json match your local environment.

Step 3: Launching the Services
Because the Payment Service polls the Order Service, they must be running simultaneously.
•	In Visual Studio: Right-click the Solution > Configure Startup Projects... > Select Multiple startup projects and set OrderService, PaymentService, and ShippingService to Start.
•	In VS Code / Terminal: Open three separate terminal tabs and run dotnet run in each project directory.
Step 4: Triggering the Flow
Use a tool like Postman or Swagger to send a POST request to the OrderService. This kicks off the Outbox/Inbox/Polling chain:
•	Endpoint: POST http://localhost:[ORDER_PORT]/api/orders

Step 5: Verification
Watch the console logs of the Payment Service to see the BackgroundService pick up the event, translate it through the ACL, and push it to the Shipping Service. Check your MySQL tables (orders, inboxmessages, outboxmessages, payments, shippingdetails) to see the state changes in real-time.
