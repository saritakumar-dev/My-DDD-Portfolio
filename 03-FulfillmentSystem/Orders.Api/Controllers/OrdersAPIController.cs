using Microsoft.AspNetCore.Mvc;
using Orders.Api.Domain;
using Orders.Api.Infrastructure;
using static Orders.Api.Dtos.OrderDtos;

namespace Orders.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class OrdersAPIController : ControllerBase
    {
        private readonly IOrderRepository _repository;
        private readonly ILogger<OrdersAPIController> _logger;
        public OrdersAPIController(IOrderRepository repository, ILogger<OrdersAPIController> logger)
        {
            _repository = repository;
            _logger = logger;
        }
        
        [HttpGet]
        public async Task<ActionResult<OrderResponse>> GetById(int id)
        {
            var order = await _repository.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            return Ok(new OrderResponse(order.Id, order.CustomerId, order.Status.ToString(), order.TotalAmount, order.OrderDate, order.ShippingAddress));
        }

        [HttpPost]
        public async Task<ActionResult<OrderResponse>> Post([FromBody] OrderCreateRequest request)
        {
            try
            {
                var order = new Order
                {
                    CustomerId = request.CustomerId,
                    BillingAddress = request.BillingAddress,
                    ShippingAddress = request.ShippingAddress,
                    Status = OrderStatus.Pending,
                    TotalAmount = request.TotalAmount,
                    OrderDate = DateTime.UtcNow
                };

                var message = new OutboxMessage
                {
                    Type = "Order Placed",
                    OccurredOnUtc = DateTime.UtcNow,
                };

                await _repository.SaveOrderAsync(order, message);

                _logger.LogInformation("Order {OrderId} created successfully with Outbox message {MessageId}",
                order.Id, message.Id);

                var response = new OrderResponse(order.Id, order.CustomerId, order.Status.ToString(), order.TotalAmount, order.OrderDate, order.ShippingAddress);

                return CreatedAtAction(nameof(GetById), new { id = order.Id }, response);
            }
            catch (Exception ex)
            {
                    _logger.LogError(ex, "Failed to create order for customer {CustomerId}", request.CustomerId);
                    return StatusCode(500, "An error occurred while processing your order.");
            }
        }

    }
}
