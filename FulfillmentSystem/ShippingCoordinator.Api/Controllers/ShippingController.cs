using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShippingCoordinator.Api.Domain;
using ShippingCoordinator.Api.Domains;
using ShippingCoordinator.Api.Dtos;
using ShippingCoordinator.Api.Infrastructure.Model;

namespace ShippingCoordinator.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly IShippingRespository _repository;
        private readonly ILogger<ShippingController> _logger;

        public ShippingController(IShippingRespository respository, ILogger<ShippingController> logger)
        {
            _repository = respository;
            _logger = logger;
        }

        [HttpPost("initiate")]
        public async Task<ActionResult<ShippingResponseDto>> Post([FromBody] ShippingRequestDto requestDto)
        {
            try
            {
                if (await _repository.IsMessageProcessedAsync(requestDto.EventId)) return Ok(new ShippingResponseDto  { ShippingStatus = "Already processed" });
                var shippingDetail = new ShippingDetail
                {
                    OrderId = requestDto.OrderId,
                    PaymentId = requestDto.PaymentId,
                    TrackingNumber = Guid.NewGuid(),
                    ShippingAddress = requestDto.ShippingAddress
                };

                var inboxMessage = new InboxMessage
                {
                    EventId = requestDto.EventId,
                    EventType = requestDto.EventType,
                    RecivedAtUtc = DateTime.UtcNow,
                    ProcessedAtUtc=null// Only be filled once the shipping processed.
                };

                await _repository.SaveShippingDetailAsync(shippingDetail, inboxMessage);
                var responseDto = new ShippingResponseDto {IsSuccessStatusCode=true, ShippingTrackingNumber = shippingDetail.TrackingNumber, ShippingStatus = shippingDetail.Status };

                return Ok(responseDto);
            }
            catch(Exception ex) 
            {
                _logger.LogError(ex, "Error initiating shipping for Order {OrderId}", requestDto.OrderId);
                return StatusCode(500, "Internal server error during shipping initiation");
            }
        }
    }
}
