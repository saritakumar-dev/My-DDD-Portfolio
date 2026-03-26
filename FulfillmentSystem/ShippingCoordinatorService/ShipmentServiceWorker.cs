namespace ShippingCoordinatorService
{
    public class ShipmentServiceWorker : BackgroundService
    {
        private readonly ILogger<ShipmentServiceWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ShipmentServiceWorker(ILogger<ShipmentServiceWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
