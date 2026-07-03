using Newtonsoft.Json;

namespace BankLedger.ReadModel.Projection.Common.Models
{
    public class AccountBalanceDocument
    {
        [JsonProperty("id")]
        public string Id { get; init; }= string.Empty;

        [JsonProperty("aggregateid")]
        public string AggregateId {  get; init; }= string.Empty;

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; } = string.Empty;

        [JsonProperty("customerName")]
        public string CustomerName {  get; init; }= string.Empty;

        [JsonProperty("currentBalance")]
        public decimal CurrentBalance { get; set; }

        [JsonProperty("currency")]
        public string Currency {  get; init; }= string.Empty;

        [JsonProperty("lastProcessedVersion")]
        public int LastProcessedVersion { get; set; }
    }
}
