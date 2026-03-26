using Microsoft.EntityFrameworkCore;
using ShippingCoordinator.Api.Domain;
using ShippingCoordinator.Api.Infrastructure;
using ShippingCoordinator.Api.Infrastructure.Persistence;
using ShippingCoordinator.Api.Infrastructure.Workers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("ShippingCoordinatorDbConnection");

builder.Services.AddDbContext<ShippingCoordinatorDBContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString) // Automatically detects if you're using MySQL 8.0, 5.7, MariaDB, etc.
    ));

//builder.Services.AddHostedService<ShipmentWorker>();

builder.Services.AddScoped<IShippingRespository, ShippingRespository>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // This generates the actual UI page
}

app.Run();
