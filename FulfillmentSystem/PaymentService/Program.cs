using Microsoft.EntityFrameworkCore;
using PaymentService;
using PaymentService.ACL;
using PaymentService.Application;
using PaymentService.Domain;
using PaymentService.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<OrderPollingWorker>();

var connectionString = builder.Configuration.GetConnectionString("PaymentsDbConnection");
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString) // Automatically detects if you're using MySQL 8.0, 5.7, MariaDB, etc.
    ));

builder.Services.AddHttpClient("OrderService", client => {
    client.BaseAddress = new Uri("http://localhost:5123/");
    client.Timeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddHttpClient("ShippingCoordinatorService", client => {
    client.BaseAddress = new Uri("http://localhost:5131/");
    client.Timeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddSingleton<IPaymentProviderAcl, PaymentProviderAcl>();
builder.Services.AddScoped<PaymentOrchestrator>();
var host = builder.Build();
host.Run();
