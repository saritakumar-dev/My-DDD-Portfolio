using Microsoft.AspNetCore.Mvc;
using Orders.Api.Domain;
using static Orders.Api.Dtos.OutboxMessageDtos;

namespace Orders.Api.Controllers
{
    [Route("internal/Outbox")]
    [ApiController]
    public class OutboxController : ControllerBase
    {
        private readonly IOutboxMessageRepository _repository;
        private readonly ILogger _logger;

        public OutboxController(IOutboxMessageRepository repository, ILogger<OutboxController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("unprocessed")]
        public async Task<ActionResult<List<OutboxMessageResponse>>> GetUnprocessed()
        {
            try
            {
                _logger.LogInformation("Received GET /internal/outbox/unprocessed.");

                var messages = await _repository.GetOutboxMessagesAsync();

                if (!messages.Any()) return Ok(new List<OutboxMessageResponse>());

                var events = messages.Select(m => new OutboxMessageResponse(
                    m.Id,m.AggregateId, m.Type, m.Content, m.OccurredOnUtc
                    ));

                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unprocessed outbox messages from Database.");
                return StatusCode(500, "Internal database error in Order Service.");
            }
        }
            
        [HttpPost("{eventId}/ack")]
        public async Task<IActionResult> Acknowledge(int eventId)
        {
            var success = await _repository.SaveProcessStatusAsync(eventId);
            if (!success) return NotFound($"Event {eventId} not found.");

            _logger.LogInformation("Received POST /internal/outbox/{0}/ack. Marked as processed.", eventId);
            return NoContent();
        }
    }
}
