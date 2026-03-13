
using HelpDesk.Application.Common;
using HelpDesk.Application.Tickets.Commands;
using HelpDesk.Infrastructure.Persistence;
using HelpDesk.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

var connectionString = "Server=localhost;User ID=root;Password=React@26;Database=HelpDeskDb";
services.AddDbContext<HelpDeskDbContext>(options =>
                                        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
services.AddScoped<ITicketRepository, TicketRepository>();
services.AddScoped<OpenTicketHandler>();
services.AddScoped<AssignAgentHandler>();
services.AddScoped<EscalateTicketHandler>();
services.AddScoped<ReassignTicketHandler>();
services.AddScoped<CloseTicketHandler>();
services.AddScoped<ReopenTicketHandler>();

var serviceProvider = services.BuildServiceProvider();

//// Creates a new Ticket
//var service = serviceProvider.GetRequiredService<OpenTicketHandler>();

//var cmd = new OpenTicketCommand(Guid.NewGuid(), Priority.Medium, "New Support Ticket");
//await service.Handle(cmd);

var ticketId = Guid.Parse("0f2e0005-e94c-4b54-a2a0-8d43514673d4");
var customerId = Guid.Parse("7b848c9e-7682-49f4-97c9-153c3dfc72fc");
//Assigns an agent to a Ticket
//var assignAgentHandlerService = serviceProvider.GetRequiredService<AssignAgentHandler>();
//var assignAgentCommand = new AssignAgentCommand(ticketId, Guid.NewGuid());
//await assignAgentHandlerService.Handle(assignAgentCommand);

//Escalate the ticket by Customer
var agentId = Guid.Parse("79abe70a-57dc-4550-9634-67f4613ec103");
//var escalateTicketHandlerService = serviceProvider.GetRequiredService<EscalateTicketHandler>();
//await escalateTicketHandlerService.Handle(new EscalateTicketCommand(ticketId, Guid.NewGuid()));

//reassign the ticket if not opened by Agent within 50% of the response time limit

//var reassignTicketHandlerService = serviceProvider.GetRequiredService<ReassignTicketHandler>();
//await reassignTicketHandlerService.Handle(new ReassignTicketCommand(ticketId, Guid.NewGuid()), TimeProvider.System);
//var newAgentId = Guid.Parse("a91537db-195e-4f02-ab13-37b252cf3ac4");

//Close Ticket
//var closeTicketHandlerService = serviceProvider.GetRequiredService<CloseTicketHandler>();
//await closeTicketHandlerService.Handle(new CloseTicketCommand(ticketId,customerId, 0));


//ReopenTicket
var reopenTicketHandlerService = serviceProvider.GetRequiredService<ReopenTicketHandler>();
await reopenTicketHandlerService.Handle(new ReopenTicketCommand(ticketId, customerId, "Dissatisfied With Service", 0), TimeProvider.System);