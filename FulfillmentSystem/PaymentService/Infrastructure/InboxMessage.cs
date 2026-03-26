

using System.ComponentModel.DataAnnotations;

namespace PaymentService.Infrastructure
{
    public class InboxMessage
    {
        [Key]
        public int EventId { get; set; } // The ID from the Order Service
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow; 
        public DateTime ProcessedAt { get; set; }

        public string Status {  get; set; }=string.Empty;

    }
}
