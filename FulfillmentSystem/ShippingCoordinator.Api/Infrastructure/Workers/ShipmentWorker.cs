
using System.Net.Http;

namespace ShippingCoordinator.Api.Infrastructure.Workers
{
    //public class ShipmentWorker : BackgroundService
    //{
    //    private readonly ILogger<ShipmentWorker> _logger;
    //    private readonly IHttpClientFactory _httpClientFactory;
    //    private readonly IServiceProvider _serviceProvider;


    //    public ShipmentWorker(ILogger<ShipmentWorker> logger, IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider)
    //    {
    //        _logger = logger;
    //        _httpClientFactory=httpClientFactory;
    //        _serviceProvider = serviceProvider;
    //    }
    //    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    //    {
    //        //while (!stoppingToken.IsCancellationRequested)
    //        //{
    //        //    if (_logger.IsEnabled(LogLevel.Information))
    //        //    {
    //        //        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
    //        //    }
               
    //        //    await Task.Delay(1000, stoppingToken);
    //        //}
    //    }
    //}
}
