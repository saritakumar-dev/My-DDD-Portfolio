using ShippingCoordinatorService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ShipmentServiceWorker>();

var host = builder.Build();
host.Run();
