using Azure.Identity;
using BankLedger.Core.Common;
using BankLedger.Core.Common.Events;
using BankLedger.Core.Common.MessageBus;
using BankLedger.Domain.Common;
using BankLedger.ReadModel.Projection.Handlers;
using BankLedger.WriteProject.API.Middleware;
using BankLedger.WriteProject.Application;
using BankLedger.WriteProject.Application.Common;
using BankLedger.WriteProject.Application.Common.Models;
using BankLedger.WriteProject.Application.Handlers;
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

builder.Services.AddScoped<ICryptoKeyVault>(serviceProvider => new MySqlCryptoKeyVault(connectionString));
builder.Services.AddScoped<IEventStore>(serviceProvider =>
{
    var keyVault = serviceProvider.GetRequiredService<ICryptoKeyVault>();
    var logger = serviceProvider.GetRequiredService<ILogger<MySqlEventStore>>();
    return new MySqlEventStore(connectionString, keyVault, logger);
});
builder.Services.AddScoped<ISagaStateRepository, SagaStateRepository>();
builder.Services.AddScoped<ICommandHandler<OpenAccountCommand>, OpenAccountCommandHandler>();
builder.Services.AddScoped<ICommandHandler<WithdrawMoneyCommand>, WithdrawMoneyCommandHandler>();
builder.Services.AddScoped<ICommandHandler<DepositMoneyCommand>, DepositMoneyCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ForgetUserCommand>, ForgetUserCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PostJournalEntryCommand>, PostJournalEntryCommandHandler>();

builder.Services.AddScoped<IMessageBus, InMemoryMessageBus>();
builder.Services.AddScoped<MoneyTransferSaga>();
builder.Services.AddScoped<IDomainEventHandler<MoneyWithdrawnEvent>>(sp => sp.GetRequiredService<MoneyTransferSaga>());
builder.Services.AddScoped<IDomainEventHandler<MoneyDepositedEvent>>(sp => sp.GetRequiredService<MoneyTransferSaga>());
builder.Services.AddScoped<IDomainEventHandler<DepositMoneyFailedEvent>>(sp => sp.GetRequiredService<MoneyTransferSaga>());
builder.Services.AddScoped<ISnapshotStore>(serviceProvider => new MySqlSnapshotStore(connectionString));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Adding the project to single-project CQRS testing loop, later it will be replaced by Azure Service Bus
string cosmosConnectionString = builder.Configuration.GetConnectionString("CosmosConnection")!;
builder.Services.AddSingleton(sp => new CosmosClient(cosmosConnectionString, new DefaultAzureCredential()));
builder.Services.AddScoped<AccountBalanceProjector>();
builder.Services.AddScoped<IDomainEventHandler<AccountOpenedEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());
builder.Services.AddScoped<IDomainEventHandler<MoneyDepositedEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());
builder.Services.AddScoped<IDomainEventHandler<MoneyWithdrawnEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());
builder.Services.AddScoped<IDomainEventHandler<JournalEntryPostedEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());
builder.Services.AddScoped<IAccountReadModelService, CosmosAccountReadModelService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// ==========================================
// 2. MIDDLEWARE PIPELINE SETUP (app.Use...)
// ==========================================
// CRITICAL FIX: These MUST come immediately after builder.Build() 
// and BEFORE any endpoints are mapped.

app.UseExceptionHandler();

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

    await handler.HandleAsync(new DepositMoneyCommand(request.AccountId, request.Amount, request.Currency, request.Reference), cancellationToken);

    return Results.Accepted();  //TODO : what should be standard return values here
})
.WithName("DepositMoney")
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status400BadRequest);

app.MapPost("/api/deleteaccount", async ([FromBody] DeleteAccountRequest request,
                                            [FromServices] ICommandHandler<ForgetUserCommand> handler,
                                            CancellationToken cancellationToken) =>
{
    if (Guid.Empty == request.AccountId)
        return Results.BadRequest("Customer name is required.");

    await handler.HandleAsync(new ForgetUserCommand(request.AccountId, request.ClosureReason), cancellationToken);

    return Results.Accepted(); //TODO : what should be standard return values here
});

app.MapPost("api/jounalentrytransfer", async (MoneyTransferRequest request,
                                              MoneyTransferSaga saga,
                                              CancellationToken cancellationToken) =>
{
    if (request == null || request.Instructions == null || request.Instructions.Count < 2)
        return Results.BadRequest("Invalid Inputs");
    var moneyTransferInstructions = request.Instructions.Select(i => new MoneyTransferInstruction(i.AccountId, i.Amount, i.Description)).ToList();

    await saga.StartAsync(moneyTransferInstructions, cancellationToken);

    return Results.Accepted();
})
.WithName("jounalentrytransfer")
.WithOpenApi()
.Produces(StatusCodes.Status202Accepted) // Explicitly state the responses for Swashbuckle
.Produces(StatusCodes.Status400BadRequest);


app.Run();


public record OpenAccountRequest(string CustomerName, string Currency);

public record DepositMoneyRequest(Guid AccountId, decimal Amount, string Currency, string Reference);

public record TransferRequest(Guid SourceAccountId, Guid TargetAccountId, decimal Amount, string Currency);

public record DeleteAccountRequest(Guid AccountId, ClosureReason ClosureReason);

public record MoneyTransferRequest(List<Instruction> Instructions, string Currency);

public record Instruction(Guid AccountId, decimal Amount, string Description);