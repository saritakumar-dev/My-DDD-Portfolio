using Azure.Identity;
using BankLedger.Core.Common;
using BankLedger.Core.Common.Commands;
using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.ReadModel.Projection.Handlers;
using BankLedger.WriteProject.Application.Commands;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Application.Sagas;
using BankLedger.WriteProject.Infrastructure.Database;
using BankLedger.WriteProject.Infrastructure.Messaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. SERVICES CONFIGURATION (builder.Services)
// ==========================================

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<BankWriteDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        b => b.MigrationsAssembly("BankLedger.Infrastructure")));

builder.Services.AddScoped<IEventStore>(serviceProvider => new MySQLEventStore(connectionString));
builder.Services.AddScoped<ISagaStateRepository, SagaStateRepository>();
builder.Services.AddScoped<ICommandHandler<OpenAccountCommand>, OpenAccountCommandHandler>();
builder.Services.AddScoped<ICommandHandler<WithdrawMoneyCommand>, WithdrawMoneyCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DepositMoneyCommand>, DepositMoneyCommandHandler>();
builder.Services.AddScoped<IMessageBus, InMemoryMessageBus>();
builder.Services.AddScoped<MoneyTransferSaga>();
builder.Services.AddScoped<IDomainEventHandler<MoneyWithdrawnEvent>>(sp => sp.GetRequiredService<MoneyTransferSaga>());
builder.Services.AddScoped<IDomainEventHandler<MoneyDepositedEvent>>(sp => sp.GetRequiredService<MoneyTransferSaga>());
builder.Services.AddScoped<IDomainEventHandler<DepositMoneyFailedEvent>>(sp => sp.GetRequiredService<MoneyTransferSaga>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Adding the project to single-project CQRS testing loop, later it will be replaced by Azure Service Bus
string cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosConnection")!;
builder.Services.AddSingleton(sp => new CosmosClient(cosmosConnectionString, new DefaultAzureCredential()));
builder.Services.AddScoped<AccountBalanceProjector>();
builder.Services.AddScoped<IDomainEventHandler<AccountOpenedEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());
builder.Services.AddScoped<IDomainEventHandler<MoneyDepositedEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());
builder.Services.AddScoped<IDomainEventHandler<MoneyWithdrawnEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());

var app = builder.Build();

// ==========================================
// 2. MIDDLEWARE PIPELINE SETUP (app.Use...)
// ==========================================
// CRITICAL FIX: These MUST come immediately after builder.Build() 
// and BEFORE any endpoints are mapped.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank Ledger Write API v1");
    });
}


// ==========================================
// 3. ENDPOINT ROUTING (app.Map...)
// ==========================================

app.MapPost("/api/accounts", async (
                            [FromBody] OpenAccountRequest request,
                            [FromServices] ICommandHandler<OpenAccountCommand> handler,
                            CancellationToken cancellationToken) =>

{
    if (string.IsNullOrWhiteSpace(request.CustomerName))
        return Results.BadRequest("Customer name is required.");

    if (string.IsNullOrWhiteSpace(request.Currency))
        return Results.BadRequest("Currency specification is required.");

    var accountId = Guid.NewGuid();

    var command = new OpenAccountCommand(accountId, request.CustomerName, request.Currency);
    await handler.HandleAsync(command, cancellationToken);
    return Results.Created($"/api/accounts/{accountId}", new { AccountId = accountId });

})
.WithName("OpenAccount")
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

app.MapPost("/api/deposit", async (
                                    [FromBody] DepositMoneyRequest request,
                                    [FromServices] ICommandHandler<DepositMoneyCommand> handler,
                                     CancellationToken cancellationToken) =>
{
    if (Guid.Empty == request.AccountId)
        return Results.BadRequest("Customer name is required.");

    if (request.Amount <= 0)
        return Results.BadRequest("Amount can't be zero");

    await handler.HandleAsync(new DepositMoneyCommand(request.AccountId, request.Amount, request.Reference), cancellationToken);

    return Results.Accepted();
})
.WithName("DepositMoney")
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status400BadRequest);


app.MapPost("/api/transfer", async (
    TransferRequest request,
    MoneyTransferSaga saga,
    CancellationToken cancellationToken) =>
{
    if (request.SourceAccountId == request.TargetAccountId)
    {
        return Results.BadRequest("Source and Target accounts cannot be the same.");
    }

    if (request.Amount <= 0)
    {
        return Results.BadRequest("Transfer amount must be greater than zero.");
    }

    await saga.StartAsync(
        request.SourceAccountId,
        request.TargetAccountId,
        request.Amount,
        cancellationToken
    );

    return Results.Accepted();
})
.WithName("InitiateTransfer")
.WithOpenApi()
.Produces(StatusCodes.Status202Accepted) // Explicitly state the responses for Swashbuckle
.Produces(StatusCodes.Status400BadRequest);


app.Run();


public record OpenAccountRequest(string CustomerName, string Currency);

public record DepositMoneyRequest(Guid AccountId, decimal Amount, string Reference);

public record TransferRequest(Guid SourceAccountId, Guid TargetAccountId, decimal Amount);
