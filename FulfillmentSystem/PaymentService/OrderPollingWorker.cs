using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PaymentService.ACL;
using PaymentService.Application;
using PaymentService.Domain;
using PaymentService.Dtos;
using PaymentService.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PaymentService
{
    public class OrderPollingWorker : BackgroundService
    {
        private readonly ILogger<OrderPollingWorker> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly IPaymentProviderAcl _paymentProviderAcl;

        public OrderPollingWorker(ILogger<OrderPollingWorker> logger, IHttpClientFactory httpClientFactory, IServiceProvider serviceProvider, IPaymentProviderAcl paymentProviderAcl)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _paymentProviderAcl = paymentProviderAcl;
            _paymentProviderAcl = paymentProviderAcl;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    }

                    await PollOrderEvents(stoppingToken);
                    await Task.Delay(5000, stoppingToken);//wait 5 seconds before polling again
                }
            }
            catch (Exception ex)
            {
                // Log the actual error so you can see it in the console
                _logger.LogCritical(ex, "Payment Service crashed due to an unhandled exception.");

                // This keeps the console window open in Debug mode so you can read the log
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private async Task PollOrderEvents(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
            var client = _httpClientFactory.CreateClient("OrderService");
            Payment? payment;


            var response = await client.GetAsync("internal/outbox/unprocessed", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Unable to get the events due to network error. Retrying again in few seconds");
                return;
            }

            var events = await response.Content.ReadFromJsonAsync<List<OutboxDto>>(cancellationToken: ct) ?? new List<OutboxDto>();

            foreach (var ev in events)
            {
                var existingInbox = await dbContext.InboxMessages.FirstOrDefaultAsync(i => i.EventId == ev.Id, ct);
                if (existingInbox != null && (existingInbox.Status == "Data Error" || existingInbox.Status == "Completed")) continue;

                using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken: ct);

                try
                {
                    var externalEvent = JsonSerializer.Deserialize<OrderPlacedEvent>(ev.Content);

                    if (externalEvent == null)
                    {
                        await FinalizeMessageAsync(dbContext, transaction, client, ev.Id, "Data Error", ct);
                        continue;
                    }

                    payment = _paymentProviderAcl.MapToDomain(externalEvent);

                    var paymentRequestDto = new PaymentRequestDto(payment.OrderId,payment.Id, payment.Amount, payment.Currency);
                    var paymentOrchestrator = scope.ServiceProvider.GetRequiredService<PaymentOrchestrator>();
                    await paymentOrchestrator.ProcessPaymentAsync(paymentRequestDto);

                    var outboxMessage = dbContext.OutboxMessages.Local.FirstOrDefault(o => o.AggregateId == payment.Id) ??
                                        await dbContext.OutboxMessages
                                        .FirstOrDefaultAsync(o => o.AggregateId == payment.Id, ct);

                    if (outboxMessage == null)
                    {
                        _logger.LogError($"Outbox message not found for Payment ID: {payment.Id}");
                        await transaction.RollbackAsync(ct); // Null Outbox message is transient error, so rollback the trancation for now, retry again
                        continue;
                    }
                    if (externalEvent.ShippingAddress == null)
                    {
                        _logger.LogError($"Shipping Address was not found Order ID : {payment.OrderId}");
                        await FinalizeMessageAsync(dbContext, transaction, client, ev.Id, "Data Error", ct);
                        continue;
                    }

                    var shippingRequest = new ShippingRequestDto
                    {
                        OrderId = payment.OrderId,
                        PaymentId = payment.Id,
                        ShippingAddress = externalEvent.ShippingAddress,
                        EventId = outboxMessage.Id,
                        EventType = "Payment Completed"
                    };

                    var shippingClient = _httpClientFactory.CreateClient("ShippingCoordinatorService");

                    var shippingResponse = await shippingClient.PostAsJsonAsync("api/shipping/initiate", shippingRequest, ct);

                    if (shippingResponse.IsSuccessStatusCode)
                    {
                        var shippingResponseDto = await shippingResponse.Content.ReadFromJsonAsync<ShippingResponseDto>(ct);
                        if (shippingResponseDto == null)
                        {
                            _logger.LogError($"No shipping detail recieved for OrderId {payment.OrderId} Retrying in next poll.");
                            await transaction.RollbackAsync(ct);
                            continue;
                        }
                        _logger.LogInformation($"Shipping {shippingResponseDto.ShippingStatus} with Tracking Number " +
                            $"{shippingResponseDto.ShippingTrackingNumber}");
                    }
                    else if (shippingResponse.StatusCode == HttpStatusCode.BadRequest)
                    {
                        _logger.LogCritical("Permanent Error for Order {Id}", payment.OrderId);

                        // 1. Mark as failed in memory for permanent errors
                        await FinalizeMessageAsync(dbContext, transaction, client, ev.Id, "Data Error", ct);
                    }
                    else
                    {
                        var errorBody = await shippingResponse.Content.ReadAsStringAsync(ct);

                        if (errorBody.Contains("Already Processed", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Duplicate detected: Shipping already handled this event.");
                        }
                        else
                        {
                            _logger.LogError($"Shipping failed: {errorBody}");
                            await transaction.RollbackAsync(ct);
                            continue;
                        }
                    }

                    await FinalizeMessageAsync(dbContext, transaction, client, ev.Id, "Completed", ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected error for order {ev.OrderId}");
                    await transaction.RollbackAsync(ct);
                }
            }
        }
        private async Task FinalizeMessageAsync(PaymentDbContext dbContext,
                                                IDbContextTransaction transaction,
                                                HttpClient client,
                                                int eventId,
                                                string status,
                                                CancellationToken ct)
        {

            dbContext.InboxMessages.Add(new InboxMessage { EventId = eventId, ProcessedAt = DateTime.UtcNow, Status = status });
            await dbContext.SaveChangesAsync(ct);

            // ACKNOWLEDGE back to Order Service
            var ack = await client.PostAsync($"internal/outbox/{eventId}/ack", null, ct);

            if (ack.IsSuccessStatusCode)
            {
                await transaction.CommitAsync(ct);
            }
            else
            {
                // If Order Service didn't get the Ack, we must rollback 
                // so we can try the whole process again later.
                await transaction.RollbackAsync(ct);
            }
        }

    }
}
