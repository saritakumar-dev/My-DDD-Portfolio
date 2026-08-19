using Azure.Identity;
using BankLedger.Core.Common;
using BankLedger.Core.Common.Events;
using BankLedger.ReadModel.Projection.Common.Models;
using BankLedger.ReadModel.Projection.Handlers;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Configure the global Minimal API JSON serializer to automatically use camelCase
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

string connectionString = builder.Configuration.GetConnectionString("CosmosConnection")!;


builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    var options = new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Direct,
        SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase }
    };
    return new CosmosClient(connectionString, new DefaultAzureCredential(), options);
});

builder.Services.AddScoped<AccountBalanceProjector>();

// Explicitly link our generic domain event contracts directly to our projector class instance
builder.Services.AddScoped<IDomainEventHandler<AccountOpenedEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());
builder.Services.AddScoped<IDomainEventHandler<MoneyDepositedEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());
builder.Services.AddScoped<IDomainEventHandler<MoneyWithdrawnEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());
builder.Services.AddScoped<IDomainEventHandler<UserForgottenEvent>>(sp=>sp.GetRequiredService<AccountBalanceProjector>());
builder.Services.AddScoped<IDomainEventHandler<JournalEntryPostedEvent>>(sp => sp.GetRequiredService<AccountBalanceProjector>());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/balances/{accountId:guid}", async (
    Guid accountId,
    CosmosClient cosmosClient,
    CancellationToken cancellationToken) =>
{
    var container = cosmosClient.GetContainer("BankLedgerReadDb", "Balances");
    string id = accountId.ToString();

    try
    {
        var response = await container.ReadItemAsync<AccountBalanceDocument>(id, new PartitionKey(id), cancellationToken: cancellationToken);

        return Results.Ok(response.Resource);
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound($"Account snapshot for ID {accountId} was not found.");
    }

});

app.Run();

