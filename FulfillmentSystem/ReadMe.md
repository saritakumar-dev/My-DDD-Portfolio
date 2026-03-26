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
