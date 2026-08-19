
using BankLedger.Core.Common.Events;
using Newtonsoft.Json;

namespace BankLedger.ReadModel.Projection.Common.Models
{
    public class PoisonEventDocument
    {
        [JsonProperty("id")]
        public string Id { get; init; } = string.Empty;

        [JsonProperty("streamId")]
        public string StreamId { get; init; } = string.Empty;

        [JsonProperty("eventType")]
        public string EventType { get; init; } = string.Empty;

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; init; } = string.Empty;

        [JsonProperty("stackTrace")]
        public string StackTrace { get; init; } = string.Empty ;

        [JsonProperty("rawEventPayload")]
        public required JournalEntryPostedEvent RawEventPayload { get; init; } 

        [JsonProperty]
        public DateTime LoggedAt { get; init; } = DateTime.MinValue;
    }
}
