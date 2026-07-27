
namespace BankLedger.Domain
{
    public static class AmbientContext
    {
        private static readonly AsyncLocal<Guid> _currentAggregateId = new();

        public static Guid CurrentAggregateId
        {
            get => _currentAggregateId.Value;
            set => _currentAggregateId.Value = value;
        }
    }
}
