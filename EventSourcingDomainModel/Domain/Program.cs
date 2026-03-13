/* 
 * 📌 ARCHITECTURAL STICKY NOTE:
 * 1. Events are immutable facts (Records).
 * 2. The Aggregate (Brain) decides if a change is allowed, then records it.
 * 3. The Service (Coordinator) "Time Travels" by replaying history 
 *    before asking the Brain to make a new decision.
 */


using EventSourcingDomainModelApp.Application;
using EventSourcingDomainModelApp.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<IEventStore, InMemoryEventStore>();
services.AddTransient<AccountService>();

var serviceProvider  = services.BuildServiceProvider();

var accountService = serviceProvider.GetRequiredService<AccountService>();

var accountId = Guid.NewGuid();
await accountService.HandleAccountOpen(accountId, "SK", 10000.00M);

Console.WriteLine("Account created successfully!");

await accountService.HandleAmountDeposit(accountId, 20000.00M);

await accountService.HandleWithdrawal(accountId, 5000.00M);


Console.WriteLine($"-------------------------------");
Console.WriteLine($"Account ID: {accountId}");
Console.WriteLine($"Current Balance: {await accountService.HandleDisplayBalance(accountId):C}");
Console.WriteLine($"-------------------------------");

Console.WriteLine($"The account {accountId} is {await accountService.HandleCloseAccount(accountId)} now");